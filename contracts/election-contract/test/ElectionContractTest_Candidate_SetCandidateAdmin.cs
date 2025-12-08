using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestCandidateSetCandidateAdmin : ElectionContractTestBase
{
    [Fact]
    public async Task SetCandidateAdminTest()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId, UserAddress);

        var candidates = await ElectionContractStub.GetCandidates.CallAsync(daoId);
        candidates.ShouldNotBeNull();
        candidates.Value.Count.ShouldBe(1);

        var result = await ElectionContractStub.SetCandidateAdmin.SendAsync(new SetCandidateAdminInput
        {
            DaoId = daoId,
            Candidate = UserAddress,
            NewAdmin = UserAddress
        });
        result.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task SetCandidateAdminTest_Permission()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId, UserAddress);

        var candidates = await ElectionContractStub.GetCandidates.CallAsync(daoId);
        candidates.ShouldNotBeNull();
        candidates.Value.Count.ShouldBe(1);

        var result = await ElectionContractStubOther.SetCandidateAdmin.SendWithExceptionAsync(new SetCandidateAdminInput
        {
            DaoId = daoId,
            Candidate = UserAddress,
            NewAdmin = UserAddress
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("No permission");
    }
}