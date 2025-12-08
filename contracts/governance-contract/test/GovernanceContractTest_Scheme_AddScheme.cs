using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeAddScheme : GovernanceContractTestBase
{
    [Fact]
    public async Task AddGovernanceSchemeTest()
    {
        var address = await AddGovernanceScheme();
        address.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task AddGovernanceSchemeTest_NoInitialized()
    {
        var input = new AddGovernanceSchemeInput
        {
            DaoId = DefaultDaoId,
            GovernanceMechanism = GovernanceMechanism.Referendum,
            SchemeThreshold = DefaultSchemeThreshold,
            GovernanceToken = DefaultGovernanceToken
        };

        var result = await GovernanceContractStub.AddGovernanceScheme.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Not initialized yet");
    }
    
    [Fact]
    public async Task AddGovernanceSchemeTest_NoPermission()
    {
        await Initialize();
        
        var input = new AddGovernanceSchemeInput
        {
            DaoId = DefaultDaoId,
            GovernanceMechanism = GovernanceMechanism.Referendum,
            SchemeThreshold = DefaultSchemeThreshold,
            GovernanceToken = DefaultGovernanceToken
        };

        var result = await GovernanceContractStub.AddGovernanceScheme.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("No permission");
    }
}