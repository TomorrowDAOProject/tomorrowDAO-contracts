using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeRemoveScheme : GovernanceContractTestBase
{
    [Fact]
    public async Task RemoveSchemeTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);

        // var setSubsistStatusInput = new SetSubsistStatusInput
        // {
        //     DaoId = daoId,
        //     Status = true
        // };
        // await DAOContractStub.SetSubsistStatus.SendAsync(setSubsistStatusInput);

        // var inpute = new RemoveGovernanceSchemeInput
        // {
        //     DaoId = daoId,
        //     SchemeAddress = addressList.Value.FirstOrDefault()
        // };
        //
        // await GovernanceContractStub.RemoveGovernanceScheme.SendAsync(inpute);
    }
    
}