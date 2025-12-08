using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestBaseSetHighCouncilConfig : ElectionContractTestBase
{
    [Fact]
    public async Task SetHighCouncilConfigTest()
    {
        await Initialize(DefaultAddress);

        var input = new SetHighCouncilConfigInput
        {
            DaoId = DefaultDaoId,
            MaxHighCouncilMemberCount = 10,
            MaxHighCouncilCandidateCount = 20,
            StakeThreshold = 100000,
            ElectionPeriod = 7 * 24 * 60 * 60,
            IsRequireHighCouncilForExecution = false,
            GovernanceToken = DefaultGovernanceToken
        };
        await ElectionContractStub.SetHighCouncilConfig.SendAsync(input);
    }
    
    [Fact]
    public async Task SetHighCouncilConfigTest_NoPermission()
    {
        await Initialize();

        var input = new SetHighCouncilConfigInput
        {
            DaoId = DefaultDaoId,
            MaxHighCouncilMemberCount = 10,
            MaxHighCouncilCandidateCount = 20,
            StakeThreshold = 100000,
            ElectionPeriod = 7 * 24 * 60 * 60,
            IsRequireHighCouncilForExecution = false,
            GovernanceToken = DefaultGovernanceToken
        };
        var result = await ElectionContractStub.SetHighCouncilConfig.SendWithExceptionAsync(input);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("no permission");
    }
    
    [Fact]
    public async Task SetHighCouncilConfigTest_InvalidParameter()
    {
        await Initialize(DefaultAddress);
        //case 1
        var input = new SetHighCouncilConfigInput
        {
            DaoId = null,
            MaxHighCouncilMemberCount = 10,
            MaxHighCouncilCandidateCount = 20,
            StakeThreshold = 100000,
            ElectionPeriod = 7 * 24 * 60 * 60,
            IsRequireHighCouncilForExecution = false,
            GovernanceToken = DefaultGovernanceToken
        };
        var result = await ElectionContractStub.SetHighCouncilConfig.SendWithExceptionAsync(input);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("cannot be null or empty");
        //case 2
        input = new SetHighCouncilConfigInput
        {
            DaoId = DefaultDaoId,
            MaxHighCouncilMemberCount = 0,
            MaxHighCouncilCandidateCount = 20,
            StakeThreshold = 100000,
            ElectionPeriod = 7 * 24 * 60 * 60,
            IsRequireHighCouncilForExecution = false,
            GovernanceToken = DefaultGovernanceToken
        };
        result = await ElectionContractStub.SetHighCouncilConfig.SendWithExceptionAsync(input);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Invalid");
        //case 3
        input = new SetHighCouncilConfigInput
        {
            DaoId = DefaultDaoId,
            MaxHighCouncilMemberCount = 10,
            MaxHighCouncilCandidateCount = 20,
            StakeThreshold = 100000,
            ElectionPeriod = 7 * 24 * 60 * 60,
            IsRequireHighCouncilForExecution = false,
            GovernanceToken = ""
        };
        result = await ElectionContractStub.SetHighCouncilConfig.SendWithExceptionAsync(input);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("cannot be null or empty");
    }
    
}