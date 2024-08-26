using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Vote
{ 
    public partial class VoteContractTest : VoteContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public VoteContractTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task RegisterTest_NoPermission()
        {
            await InitializeAll();
            var result = await VoteContractStub.Register.SendWithExceptionAsync(new VotingRegisterInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        
        [Fact]
        public async Task RegisterTest()
        {
            await InitializeAll();
            // Governance + R + 1a1v
            GovernanceR1A1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            // Governance + HC + 1a1v
            GovernanceHc1A1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, HcSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            // Governance + O + 1a1v
            GovernanceO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Governance, OSchemeAddress, UniqueVoteVoteSchemeId);
            // Governance + R + 1t1v
            GovernanceR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, RSchemeAddress, TokenBallotVoteSchemeId);
            // Governance + HC + 1t1v
            GovernanceHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Governance, HcSchemeAddress, TokenBallotVoteSchemeId);
            // Governance + O + 1t1v
            GovernanceO1T1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Governance, OSchemeAddress, TokenBallotVoteSchemeId, "Invalid voteSchemeId.");
            
            // Advisory + R + 1a1v
            AdvisoryR1A1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            // Advisory + HC + 1a1v
            AdvisoryHc1A1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, HcSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            // Advisory + O + 1a1v
            AdvisoryO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Advisory, OSchemeAddress, UniqueVoteVoteSchemeId);
            // Advisory + R + 1t1v
            AdvisoryR1T1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, TokenBallotVoteSchemeId);
            // Advisory + HC + 1t1v
            AdvisoryHc1T1VProposalId = await CreateProposal(DaoId, ProposalType.Advisory, HcSchemeAddress, TokenBallotVoteSchemeId);
            // Advisory + O + 1t1v
            AdvisoryO1A1VProposalId = await CreateProposal(OrganizationDaoId, ProposalType.Advisory, OSchemeAddress, TokenBallotVoteSchemeId, "Invalid voteSchemeId.");
            
            // NetworkDao
            NetworkDaoGovernanceR1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoRSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            NetworkDaoGovernanceHc1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoHcSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            NetworkDaoGovernanceR1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoRSchemeAddress, TokenBallotVoteSchemeId);
            NetworkDaoGovernanceHc1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Governance, NetworkDaoHcSchemeAddress, TokenBallotVoteSchemeId);
            NetworkDaoAdvisoryR1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoRSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            NetworkDaoAdvisoryHc1A1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoHcSchemeAddress, UniqueVoteVoteSchemeId, "Invalid voteSchemeId.");
            NetworkDaoAdvisoryR1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoRSchemeAddress, TokenBallotVoteSchemeId);
            NetworkDaoAdvisoryHc1T1VProposalId = await CreateProposal(NetworkDaoId, ProposalType.Advisory, NetworkDaoHcSchemeAddress, TokenBallotVoteSchemeId);
            
            // tg ranking 
            // token dao && 1t1v && day vote
            AdvisoryR1T1VProposalId_NoLock_DayVote = await CreateProposal(DaoId, ProposalType.Advisory, RSchemeAddress, TokenBallotVoteSchemeId_NoLock_DayVote);
        }

        [Fact]
        public async Task RegisterTest_Veto()
        {
            await RegisterTest();
            await HighCouncilElection(DaoId);
            await HighCouncilElectionFor(DaoId, UserAddress);
            // await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceHc1A1VProposalId);
            await ApproveElf(OneElf * 10, VoteContractAddress);
            await Vote(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId);
            BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
            
            // Veto + R + 1a1v
            // VetoR1A1VProposalId = await CreateVetoProposal(HcSchemeAddress, UniqueVoteVoteSchemeId, GovernanceHc1A1VProposalId);
            // Veto + R + 1t1v
            VetoR1T1VProposalId = await CreateVetoProposal(HcSchemeAddress, TokenBallotVoteSchemeId, GovernanceHc1T1VProposalId);
        }

        [Fact]
        public async Task VoteTest_NotInitialized()
        {
            var result = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput());
            result.TransactionResult.Error.ShouldContain("Not initialized yet.");
        }
        
        [Fact]
        public async Task VoteTest_1()
        {
            await RegisterTest();
            await ApproveElf(OneElf * 10, VoteContractAddress);
            // await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceR1A1VProposalId);
            // await VoteException(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceHc1A1VProposalId, "Invalid voter: not HC");
            await Vote(OneElf, VoteOption.Approved, GovernanceR1T1VProposalId);
            await Vote(OneElf, VoteOption.Approved, GovernanceHc1T1VProposalId);
            // await Vote(UniqueVoteVoteAmount, VoteOption.Approved, AdvisoryR1A1VProposalId);
            // await VoteException(UniqueVoteVoteAmount, VoteOption.Approved, AdvisoryHc1A1VProposalId, "Invalid voter: not HC");
            await Vote(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId);
            await Vote(OneElf, VoteOption.Approved, AdvisoryHc1T1VProposalId);
            
            // organization
            await Vote(UniqueVoteVoteAmount, VoteOption.Approved, GovernanceO1A1VProposalId);
            
            // NetworkDao
            // await Vote(UniqueVoteVoteAmount, VoteOption.Abstained, NetworkDaoGovernanceR1A1VProposalId);
            // await Vote(UniqueVoteVoteAmount, VoteOption.Rejected, NetworkDaoGovernanceHc1A1VProposalId);
            await Vote(OneElf, VoteOption.Abstained, NetworkDaoGovernanceR1T1VProposalId);
            await Vote(OneElf, VoteOption.Rejected, NetworkDaoGovernanceHc1T1VProposalId);
            // await Vote(UniqueVoteVoteAmount, VoteOption.Abstained, NetworkDaoAdvisoryR1A1VProposalId);
            // await Vote(UniqueVoteVoteAmount, VoteOption.Rejected, NetworkDaoAdvisoryHc1A1VProposalId);
            await Vote(OneElf, VoteOption.Abstained, NetworkDaoAdvisoryR1T1VProposalId);
            await Vote(OneElf, VoteOption.Rejected, NetworkDaoAdvisoryHc1T1VProposalId);
        }

        [Fact]
        public async Task VoteTest_2()
        {
            await RegisterTest();
            
            // tg ranking
            await Vote(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId_NoLock_DayVote);
            await GetDaoRemainAmount(DaoId, DefaultAddress, 0);
            await GetDaoProposalRemainAmount(DaoId, DefaultAddress, AdvisoryR1T1VProposalId_NoLock_DayVote, 0);
            var votingResult = await VoteContractStub.GetVotingResult.CallAsync(AdvisoryR1T1VProposalId_NoLock_DayVote);
            votingResult.TotalVotersCount.ShouldBe(1);
            votingResult.ApproveCounts.ShouldBe(OneElf);
            votingResult.VotesAmount.ShouldBe(OneElf);
            
            await VoteException(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId_NoLock_DayVote, "Voter already voted today.");
            
            BlockTimeProvider.SetBlockTime(3600 * 24 * 1 * 1000);
            await Vote(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId_NoLock_DayVote);
            votingResult = await VoteContractStub.GetVotingResult.CallAsync(AdvisoryR1T1VProposalId_NoLock_DayVote);
            votingResult.TotalVotersCount.ShouldBe(1);
            votingResult.ApproveCounts.ShouldBe(OneElf * 2);
            votingResult.VotesAmount.ShouldBe(OneElf * 2);
            
            BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000);
            await VoteException(OneElf, VoteOption.Approved, AdvisoryR1T1VProposalId_NoLock_DayVote, "Vote ended.");
        }

        [Fact]
        public async Task WithdrawTest()
        {
            await RegisterTest();
            await ApproveElf(OneElf * 10, VoteContractAddress);
            await Vote(OneElf, VoteOption.Approved, GovernanceR1T1VProposalId);
            await GetDaoRemainAmount(DaoId, DefaultAddress, OneElf);
            await GetDaoProposalRemainAmount(DaoId, DefaultAddress, GovernanceR1T1VProposalId, OneElf);
            BlockTimeProvider.SetBlockTime(3600 * 24 * 8 * 1000);
            await Withdraw(DaoId, new VotingItemIdList { Value = { GovernanceR1T1VProposalId } }, OneElf);
            await GetDaoRemainAmount(DaoId, DefaultAddress, 0);
            await GetDaoProposalRemainAmount(DaoId, DefaultAddress, GovernanceR1T1VProposalId, 0);
        }

        [Fact]
        public async Task GetVirtualAddressTest()
        {
            var daoId = HashHelper.ComputeFrom("daoId");
            await InitializeTest();
            var result = await VoteContractStub.GetVirtualAddress.CallAsync(new GetVirtualAddressInput
            {
                DaoId = daoId,
                Voter = DefaultAddress
            });
            _testOutputHelper.WriteLine("virtual address={0}", result);
        }

        [Fact]
        public async Task Test()
        {
            var input = new VoteInput
            {
                VoteOption = 0,
                VoteAmount = 100000000,
                VotingItemId = Hash.LoadFromHex("7a9ce919a59f04ee235690d30689401b8fe260fc632250489f7f49246c67eb3b"),
                Memo = "memo"
            };
            var sender = Address.FromBase58("izyQqrVvraoDC69HvUX8gEAgNrK3hWq9qhUKh5vh4MfzNjfc6");
            var txId = Hash.LoadFromHex("d4676644fd2c142a7227e8b62c5093573cbfb74dfd6f5da03925c382e97b9ccc");
            var voteId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input),  HashHelper.ComputeFrom(sender), txId);
            _testOutputHelper.WriteLine("voteid1={0}", voteId.ToHex());
        }
    }
    
}