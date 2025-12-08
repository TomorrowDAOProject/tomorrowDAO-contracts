using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Vote
{ 
    public partial class VoteContractTest : VoteContractTestBase
    {
        [Fact]
        public async Task InitializeTest()
        {
            var result = await InitializeVote();
            result.TransactionResult.Error.ShouldBe("");
        }
        
        [Fact]
        public async Task InitializeTest_AlreadyInitialized()
        {
            await InitializeTest();
            var result = await VoteContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
        
        [Fact]
        public async Task InitializeTest_NoPermission()
        {
            var result = await VoteContractStubOther.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task InitializeTest_InvalidInput()
        {
            var result = await VoteContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Invalid governance contract address.");
        }
    }
    
}