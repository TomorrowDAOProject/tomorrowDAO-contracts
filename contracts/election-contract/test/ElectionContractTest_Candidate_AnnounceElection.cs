using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestCandidateAnnounceElection : ElectionContractTestBase
{
    [Fact]
    public async Task AnnounceElectionTest()
    {
        var daoId = await InitializeContractAndCreateDao();

        var result = await AnnounceElection(daoId);
        result.ShouldNotBeNull();

        var candidateAdded = GetLogEvent<CandidateAdded>(result.TransactionResult);
        candidateAdded.ShouldNotBeNull();
        candidateAdded.Candidate.ShouldBe(((MethodStubFactory)ElectionContractStub.__factory).Sender);

        var candidates = await ElectionContractStub.GetCandidates.CallAsync(daoId);
        candidates.ShouldNotBeNull();
        candidates.Value.Count.ShouldBe(1);

        var candidateInfo = await ElectionContractStub.GetCandidateInformation.CallAsync(new GetCandidateInformationInput
        {
            DaoId = daoId,
            Candidate = candidates.Value.FirstOrDefault()
        });
        candidateInfo.ShouldNotBeNull();
        
        var transferred = GetLogEvent<Transferred>(result.TransactionResult);
        transferred.ShouldNotBeNull();
        transferred.Amount.ShouldBe(StakeAmount);
        //var smartContractBridgeContext = Application.ServiceProvider.GetRequiredService<ISmartContractBridgeContext>();
        //transferred.To.ShouldBe(smartContractBridgeContext?.ConvertVirtualAddressToContractAddress(candidateInfo.AnnouncementTransactionId, smartContractBridgeContext.Self));
    }
}