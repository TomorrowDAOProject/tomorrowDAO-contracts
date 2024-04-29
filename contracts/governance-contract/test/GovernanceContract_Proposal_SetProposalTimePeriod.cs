using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractProposalSetProposalTimePeriod : GovernanceContractTestBase
{
    [Fact]
    public async Task SetProposalTimePeriodTest()
    {
        await Initialize();
        var input = new SetProposalTimePeriodInput
        {
            DaoId = DefaultDaoId,
            ProposalTimePeriod = new DaoProposalTimePeriod
            {
                ActiveTimePeriod = 7,
                VetoActiveTimePeriod = 3,
                PendingTimePeriod = 5,
                ExecuteTimePeriod = 3,
                VetoExecuteTimePeriod = 1
            }
        };
        var result = await GovernanceContractStub.SetProposalTimePeriod.SendAsync(input);
        result.ShouldNotBeNull();
        var daoProposalTimePeriodSet =
            result.TransactionResult.Logs.FirstOrDefault(l => l.Name == "DaoProposalTimePeriodSet");
        daoProposalTimePeriodSet.ShouldNotBeNull();

        var timePeriod = await GovernanceContractStub.GetDaoProposalTimePeriod.CallAsync(DefaultDaoId);
        timePeriod.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetProposalTimePeriodTest_InvalidInput()
    {
        await Initialize();
        var result =
            await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(new SetProposalTimePeriodInput());
        result.TransactionResult.Error.ShouldContain("Invalid input");

        //ProposalTimePeriod is null
        var input = new SetProposalTimePeriodInput
        {
            DaoId = DefaultDaoId,
        };
        result = await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Invalid input");
        
        //DaoId is null
        input = new SetProposalTimePeriodInput
        {
            ProposalTimePeriod = new DaoProposalTimePeriod
            {
                ActiveTimePeriod = 7,
                VetoActiveTimePeriod = 3,
                PendingTimePeriod = 5,
                ExecuteTimePeriod = 3,
                VetoExecuteTimePeriod = 5
            }
        };
        result = await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Invalid input");

        //out of range
        input = new SetProposalTimePeriodInput
        {
            DaoId = DefaultDaoId,
            ProposalTimePeriod = new DaoProposalTimePeriod
            {
                ActiveTimePeriod = 7,
                VetoActiveTimePeriod = 3,
                PendingTimePeriod = 5,
                ExecuteTimePeriod = 3,
                VetoExecuteTimePeriod = 5
            }
        };
        result = await GovernanceContractStub.SetProposalTimePeriod.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("VetoExecuteTimePeriod should be between 1 and 3");
    }
}