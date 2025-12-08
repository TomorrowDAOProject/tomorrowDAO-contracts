using System.Threading.Tasks;
using Google.Protobuf;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Fact]
    public async Task AddMemberTest()
    {
        await InitializeAll();
        await GetIsMember(OrganizationDaoId, UserAddress, false);
        GovernanceO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Governance, 
            OSchemeAddress, UniqueVoteVoteSchemeId, "AddMember", DAOContractAddress, 
            new AddMemberInput { DaoId = OrganizationDaoId, AddMembers = new AddressList{Value = { UserAddress }} }.ToByteString());
        await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceO1A1VProposalId);
        BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
        await GovernanceContractStub.ExecuteProposal.SendAsync(GovernanceO1A1VProposalId);
        await GetIsMember(OrganizationDaoId, UserAddress, true);
        await GetMemberCount(OrganizationDaoId, 2);
    }

    [Fact]
    public async Task RemoveMemberTest()
    {
        await AddMemberTest();
        await RemoveMemberVote();
        await GovernanceContractStub.ExecuteProposal.SendAsync(GovernanceO1A1VProposalId);
        await GetIsMember(OrganizationDaoId, DefaultAddress, false);
        await GetMemberCount(OrganizationDaoId, 1);
    }

    [Fact]
    public async Task RemoveMemberTest_LessThanMinVoter()
    {
        await GetIsMemberTest();
        await RemoveMemberVote();
        var result = await GovernanceContractStub.ExecuteProposal.SendWithExceptionAsync(GovernanceO1A1VProposalId);
        result.TransactionResult.Error.ShouldContain("members after remove will be less than 0.");
    }

    private async Task RemoveMemberVote()
    {
        GovernanceO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Governance, 
            OSchemeAddress, UniqueVoteVoteSchemeId, "RemoveMember", DAOContractAddress, 
            new RemoveMemberInput { DaoId = OrganizationDaoId, RemoveMembers = new AddressList{ Value = { DefaultAddress }} }.ToByteString());
        await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceO1A1VProposalId);
        BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
    }

    [Fact]
    public async Task GetIsMemberTest()
    {
        await InitializeAll();
        await GetIsMember(OrganizationDaoId, DefaultAddress, true);
    }

    [Fact]
    public async Task GetMemberCountTest()
    {
        await InitializeAll();
        await GetMemberCount(OrganizationDaoId, 1);
    }

    [Fact]
    public async Task GetOrganizationAddressTest()
    {
        await InitializeAll();
        var address = await DAOContractStub.GetOrganizationAddress.CallAsync(OrganizationDaoId);
        address.ShouldNotBeNull();
        address.ShouldBe(OSchemeAddress);
    }
}