using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsBaseInitialize : TreasuryContractTestsBase
{
    [Fact]
    public async Task InitializeTest()
    {
        await Initialize();
    }
    
    [Fact]
    public async Task InitializeTest_Deployer()
    {
        await Initialize();
    }

    [Fact]
    public async Task InitializeTest_Initialized()
    {
        await Initialize();
        
        var result = await TreasuryContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            GovernanceContractAddress = GovernanceContractAddress
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Already initialized.");
    }
    
    [Fact]
    public async Task InitializeTest_Empty()
    {
        var result = await TreasuryContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            DaoContractAddress = null,
            GovernanceContractAddress = GovernanceContractAddress
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("DaoContract");
        
        result = await TreasuryContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            GovernanceContractAddress = null
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("GovernanceContract");
    }
}