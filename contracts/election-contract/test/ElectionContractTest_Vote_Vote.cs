using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestVoteVote : ElectionContractTestBase
{
    [Fact]
    public async Task VoteTest()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId, UserAddress);

        var candidates = await ElectionContractStub.GetCandidates.CallAsync(daoId);
        candidates.ShouldNotBeNull();
        candidates.Value.Count.ShouldBe(1);

        var result = await Vote(daoId, UserAddress);

        var candidateVote = await ElectionContractStub.GetCandidateVote.CallAsync(new GetCandidateVoteInput
        {
            DaoId = daoId,
            Candidate = UserAddress
        });
        candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(VotingAmount);
    }
}