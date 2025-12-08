using System.Threading.Tasks;
using AElf.ContractTestKit;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeSetToken : GovernanceContractTestBase
{
    [Fact]
    public async Task SetGovernanceTokenTest()
    {
        var address =  ((MethodStubFactory)GovernanceContractStub.__factory).Sender;
        await InitializeAllContract();
        var daoId = await MockDao();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        
        var inpute = new SetGovernanceTokenInput
        {
            DaoId = daoId,
            GovernanceToken = "CPU"
        };

        //await GovernanceContractStub.SetGovernanceToken.SendAsync(inpute);
    }
}