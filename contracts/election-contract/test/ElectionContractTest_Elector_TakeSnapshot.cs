using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestElectorTakeSnapshot : ElectionContractTestBase
{
    [Fact]
    public async Task TakeSnapshot()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId, UserAddress);

        var result = await Vote(daoId, UserAddress);
        var voteId = result.Output;

        // var executionResult = await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput
        // {
        //     DaoId = daoId,
        //     TermNumber = 1
        // });
        // executionResult.ShouldNotBeNull();
        //
        // var addressList = await ElectionContractStub.GetVictories.CallAsync(daoId);
        // addressList.ShouldNotBeNull();
        // addressList.Value.Count.ShouldBe(1);
        //
        // var termSnapshot  = await ElectionContractStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
        // {
        //     DaoId = daoId,
        //     TermNumber = 1
        // });
        // termSnapshot.ShouldNotBeNull();
        // termSnapshot.TermNumber.ShouldBe(1);
    }
}