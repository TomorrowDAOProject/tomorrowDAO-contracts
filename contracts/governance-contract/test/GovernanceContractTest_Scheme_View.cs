using System.Threading.Tasks;
using Shouldly;
using TomorrowDAO.Contracts.Vote;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestSchemeView : GovernanceContractTestBase
{
    [Fact]
    public async Task GetGovernanceSchemeTest()
    {
        var address = await AddGovernanceScheme();
        var scheme = await GovernanceContractStub.GetGovernanceScheme.CallAsync(address);
        scheme.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetDaoGovernanceSchemeAddressListTest()
    {
        await AddGovernanceScheme();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(DefaultDaoId);
        addressList.ShouldNotBeNull();
        addressList.Value.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetDaoGovernanceSchemeListTest()
    {
        await AddGovernanceScheme();
        var schemeList = await GovernanceContractStub.GetDaoGovernanceSchemeList.CallAsync(DefaultDaoId);
        schemeList.ShouldNotBeNull();
        schemeList.Value.ShouldNotBeNull();
        schemeList.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetProposalSnapShotSchemeTest()
    {
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false, VoteMechanism.TokenBallot);
        var proposalId = executionResult.Output;

        var threshold = await GovernanceContractStub.GetProposalSnapShotScheme.CallAsync(proposalId);
        threshold.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProposalStatusTest()
    {
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false, VoteMechanism.TokenBallot);
        var proposalId = executionResult.Output;
        
        var proposalStatusOutput = await GovernanceContractStub.GetProposalStatus.CallAsync(proposalId);
        proposalStatusOutput.ShouldNotBeNull();
    }
}