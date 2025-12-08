using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestCandidateQuitElection : ElectionContractTestBase
{
    [Fact]
    public async Task QuitElectionTest()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId);

        var candidates = await ElectionContractStub.GetCandidates.CallAsync(daoId);
        candidates.ShouldNotBeNull();
        candidates.Value.Count.ShouldBe(1);

        var result = await QuitElection(daoId);
        result.ShouldNotBeNull();

        var senderAddress = ((MethodStubFactory)ElectionContractStub.__factory).Sender;
        
        var candidateRemoved = GetLogEvent<CandidateRemoved>(result.TransactionResult);
        candidateRemoved.ShouldNotBeNull();
        candidateRemoved.DaoId.ShouldBe(daoId);
        candidateRemoved.Candidate.ShouldBe(senderAddress);

        var transferred = GetLogEvent<Transferred>(result.TransactionResult);
        transferred.ShouldNotBeNull();
        
        candidates = await ElectionContractStub.GetCandidates.CallAsync(daoId);
        candidates.ShouldNotBeNull();
        candidates.Value.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task QuitElectionTest_Permission()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId);

        var senderAddress = ((MethodStubFactory)ElectionContractStub.__factory).Sender;
        var result = await ElectionContractStubOther.QuitElection.SendWithExceptionAsync(new QuitElectionInput
        {
            DaoId = daoId,
            Candidate = senderAddress
        });

        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Only admin can quit election");
    }
    
    [Fact]
    public async Task QuitElectionTest_Repeated()
    {
        var daoId = await InitializeContractAndCreateDao();
        await AnnounceElection(daoId);

        var result = await QuitElection(daoId);
        result.ShouldNotBeNull();

        var senderAddress = ((MethodStubFactory)ElectionContractStub.__factory).Sender;
        
        result = await QuitElection(daoId, withException:true);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Target is not a candidate.");
    }
}