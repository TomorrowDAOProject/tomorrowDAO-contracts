using System;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    public override AddressList GetCandidates(Hash input)
    {
        return State.Candidates[input] ?? new AddressList();
    }

    public override AddressList GetVotedCandidates(Hash input)
    {
        var votedCandidates = new AddressList();
        var candidates = State.Candidates[input] ?? new AddressList();
        if (candidates.Value.Count == 0)
        {
            return votedCandidates;
        }

        foreach (var address in candidates.Value)
        {
            var candidateVotes = State.CandidateVotes[input][address];
            if (candidateVotes != null && candidateVotes.ObtainedActiveVotedVotesAmount > 0)
                votedCandidates.Value.Add(address);
        }

        return votedCandidates;
    }

    public override CandidateInformation GetCandidateInformation(GetCandidateInformationInput input)
    {
        return State.CandidateInformationMap[input.DaoId][input.Candidate] ?? new CandidateInformation();
    }

    public override GetPageableCandidateInformationOutput GetPageableCandidateInformation(PageInformation input)
    {
        var output = new GetPageableCandidateInformationOutput();
        var candidates = State.Candidates[input.DaoId] ?? new AddressList();

        var count = candidates.Value.Count;
        if (count <= input.Start) return output;

        var length = Math.Min(Math.Min(input.Length, 20), candidates.Value.Count.Sub(input.Start));
        foreach (var candidate in candidates.Value.Skip(input.Start).Take(length))
            output.Value.Add(new CandidateDetail
            {
                CandidateInformation = State.CandidateInformationMap[input.DaoId][candidate],
                ObtainedVotesAmount = GetCandidateVote(new GetCandidateVoteInput
                {
                    DaoId = input.DaoId,
                    Candidate = candidate
                }).ObtainedActiveVotedVotesAmount
            });
        return output;
    }

    public override CandidateVote GetCandidateVote(GetCandidateVoteInput input)
    {
        return State.CandidateVotes[input.DaoId][input.Candidate] ?? new CandidateVote
        {
            Address = input.Candidate
        };
    }

    public override AddressList GetVictories(Hash input)
    {
        return State.ElectedCandidates[input] ?? new AddressList();
    }

    public override HighCouncilConfig GetHighCouncilConfig(Hash input)
    {
        return State.HighCouncilConfig[input] ?? new HighCouncilConfig();
    }

    public override ElectorVote GetElectorVote(GetElectorVoteInput input)
    {
        return State.ElectorVotes[input.DaoId][input.Voter] ?? new ElectorVote();
    }

    public override TermSnapshot GetTermSnapshot(GetTermSnapshotInput input)
    {
        return State.Snapshots[input.DaoId][input.TermNumber] ?? new TermSnapshot();
    }

    public override ElectionResult GetElectionResult(GetElectionResultInput input)
    {
        var votingItemId = State.HighCouncilElectionVotingItemId[input.DaoId];
        var votingResultHash = new VotingResult
        {
            VotingItemId = votingItemId,
            SnapshotNumber = input.TermNumber
        }.GetHash();

        var votingResult = State.VotingResults[votingResultHash];
        var result = new ElectionResult
        {
            TermNumber = input.TermNumber,
            IsActive = input.TermNumber == State.CurrentTermNumber[input.DaoId],
            Results = { votingResult.Results }
        };
        return result;
    }

    public override AddressList GetInitialHighCouncilMembers(Hash input)
    {
        return State.InitialHighCouncilMembers[input] ?? new AddressList();
    }

    public override AddressList GetHighCouncilMembers(Hash input)
    {
        if (State.VotingEventEnabledStatus[input])
        {
            return GetVictories(input);
        }
        else
        {
            return GetInitialHighCouncilMembers(input);
        }
    }
}