using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeUpdateThreshold : GovernanceContractTestBase
{
    [Fact]
    public async Task UpdateSchemeThresholdTest()
    {
        var address =  ((MethodStubFactory)GovernanceContractStub.__factory).Sender;
        await InitializeAllContract();
        var daoId = await MockDao();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        
        var inpute = new UpdateGovernanceSchemeThresholdInput
        {
            DaoId = DefaultDaoId,
            SchemeAddress = addressList.Value.LastOrDefault(),
            SchemeThreshold = null
        };

        //await GovernanceContractStub.UpdateGovernanceSchemeThreshold.SendAsync(inpute);
    }
}