using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using TomorrowDAO.Contracts.Vote;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestProposalCreateProposal : GovernanceContractTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GovernanceContractTestProposalCreateProposal(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    [Fact]
    public async Task CreateProposalTest()
    {
        var input = MockCreateProposalInput();
        var result = await CreateProposalAsync(input, false, VoteMechanism.TokenBallot);
        result.ShouldNotBeNull();
        _testOutputHelper.WriteLine("ProposalId = {0}", result);
    }

    [Fact]
    public async Task CreateProposalTest_InvalidVoteSchemeId()
    {
        var input = MockCreateProposalInput();
        var result = await CreateProposalAsync(input, true, VoteMechanism.UniqueVote);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("Invalid voteSchemeId.");
        _testOutputHelper.WriteLine("ProposalId = {0}", result);
    }

    [Fact]
    public async Task CreateProposalTest_EventTest()
    {
        var input = MockCreateProposalInput();
        var executionResult = await CreateProposalAsync(input, false, VoteMechanism.TokenBallot);
        var logEvents = executionResult.TransactionResult.Logs;
        var existProposalCreated = false;
        foreach (var logEvent in logEvents)
        {
            if (logEvent.Name == "ProposalCreated")
            {
                existProposalCreated = true;
            }
        }

        existProposalCreated.ShouldBe(true);

        LogEvent proposalCreatedEvent = null;
        foreach (var logEvent in executionResult.TransactionResult.Logs)
        {
            if (logEvent.Name.Contains(nameof(ProposalCreated)))
            {
                proposalCreatedEvent = logEvent;
            }
        }

        proposalCreatedEvent.ShouldNotBeNull();
        var url = ProposalCreated.Parser
            .ParseFrom(proposalCreatedEvent.NonIndexed)
            .ForumUrl;
        url.ShouldNotBeNull();
        url.ShouldContain("https://www.ForumUrl.com");
    }

    [Fact]
    public async Task CreateProposalTest_CannotBeUnusedOrVeto()
    {
        var input = MockCreateProposalInput();
        input.ProposalType = (int)ProposalType.Unused;
        var executionResult = await CreateProposalAsync(input, true, VoteMechanism.TokenBallot);
        executionResult.TransactionResult.Error.ShouldContain("ProposalType cannot be Unused or Veto");

        input.ProposalType = (int)ProposalType.Veto;
        executionResult = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input);
        executionResult.TransactionResult.Error.ShouldContain("ProposalType cannot be Unused or Veto");
    }

    [Fact]
    public async Task CreateProposalTest_ExecuteTransactionIsNull()
    {
        var input = MockCreateProposalInput();
        input.ProposalType = (int)ProposalType.Governance;
        input.Transaction = null;
        var executionResult = await CreateProposalAsync(input, true, VoteMechanism.TokenBallot);
        _testOutputHelper.WriteLine(executionResult.TransactionResult.Error);
        executionResult.TransactionResult.Error.ShouldContain("Invalid input or parameter does not exist");
    }
    
    [Fact]
    public async Task CreateProposalTest_MultisigDao()
    {
        var input = MockCreateProposalInput(1 * 24);
        var executionResult =
            await CreateProposalAsync(input, false,
                GovernanceMechanism.Organization,
                VoteMechanism.UniqueVote,
                buildDaoFunc: async () =>
                {
                    var createDaoInput = BuildCreateDaoInput(isNetworkDao: false,
                        governanceMechanism: GovernanceMechanism.Organization);
                    createDaoInput.Members.Value.Remove(UserAddress);
                    var daoId = await MockDao(input: createDaoInput);
                    return daoId;
                });
        var proposalId = executionResult.Output;
        
        //Vote 10s
        BlockTimeProvider.SetBlockTime(10000);
        await VoteProposalAsync(proposalId, 1, VoteOption.Approved);
        var output = await GovernanceContractStub.GetProposalStatus.CallAsync(proposalId);
        output.ShouldNotBeNull();
        output.ProposalStage.ShouldBe(ProposalStage.Active);
        output.ProposalStatus.ShouldBe(ProposalStatus.PendingVote);
        
        //Vote 1d
        BlockTimeProvider.SetBlockTime(24 * 3600 * 1000);
        output = await GovernanceContractStub.GetProposalStatus.CallAsync(proposalId);
        output.ShouldNotBeNull();
        output.ProposalStage.ShouldBe(ProposalStage.Execute);
        output.ProposalStatus.ShouldBe(ProposalStatus.Approved);
    }

    [Fact]
    public async Task CreateProposalTest_MultisigDao_BelowThreshold()
    {
        var input = MockCreateProposalInput(1 * 24);
        var executionResult =
            await CreateProposalAsync(input, false,
                GovernanceMechanism.Organization,
                VoteMechanism.UniqueVote,
                buildDaoFunc: async () =>
                {
                    var createDaoInput = BuildCreateDaoInput(isNetworkDao: false,
                        governanceMechanism: GovernanceMechanism.Organization);
                    createDaoInput.Members.Value.Add(Accounts[2].Address);
                    createDaoInput.GovernanceSchemeThreshold.MinimalRequiredThreshold = 0;
                        var daoId = await MockDao(input: createDaoInput);
                    return daoId;
                });
        var proposalId = executionResult.Output;
        
        //Vote 10s
        BlockTimeProvider.SetBlockTime(10000);
        await VoteProposalAsync(proposalId, 1, VoteOption.Approved);
        var output = await GovernanceContractStub.GetProposalStatus.CallAsync(proposalId);
        output.ShouldNotBeNull();
        output.ProposalStage.ShouldBe(ProposalStage.Active);
        output.ProposalStatus.ShouldBe(ProposalStatus.PendingVote);
        
        //Vote 1d
        BlockTimeProvider.SetBlockTime(24 * 3600 * 1000);
        output = await GovernanceContractStub.GetProposalStatus.CallAsync(proposalId);
        output.ShouldNotBeNull();
        output.ProposalStage.ShouldBe(ProposalStage.Finished);
        output.ProposalStatus.ShouldBe(ProposalStatus.BelowThreshold);
    }
}