using System;
using System.Text;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract
{
    public override Hash CreateProposal(CreateProposalInput input)
    {
        var proposalId = CheckAndGetProposalId(input, input.ProposalBasicInfo, out var scheme, input.Token);
        Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
        var proposalType = (ProposalType)input.ProposalType;
        Assert(proposalType != ProposalType.Unused && proposalType != ProposalType.Veto,
            "ProposalType cannot be Unused or Veto.");
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo,
            proposalType, input.Transaction);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, input.ProposalBasicInfo.VoteSchemeId, scheme.GovernanceToken);
        FireProposalCreatedEvent(proposal);
        return proposalId;
    }

    public override Hash CreateTransferProposal(CreateTransferProposalInput input)
    {
        var proposalId = CheckAndGetProposalId(input, input.ProposalBasicInfo, out var scheme);
        Assert(
            input.Memo == null || Encoding.UTF8.GetByteCount(input.Memo) <= GovernanceContractConstants.MemoMaxLength,
            "Invalid memo size.");
        var daoInfo = AssertDaoSubsistAndTreasuryStatus(input.ProposalBasicInfo.DaoId, input.Symbol, input.Amount, input.Recipient);

        var transaction = new ExecuteTransaction
        {
            ContractMethodName = GovernanceContractConstants.TransferMethodName,
            ToAddress = daoInfo.ContractAddressList.TreasuryContractAddress,
            Params = new Treasury.TransferInput
            {
                DaoId = input.ProposalBasicInfo.DaoId,
                Amount = input.Amount,
                Symbol = input.Symbol,
                Recipient = input.Recipient,
                Memo = input.Memo,
                ProposalId = proposalId
            }.ToByteString()
        };
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo, ProposalType.Governance, transaction);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, input.ProposalBasicInfo.VoteSchemeId, scheme.GovernanceToken);
        FireProposalCreatedEvent(proposal);
        return proposalId;
    }

    public override Hash CreateVetoProposal(CreateVetoProposalInput input)
    {
        var proposalId = CheckAndGetProposalId(input, input.ProposalBasicInfo, out var scheme);
        Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
        var vetoProposalId = input.VetoProposalId;
        var vetoProposal = State.Proposals[vetoProposalId];
        Assert(vetoProposal != null, "Veto proposal not found.");

        var status = GetProposalStatus(vetoProposal);
        Assert(status.ProposalStage == ProposalStage.Pending, "Invalid veto proposal stage.");
        Assert(status.ProposalStatus == ProposalStatus.Approved, "Invalid veto proposal status.");
        var transaction = new ExecuteTransaction
        {
            ToAddress = Context.Self,
            ContractMethodName = nameof(VetoProposal),
            Params = new VetoProposalInput
            {
                ProposalId = proposalId,
                VetoProposalId = input.VetoProposalId
            }.ToByteString()
        };
        var proposal = ValidateAndGetProposalInfo(proposalId, input.ProposalBasicInfo,
            ProposalType.Veto, transaction, vetoProposalId);
        State.Proposals[proposalId] = proposal;
        State.ProposalGovernanceSchemeSnapShot[proposalId] = scheme.SchemeThreshold;
        RegisterVotingItem(proposal, input.ProposalBasicInfo.VoteSchemeId, scheme.GovernanceToken);
        vetoProposal.ProposalStatus = ProposalStatus.Challenged;
        State.Proposals[vetoProposalId] = vetoProposal;
        FireProposalCreatedEvent(proposal);
        return proposalId;
    }

    private Hash CheckAndGetProposalId<T>(T input, ProposalBasicInfo proposalBasicInfo, out GovernanceScheme scheme,
        Hash token = null)
        where T : IMessage<T>
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        AssertParams(proposalBasicInfo, proposalBasicInfo.DaoId, proposalBasicInfo.VoteSchemeId,
            proposalBasicInfo.SchemeAddress);
        Assert(
            proposalBasicInfo.ProposalDescription.Length <=
            GovernanceContractConstants.MaxProposalDescriptionUrlLength && ValidateForumUrl(proposalBasicInfo.ForumUrl),
            "Invalid proposal description or forum url.");
        
        AssertNumberInRange(proposalBasicInfo.ActiveTimePeriod, GovernanceContractConstants.MinActiveTimePeriod,
            GovernanceContractConstants.MaxActiveTimePeriod, "ProposalBasicInfo.ActiveTimePeriod");
        
        scheme = State.GovernanceSchemeMap[proposalBasicInfo.SchemeAddress];
        var schemeAddressList = State.DaoSchemeAddressList[proposalBasicInfo.DaoId];
        Assert(
            scheme != null && schemeAddressList != null && schemeAddressList.Value.Count > 0 &&
            schemeAddressList.Value.Contains(proposalBasicInfo.SchemeAddress), "Invalid scheme address.");
        
        AssertTokenBalance(Context.Sender, scheme!.GovernanceToken, scheme.SchemeThreshold.ProposalThreshold);
        AssertVoteMechanism(scheme.GovernanceMechanism, proposalBasicInfo.VoteSchemeId);
        AssertProposer(scheme.GovernanceMechanism, Context.Sender, proposalBasicInfo.DaoId);
        var proposalId = GenerateId(input, token == null ? Context.TransactionId : token);
        return proposalId;
    }

    private bool ValidateForumUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        var result = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        return result;
    }

    private ProposalInfo ValidateAndGetProposalInfo(Hash proposalId, ProposalBasicInfo proposalBasicInfo,
        ProposalType proposalType, ExecuteTransaction executeTransaction = null, Hash vetoProposalId = null)
    {
        if (proposalType == ProposalType.Governance)
        {
            Assert(!ValidateExecuteTransaction(executeTransaction), "Invalid execute transaction.");
        }

        var proposal = new ProposalInfo
        {
            ProposalBasicInfo = proposalBasicInfo,
            ProposalId = proposalId,
            ProposalType = proposalType,
            ProposalTime = GetProposalTime(proposalBasicInfo, proposalType),
            ProposalStatus = ProposalStatus.PendingVote,
            ProposalStage = ProposalStage.Active,
            Proposer = Context.Sender,
            Transaction = executeTransaction,
            VetoProposalId = vetoProposalId,
            ForumUrl = proposalBasicInfo.ForumUrl
        };
        return proposal;
    }

    private bool ValidateExecuteTransaction(ExecuteTransaction transaction)
    {
        AssertParams(transaction, transaction?.ToAddress, transaction?.ContractMethodName);
        // normal proposal can not call VetoProposal method
        return transaction.ContractMethodName == nameof(VetoProposal) && transaction.ToAddress == Context.Self;
    }

    private GovernanceScheme GetScheme(Address schemeAddress)
    {
        var scheme = State.GovernanceSchemeMap[schemeAddress];
        Assert(scheme != null, "Scheme not found.");
        return scheme;
    }

    private void RegisterVotingItem(ProposalInfo proposal, Hash voteSchemeId, string governanceToken)
    {
        State.VoteContract.Register.Send(new VotingRegisterInput
        {
            VotingItemId = proposal.ProposalId,
            SchemeId = voteSchemeId,
            StartTimestamp = proposal.ProposalTime.ActiveStartTime,
            EndTimestamp = proposal.ProposalTime.ActiveEndTime,
            AcceptedToken = governanceToken
        });
    }

    private void FireProposalCreatedEvent(ProposalInfo proposal)
    {
        var basicInfo = proposal.ProposalBasicInfo;
        var proposalTime = proposal.ProposalTime;
        Context.Fire(new ProposalCreated
        {
            DaoId = basicInfo.DaoId,
            ProposalId = proposal.ProposalId,
            ProposalType = proposal.ProposalType,
            ProposalTitle = basicInfo.ProposalTitle,
            ProposalDescription = basicInfo.ProposalDescription,
            ProposalStatus = proposal.ProposalStatus,
            ProposalStage = ProposalStage.Active,
            ActiveStartTime = proposalTime.ActiveStartTime,
            ActiveEndTime = proposalTime.ActiveEndTime,
            ExecuteStartTime = proposalTime.ExecuteStartTime,
            ExecuteEndTime = proposalTime.ExecuteEndTime,
            SchemeAddress = basicInfo.SchemeAddress,
            VoteSchemeId = basicInfo.VoteSchemeId,
            Transaction = proposal.Transaction,
            Proposer = proposal.Proposer,
            VetoProposalId = proposal.VetoProposalId,
            ActiveTimePeriod = basicInfo.ActiveTimePeriod,
            ForumUrl = proposal.ForumUrl

        });
    }

    public override Empty VetoProposal(VetoProposalInput input)
    {
        var proposal = State.Proposals[input.ProposalId];
        Assert(proposal != null, $"Proposal {input.ProposalId} not found.");
        var vetoProposal = State.Proposals[input.VetoProposalId];
        Assert(vetoProposal != null && ValidatePermission(vetoProposal.ProposalBasicInfo.DaoId, Context.Sender),
            "Invalid proposal or no permission.");
        Assert(HasPendingStatus(input.VetoProposalId),
            "Not a governance proposal of the High Council type or the challenge period has passed.");
        var proposalStatusOutput = GetProposalStatus(vetoProposal);
        Assert(proposalStatusOutput.ProposalStage == ProposalStage.Pending,
            "The proposal is not in the challenge period.");

        vetoProposal!.ProposalStatus = ProposalStatus.Vetoed;
        State.Proposals[input.VetoProposalId] = vetoProposal;

        proposal!.ProposalStatus = ProposalStatus.Executed;
        State.Proposals[input.ProposalId] = proposal;

        Context.Fire(new ProposalVetoed
        {
            DaoId = vetoProposal.ProposalBasicInfo.DaoId,
            ProposalId = input.ProposalId,
            VetoProposalId = input.VetoProposalId,
            VetoTime = Context.CurrentBlockTime
        });

        return new Empty();
    }

    public override Empty ExecuteProposal(Hash input)
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        AssertParams(input);
        var proposal = State.Proposals[input];
        Assert(proposal != null, "Proposal not found.");
        Assert(Context.Sender == proposal.Proposer, "No permission.");
        ExecuteProposal(proposal);

        proposal.ProposalStatus = ProposalStatus.Executed;
        State.Proposals[input] = proposal;

        //FIXME Since there is a ClearProposal method, there is no need to clean up immediately after execution.
        //State.Proposals.Remove(proposal.ProposalId);
        return new Empty();
    }

    private void ExecuteProposal(ProposalInfo proposal)
    {
        Assert(Context.CurrentBlockTime > proposal.ProposalTime.ExecuteStartTime
               && Context.CurrentBlockTime < proposal.ProposalTime.ExecuteEndTime,
            "The proposal is in active or expired.");
        var proposalStatusOutput = GetProposalStatus(proposal);
        Assert(proposalStatusOutput.ProposalStatus is ProposalStatus.Approved or ProposalStatus.Challenged
               && proposalStatusOutput.ProposalStage == ProposalStage.Execute, "Proposal can not execute.");
        var governanceScheme = GetGovernanceScheme(proposal.ProposalBasicInfo.SchemeAddress);
        Assert(governanceScheme != null, "GovernanceScheme not found.");
        var schemeId = governanceScheme.SchemeId;

        Context.SendVirtualInline(
            schemeId,
            proposal.Transaction.ToAddress,
            proposal.Transaction.ContractMethodName, proposal.Transaction.Params);

        Context.Fire(new ProposalExecuted
        {
            DaoId = proposal.ProposalBasicInfo.DaoId,
            ProposalId = proposal.ProposalId,
            ExecuteTime = Context.CurrentBlockTime
        });
    }

    public override ProposalStatusOutput 
        GetProposalStatus(Hash input)
    {
        Assert(input != null && input != Hash.Empty, "Invalid input.");
        var proposal = State.Proposals[input];
        return proposal == null ? new ProposalStatusOutput() : GetProposalStatus(proposal);
    }

    private ProposalStatusOutput GetProposalStatus(ProposalInfo proposalInfo)
    {
        var proposalStage = proposalInfo.ProposalStage;
        var proposalStatus = proposalInfo.ProposalStatus;
        var proposalTime = proposalInfo.ProposalTime;
        if (proposalStage == ProposalStage.Active && Context.CurrentBlockTime < proposalTime.ActiveEndTime)
        {
            return CreateProposalStatusOutput(proposalStatus, proposalStage);
        }

        var proposalStatusOutput = GetProposalVotedStatus(proposalInfo);
        if (proposalStatusOutput != null)
        {
            return proposalStatusOutput;
        }

        switch (proposalInfo.ProposalType)
        {
            case ProposalType.Governance:
                return GetGovernanceProposalStatus(proposalInfo);
            case ProposalType.Advisory:
                return GetAdvisoryProposalStatus(proposalInfo);
            case ProposalType.Veto:
                return GetVetoProposalStatus(proposalInfo);
        }

        return new ProposalStatusOutput();
    }

    private ProposalStatusOutput GetProposalVotedStatus(ProposalInfo proposalInfo)
    {
        var threshold = State.ProposalGovernanceSchemeSnapShot[proposalInfo.ProposalId];
        Assert(threshold != null, "GovernanceSchemeThreshold not exists.");
        var votingResult = State.VoteContract.GetVotingResult.Call(proposalInfo.ProposalId);
        var approveVote = votingResult.ApproveCounts;
        var rejectVote = votingResult.RejectCounts;
        var abstainVote = votingResult.AbstainCounts;
        var totalVoter = votingResult.TotalVotersCount;
        //Minimum participation rate
        var enoughVoter = totalVoter >= GetRealMinimalRequiredThreshold(proposalInfo, threshold);
        if (!enoughVoter)
        {
            return CreateProposalStatusOutput(ProposalStatus.BelowThreshold, ProposalStage.Finished);
        }

        //Minimal vote threshold, the proposal of 1a1v is not subject to this control
        var voteSchemeId = proposalInfo.ProposalBasicInfo.VoteSchemeId;
        var voteScheme = State.VoteContract.GetVoteScheme.Call(voteSchemeId);
        if (voteScheme?.VoteMechanism != VoteMechanism.UniqueVote)
        {
            var enoughVote = rejectVote.Add(abstainVote).Add(approveVote) >= threshold.MinimalVoteThreshold;
            if (!enoughVote)
            {
                return CreateProposalStatusOutput(ProposalStatus.BelowThreshold, ProposalStage.Finished);
            }
        }

        var totalVote = GetTotalVoteCount(proposalInfo,  votingResult);
        if (threshold!.MaximalRejectionThreshold > 0)
        {
            var isReject = rejectVote * GovernanceContractConstants.AbstractVoteTotal >=
                           threshold.MaximalRejectionThreshold * totalVote;
            if (isReject)
            {
                return CreateProposalStatusOutput(ProposalStatus.Rejected, ProposalStage.Finished);
            }
        }

        if (threshold.MaximalAbstentionThreshold > 0)
        {
            var isAbstained = abstainVote * GovernanceContractConstants.AbstractVoteTotal >=
                              threshold.MaximalAbstentionThreshold * totalVote;
            if (isAbstained)
            {
                return CreateProposalStatusOutput(ProposalStatus.Abstained, ProposalStage.Finished);
            }
        }

        var isApproved = approveVote * GovernanceContractConstants.AbstractVoteTotal >=
                         threshold.MinimalApproveThreshold * totalVote;
        return !isApproved ? CreateProposalStatusOutput(ProposalStatus.BelowThreshold, ProposalStage.Finished) : null;
    }

    private ProposalStatusOutput GetGovernanceProposalStatus(ProposalInfo proposalInfo)
    {
        var proposalStatus = proposalInfo.ProposalStatus;
        if (proposalStatus is ProposalStatus.Vetoed or ProposalStatus.Executed)
        {
            return CreateProposalStatusOutput(proposalStatus, ProposalStage.Finished);
        }

        proposalStatus = proposalStatus == ProposalStatus.PendingVote ? ProposalStatus.Approved : proposalStatus;
        if (HasPendingStatus(proposalInfo.ProposalId))
        {
            return CreateProposalStatusOutput(proposalStatus, ProposalStage.Pending);
        }

        if (Context.CurrentBlockTime >= proposalInfo.ProposalTime.ExecuteEndTime)
        {
            return CreateProposalStatusOutput(ProposalStatus.Expired, ProposalStage.Finished);
        }

        return CreateProposalStatusOutput(proposalStatus, ProposalStage.Execute);
    }

    private ProposalStatusOutput GetAdvisoryProposalStatus(ProposalInfo proposalInfo)
    {
        return CreateProposalStatusOutput(ProposalStatus.Approved, ProposalStage.Finished);
    }

    private ProposalStatusOutput GetVetoProposalStatus(ProposalInfo proposalInfo)
    {
        var proposalStatus = proposalInfo.ProposalStatus;
        if (proposalStatus == ProposalStatus.Executed)
        {
            return CreateProposalStatusOutput(proposalStatus, ProposalStage.Finished);
        }

        if (Context.CurrentBlockTime >= proposalInfo.ProposalTime.ExecuteEndTime)
        {
            return CreateProposalStatusOutput(ProposalStatus.Expired, ProposalStage.Finished);
        }

        return CreateProposalStatusOutput(ProposalStatus.Approved, ProposalStage.Execute);
    }

    private long GetRealMinimalRequiredThreshold(ProposalInfo proposalInfo, GovernanceSchemeThreshold threshold)
    {
        var schemeAddress = proposalInfo.ProposalBasicInfo.SchemeAddress;
        var governanceScheme = State.GovernanceSchemeMap[schemeAddress];
        Assert(governanceScheme != null, $"Governance Scheme {schemeAddress} not exists.");
        if (governanceScheme!.GovernanceMechanism == GovernanceMechanism.Referendum)
        {
            return threshold.MinimalRequiredThreshold;
        }
        
        var daoId = proposalInfo.ProposalBasicInfo.DaoId;
        var daoInfo = CallAndCheckDaoInfo(daoId);
        long convertCount;
        if (governanceScheme!.GovernanceMechanism == GovernanceMechanism.HighCouncil)
        {
            convertCount = daoInfo.IsNetworkDao ? CallAndCheckBpCount() : CallAndCheckHighCouncilCount(daoId);
        }
        else
        {
            convertCount = CallAndCheckMemberCount(daoId);
        }

        var realMinimalRequiredThreshold = threshold.MinimalRequiredThreshold * convertCount;
        realMinimalRequiredThreshold =
            realMinimalRequiredThreshold / GovernanceContractConstants.AbstractVoteTotal +
            (realMinimalRequiredThreshold % GovernanceContractConstants.AbstractVoteTotal == 0 ? 0 : 1);
        return realMinimalRequiredThreshold;
    }

    private long GetTotalVoteCount(ProposalInfo proposalInfo, VotingResult votingResult)
    {
        var schemeAddress = proposalInfo.ProposalBasicInfo.SchemeAddress;
        var governanceScheme = State.GovernanceSchemeMap[schemeAddress];
        Assert(governanceScheme != null, $"Governance Scheme {schemeAddress} not exists.");
        if (governanceScheme!.GovernanceMechanism == GovernanceMechanism.Organization)
        {
            var daoId = proposalInfo.ProposalBasicInfo.DaoId;
            return CallAndCheckMemberCount(daoId);
        }
        return votingResult.VotesAmount;
    }

    private bool HasPendingStatus(Hash proposalId)
    {
        var proposal = State.Proposals[proposalId];
        var governanceMechanism =
            State.GovernanceSchemeMap[proposal.ProposalBasicInfo.SchemeAddress].GovernanceMechanism;
        return proposal.ProposalType == ProposalType.Governance &&
               governanceMechanism == GovernanceMechanism.HighCouncil &&
               Context.CurrentBlockTime >= proposal.ProposalTime.ActiveEndTime &&
               Context.CurrentBlockTime < proposal.ProposalTime.ExecuteStartTime;
    }

    public override Empty SetProposalTimePeriod(SetProposalTimePeriodInput input)
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        Assert(Context.Sender == State.DaoContract.Value, "No permission.");
        AssertParams(input, input?.DaoId, input?.ProposalTimePeriod);
        var daoInfo = CallAndCheckDaoInfo(input!.DaoId);
        Assert(daoInfo.SubsistStatus, "DAO is not in subsistence.");
        var activeTimePeriod = input!.ProposalTimePeriod.ActiveTimePeriod;
        var vetoActiveTimePeriod = input.ProposalTimePeriod.VetoActiveTimePeriod;
        var pendingTimePeriod = input.ProposalTimePeriod.PendingTimePeriod;
        var executeTimePeriod = input.ProposalTimePeriod.ExecuteTimePeriod;
        var vetoExecuteTimePeriod = input.ProposalTimePeriod.VetoExecuteTimePeriod;
        AssertNumberInRange(activeTimePeriod, GovernanceContractConstants.MinActiveTimePeriod,
            GovernanceContractConstants.MaxActiveTimePeriod, "ActiveTimePeriod");
        AssertNumberInRange(vetoActiveTimePeriod, GovernanceContractConstants.MinVetoActiveTimePeriod,
            GovernanceContractConstants.MaxVetoActiveTimePeriod, "VetoActiveTimePeriod");
        AssertNumberInRange(pendingTimePeriod, GovernanceContractConstants.MinPendingTimePeriod,
            GovernanceContractConstants.MaxPendingTimePeriod, "PendingTimePeriod");
        AssertNumberInRange(executeTimePeriod, GovernanceContractConstants.MinExecuteTimePeriod,
            GovernanceContractConstants.MaxExecuteTimePeriod, "ExecuteTimePeriod");
        AssertNumberInRange(vetoExecuteTimePeriod, GovernanceContractConstants.MinVetoExecuteTimePeriod,
            GovernanceContractConstants.MaxVetoExecuteTimePeriod, "VetoExecuteTimePeriod");

        var timePeriod = State.DaoProposalTimePeriods[input.DaoId] ?? new DaoProposalTimePeriod();

        timePeriod.ActiveTimePeriod = activeTimePeriod;
        timePeriod.VetoActiveTimePeriod = vetoActiveTimePeriod;
        timePeriod.PendingTimePeriod = pendingTimePeriod;
        timePeriod.ExecuteTimePeriod = executeTimePeriod;
        timePeriod.VetoExecuteTimePeriod = vetoExecuteTimePeriod;

        State.DaoProposalTimePeriods[input.DaoId] = timePeriod;

        Context.Fire(new DaoProposalTimePeriodSet
        {
            DaoId = input.DaoId,
            ActiveTimePeriod = activeTimePeriod,
            VetoActiveTimePeriod = vetoActiveTimePeriod,
            PendingTimePeriod = pendingTimePeriod,
            ExecuteTimePeriod = executeTimePeriod,
            VetoExecuteTimePeriod = vetoExecuteTimePeriod
        });
        return new Empty();
    }

    public override ProposalInfoOutput GetProposalInfo(Hash input)
    {
        var proposal = State.Proposals[input];
        if (proposal == null)
        {
            return new ProposalInfoOutput();
        }

        var voteResult = State.VoteContract.GetVotingResult.Call(input);
        var proposalStatusOutput = GetProposalStatus(proposal);

        var proposalInfoOutput = new ProposalInfoOutput
        {
            DaoId = proposal.ProposalBasicInfo?.DaoId,
            ProposalId = proposal.ProposalId,
            ProposalTitle = proposal.ProposalBasicInfo?.ProposalTitle,
            ProposalDescription = proposal.ProposalBasicInfo?.ProposalDescription,
            ForumUrl = proposal.ForumUrl,
            ProposalType = proposal.ProposalType,
            ActiveStartTime = proposal.ProposalTime?.ActiveStartTime,
            ActiveEndTime = proposal.ProposalTime?.ActiveEndTime,
            ExecuteStartTime = proposal.ProposalTime?.ExecuteStartTime,
            ExecuteEndTime = proposal.ProposalTime?.ExecuteEndTime,
            ProposalStatus = proposalStatusOutput.ProposalStatus,
            ProposalStage = proposalStatusOutput.ProposalStage,
            Proposer = proposal.Proposer,
            SchemeAddress = proposal.ProposalBasicInfo?.SchemeAddress,
            Transaction = proposal.Transaction,
            VoteSchemeId = proposal.ProposalBasicInfo?.VoteSchemeId,
            VetoProposalId = proposal.VetoProposalId,
            VotersCount = voteResult.VotesAmount,
            VoteCount = voteResult.TotalVotersCount,
            ApprovalCount = voteResult.ApproveCounts,
            RejectionCount = voteResult.RejectCounts,
            AbstentionCount = voteResult.AbstainCounts
        };
        return proposalInfoOutput;
    }

    public override DaoProposalTimePeriod GetDaoProposalTimePeriod(Hash input)
    {
        var timePeriod = State.DaoProposalTimePeriods[input];
        return timePeriod ?? new DaoProposalTimePeriod
        {
            ActiveTimePeriod = GovernanceContractConstants.DefaultActiveTimePeriod,
            VetoActiveTimePeriod = GovernanceContractConstants.DefaultVetoActiveTimePeriod,
            PendingTimePeriod = GovernanceContractConstants.MinPendingTimePeriod,
            ExecuteTimePeriod = GovernanceContractConstants.MinExecuteTimePeriod,
            VetoExecuteTimePeriod = GovernanceContractConstants.MinVetoExecuteTimePeriod
        };
    }

    public override GovernanceSchemeThreshold GetProposalSnapShotScheme(Hash input)
    {
        if (input == null || input == Hash.Empty)
        {
            return new GovernanceSchemeThreshold();
        }

        return State.ProposalGovernanceSchemeSnapShot[input];
    }
}