using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestVoteWithdraw : ElectionContractTestBase
{
    [Fact]
    public async Task WithdrawTest()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId, UserAddress);

        var result = await Vote(daoId, UserAddress);
        var voteId = result.Output;

        var candidateVote = await ElectionContractStub.GetCandidateVote.CallAsync(new GetCandidateVoteInput
        {
            DaoId = daoId,
            Candidate = UserAddress
        });
        candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(VotingAmount);
        
        BlockTimeProvider.SetBlockTime(DateTime.UtcNow.AddSeconds(MaximumLockTime).AddSeconds(100).ToTimestamp());
        var executionResult = await ElectionContractStub.Withdraw.SendAsync(voteId);
        
        candidateVote = await ElectionContractStub.GetCandidateVote.CallAsync(new GetCandidateVoteInput
        {
            DaoId = daoId,
            Candidate = UserAddress
        });
        candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(0);
        candidateVote.ObtainedActiveVotingRecords.Count.ShouldBe(0);
        
    }
    
    [Fact]
    public async Task WithdrawTest_Permission()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId, UserAddress);

        var result = await Vote(daoId, UserAddress);
        var voteId = result.Output;

        var candidateVote = await ElectionContractStub.GetCandidateVote.CallAsync(new GetCandidateVoteInput
        {
            DaoId = daoId,
            Candidate = UserAddress
        });
        candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(VotingAmount);
        
        BlockTimeProvider.SetBlockTime(DateTime.UtcNow.AddSeconds(MaximumLockTime).AddSeconds(100).ToTimestamp());
        var executionResult = await ElectionContractStubOther.Withdraw.SendWithExceptionAsync(voteId);
        executionResult.TransactionResult.Error.ShouldContain("No permission");
    }
}