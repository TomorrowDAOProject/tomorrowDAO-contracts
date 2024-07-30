using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    public override Empty TakeSnapshot(TakeElectionSnapshotInput input)
    {
        AssertInitialized();
        AssertSenderDaoContract();
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        Assert(input.TermNumber == State.CurrentTermNumber[input.DaoId],
            $"Can only take snapshot of current snapshot number: {State.CurrentTermNumber[input.DaoId]}, but {input.TermNumber}");

        SavePreviousTermInformation(input);

        var votingItemId = State.HighCouncilElectionVotingItemId[input.DaoId];
        Assert(votingItemId != null, "Voting item not exists");
        var votingItem = State.VotingItems[votingItemId];
        Assert(votingItem != null, "Voting item not exists");

        // Update previous voting going information.
        var previousVotingResultHash = GetVotingResultHash(votingItemId, votingItem!.CurrentSnapshotNumber);
        var previousVotingResult = State.VotingResults[previousVotingResultHash];
        previousVotingResult.SnapshotEndTimestamp = Context.CurrentBlockTime;
        State.VotingResults[previousVotingResultHash] = previousVotingResult;

        var nextSnapshotNumber = input.TermNumber.Add(1);
        votingItem.CurrentSnapshotNumber = nextSnapshotNumber;
        State.VotingItems[votingItem.VotingItemId] = votingItem;

        var currentVotingGoingHash = GetVotingResultHash(votingItemId, nextSnapshotNumber);
        State.VotingResults[currentVotingGoingHash] = new VotingResult
        {
            VotingItemId = votingItemId,
            SnapshotNumber = nextSnapshotNumber,
            SnapshotStartTimestamp = Context.CurrentBlockTime,
            VotersCount = previousVotingResult.VotersCount,
            VotesAmount = previousVotingResult.VotesAmount
        };

        State.CurrentTermNumber[input.DaoId] = nextSnapshotNumber;

        UpdateElectedCandidateInformation(input.DaoId, input.TermNumber);

        Context.Fire(new CandidateElected
        {
            DaoId = input.DaoId,
            PreTermNumber = input.TermNumber,
            NewNumber = nextSnapshotNumber
        });

        return new Empty();
    }

    private void SavePreviousTermInformation(TakeElectionSnapshotInput input)
    {
        var snapshot = new TermSnapshot
        {
            DaoId = input.DaoId,
            TermNumber = input.TermNumber,
        };
        if (State.Candidates[input.DaoId] == null) return;

        foreach (var candidate in State.Candidates[input.DaoId].Value)
        {
            var votes = State.CandidateVotes[input.DaoId][candidate];
            var validObtainedVotesAmount = 0L;
            if (votes != null) validObtainedVotesAmount = votes.ObtainedActiveVotedVotesAmount;
            snapshot.ElectionResult.Add(candidate.ToBase58(), validObtainedVotesAmount);
        }

        State.Snapshots[input.DaoId][input.TermNumber] = snapshot;
    }

    private void UpdateElectedCandidateInformation(Hash daoId, long lastTermNumber)
    {
        var previousElectedCandidates = State.ElectedCandidates[daoId] ?? new AddressList();
        var previousElectedCandidateAddresses = previousElectedCandidates.Value.ToList();

        var electedCandidateAddresses = GetVictories(daoId, previousElectedCandidateAddresses);
        foreach (var address in previousElectedCandidateAddresses)
        {
            var candidateInformation = State.CandidateInformationMap[daoId][address];
            if (candidateInformation == null)
            {
                continue;
            }

            if (electedCandidateAddresses.Contains(address))
            {
                candidateInformation.Terms.Add(lastTermNumber);
                candidateInformation.ContinualAppointmentCount = candidateInformation.ContinualAppointmentCount.Add(1);
            }
            else
            {
                candidateInformation.ContinualAppointmentCount = 0;
            }

            State.CandidateInformationMap[daoId][address] = candidateInformation;
        }

        State.ElectedCandidates[daoId].Value.Clear();
        State.ElectedCandidates[daoId].Value.AddRange(electedCandidateAddresses);
    }

    private List<Address> GetVictories(Hash daoId, List<Address> currentHighCouncilMembers)
    {
        var highCouncilConfig = State.HighCouncilConfig[daoId];
        Assert(highCouncilConfig != null, "HighCouncilConfig not found.");

        var validCandidates = GetValidCandidates(daoId);

        List<Address> victories;

        var maxMemberCount = highCouncilConfig!.MaxHighCouncilMemberCount;
        var diff = maxMemberCount - validCandidates.Count;
        //Valid candidates not enough
        if (diff > 0)
        {
            victories = new List<Address>(validCandidates);
            var backups = currentHighCouncilMembers.Where(k => !validCandidates.Contains(k)).ToList();
            if (State.InitialHighCouncilMembers[daoId]?.Value is { Count: > 0 })
            {
                backups.AddRange(
                    State.InitialHighCouncilMembers[daoId].Value.Where(k => !backups.Contains(k)));
            }

            victories.AddRange(backups.OrderBy(p => p)
                .Take(Math.Min((int)diff, (int)maxMemberCount)));
            return victories;
        }

        victories = validCandidates.Select(k => State.CandidateVotes[daoId][k])
            .OrderByDescending(v => v.ObtainedActiveVotedVotesAmount).Select(v => v.Address)
            .Take((int)maxMemberCount).ToList();

        return victories;
    }

    private List<Address> GetValidCandidates(Hash daoId)
    {
        if (State.Candidates[daoId]?.Value == null)
        {
            return new List<Address>();
        }

        return State.Candidates[daoId].Value
            .Where(address => State.CandidateVotes[daoId][address] != null &&
                              State.CandidateVotes[daoId][address].ObtainedActiveVotedVotesAmount > 0)
            .ToList();
    }
}