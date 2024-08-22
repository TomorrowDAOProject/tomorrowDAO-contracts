using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Fact]
    public async Task UpdateGovernanceSchemeThresholdTest()
    {
        await InitializeAll();
        GovernanceHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, TokenBallotVoteSchemeId, 
            "UpdateGovernanceSchemeThreshold", DAOContractAddress, new UpdateGovernanceSchemeThresholdInput{DaoId = DaoId, SchemeAddress = RSchemeAddress, SchemeThreshold = new GovernanceSchemeThreshold
        {
            MinimalRequiredThreshold = 1,
            MinimalVoteThreshold = 1,
            MinimalApproveThreshold = 1,
            MaximalRejectionThreshold = 1,
            MaximalAbstentionThreshold = 1
        }}.ToByteString());
        CheckGovernanceSchemeThreshold(RSchemeAddress, 0);
        await ApproveElf(OneElf * 100, VoteContractAddress);
        await Vote(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId);
        BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
        await GovernanceContractStub.ExecuteProposal.SendAsync(GovernanceHc1T1VProposalId);
        CheckGovernanceSchemeThreshold(RSchemeAddress, 1);
    }

    private async void CheckGovernanceSchemeThreshold(Address schemeAddress, int value)
    {
        var governanceScheme = await GovernanceContractStub.GetGovernanceScheme.CallAsync(schemeAddress);
        var threshold = governanceScheme.SchemeThreshold;
        threshold.MinimalApproveThreshold.ShouldBe(value);
    }
    
    [Fact]
    public async Task RemoveGovernanceSchemeTest()
    {
        await InitializeAll();
        GovernanceHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, HcSchemeAddress, TokenBallotVoteSchemeId,
            "RemoveGovernanceScheme", DAOContractAddress, new RemoveGovernanceSchemeInput{DaoId = DaoId, SchemeAddress = HcSchemeAddress}.ToByteString());
        CheckGovernanceSchemeThreshold(RSchemeAddress, 0);
        //await HighCouncilElection(DaoId);
        //await HighCouncilElectionFor(DaoId, UserAddress);
        await ApproveElf(OneElf * 100, VoteContractAddress);
        await Vote(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId);
        BlockTimeProvider.SetBlockTime(3600 * 24 * 14 * 1000);
        await GovernanceContractStub.ExecuteProposal.SendAsync(GovernanceHc1T1VProposalId);
        var governanceScheme = await GovernanceContractStub.GetGovernanceScheme.CallAsync(HcSchemeAddress);
        governanceScheme.SchemeThreshold.ShouldBeNull();
    }

    [Fact]
    public async Task SetGovernanceTokenTest()
    {
        await InitializeAll();
        GovernanceR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, TokenBallotVoteSchemeId, 
            "SetGovernanceToken", DAOContractAddress, new SetGovernanceTokenInput{DaoId = DaoId, GovernanceToken = TokenUsdt}.ToByteString());
        CheckGovernanceToken(RSchemeAddress, TokenElf);
        CheckGovernanceToken(HcSchemeAddress, TokenElf);
        CheckDaoGovernanceToken(DaoId, TokenElf);
        await ApproveElf(OneElf * 100, VoteContractAddress);
        await Vote(OneElf, VoteOption.Approved, GovernanceR1T1VProposalId);
        BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
        var result = await GovernanceContractStub.ExecuteProposal.SendWithExceptionAsync(GovernanceR1T1VProposalId);
        result.TransactionResult.Error.ShouldContain("Token not found.");
    }

    private async void CheckGovernanceToken(Address schemeAddress, string token)
    {
        var governanceScheme = await GovernanceContractStub.GetGovernanceScheme.CallAsync(schemeAddress);
        governanceScheme.GovernanceToken.ShouldBe(token);
    }
    
    private async void CheckDaoGovernanceToken(Hash daoId, string token)
    {
        var daoInfo = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
        daoInfo.GovernanceToken.ShouldBe(token);
    }

    [Fact]
    public async Task SetProposalTimePeriodTest()
    {
        await InitializeAll();
        GovernanceR1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Governance, OSchemeAddress, UniqueVoteVoteSchemeId, 
            "SetProposalTimePeriod", DAOContractAddress, new SetProposalTimePeriodInput{DaoId = OrganizationDaoId, ProposalTimePeriod = new DaoProposalTimePeriod
            {
                ActiveTimePeriod = 15 * 24 * 60 * 60,
                VetoActiveTimePeriod = 3 * 24 * 60 * 60,
                PendingTimePeriod = 7 * 24 * 60 * 60,
                ExecuteTimePeriod = 5 * 24 * 60 * 60,
                VetoExecuteTimePeriod = 3 * 24 * 60 * 60
            }}.ToByteString());
        CheckProposalTimePeriod(DaoId, 7 * 24 * 60 * 60);
        await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceR1A1VProposalId);
        BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
        await GovernanceContractStub.ExecuteProposal.SendAsync(GovernanceR1A1VProposalId);
        CheckProposalTimePeriod(DaoId, 15 * 24 * 60 * 60);
    }
    
    private async void CheckProposalTimePeriod(Hash daoId, int value)
    {
        var daoProposalTimePeriod = await GovernanceContractStub.GetDaoProposalTimePeriod.CallAsync(daoId);
        daoProposalTimePeriod.ActiveTimePeriod.ShouldBe(value);
    }
}