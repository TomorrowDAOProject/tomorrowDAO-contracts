using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using HighCouncilConfig = TomorrowDAO.Contracts.DAO.HighCouncilConfig;

namespace TomorrowDAO.Contracts.Vote;

public class VoteContractTestBase : TestBase
{
    internal IBlockTimeProvider BlockTimeProvider;
    
    public const int UniqueVoteVoteAmount = 1;
    public const long OneElf = 1_00000000;
    public const long ActiveTimePeriod = 7 * 24 * 60 * 60;
    public const long VetoActiveTimePeriod = 3 * 24 * 60 * 60;
    protected Hash UniqueVoteVoteSchemeId; //1a1v
    protected Hash TokenBallotVoteSchemeId; //1t1v
    protected Hash TokenBallotVoteSchemeId_NoLock_DayVote; 
    protected string TokenElf = "ELF";
    protected Hash DaoId;
    protected Hash OrganizationDaoId; // organization dao
    protected Hash NetworkDaoId;
    protected Address HcSchemeAddress;
    protected Hash HcSchemeId;
    protected Address RSchemeAddress;
    protected Hash RSchemeId;
    protected Address OSchemeAddress; // organization
    protected Hash OSchemeId;
    protected Address NetworkDaoHcSchemeAddress;
    protected Hash NetworkDaoHcSchemeId;
    protected Address NetworkDaoRSchemeAddress;
    protected Hash NetworkDaoRSchemeId;
    
    protected Hash GovernanceR1A1VProposalId;
    protected Hash GovernanceR1T1VProposalId;
    protected Hash GovernanceHc1A1VProposalId;
    protected Hash GovernanceHc1T1VProposalId;
    protected Hash GovernanceO1A1VProposalId;
    protected Hash GovernanceO1T1VProposalId;
    
    protected Hash AdvisoryR1A1VProposalId;
    protected Hash AdvisoryR1T1VProposalId;
    protected Hash AdvisoryR1T1VProposalId_NoLock_DayVote;
    protected Hash AdvisoryHc1A1VProposalId;
    protected Hash AdvisoryHc1T1VProposalId;
    protected Hash AdvisoryO1A1VProposalId;
    protected Hash AdvisoryO1T1VProposalId;

    protected Hash VetoR1A1VProposalId;
    protected Hash VetoR1T1VProposalId;
    
    protected Hash NetworkDaoGovernanceR1A1VProposalId;
    protected Hash NetworkDaoGovernanceR1T1VProposalId;
    protected Hash NetworkDaoGovernanceHc1A1VProposalId;
    protected Hash NetworkDaoGovernanceHc1T1VProposalId;
    
    protected Hash NetworkDaoAdvisoryR1A1VProposalId;
    protected Hash NetworkDaoAdvisoryR1T1VProposalId;
    protected Hash NetworkDaoAdvisoryHc1A1VProposalId;
    protected Hash NetworkDaoAdvisoryHc1T1VProposalId;

    public VoteContractTestBase()
    {
        BlockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();
    }
    
