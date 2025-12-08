using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestBaseInitialize : ElectionContractTestBase
{
    [Fact]
    public async Task InitializeTest()
    {
        await Initialize();
    }

    [Fact]
    public async Task InitializeTest_NullParameter()
    {
        var input = new InitializeInput
        {
            DaoContractAddress = null,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress
        };
        var result = await ElectionContractStub.Initialize.SendWithExceptionAsync(input);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public async Task InitializeTest_Already()
    {
        await Initialize();
        
        var input = new InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress
        };
        var result = await ElectionContractStub.Initialize.SendWithExceptionAsync(input);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Already");
    }
}