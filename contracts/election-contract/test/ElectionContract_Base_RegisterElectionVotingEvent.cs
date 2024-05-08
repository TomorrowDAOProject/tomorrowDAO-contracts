using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractBaseRegisterElectionVotingEvent : ElectionContractBaseTests
{
    [Fact]
    public async Task RegisterElectionVotingEventTest()
    {
        await Initialize(DefaultAddress);

        var result =
            await ElectionContractStub.RegisterElectionVotingEvent.SendAsync(new RegisterElectionVotingEventInput
            {
                DaoId = DefaultDaoId,
                MaxHighCouncilMemberCount = 10,
                MaxHighCouncilCandidateCount = 20,
                StakeThreshold = 100000,
                ElectionPeriod = 7 * 24 * 60 * 60,
                IsRequireHighCouncilForExecution = false,
                GovernanceToken = DefaultGovernanceToken,
                LockTokenForElection = 50000
            });

        var logEvent = GetLogEvent<ElectionVotingEventRegistered>(result.TransactionResult);
        logEvent.ShouldNotBeNull();
    }
}