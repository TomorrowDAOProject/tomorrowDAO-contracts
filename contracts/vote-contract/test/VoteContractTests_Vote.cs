using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Vote
{ 
    public partial class VoteContractTest : VoteContractTestBase
    {
        [Fact]
        public async Task RegisterTest_NoPermission()
        {
            var result = await VoteContractStub.Register.SendWithExceptionAsync(new VotingRegisterInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task RegisterTest()
        {
            await CreateVoteSchemeTest();
            var result = await VoteContractStub.Register.SendAsync(new VotingRegisterInput
            {
                VotingItemId = ProposalId,
                SchemeId = UniqueVoteVoteSchemeId,
                AcceptedToken = TokenElf,
                StartTimestamp = Timestamp.FromDateTime(new DateTime(2024, 5, 8, 0, 0, 0, DateTimeKind.Utc)),
                EndTimestamp = Timestamp.FromDateTime(new DateTime(2024, 5, 9, 0, 0, 0, DateTimeKind.Utc))
            });
            
        }
        
        [Fact]
        public async Task VoteTest_NotInitialized()
        {
            var result = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput());
            result.TransactionResult.Error.ShouldContain("Not initialized yet.");
        }
        
        [Fact]
        public async Task VoteTest()
        {
        }
    }
    
}