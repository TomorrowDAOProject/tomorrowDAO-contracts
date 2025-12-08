using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Theory]
    [InlineData("SetSubsistStatus", true, true, false)]
    [InlineData("UploadFileInfos", false, false, false)]
    [InlineData("RemoveFileInfos", false, false, false)]
    [InlineData("AddGovernanceScheme", true, true, false)]
    public async Task HashPermissionTests(string methodName, bool referendumPermission, bool highCouncilPermission, bool otherPermission)
    {
        await InitializeAsync();

        var daoId = await CreateDAOAsync();
        
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();

        {
            var result = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = addressList.Value.FirstOrDefault(),
                What = methodName
            });
            result.Value.ShouldBe(referendumPermission);
        }
        {
            var result = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = addressList.Value.LastOrDefault(),
                What = methodName
            });
            result.Value.ShouldBe(highCouncilPermission);
        }
        {
            var result = await DAOContractStub.HasPermission.CallAsync(new HasPermissionInput
            {
                DaoId = daoId,
                Where = DAOContractAddress,
                Who = OtherAddress,
                What = methodName
            });
            result.Value.ShouldBe(otherPermission);
        }
    }
}