    public async Task<IExecutionResult<Empty>> InitializeVote()
    {
        return await VoteContractStub.Initialize.SendAsync(new InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
        });
    }

    public async Task<IExecutionResult<Empty>> InitializeDAO()
    {
        var result = await DAOContractStub.Initialize.SendAsync(new DAO.InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TimelockContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress
        });
        await DAOContractStub.SetTreasuryContractAddress.SendAsync(TreasuryContractAddress);
        return result;
    }

    public async Task<IExecutionResult<Empty>> InitializeGovernance()
    {
        return await GovernanceContractStub.Initialize.SendAsync(new Governance.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress
        });
    }

    public async Task<IExecutionResult<Empty>> InitializeElection()
    {
        return await ElectionContractStub.Initialize.SendAsync(new Election.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            MinimumLockTime = 3600, //s
            MaximumLockTime = 360000 //s
        });
    }

    public async Task InitializeAll()
    {
        await InitializeGovernance();
        await InitializeDAO();
        await InitializeElection();
        await InitializeVote();
        await CreateVoteScheme(VoteMechanism.UniqueVote);
        await CreateVoteScheme(VoteMechanism.TokenBallot);
        await CreateVoteScheme(VoteMechanism.TokenBallot, true, VoteStrategy.DayDistinct);
        await CreateDao("DAO", true);
        await CreateDao("NetworkDAO");
        await CreateDao("Organization DAO", false, 2);
    }

    private async Task CreateVoteScheme(VoteMechanism voteMechanism, bool withoutLockToken = false, 
        VoteStrategy voteStrategy = VoteStrategy.ProposalDistinct)
    {
        var result = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = voteMechanism, WithoutLockToken = withoutLockToken, VoteStrategy = voteStrategy
        });

        var log = GetLogEvent<VoteSchemeCreated>(result.TransactionResult);
        switch (voteMechanism)
        {
            case VoteMechanism.UniqueVote:
                UniqueVoteVoteSchemeId = log.VoteSchemeId;
                break;
            case VoteMechanism.TokenBallot:
                if (withoutLockToken &&  VoteStrategy.DayDistinct == voteStrategy)
                {
                    TokenBallotVoteSchemeId_NoLock_DayVote = log.VoteSchemeId;
                }
                else
                {
                    TokenBallotVoteSchemeId = log.VoteSchemeId;
                }
                break;
        } 
    }

    private async Task CreateVoteScheme_NoLock_DayVote(VoteMechanism voteMechanism)
    {
        var result3 = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.TokenBallot, WithoutLockToken = true, VoteStrategy = VoteStrategy.DayDistinct
        });
    }

    public async Task CreateDao(string daoName, bool isNetworkDao = false, int governanceMechanism = 0)
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(GetCreateDAOInput(daoName, isNetworkDao, governanceMechanism));
        var dAOCreatedLog = GetLogEvent<DAOCreated>(result.TransactionResult);
        if (isNetworkDao)
        {
            NetworkDaoId = dAOCreatedLog.DaoId;
        }
        else
        {
            if (governanceMechanism == 2)
            {
                OrganizationDaoId = dAOCreatedLog.DaoId;
            }
            else
            {
                DaoId = dAOCreatedLog.DaoId;
            }
        }
        
        var governanceSchemeAddedLogs = GetMultiLogEvent<GovernanceSchemeAdded>(result.TransactionResult);
        foreach (var governanceSchemeAddedLog in governanceSchemeAddedLogs)
        {
            switch (governanceSchemeAddedLog.GovernanceMechanism)
            {
                case (Governance.GovernanceMechanism)GovernanceMechanism.HighCouncil when isNetworkDao:
                    NetworkDaoHcSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    NetworkDaoHcSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.HighCouncil:
                    HcSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    HcSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.Referendum when isNetworkDao:
                    NetworkDaoRSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    NetworkDaoRSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.Referendum:
                    RSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    RSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
                case (Governance.GovernanceMechanism)GovernanceMechanism.Organization:
                    OSchemeAddress = governanceSchemeAddedLog.SchemeAddress;
                    OSchemeId = governanceSchemeAddedLog.SchemeId;
                    break;
            }
        }
    }

    internal async Task<Hash> CreateProposal(Hash DaoId, ProposalType proposalType, Address schemeAddress, Hash voteSchemeId, string error = "")
    {
        IExecutionResult<Hash> result;
        if (string.IsNullOrEmpty(error))
        {
            result = await GovernanceContractStub.CreateProposal.SendAsync(GetCreateProposalInput(DaoId, proposalType, schemeAddress, voteSchemeId));
            result.TransactionResult.Error.ShouldBe(error);
            var governanceProposalLog = GetLogEvent<ProposalCreated>(result.TransactionResult);
            return governanceProposalLog.ProposalId;
        }

        result = await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(GetCreateProposalInput(DaoId, proposalType, schemeAddress, voteSchemeId));
        result.TransactionResult.Error.ShouldContain(error);
        return null;
    }
    
    protected async Task HighCouncilElection(Hash daoId)
    {
        await ApproveElf(OneElf * 100, ElectionContractAddress);;
        await ElectionContractStub.AnnounceElection.SendAsync(new AnnounceElectionInput
        {
            DaoId = daoId,
            CandidateAdmin = DefaultAddress
        });
        await ElectionVote(DefaultAddress);
        //var result = await TakeSnapshot(DaoId, 1);
        (await ElectionContractStub.GetHighCouncilMembers.CallAsync(DaoId)).Value.ShouldContain(DefaultAddress);
    }
    
    protected async Task HighCouncilElectionFor(Hash daoId, Address candidateAddress)
    {
        await ApproveElf(OneElf * 100, ElectionContractAddress);
        await ElectionContractStub.AnnounceElectionFor.SendAsync(new AnnounceElectionForInput
        {
            DaoId = daoId, Candidate = candidateAddress, CandidateAdmin = DefaultAddress
        });
        await ElectionVote(candidateAddress);
        //var result = await TakeSnapshot(DaoId, 2);
        (await ElectionContractStub.GetHighCouncilMembers.CallAsync(DaoId)).Value.ShouldContain(candidateAddress);
    }

    protected async Task ApproveElf(long amount, Address spender)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput { Spender = spender, Symbol = TokenElf, Amount = amount });
    }

    protected async Task<IExecutionResult<Hash>> ElectionVote(Address candidateAddress)
    {
        return await ElectionContractStub.Vote.SendAsync(new VoteHighCouncilInput
        {
            DaoId = DaoId, CandidateAddress = candidateAddress, Amount = OneElf * 10,
            EndTimestamp = DateTime.UtcNow.AddDays(4).ToTimestamp(), Token = null
        });
    }

    protected async Task<IExecutionResult<Empty>> TakeSnapshot(Hash daoId, long termNumber)
    {
        return await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput { DaoId = daoId, TermNumber = termNumber });
    }

    internal async Task<IExecutionResult<Empty>> Vote(long amount, VoteOption voteOption, Hash votingItemId)
    {
        var result = await VoteContractStub.Vote.SendAsync(new VoteInput { VoteAmount = amount, VoteOption = (int)voteOption, VotingItemId = votingItemId, Memo = "memo"});
        result.TransactionResult.Error.ShouldBe("");
        return result;
    }
    
    internal async Task<long> GetDaoRemainAmount(Hash daoId, Address voter, long amount)
    {
        var result = (await VoteContractStub.GetDaoRemainAmount.CallAsync(
            new GetDaoRemainAmountInput { DaoId = DaoId, Voter = DefaultAddress })).Amount;
        result.ShouldBe(amount);
        return result;
    }
    
    internal async Task<long> GetDaoProposalRemainAmount(Hash daoId, Address voter, Hash votingItemId, long amount)
    {
        var result = (await VoteContractStub.GetProposalRemainAmount.CallAsync(
            new GetProposalRemainAmountInput { DaoId = DaoId, Voter = DefaultAddress, VotingItemId = votingItemId})).Amount;
        amount.ShouldBe(amount);
        return result;
    }

    internal async Task<IExecutionResult<Empty>> Withdraw(Hash daoId, VotingItemIdList list, long withdrawAmount)
    {
        var result = await VoteContractStub.Withdraw.SendAsync(new WithdrawInput { DaoId = daoId, VotingItemIdList = list, WithdrawAmount = withdrawAmount });
        result.TransactionResult.Error.ShouldBe("");
        return result;
    }

    internal async Task<IExecutionResult<Empty>> VoteException(long amount, VoteOption voteOption, Hash votingItemId, string error)
    {
        var result = await VoteContractStub.Vote.SendWithExceptionAsync(new VoteInput { VoteAmount = amount, VoteOption = (int)voteOption, VotingItemId = votingItemId });
        result.TransactionResult.Error.ShouldContain(error);
        return result;
    }

    internal async Task<Hash> CreateVetoProposal(Address schemeAddress, Hash voteSchemeId, Hash vetoProposalId)
    {
        var result = await GovernanceContractStub.CreateVetoProposal.SendAsync(GetCreateVetoProposalInput(schemeAddress, voteSchemeId, vetoProposalId));
        result.TransactionResult.Error.ShouldBe("");
        var governanceProposalLog = GetLogEvent<ProposalCreated>(result.TransactionResult);
        return governanceProposalLog.ProposalId;
    }

    internal static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private static List<T> GetMultiLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var res = new List<T>();
        foreach (var log in transactionResult.Logs.Where(log => log.Name == typeof(T).Name))
        {
            var logEvent = new T();
            logEvent.MergeFrom(log.NonIndexed);
            res.Add(logEvent);
        }

        return res;
    }

    private CreateDAOInput GetCreateDAOInput(string daoName, bool isNetworkDao = false, int governanceMechanism = 0)
    {
        return new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = daoName,
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string> { { "aa", "bb" } }
                }
            },
            GovernanceToken = governanceMechanism == 2 ? "" : "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 1,
                MinimalApproveThreshold = 0,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0
            },
            HighCouncilInput = new HighCouncilInput
            {
                HighCouncilConfig = new HighCouncilConfig
                {
                    MaxHighCouncilMemberCount = 2,
                    MaxHighCouncilCandidateCount = 20,
                    ElectionPeriod = 7,
                    StakingAmount = 100000000
                },
                GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MinimalApproveThreshold = 1,
                    MaximalRejectionThreshold = 2000,
                    MaximalAbstentionThreshold = 2000
                },
                HighCouncilMembers = new DAO.AddressList(){Value = { new []{DefaultAddress, UserAddress} }},
                IsHighCouncilElectionClose = false
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao,
            GovernanceMechanism = governanceMechanism,
            Members = new DAO.AddressList{Value = { DefaultAddress }}
        };
    }

    private CreateProposalInput GetCreateProposalInput(Hash DaoId, ProposalType proposalType, Address schemeAddress, Hash voteSchemeId)
    {
        return new CreateProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = DaoId,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "https://www.ForumUrl.com",
                SchemeAddress = schemeAddress,
                VoteSchemeId = voteSchemeId,
                ActiveTimePeriod = ActiveTimePeriod
            },
            ProposalType = (int)proposalType,
            Transaction = new ExecuteTransaction
            {
                ContractMethodName = "ContractMethodName",
                ToAddress = UserAddress,
                Params = ByteStringHelper.FromHexString(StringExtensions.GetBytes("Params").ToHex())
            }
        };
    }

    private CreateVetoProposalInput GetCreateVetoProposalInput(Address schemeAddress, Hash voteSchemeId, Hash vetoProposalId)
    {
        return new CreateVetoProposalInput
        {
            ProposalBasicInfo = new ProposalBasicInfo
            {
                DaoId = DaoId,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = "https://www.ForumUrl.com",
                SchemeAddress = schemeAddress,
                VoteSchemeId = voteSchemeId,
                ActiveTimePeriod = VetoActiveTimePeriod 
            },
            VetoProposalId = vetoProposalId
        };
    }
}