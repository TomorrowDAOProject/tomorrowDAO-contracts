using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeCalcSchemeAddress : GovernanceContractTestBase
{
    [Fact]
    public async Task CalculateGovernanceSchemeAddress()
    {
        var inpute = new CalculateGovernanceSchemeAddressInput
        {
            DaoId = DefaultDaoId,
            GovernanceMechanism = GovernanceMechanism.Referendum
        };

        var result = await GovernanceContractStub.CalculateGovernanceSchemeAddress.SendAsync(inpute);
        result.Output.ShouldNotBeNull();
    }
    
}