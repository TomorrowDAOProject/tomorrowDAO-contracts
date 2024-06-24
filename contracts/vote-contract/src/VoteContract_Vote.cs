using System;
using System.Linq;
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
        AssertCommon(input);
        Assert(Context.Sender == State.GovernanceContract.Value, "No permission.");
        var voteScheme = AssertVoteScheme(input.SchemeId);
        if (VoteMechanism.TokenBallot == voteScheme.VoteMechanism)
        {
            AssertToken(input.AcceptedToken);
        }

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
        Assert(votingItem.StartTimestamp <= Context.CurrentBlockTime, "Vote not begin.");
        Assert(votingItem.EndTimestamp >= Context.CurrentBlockTime, "Vote ended.");
        var daoInfo = AssertDaoSubsist(votingItem.DaoId);
        AssertVotingRecord(votingItem.VotingItemId, Context.Sender);
        var voteScheme = AssertVoteScheme(votingItem.SchemeId);

        if (GovernanceMechanism.HighCouncil.ToString() == votingItem.GovernanceMechanism)
        {
            if (daoInfo.IsNetworkDao)
            {
                AssertBP(Context.Sender);
            }
            else
            {
                AssertHighCouncil(daoInfo.DaoId, Context.Sender);
            }
        }else if (GovernanceMechanism.Organization.ToString() == votingItem.GovernanceMechanism)
        {
            AssertOrganizationMember(daoInfo.DaoId, Context.Sender);
        }

        switch (voteScheme.VoteMechanism)
        {
            case VoteMechanism.TokenBallot: // 1t1v
                TokenBallotTransfer(votingItem, input);
                AddAmount(votingItem, input.VoteAmount);
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
            Option = (VoteOption)input.VoteOption,
            VoteId = voteId,
            DaoId = votingItem.DaoId,
            VoteMechanism = voteScheme.VoteMechanism,
            StartTime = votingItem.StartTimestamp,
            EndTime = votingItem.EndTimestamp
        });
        return new Empty();
    }

    public override Empty Withdraw(WithdrawInput input)
    {
        AssertCommon(input);
        var daoInfo = AssertDao(input.DaoId);
        var withdrawAmount = AssertWithdraw(Context.Sender, input);
        var virtualAddressHash = GetVirtualAddressHash(Context.Sender, input.DaoId);
        TransferOut(virtualAddressHash, Context.Sender, daoInfo.GovernanceToken, withdrawAmount);
        RemoveAmount(input);
        Context.Fire(new Withdrawn
        {
            DaoId = input.DaoId,
            WithdrawAmount = withdrawAmount,
            Withdrawer = Context.Sender,
            WithdrawTimestamp = Context.CurrentBlockTime,
            VotingItemIdList = input.VotingItemIdList
        });
        return new Empty();
    }

    private void TokenBallotTransfer(VotingItem votingItem, VoteInput input)
    {
        var virtualAddress = GetVirtualAddress(Context.Sender, votingItem.DaoId);
        TransferIn(virtualAddress, Context.Sender, votingItem.AcceptedSymbol, input.VoteAmount);
    }

    private void AddAmount(VotingItem votingItem, long amount)
    {
        State.DaoRemainAmounts[Context.Sender][votingItem.DaoId] += amount;
        State.DaoProposalRemainAmounts[Context.Sender][GetDaoProposalId(votingItem.DaoId, votingItem.VotingItemId)] =
            amount;
    }

    private void RemoveAmount(WithdrawInput input)
    {
        State.DaoRemainAmounts[Context.Sender][input.DaoId] -= input.WithdrawAmount;
        foreach (var votingItemId in input.VotingItemIdList.Value)
        {
            State.DaoProposalRemainAmounts[Context.Sender].Remove(GetDaoProposalId(input.DaoId, votingItemId));
        }
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
            Option = (VoteOption)input.VoteOption,
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
            case (int)VoteOption.Approved:
                votingResult.ApproveCounts += input.VoteAmount;
                break;
            case (int)VoteOption.Rejected:
                votingResult.RejectCounts += input.VoteAmount;
                break;
            case (int)VoteOption.Abstained:
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

    public override Address GetVirtualAddress(GetVirtualAddressInput input)
    {
        return GetVirtualAddress(input.Voter, input.DaoId);
    }

    public override DaoRemainAmount GetDaoRemainAmount(GetDaoRemainAmountInput input)
    {
        return new DaoRemainAmount
        {
            DaoId = input.DaoId,
            Amount = State.DaoRemainAmounts[input.Voter][input.DaoId]
        };
    }

    public override ProposalRemainAmount GetProposalRemainAmount(GetProposalRemainAmountInput input)
    {
        AssertCommon(input);
        return new ProposalRemainAmount
        {
            DaoId = input.DaoId,
            VotingItemId = input.VotingItemId,
            Amount = State.DaoProposalRemainAmounts[input.Voter][GetDaoProposalId(input.DaoId, input.VotingItemId)]
        };
    }

    public override AddressList GetBPAddresses(Empty input)
    {
        var minerList = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty());
        var minerAddressList = minerList.Pubkeys
            .Select(x => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(x.ToHex()))).ToList();
        return new AddressList
        {
            Value = { minerAddressList }
        };
    }

    #endregion
}