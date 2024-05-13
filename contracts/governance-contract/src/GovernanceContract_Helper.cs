using System;
using System.Linq;
using AElf;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.DAO;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract
{
    private void AssertParams(params object[] parameters)
    {
        foreach (var p in parameters)
        {
            Assert(p != null, "Invalid input or parameter does not exist.");
            switch (p)
            {
                case Address address:
                    Assert(address.Value.Any(), "Invalid input.");
                    break;
                case string s:
                    Assert(!string.IsNullOrEmpty(s), "Invalid input.");
                    break;
            }
        }
    }

    private void AssertNumberInRange(long numberToCheck, long minRange, long maxRange, string message)
    {
        Assert(IsNumberInRange(numberToCheck, minRange, maxRange),
            $"{message ?? "number"} should be between {minRange} and {maxRange}");
    }

    private GovernanceSchemeHashAddressPair CalculateGovernanceSchemeHashAddressPair(Hash daoId,
        GovernanceMechanism mechanism)
    {
        var schemeHash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(daoId),
            HashHelper.ComputeFrom(Context.Self), HashHelper.ComputeFrom(mechanism.ToString()));
        var schemeAddress = Context.ConvertVirtualAddressToContractAddress(schemeHash);
        return new GovernanceSchemeHashAddressPair
        {
            SchemeAddress = schemeAddress,
            SchemeId = schemeHash
        };
    }

    private bool ValidateSchemeInfo(GovernanceSchemeHashAddressPair scheme, GovernanceSchemeThreshold threshold)
    {
        return scheme.SchemeAddress != null && scheme.SchemeId != null &&
               State.GovernanceSchemeMap[scheme.SchemeAddress] == null && ValidateGovernanceSchemeThreshold(threshold);
    }

    private bool ValidateGovernanceSchemeThreshold(GovernanceSchemeThreshold threshold)
    {
        return threshold.MinimalRequiredThreshold > 0 &&
               threshold.MinimalVoteThreshold >= 0 &&
               threshold.MinimalApproveThreshold >= 0 &&
               threshold.MaximalAbstentionThreshold >= 0 &&
               threshold.MaximalRejectionThreshold >= 0 &&
               threshold.MaximalAbstentionThreshold +
               threshold.MinimalApproveThreshold <= GovernanceContractConstants.AbstractVoteTotal &&
               threshold.MaximalRejectionThreshold +
               threshold.MinimalApproveThreshold <= GovernanceContractConstants.AbstractVoteTotal;
    }

    private Hash GenerateId<T>(T input, Hash token) where T : IMessage<T>
    {
        return Context.GenerateId(Context.Self, token ?? HashHelper.ComputeFrom(input));
    }

    private bool ValidatePermission(Hash daoId, Address sender)
    {
        return State.DaoSchemeAddressList[daoId] != null &&
               State.DaoSchemeAddressList[daoId].Value.Contains(sender);
    }

    private bool ValidateDaoSubsistStatus(Hash daoId)
    {
        return State.DaoContract.GetSubsistStatus.Call(daoId).Value;
    }

    private bool IsNumberInRange(long numberToCheck, long minRange, long maxRange)
    {
        return numberToCheck >= minRange && numberToCheck <= maxRange;
    }

    private DAOInfo CallAndCheckDaoInfo(Hash daoId)
    {
        var daoInfo = State.DaoContract.GetDAOInfo.Call(daoId);
        Assert(daoInfo != null, $"Dao {daoId} not exists.");
        return daoInfo;
    }

    private int CallAndCheckHighCouncilCount(Hash daoId)
    {
        var addressList = State.ElectionContract.GetVictories.Call(daoId);
        Assert(addressList != null && addressList.Value.Count > 0, "The 'High Council' elections have not taken place yet.");
        return addressList!.Value.Count;
    }

    private int CallAndCheckBpCount()
    {
        var minerList = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty());
        Assert(minerList != null && minerList.Pubkeys.Count > 0, "Invalid BP Count.");
        return minerList!.Pubkeys.Count;
    }

    #region proposal

    private ProposalTime GetProposalTimePeriod(ProposalBasicInfo proposalBasicInfo, ProposalType proposalType)
    {
        var timePeriod = GetDaoProposalTimePeriod(proposalBasicInfo.DaoId);
        var proposalTime = new ProposalTime
        {
            ActiveStartTime = Context.CurrentBlockTime,
            ActiveEndTime = Context.CurrentBlockTime.AddDays(timePeriod.ActiveTimePeriod)
        };
        switch (proposalType)
        {
            case ProposalType.Governance:
                var scheme = GetScheme(proposalBasicInfo.SchemeAddress);
                if (scheme.GovernanceMechanism == GovernanceMechanism.HighCouncil)
                {
                    proposalTime.ExecuteStartTime = proposalTime.ActiveEndTime.AddDays(timePeriod.PendingTimePeriod);
                    proposalTime.ExecuteEndTime = proposalTime.ExecuteStartTime.AddDays(timePeriod.ExecuteTimePeriod);
                }
                else
                {
                    proposalTime.ExecuteStartTime = proposalTime.ActiveEndTime;
                    proposalTime.ExecuteEndTime = proposalTime.ExecuteStartTime.AddDays(timePeriod.ExecuteTimePeriod);
                }

                break;
            case ProposalType.Veto:
                proposalTime.ExecuteStartTime = proposalTime.ActiveEndTime;
                proposalTime.ExecuteEndTime = proposalTime.ExecuteStartTime.AddDays(timePeriod.VetoExecuteTimePeriod);
                break;
            case ProposalType.Advisory:
                break;
            case ProposalType.Unused:
            default:
                throw new AssertionException("Invalid proposal type.");
        }

        return proposalTime;
    }

    private bool ValidateProposalTimePeriod(ProposalInfo proposal)
    {
        bool result;
        var timePeriod = GetDaoProposalTimePeriod(proposal.ProposalBasicInfo.DaoId);
        var proposalTime = proposal.ProposalTime;
        switch (proposal.ProposalType)
        {
            case ProposalType.Governance:
            {
                AssertParams(proposalTime.ActiveStartTime, proposalTime.ActiveEndTime, proposalTime.ExecuteStartTime,
                    proposalTime.ExecuteEndTime);
                var scheme = GetScheme(proposal.ProposalBasicInfo.SchemeAddress);
                result = ValidateTime(proposalTime.ActiveStartTime, proposalTime.ActiveEndTime,
                    timePeriod.ActiveTimePeriod);
                if (scheme.GovernanceMechanism == GovernanceMechanism.HighCouncil)
                {
                    result = result && ValidateTime(proposalTime.ActiveEndTime, proposalTime.ExecuteStartTime,
                        timePeriod.PendingTimePeriod);
                }

                result = result && ValidateTime(proposalTime.ExecuteStartTime, proposalTime.ExecuteEndTime,
                    timePeriod.ExecuteTimePeriod);
                break;
            }
            case ProposalType.Veto:
                AssertParams(proposalTime.ActiveStartTime, proposalTime.ActiveEndTime, proposalTime.ExecuteStartTime,
                    proposalTime.ExecuteEndTime);
                result = ValidateTime(proposalTime.ActiveStartTime, proposalTime.ActiveEndTime,
                             timePeriod.VetoActiveTimePeriod) &&
                         ValidateTime(proposalTime.ExecuteStartTime, proposalTime.ExecuteEndTime,
                             timePeriod.VetoExecuteTimePeriod);
                break;
            case ProposalType.Advisory:
                AssertParams(proposalTime.ActiveStartTime, proposalTime.ActiveEndTime);
                result = ValidateTime(proposalTime.ActiveStartTime, proposalTime.ActiveEndTime,
                    timePeriod.ActiveTimePeriod);
                break;
            case ProposalType.Unused:
            default:
                throw new AssertionException("Invalid proposal type.");
        }

        return result;
    }

    private bool ValidateTime(Timestamp startTime, Timestamp targetTime, long period)
    {
        return startTime.AddDays(period).ToDateTime().Day == targetTime.ToDateTime().Day;
    }

    #endregion
}