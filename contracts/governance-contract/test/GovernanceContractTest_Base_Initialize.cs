using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeInitialize : GovernanceContractTestBase
{

    [Fact]
    public async Task InitializeTest()
    {
        await Initialize();
    }

    [Fact]
    public async Task InitializeTest_AlreadyInitialized()
    {
        await Initialize();
        var result = await GovernanceContractStubOther.Initialize.SendWithExceptionAsync(new InitializeInput());
        result.TransactionResult.Error.ShouldContain("Already initialized");
    }
    
    [Fact]
    public async Task InitializeTest_NoPermission()
    {
        var input = new InitializeInput
        {
            DaoContractAddress = null
        };
        var result = await GovernanceContractStubOther.Initialize.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("No permission");
    }
    
    [Fact]
    public async Task InitializeTest_InvalidInput()
    {
        var input = new InitializeInput
        {
            DaoContractAddress = null
        };
        var result = await GovernanceContractStub.Initialize.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Invalid input or parameter does not exist");
    }
}