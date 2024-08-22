using System;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract
{
    private void AssertTokenBalance(Address owner, string token, long threshold)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var tokenBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Owner = owner,
            Symbol = token
        });
        Assert(tokenBalance != null && tokenBalance.Balance >= threshold, "Token balance not enough.");
    }

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

    private bool ValidateSchemeInfo(AddGovernanceSchemeInput input)
    {
        var threshold = input.SchemeThreshold;
        return ValidateBaseGovernanceSchemeThreshold(threshold);
    }

    private bool ValidateBaseGovernanceSchemeThreshold(GovernanceSchemeThreshold threshold)
    {
        return threshold.MinimalVoteThreshold >= 0 &&
               threshold.MinimalRequiredThreshold >= 0 &&
               threshold.MinimalApproveThreshold >= 0 &&
               threshold.MaximalAbstentionThreshold >= 0 &&
               threshold.MaximalRejectionThreshold >= 0 &&
               threshold.MaximalAbstentionThreshold +
               threshold.MinimalApproveThreshold <= GovernanceContractConstants.AbstractVoteTotal &&
               threshold.MaximalRejectionThreshold +
               threshold.MinimalApproveThreshold <= GovernanceContractConstants.AbstractVoteTotal &&
               threshold.ProposalThreshold >= 0;
    }

    private void AssertVoteMechanism(GovernanceMechanism governanceMechanism, Hash voteSchemeId)
    {
        var voteScheme = State.VoteContract.GetVoteScheme.Call(voteSchemeId);
        var voteMechanism = voteScheme.VoteMechanism;
        Assert(
            GovernanceMechanism.Organization == governanceMechanism
                ? VoteMechanism.UniqueVote == voteMechanism
                : VoteMechanism.TokenBallot == voteMechanism, "Invalid voteSchemeId.");
    }

    private void AssertProposer(GovernanceMechanism governanceMechanism, Address proposer, Hash daoId)
    {
        if (GovernanceMechanism.Organization != governanceMechanism) return;
        var isProposer = State.DaoContract.GetIsMember.Call(new GetIsMemberInput { DaoId = daoId, Member = proposer })
            .Value;
        Assert(isProposer, "Invalid proposer.");
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

    private DAOInfo AssertDaoSubsistAndTreasuryStatus(Hash daoId, out Address treasuryAddress)
    {
        Assert(daoId != null && daoId != Hash.Empty, "Invalid DaoId.");
        var daoInfo = State.DaoContract.GetDAOInfo.Call(daoId);
        Assert(daoInfo != null, $"Dao {daoId} not exist.");
        Assert(daoInfo!.SubsistStatus, "DAO is not in subsistence.");
        treasuryAddress = State.DaoContract.GetTreasuryAddress.Call(daoId);
        Assert(treasuryAddress != null && treasuryAddress.Value.Any(),
            "Treasury has not bean created yet.");
        return daoInfo;
    }

    private DAOInfo AssertDaoSubsistAndTreasuryStatus(Hash daoId, string symbol, long amount, Address recipient)
    {
        Assert(!string.IsNullOrWhiteSpace(symbol), "Invalid symbol.");
        Assert(recipient != null && recipient.Value.Any(), "Invalid recipient.");
        Assert(amount > 0, "Amount must be greater than 0.");
        var daoInfo = AssertDaoSubsistAndTreasuryStatus(daoId, out var treasuryAddress);

        var getBalanceOutput = State.TokenContract.GetBalance.Call(new GetBalanceInput
        {
            Symbol = symbol,
            Owner = treasuryAddress
        });
        Assert(getBalanceOutput != null && getBalanceOutput.Balance >= amount,
            "The Treasury has insufficient available funds.");
        return daoInfo;
    }

    private int CallAndCheckHighCouncilCount(Hash daoId)
    {
        var addressList = State.ElectionContract.GetHighCouncilMembers.Call(daoId);
        //HC has not been initialized, and a maximum number is returned, making the proposal not pass
        // Assert(addressList != null && addressList.Value.Count > 0,
        //     "The 'High Council' elections have not taken place yet.");
        var count = addressList?.Value.Count ?? 0;
        return count == 0 ? int.MaxValue : count;
    }

    private int CallAndCheckBpCount()
    {
        var minerList = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty());
        Assert(minerList != null && minerList.Pubkeys.Count > 0, "Invalid BP Count.");
        return minerList!.Pubkeys.Count;
    }

    private long CallAndCheckMemberCount(Hash daoId)
    {
        var result = State.DaoContract.GetMemberCount.Call(daoId).Value;
        Assert(result > 0, "Invalid Member Count.");
        return result;
    }

    #region proposal

    private ProposalStatusOutput CreateProposalStatusOutput(ProposalStatus proposalStatus, ProposalStage proposalStage)
    {
        return new ProposalStatusOutput
        {
            ProposalStatus = proposalStatus,
            ProposalStage = proposalStage
        };
    }

    private ProposalTime GetProposalTime(ProposalBasicInfo proposalBasicInfo, ProposalType proposalType)
    {
        var activeStartTime = new Timestamp { Seconds = proposalBasicInfo.ActiveStartTime };
        var activeEndTime = new Timestamp { Seconds = proposalBasicInfo.ActiveEndTime };
        ValidActiveTime(proposalBasicInfo, activeStartTime, activeEndTime);
        
        var timePeriod = GetProposalTimePeriod(proposalBasicInfo, proposalType);
        var usePeriod = proposalBasicInfo.ActiveTimePeriod > 0;
        var proposalTime = new ProposalTime();
        switch (proposalType)
        {
            case ProposalType.Veto:
                proposalTime.ActiveStartTime = usePeriod ? Context.CurrentBlockTime : activeStartTime;
                proposalTime.ActiveEndTime = usePeriod ? Context.CurrentBlockTime.AddSeconds(timePeriod.VetoActiveTimePeriod) : activeEndTime;
                break;
            case ProposalType.Governance:
            case ProposalType.Advisory:
            default:
                proposalTime.ActiveStartTime = usePeriod ? Context.CurrentBlockTime : activeStartTime;
                proposalTime.ActiveEndTime = usePeriod ? Context.CurrentBlockTime.AddSeconds(timePeriod.ActiveTimePeriod) : activeEndTime;
                break;
        }

        switch (proposalType)
        {
            case ProposalType.Governance:
                var scheme = GetScheme(proposalBasicInfo.SchemeAddress);
                if (scheme.GovernanceMechanism == GovernanceMechanism.HighCouncil)
                {
                    proposalTime.ExecuteStartTime = proposalTime.ActiveEndTime.AddSeconds(timePeriod.PendingTimePeriod);
                    proposalTime.ExecuteEndTime = proposalTime.ExecuteStartTime.AddSeconds(timePeriod.ExecuteTimePeriod);
                }
                else
                {
                    proposalTime.ExecuteStartTime = proposalTime.ActiveEndTime;
                    proposalTime.ExecuteEndTime = proposalTime.ExecuteStartTime.AddSeconds(timePeriod.ExecuteTimePeriod);
                }

                break;
            case ProposalType.Veto:
                proposalTime.ExecuteStartTime = proposalTime.ActiveEndTime;
                proposalTime.ExecuteEndTime = proposalTime.ExecuteStartTime.AddSeconds(timePeriod.VetoExecuteTimePeriod);
                break;
            case ProposalType.Advisory:
                break;
            case ProposalType.Unused:
            default:
                throw new AssertionException("Invalid proposal type.");
        }

        return proposalTime;
    }

    private DaoProposalTimePeriod GetProposalTimePeriod(ProposalBasicInfo proposalBasicInfo, ProposalType proposalType)
    {
        var timePeriod = GetDaoProposalTimePeriod(proposalBasicInfo.DaoId);
        
        if (proposalBasicInfo.ActiveTimePeriod <= 0) return timePeriod;
        
        if (proposalType == ProposalType.Veto)
        {
            timePeriod.VetoActiveTimePeriod = proposalBasicInfo.ActiveTimePeriod;
        }
        else
        {
            timePeriod.ActiveTimePeriod = proposalBasicInfo.ActiveTimePeriod;
        }

        return timePeriod;
    }

    private void ValidActiveTime(ProposalBasicInfo proposalBasicInfo, Timestamp activeStartTime, Timestamp activeEndTime)
    {
        var activeTimePeriod = proposalBasicInfo.ActiveTimePeriod;
        if (activeTimePeriod > 0)
        {
            Assert(proposalBasicInfo.ActiveStartTime == 0 && proposalBasicInfo.ActiveEndTime == 0, "Duplicated active period params.");
            AssertNumberInRange(activeTimePeriod, GovernanceContractConstants.MinActiveTimePeriod,
                GovernanceContractConstants.MaxActiveTimePeriod, "ProposalBasicInfo.ActiveTimePeriod");
        }
        else
        {
            Assert(activeEndTime > activeStartTime,
                "Invalid active time period, active start time larger than or equal to active end time.");
            Assert(activeStartTime >= Context.CurrentBlockTime, "Invalid active start time, early than block time.");
            Assert( activeStartTime.AddSeconds(GovernanceContractConstants.MinActiveTimePeriod) <= activeEndTime, 
                "Invalid active params, active period should no less than one hour.");
            Assert( activeStartTime.AddSeconds(GovernanceContractConstants.MaxActiveTimePeriod) >= activeEndTime, 
                "Invalid active params, active period should no more than fifteen day.");
        }
    }

    #endregion
}