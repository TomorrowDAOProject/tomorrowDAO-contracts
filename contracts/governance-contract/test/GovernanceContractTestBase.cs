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
using TomorrowDAO.Contracts.Vote;
using HighCouncilConfig = TomorrowDAO.Contracts.DAO.HighCouncilConfig;

namespace TomorrowDAO.Contracts.Governance;

public class GovernanceContractTestBase : TestBase
{
    internal IBlockTimeProvider BlockTimeProvider;
    internal readonly string DefaultGovernanceToken = "ELF";
    internal readonly long OneElfAmount = 100000000;
    internal Hash DefaultDaoId = HashHelper.ComputeFrom("DaoId");
    internal readonly GovernanceSchemeThreshold DefaultSchemeThreshold = new GovernanceSchemeThreshold
    {
        MinimalRequiredThreshold = 1,
        MinimalVoteThreshold = 1,
        MinimalApproveThreshold = 1,
        MaximalRejectionThreshold = 2,
        MaximalAbstentionThreshold = 2
    };

    public GovernanceContractTestBase()
    {
        BlockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();
    }

    #region Initiate

    protected async Task Initialize(Address daoAddress = null, Address voteAddress = null)
    {
        //init governance contrct
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress
        };
        await GovernanceContractStub.Initialize.SendAsync(input);
    }

    protected async Task InitializeAllContract(Address daoAddress = null, Address voteAddress = null)
    {
        //init governance contrct
        var input = new InitializeInput
        {
            DaoContractAddress = daoAddress ?? DAOContractAddress,
            VoteContractAddress = voteAddress ?? VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress
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
            MinimumLockTime = 3600, //s
            MaximumLockTime = 360000 //s
        });
    }

    #endregion

    #region DAO

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
                }
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao
        });

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        return log.DaoId;
    }

    #endregion

    #region Proposal

    /// <summary>
    /// Dependent on the InitializeAllContract/MockDao/MockVoteScheme method.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="withException"></param>
    /// <param name="voteMechanism"></param>
    /// <returns></returns>
    internal async Task<IExecutionResult<Hash>> CreateProposalAsync(CreateProposalInput input, bool withException,
        VoteMechanism voteMechanism = VoteMechanism.UniqueVote)
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        var schemeAddress = addressList.Value.LastOrDefault();
        await MockVoteScheme();
        var voteMechanismId = await GetVoteSchemeId(voteMechanism);

        input.ProposalBasicInfo.DaoId = daoId;
        input.ProposalBasicInfo.SchemeAddress = schemeAddress;
        input.ProposalBasicInfo.VoteSchemeId = voteMechanismId;

        return withException
            ? await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input)
            : await GovernanceContractStub.CreateProposal.SendAsync(input);
    }

    #endregion

    #region HC

    internal async Task<IExecutionResult<Empty>> HighCouncilElection(Hash DaoId)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = ElectionContractAddress,
            Symbol = "ELF",
            Amount = 1000000000
        });

        await ElectionContractStub.AnnounceElection.SendAsync(new AnnounceElectionInput
        {
            DaoId = DaoId,
            CandidateAdmin = DefaultAddress
        });
        await ElectionContractStub.Vote.SendAsync(new VoteHighCouncilInput
        {
            DaoId = DaoId,
            CandidateAddress = DefaultAddress,
            Amount = 100000000,
            EndTimestamp = DateTime.UtcNow.AddDays(4).ToTimestamp(),
            Token = null
        });
        return await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput
        {
            DaoId = DaoId,
            TermNumber = 1
        });
    }

    internal async Task<IExecutionResult<Empty>> HighCouncilElectionFor(Hash DaoId, Address candidateAddress)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = ElectionContractAddress,
            Symbol = "ELF",
            Amount = 1000000000
        });

        await ElectionContractStub.AnnounceElectionFor.SendAsync(new AnnounceElectionForInput
        {
            DaoId = DaoId,
            Candidate = candidateAddress,
            CandidateAdmin = DefaultAddress
        });
        await ElectionContractStub.Vote.SendAsync(new VoteHighCouncilInput
        {
            DaoId = DaoId,
            CandidateAddress = candidateAddress,
            Amount = 100000000,
            EndTimestamp = DateTime.UtcNow.AddDays(4).ToTimestamp(),
            Token = null
        });
        return await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput
        {
            DaoId = DaoId,
            TermNumber = 2
        });
    }

    #endregion

    #region Vote

    protected async Task<Hash> MockVoteScheme()
    {
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.UniqueVote
        });
        await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.TokenBallot
        });

        return await GetVoteSchemeId(VoteMechanism.UniqueVote);
    }

    /// <summary>
    /// Dependent on the MockVoteScheme method.
    /// </summary>
    /// <param name="voteMechanism"></param>
    /// <returns></returns>
    private async Task<Hash> GetVoteSchemeId(VoteMechanism voteMechanism)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(VoteContractAddress),
            HashHelper.ComputeFrom(voteMechanism.ToString()));
    }
    
    internal async Task<IExecutionResult<Empty>> VoteProposalAsync(Hash proposalId, long amount, VoteOption voteOption)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = VoteContractAddress,
            Symbol = "ELF",
            Amount = 10000000000
        });

        return await VoteContractStub.Vote.SendAsync(new VoteInput
        {
            VotingItemId = proposalId,
            VoteOption = (int)VoteOption.Approved,
            VoteAmount = amount
        });
    }
    
    internal async Task<IExecutionResult<Hash>> CreateVetoProposalAsync(CreateVetoProposalInput input,
        bool withException)
    {
        var proposalInfoOutput = await GovernanceContractStub.GetProposalInfo.CallAsync(input.VetoProposalId);
        input.ProposalBasicInfo.DaoId = proposalInfoOutput.DaoId;

        var addressList =
            await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(proposalInfoOutput.DaoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        var referendumSchemeAddress = addressList.Value.LastOrDefault();

        input.ProposalBasicInfo.DaoId = proposalInfoOutput.DaoId;
        input.ProposalBasicInfo.SchemeAddress = referendumSchemeAddress;
        input.ProposalBasicInfo.VoteSchemeId = proposalInfoOutput.VoteSchemeId;

        return withException
            ? await GovernanceContractStub.CreateVetoProposal.SendWithExceptionAsync(input)
            : await GovernanceContractStub.CreateVetoProposal.SendAsync(input);
    }

    #endregion

    internal async Task<Address> AddGovernanceScheme(Hash daoId = default,
        GovernanceMechanism mechanism = GovernanceMechanism.Referendum, GovernanceSchemeThreshold threshold = null,
        string governanceToken = null)
    {
        await Initialize(DefaultAddress);
        
        var input = new AddGovernanceSchemeInput
        {
            DaoId = daoId ?? DefaultDaoId,
            GovernanceMechanism = mechanism,
            SchemeThreshold = threshold ?? DefaultSchemeThreshold,
            GovernanceToken = governanceToken ?? DefaultGovernanceToken
        };

        var executionResult = await GovernanceContractStub.AddGovernanceScheme.SendAsync(input);
        return executionResult.Output;
    }

    internal CreateProposalInput MockCreateProposalInput()
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "ProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = null
        };
        var executeTransaction = new ExecuteTransaction
        {
            ContractMethodName = "ContractMethodName",
            ToAddress = UserAddress,
            Params = ByteStringHelper.FromHexString(StringExtensions.GetBytes("Params").ToHex())
        };

        var input = new CreateProposalInput
        {
            ProposalBasicInfo = proposalBasicInfo,
            ProposalType = (int)ProposalType.Governance,
            Transaction = executeTransaction
        };
        return input;
    }
    
    internal CreateVetoProposalInput MockCreateVetoProposalInput()
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "VetoProposalTitle",
            ProposalDescription = "VetoProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = null
        };
        var proposalInput = new CreateVetoProposalInput
        {
            ProposalBasicInfo = proposalBasicInfo,
            VetoProposalId = null
        };
        return proposalInput;
    }

    private static T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }
}