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
    protected Hash UniqueVoteVoteSchemeId; //1a1v
    protected Hash TokenBallotVoteSchemeId; //1t1v
    protected Hash TokenBallotVoteSchemeId_NoLock_DayVote; 

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
    internal async Task<Hash> MockDao(bool isNetworkDao = false, int governanceMechanism = 0,
        CreateDAOInput input = null)
    {
        var result = await DAOContractStub.CreateDAO.SendAsync(input ?? new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "DaoName",
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string>
                    {
                        {
                            "aa", "bb"
                        }
                    }
                }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
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
                },
                HighCouncilMembers = new DAO.AddressList(){Value = { new []{DefaultAddress, UserAddress} }},
                IsHighCouncilElectionClose = false
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao,
            GovernanceMechanism = governanceMechanism
        });

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        return log.DaoId;
    }

    internal CreateDAOInput BuildCreateDaoInput(bool isNetworkDao = false,
        GovernanceMechanism governanceMechanism = 0)
    {
        DAO.GovernanceSchemeThreshold governanceSchemeThreshold = null;
        HighCouncilInput highCouncilInput = null;
        DAO.AddressList members = null;
        if (governanceMechanism == GovernanceMechanism.Organization)
        {
            governanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 0,
                MinimalVoteThreshold = 0,
                MinimalApproveThreshold = 5000,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0
            };
            members = new DAO.AddressList() { Value = { DefaultAddress, UserAddress } };
        }
        else
        {
            governanceSchemeThreshold = new DAO.GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 100000000,
                MinimalApproveThreshold = 5000,
                MaximalRejectionThreshold = 2000,
                MaximalAbstentionThreshold = 2000
            };
            highCouncilInput = new HighCouncilInput
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
                HighCouncilMembers = new DAO.AddressList() { Value = { DefaultAddress, UserAddress } },
                IsHighCouncilElectionClose = false
            };
        }

        return new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "DaoName",
                LogoUrl = "www.logo.com",
                Description = "Dao Description",
                SocialMedia =
                {
                    new Dictionary<string, string>
                    {
                        {
                            "aa", "bb"
                        }
                    }
                }
            },
            GovernanceToken = governanceMechanism == GovernanceMechanism.Organization ? string.Empty : "ELF",
            GovernanceSchemeThreshold = governanceSchemeThreshold,
            HighCouncilInput = highCouncilInput,
            IsTreasuryNeeded = false,
            IsNetworkDao = isNetworkDao,
            ProposalThreshold = 0,
            GovernanceMechanism = (int)governanceMechanism,
            Members = members
        };
    }

    #endregion

    #region Proposal

    /// <summary>
    /// Dependent on the InitializeAllContract/MockDao/MockVoteScheme method.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="withException"></param>
    /// <param name="voteMechanism"></param>
    /// <param name="governanceMechanism"></param>
    /// <param name="voteSchemeId"></param>
    /// <returns></returns>
    [Obsolete]
    internal async Task<IExecutionResult<Hash>> CreateProposalAsync(CreateProposalInput input, bool withException,
        VoteMechanism voteMechanism = VoteMechanism.UniqueVote, int governanceMechanism = 0, Hash voteSchemeId = null)
    {
        await InitializeAllContract();
        var daoId = await MockDao(false, governanceMechanism);
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
        var schemeAddress = addressList.Value.LastOrDefault();
        await MockVoteScheme();
        var voteMechanismId = voteSchemeId != null ? voteSchemeId : await GetVoteSchemeId(voteMechanism);

        input.ProposalBasicInfo.DaoId = daoId;
        input.ProposalBasicInfo.SchemeAddress = schemeAddress;
        input.ProposalBasicInfo.VoteSchemeId = voteMechanismId;

        return withException
            ? await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input)
            : await GovernanceContractStub.CreateProposal.SendAsync(input);
    }

    internal async Task<IExecutionResult<Hash>> CreateProposalAsync(CreateProposalInput input, bool withException,
        GovernanceMechanism governanceMechanism = GovernanceMechanism.Referendum,
        VoteMechanism voteMechanism = VoteMechanism.UniqueVote,
        Func<Task<Hash>> buildDaoFunc = null,
        Func<Task> buildVoteMechanismFunc = null)
    {
        await InitializeAllContract();

        buildDaoFunc ??= async () =>
        {
            var daoId = await MockDao();
            return daoId;
        };
        var daoId = await buildDaoFunc();

        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();

        var schemeAddress = governanceMechanism switch
        {
            GovernanceMechanism.Referendum => await DAOContractStub.GetReferendumAddress.CallAsync(daoId),
            GovernanceMechanism.Organization => await DAOContractStub.GetOrganizationAddress.CallAsync(daoId),
            GovernanceMechanism.HighCouncil => await DAOContractStub.GetHighCouncilAddress.CallAsync(daoId),
            _ => null
        };
        schemeAddress.ShouldNotBeNull();

        buildVoteMechanismFunc ??= async () => { await MockVoteScheme(); };
        await buildVoteMechanismFunc();
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

    internal async Task MockVoteScheme()
    {
        var result1 = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.UniqueVote
        });
        var result2 = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.TokenBallot
        });
        var result3 = await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
        {
            VoteMechanism = VoteMechanism.TokenBallot, WithoutLockToken = true, VoteStrategy = VoteStrategy.DayDistinct
        });
        
        UniqueVoteVoteSchemeId = GetLogEvent<VoteSchemeCreated>(result1.TransactionResult).VoteSchemeId;
        TokenBallotVoteSchemeId = GetLogEvent<VoteSchemeCreated>(result2.TransactionResult).VoteSchemeId;
        TokenBallotVoteSchemeId_NoLock_DayVote = GetLogEvent<VoteSchemeCreated>(result3.TransactionResult).VoteSchemeId;
    }

    /// <summary>
    /// Dependent on the MockVoteScheme method.
    /// </summary>
    /// <param name="voteMechanism"></param>
    /// <returns></returns>
    private async Task<Hash> GetVoteSchemeId(VoteMechanism voteMechanism)
    {
        return VoteMechanism.UniqueVote == voteMechanism ? UniqueVoteVoteSchemeId : TokenBallotVoteSchemeId;
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

    internal CreateProposalInput MockCreateProposalInput(long activeTimePeriod = 7 * 24 * 60 * 60, Hash voteSchemeId = null,
        long activeStartTime = 0, long activeEndTime = 0)
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "ProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = voteSchemeId,
            ActiveTimePeriod = activeTimePeriod,
            ActiveStartTime = activeStartTime,
            ActiveEndTime = activeEndTime
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

    internal CreateVetoProposalInput MockCreateVetoProposalInput(long activeTimePeriod = 3 * 24 * 60 * 60)
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "VetoProposalTitle",
            ProposalDescription = "VetoProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = null,
            ActiveTimePeriod = activeTimePeriod
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