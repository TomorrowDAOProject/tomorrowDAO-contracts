using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Vote
{ 
    public partial class VoteContractTest : VoteContractTestBase
    {
        [Fact]
        public async Task CreateVoteSchemeTest_NotInitialized()
        {
            var result = await VoteContractStub.CreateVoteScheme.SendWithExceptionAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
            result.TransactionResult.Error.ShouldContain("Not initialized yet.");
        }
        
        [Fact]
        public async Task CreateVoteSchemeTest_1()
        {
            await InitializeTest();
            var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
            result.TransactionResult.Error.ShouldBe("");
            var logEvent = result.TransactionResult.Logs.Single(x => x.Name.Contains(nameof(VoteSchemeCreated)));
            var voteScheme = VoteSchemeCreated.Parser.ParseFrom(logEvent.NonIndexed);
            voteScheme.VoteMechanism.ShouldBe(VoteMechanism.UniqueVote);
            voteScheme.VoteStrategy.ShouldBe(VoteStrategy.ProposalDistinct);
            voteScheme.WithoutLockToken.ShouldBe(false);
        }
        
        [Fact]
        public async Task CreateVoteSchemeTest_2()
        {
            await InitializeTest();
            var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.TokenBallot
            });
            result.TransactionResult.Error.ShouldBe("");
            var logEvent = result.TransactionResult.Logs.Single(x => x.Name.Contains(nameof(VoteSchemeCreated)));
            var voteScheme = VoteSchemeCreated.Parser.ParseFrom(logEvent.NonIndexed);
            voteScheme.VoteMechanism.ShouldBe(VoteMechanism.TokenBallot);
            voteScheme.VoteStrategy.ShouldBe(VoteStrategy.ProposalDistinct);
            voteScheme.WithoutLockToken.ShouldBe(false);
        }
        
        [Fact]
        public async Task CreateVoteSchemeTest_3()
        {
            await InitializeTest();
            var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.TokenBallot, WithoutLockToken = true, VoteStrategy = VoteStrategy.DayDistinct
            });
            result.TransactionResult.Error.ShouldBe("");
            var logEvent = result.TransactionResult.Logs.Single(x => x.Name.Contains(nameof(VoteSchemeCreated)));
            var voteScheme = VoteSchemeCreated.Parser.ParseFrom(logEvent.NonIndexed);
            voteScheme.VoteMechanism.ShouldBe(VoteMechanism.TokenBallot);
            voteScheme.VoteStrategy.ShouldBe(VoteStrategy.DayDistinct);
            voteScheme.WithoutLockToken.ShouldBe(true);
        }
        
        [Fact]
        public async Task CreateVoteSchemeTest_AlreadyExisted()
        {
            await CreateVoteSchemeTest_1();
            var result = await VoteContractStub.CreateVoteScheme.SendWithExceptionAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
            result.TransactionResult.Error.ShouldContain("VoteScheme already exists.");
        }
    }
    
}