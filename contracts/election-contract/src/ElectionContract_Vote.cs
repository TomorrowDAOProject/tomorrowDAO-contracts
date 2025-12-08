using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    public override Hash Vote(VoteHighCouncilInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        AssertNotNullOrEmpty(input.CandidateAddress, "CandidateAddress");
        var votingItemId = State.HighCouncilElectionVotingItemId[input.DaoId];
        Assert(votingItemId != null, "Voting item not exists");
        var votingItem = State.VotingItems[votingItemId];
        Assert(votingItem != null, "Voting item not exists");

        var targetInformation = State.CandidateInformationMap[input.DaoId][input.CandidateAddress];
        AssertValidCandidateInformation(targetInformation);

        var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime).Seconds;
        AssertValidLockSeconds(lockSeconds);

        var voteId = GenerateVoteId(input);
        Assert(State.LockTimeMap[voteId] == 0, "Vote already exists.");
        State.LockTimeMap[voteId] = lockSeconds;

        UpdateElectorInformation(input.DaoId, input.Amount, voteId);
        UpdateCandidateInformation(input.DaoId, input.CandidateAddress, input.Amount, voteId);
        SetVotingRecord(input, votingItemId, votingItem, voteId);
        UpdateVotingResult(votingItem, input.CandidateAddress.ToBase58(), input.Amount);
        LockToken(input, votingItem, voteId);

        Context.Fire(new Voted
        {
            DaoId = input.DaoId,
            CandidateAddress = input.CandidateAddress,
            Amount = input.Amount,
            EndTimestamp = input.EndTimestamp,
            VoteId = voteId
        });

        return voteId;
    }

    public override Empty Withdraw(Hash input)
    {
        AssertNotNullOrEmpty(input);

        var votingRecord = State.VotingRecords[input];
        Assert(votingRecord != null, $"Vote {input} not exists.");
        Assert(votingRecord!.Voter == Context.Sender, "No permission.");
        var actualLockedTime = Context.CurrentBlockTime.Seconds.Sub(votingRecord.VoteTimestamp.Seconds);
        var claimedLockSeconds = State.LockTimeMap[input];
        Assert(actualLockedTime >= claimedLockSeconds,
            $"Still need {claimedLockSeconds.Sub(actualLockedTime).Div(86400)} days to unlock your token.");

        var votingItem = GetAndValidateVotingItemByDaoId(votingRecord.DaoId);
        var highCouncilConfig = GetAndValidateHighCouncilConfig(votingRecord.DaoId);

        var electorVote = State.ElectorVotes[votingRecord.DaoId][Context.Sender];
        Assert(electorVote != null, $"Voter {Context.Sender.ToBase58()} never votes before");
        electorVote.ActiveVotingRecordIds.Remove(input);
        electorVote.WithdrawnVotingRecordIds.Add(input);
        electorVote.ActiveVotedVotesAmount = electorVote.ActiveVotedVotesAmount.Sub(votingRecord.Amount);
        State.ElectorVotes[votingRecord.DaoId][Context.Sender] = electorVote;

        var candidateVote = State.CandidateVotes[votingRecord.DaoId][votingRecord.Candidate];
        Assert(candidateVote != null, "Candidate vote record not found");
        candidateVote.ObtainedActiveVotingRecordIds.Remove(input);
        candidateVote.ObtainedWithdrawnVotingRecordIds.Add(input);
        candidateVote.ObtainedActiveVotedVotesAmount = candidateVote.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
        State.CandidateVotes[votingRecord.DaoId][votingRecord.Candidate] = candidateVote;

        UnlockTokensOfVoter(input, votingRecord.Amount, votingItem, votingRecord.Voter);

        return new Empty();
    }

    private void LockToken(VoteHighCouncilInput input, VotingItem votingItem, Hash voteId)
    {
        if (!votingItem.IsLockToken) return;

        var lockId =
            HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.Sender), HashHelper.ComputeFrom(voteId));
        var lockVirtualAddress = Context.ConvertVirtualAddressToContractAddress(lockId);
        var sponsorAddress = Context.Sender;
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = sponsorAddress,
            To = lockVirtualAddress,
            Symbol = votingItem.AcceptedCurrency,
            Amount = input.Amount,
            Memo = "Lock for dao announcing election vote."
        });
    }

    private void AssertValidCandidateInformation(CandidateInformation candidateInformation)
    {
        Assert(candidateInformation != null, "Candidate not found.");
        Assert(candidateInformation!.IsCurrentCandidate, "Candidate quited election.");
    }

    private void AssertValidLockSeconds(long lockSeconds)
    {
        Assert(lockSeconds >= State.MinimumLockTime.Value,
            $"Invalid lock time. At least {State.MinimumLockTime.Value.Div(60).Div(60).Div(24)} days");
        Assert(lockSeconds <= State.MaximumLockTime.Value,
            $"Invalid lock time. At most {State.MaximumLockTime.Value.Div(60).Div(60).Div(24)} days");
    }

    private Hash GenerateVoteId(VoteHighCouncilInput input)
    {
        if (input.Token != null)
            return Context.GenerateId(Context.Self, HashHelper.ConcatAndCompute(input.DaoId, input.Token));

        var candidateVotesCount =
            State.CandidateVotes[input.DaoId][input.CandidateAddress]?.ObtainedActiveVotedVotesAmount ?? 0;
        return Context.GenerateId(Context.Self,
            ByteArrayHelper.ConcatArrays(input.DaoId.ToByteArray(),
                ByteArrayHelper.ConcatArrays(input.CandidateAddress.ToByteArray(),
                    candidateVotesCount.ToBytes(false))));
    }

    private void UpdateElectorInformation(Hash daoId, long amount, Hash voteId)
    {
        var electorAddress = Context.Sender;
        var voterVotes = State.ElectorVotes[daoId][electorAddress];
        if (voterVotes == null)
        {
            voterVotes = new ElectorVote
            {
                Address = electorAddress,
                ActiveVotingRecordIds = { voteId },
                ActiveVotedVotesAmount = amount,
                AllVotedVotesAmount = amount
            };
        }
        else
        {
            voterVotes.ActiveVotingRecordIds.Add(voteId);
            voterVotes.ActiveVotedVotesAmount = voterVotes.ActiveVotedVotesAmount.Add(amount);
            voterVotes.AllVotedVotesAmount = voterVotes.AllVotedVotesAmount.Add(amount);
        }

        State.ElectorVotes[daoId][electorAddress] = voterVotes;
    }

    private long UpdateCandidateInformation(Hash daoId, Address candidateAddress, long amount, Hash voteId)
    {
        var candidateVotes = State.CandidateVotes[daoId][candidateAddress];
        if (candidateVotes == null)
        {
            candidateVotes = new CandidateVote
            {
                Address = candidateAddress,
                ObtainedActiveVotingRecordIds = { voteId },
                ObtainedActiveVotedVotesAmount = amount,
                AllObtainedVotedVotesAmount = amount
            };
        }
        else
        {
            candidateVotes.ObtainedActiveVotingRecordIds.Add(voteId);
            candidateVotes.ObtainedActiveVotedVotesAmount =
                candidateVotes.ObtainedActiveVotedVotesAmount.Add(amount);
            candidateVotes.AllObtainedVotedVotesAmount =
                candidateVotes.AllObtainedVotedVotesAmount.Add(amount);
        }

        State.CandidateVotes[daoId][candidateAddress] = candidateVotes;

        return candidateVotes.ObtainedActiveVotedVotesAmount;
    }

    private void SetVotingRecord(VoteHighCouncilInput input, Hash votingItemId, VotingItem votingItem, Hash voteId)
    {
        var votingRecord = new VotingRecord
        {
            DaoId = input.DaoId,
            Voter = Context.Sender,
            VotingItemId = votingItemId,
            Amount = input.Amount,
            TermNumber = votingItem.CurrentSnapshotNumber,
            VoteId = voteId,
            SnapshotNumber = votingItem.CurrentSnapshotNumber,
            IsWithdrawn = false,
            VoteTimestamp = Context.CurrentBlockTime,
            Candidate = input.CandidateAddress,
            IsChangeTarget = false
        };
        State.VotingRecords[voteId] = votingRecord;
    }

    private void UpdateVotingResult(VotingItem votingItem, string option, long amount)
    {
        var votingResultHash = GetVotingResultHash(votingItem.VotingItemId, votingItem.CurrentSnapshotNumber);
        var votingResult = State.VotingResults[votingResultHash];
        if (!votingResult.Results.ContainsKey(option))
        {
            votingResult.Results.Add(option, 0);
        }

        var currentVotes = votingResult.Results[option];
        votingResult.Results[option] = currentVotes.Add(amount);
        votingResult.VotersCount = votingResult.VotersCount.Add(1);
        votingResult.VotesAmount = votingResult.VotesAmount.Add(amount);
        State.VotingResults[votingResultHash] = votingResult;
    }

    private void UnlockTokensOfVoter(Hash voteId, long amount, VotingItem votingItem,
        Address voterAddress = null)
    {
        if (!votingItem.IsLockToken) return;

        var lockId =
            HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.Sender), HashHelper.ComputeFrom(voteId));
        State.TokenContract.Transfer.VirtualSend(lockId,
            new TransferInput
            {
                To = voterAddress ?? Context.Sender,
                Symbol = votingItem.AcceptedCurrency,
                Amount = amount,
                Memo = "Withdraw votes for High Council Election"
            });
    }
}