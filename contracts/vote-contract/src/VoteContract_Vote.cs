using System;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract : VoteContractContainer.VoteContractBase
{
    public override Empty Register(VotingRegisterInput input)
    {
        Assert(Context.Sender == State.GovernanceContract.Value, "No permission.");
        AssertCommon(input);
        AssertToken(input.AcceptedToken);
        Assert(input.StartTimestamp <= input.EndTimestamp && input.StartTimestamp > Context.CurrentBlockTime,"Invalid startTime or endTime input");
        AssertVoteScheme(input.VotingItemId);
        var proposalInfo = AssertProposal(input.VotingItemId);
        AssertDaoSubsist(proposalInfo.DaoId);
        var governanceScheme = AssertGovernanceScheme(proposalInfo.SchemeAddress);

        State.VotingItems[input.VotingItemId] = new VotingItem
        {
            DaoId = proposalInfo.DaoId,
            VotingItemId = input.VotingItemId,
            SchemeId = input.SchemeId,
            AcceptedSymbol = input.AcceptedToken,
            RegisterTimestamp = Context.CurrentBlockTime,
            StartTimestamp = input.StartTimestamp,
            EndTimestamp = input.EndTimestamp,
            GovernanceMechanism = governanceScheme.GovernanceMechanism.ToString()
        };
        State.VotingResults[input.VotingItemId] = new VotingResult
        {
            VotingItemId = input.VotingItemId,
            ApproveCounts = 0,
            RejectCounts = 0,
            AbstainCounts = 0,
            VotesAmount = 0,
            TotalVotersCount = 0,
            StartTimestamp = input.StartTimestamp,
            EndTimestamp = input.EndTimestamp
        };
        Context.Fire(new VotingItemRegistered
        {
            DaoId = proposalInfo.DaoId,
            VotingItemId = input.VotingItemId,
            SchemeId = input.SchemeId,
            AcceptedCurrency = input.AcceptedToken,
            RegisterTimestamp = Context.CurrentBlockTime,
            StartTimestamp = input.StartTimestamp,
            EndTimestamp = input.EndTimestamp
        });
        return new Empty();
    }
    
    public override Empty Vote(VoteInput input)
    {
        AssertCommon(input);
        var votingItem = AssertVotingItem(input.VotingItemId);
        Assert(votingItem.StartTimestamp <= Context.CurrentBlockTime,"Vote not begin.");
        Assert(votingItem.EndTimestamp >= Context.CurrentBlockTime, "Vote ended.");
        AssertDaoSubsist(votingItem.DaoId);
        AssertVotingRecord(votingItem.VotingItemId, Context.Sender);
        var voteScheme = AssertVoteScheme(votingItem.SchemeId);

        if (GovernanceMechanism.HighCouncil.ToString() == votingItem.GovernanceMechanism)
        {
            AssertHighCouncil(Context.Sender);
        }

        switch (voteScheme.VoteMechanism)
        {
            case VoteMechanism.TokenBallot: // 1t1v
                TokenBallotTransfer(votingItem, input);
                break;
            case VoteMechanism.UniqueVote: // 1a1v
                Assert(input.VoteAmount == VoteContractConstants.UniqueVoteVoteAmount, "Invalid vote amount");
                break;
        }

        var voteId = AddVotingRecords(input);
        UpdateVotingResults(input);
        Context.Fire(new Voted
        {
            VotingItemId = votingItem.VotingItemId,
            Voter = Context.Sender,
            Amount = input.VoteAmount,
            VoteTimestamp = Context.CurrentBlockTime,
            Option = input.VoteOption,
            VoteId = voteId,
            DaoId = votingItem.DaoId
        });
        return new Empty();
    }

    public override Empty Withdraw(WithdrawInput input)
    {
        AssertCommon(input);
        var daoInfo = AssertDao(input.DaoId);
        var withdrawAmount = AssertWithdraw(Context.Sender, input.DaoId);
        var virtualAddressHash = GetVirtualAddressHash(Context.Sender, input.DaoId);
        TransferOut(virtualAddressHash, Context.Sender, daoInfo.GovernanceToken, withdrawAmount);
        State.RemainVoteAmounts[Context.Sender][input.DaoId] = 0;
        Context.Fire(new Withdrawn
        {
            DaoId = input.DaoId,
            Amount = withdrawAmount,
            User = Context.Sender,
            WithdrawTimestamp = Context.CurrentBlockTime
        });
        return new Empty();
    }

    private void TokenBallotTransfer(VotingItem votingItem, VoteInput input)
    {
        var virtualAddress = GetVirtualAddress(Context.Sender, votingItem.DaoId);
        TransferIn(virtualAddress, Context.Sender, votingItem.AcceptedSymbol, input.VoteAmount.Mul(VoteContractConstants.Mantissa));
        State.RemainVoteAmounts[Context.Sender][votingItem.DaoId] += input.VoteAmount;
    }

    private Hash AddVotingRecords(VoteInput input)
    {
        var voteId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input),
            HashHelper.ComputeFrom(Context.Sender));
        State.VotingRecords[input.VotingItemId][Context.Sender] = new VotingRecord
        {
            VotingItemId = input.VotingItemId,
            Voter = Context.Sender,
            Amount = input.VoteAmount,
            VoteTimestamp = Context.CurrentBlockTime,
            Option = input.VoteOption,
            VoteId = voteId
        };
        return voteId;
    }

    private void UpdateVotingResults(VoteInput input)
    {
        var votingResult = State.VotingResults[input.VotingItemId];
        votingResult.VotesAmount += input.VoteAmount;
        votingResult.TotalVotersCount += 1;
        switch (input.VoteOption)
        {
            case VoteOption.Approved:
                votingResult.ApproveCounts += input.VoteAmount;
                break;
            case VoteOption.Rejected:
                votingResult.RejectCounts += input.VoteAmount;
                break;
            case VoteOption.Abstained:
                votingResult.AbstainCounts += input.VoteAmount;
                break;
        }

        State.VotingResults[input.VotingItemId] = votingResult;
    }


    private void TransferIn(Address virtualAddress, Address from, string symbol, long amount)
    {
        State.TokenContract.TransferFrom.Send(
            new TransferFromInput
            {
                Symbol = symbol,
                Amount = amount,
                From = from,
                Memo = "TransferIn",
                To = virtualAddress
            });
    }
    
    private void TransferOut(Hash virtualAddressHash, Address to, string symbol, long amount)
    { 
        State.TokenContract.Transfer.VirtualSend(virtualAddressHash, 
            new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                Memo = "TransferOut",
                To = to
            });
    }
    
    #region View
    public override VotingItem GetVotingItem(Hash input)
    {
        return State.VotingItems[input] ?? new VotingItem();
    }

    public override VotingResult GetVotingResult(Hash input)
    {
        return State.VotingResults[input] ?? new VotingResult();
    }

    public override VotingRecord GetVotingRecord(GetVotingRecordInput input)
    {
        AssertCommon(input);
        return State.VotingRecords[input.VotingItemId][input.Voter] ?? new VotingRecord();
    }

    public override Address GetVirtualAddress(Hash input)
    {
        return GetVirtualAddress(Context.Sender, input);
    }

    #endregion
}