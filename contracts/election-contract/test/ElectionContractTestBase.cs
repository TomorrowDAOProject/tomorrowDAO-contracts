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

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestBase : TestBase
{
    internal IBlockTimeProvider BlockTimeProvider;
    
    protected readonly Hash DefaultDaoId = HashHelper.ComputeFrom("DaoId");
    protected const string DefaultGovernanceToken = "ELF";
    protected const long StakeAmount = 1_000_00000000;
    protected const long VotingAmount = 1_0_00000000;
    protected const long MinimumLockTime = 3600;
    protected const long MaximumLockTime = 3600 * 24 * 7;

    public ElectionContractTestBase()
    {
        BlockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();
    }
    
    #region Initiate

    protected async Task Initialize(Address daoAddress = null, Address voteAddress = null,
        Address governanceAddress = null)
    {
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress,
            GovernanceContractAddress = governanceAddress ?? GovernanceContractAddress,
            MinimumLockTime = 3600, //s
            MaximumLockTime = 360000 //s
        };
        await ElectionContractStub.Initialize.SendAsync(input);
    }

    protected async Task InitializeAllContract(Address daoAddress = null, Address voteAddress = null)
    {
        //init governance contrct
        var input = new Governance.InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
        };
        await GovernanceContractStub.Initialize.SendAsync(input);

        //init vote contract
        await VoteContractStub.Initialize.SendAsync(new Vote.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            ElectionContractAddress = ElectionContractAddress
        });

        //init dao contract
        await DAOContractStub.Initialize.SendAsync(new DAO.InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TimelockContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress
        });
        await DAOContractStub.SetTreasuryContractAddress.SendAsync(TreasuryContractAddress);

        //init election contract
        await ElectionContractStub.Initialize.SendAsync(new Election.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            MinimumLockTime = MinimumLockTime, //s
            MaximumLockTime = MaximumLockTime //s
        });
    }

    #endregion

    #region DAO

    internal async Task<Hash> InitializeContractAndCreateDao(bool isNetWorkDao = false)
    {
        await InitializeAllContract();
        return await MockDao(isNetWorkDao);
    }

    /// <summary>
    /// Dependent on the InitializeAllContract method.
    /// </summary>
    /// <returns>DaoId</returns>
    internal async Task<Hash> MockDao(bool isNetworkDao = false)
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "DaoName",
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string>()
                    {
                        {
                            "aa", "bb"
                        }
                    }
                }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold()
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 100000000,
                MinimalApproveThreshold = 5000,
                MaximalRejectionThreshold = 2000,
                MaximalAbstentionThreshold = 2000
            },
            HighCouncilInput = new HighCouncilInput
            {
                HighCouncilConfig = new DAO.HighCouncilConfig
                {
                    MaxHighCouncilMemberCount = 3,
                    MaxHighCouncilCandidateCount = 5,
                    ElectionPeriod = 7,
                    StakingAmount = StakeAmount
                },
                GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MinimalApproveThreshold = 1,
                    MaximalRejectionThreshold = 2000,
                    MaximalAbstentionThreshold = 2000
                }
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao
        });

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        return log.DaoId;
    }

    #endregion

    #region Election

    internal async Task<IExecutionResult<Empty>> AnnounceElection(Hash daoId, Address candidateAddress = null,
        bool withException = false)
    {
        var approveResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = ElectionContractAddress,
            Symbol = DefaultGovernanceToken,
            Amount = StakeAmount
        });
        var approved = GetLogEvent<Approved>(approveResult.TransactionResult);
        approved.ShouldNotBeNull();

        var senderAddress = ((MethodStubFactory)ElectionContractStub.__factory).Sender;

        if (candidateAddress == null)
        {
            var input = new AnnounceElectionInput
            {
                DaoId = daoId,
                CandidateAdmin = senderAddress
            };
            return withException
                ? await ElectionContractStub.AnnounceElection.SendWithExceptionAsync(input)
                : await ElectionContractStub.AnnounceElection.SendAsync(input);
        }
        else
        {
            var input = new AnnounceElectionForInput
            {
                DaoId = daoId,
                Candidate = candidateAddress,
                CandidateAdmin = senderAddress
            };
            return withException
                ? await ElectionContractStub.AnnounceElectionFor.SendWithExceptionAsync(input)
                : await ElectionContractStub.AnnounceElectionFor.SendAsync(input);
        }
    }

    internal async Task<IExecutionResult<Empty>> QuitElection(Hash daoId, Address candidateAddress = null,
        bool withException = false)
    {
        var senderAddress = ((MethodStubFactory)ElectionContractStub.__factory).Sender;

        var input = new QuitElectionInput
        {
            DaoId = daoId,
            Candidate = candidateAddress ?? senderAddress
        };
        return withException
            ? await ElectionContractStub.QuitElection.SendWithExceptionAsync(input)
            : await ElectionContractStub.QuitElection.SendAsync(input);
    }

    #endregion

    #region Vote

    internal async Task<IExecutionResult<Hash>> Vote(Hash daoId, Address candidateAddress, bool withException = false)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = ElectionContractAddress,
            Symbol = DefaultGovernanceToken,
            Amount = VotingAmount
        });
        
        var result = await ElectionContractStub.Vote.SendAsync(new VoteHighCouncilInput
        {
            DaoId = daoId,
            CandidateAddress = candidateAddress,
            Amount = VotingAmount,
            EndTimestamp = DateTime.UtcNow.AddSeconds(MinimumLockTime).AddHours(1).ToTimestamp(),
            Token = null
        });
        result.ShouldNotBeNull();

        var transferred = GetLogEvent<Transferred>(result.TransactionResult);
        transferred.ShouldNotBeNull();
        transferred.Amount.ShouldBe(VotingAmount);

        return result;
    }

    #endregion

    protected static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }
}