using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    public const string DefaultGovernanceToken = "ELF";
    public const long OneElfAmount = 100000000;

    private T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private async Task InitializeAsync()
    {
        var result = await DAOContractStub.Initialize.SendAsync(new InitializeInput
        {
            GovernanceContractAddress = GovernanceContractAddress,
            ElectionContractAddress = ElectionContractAddress,
            TreasuryContractAddress = DefaultAddress,
            VoteContractAddress = VoteContractAddress,
            TimelockContractAddress = DefaultAddress
        });
        
        await DAOContractStub.SetTreasuryContractAddress.SendAsync(TreasuryContractAddress);

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        await GovernanceContractStub.Initialize.SendAsync(new Governance.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            ElectionContractAddress = ElectionContractAddress,
        });

        await ElectionContractStub.Initialize.SendAsync(new Election.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            VoteContractAddress = VoteContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            MinimumLockTime = 10,
            MaximumLockTime = 100000000
        });

        await VoteContractStub.Initialize.SendAsync(new Vote.InitializeInput
        {
            DaoContractAddress = DAOContractAddress,
            GovernanceContractAddress = GovernanceContractAddress,
            ElectionContractAddress = ElectionContractAddress
        });
    }

    private async Task<Hash> CreateDAOAsync(bool enableHighCouncil = true, int governanceMechanism = 0)
    {
        var input = new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "TestDAO",
                LogoUrl = "logo_url",
                Description = "Description",
                SocialMedia =
                {
                    new Dictionary<string, string>
                    {
                        { "X", "twitter" },
                        { "Facebook", "facebook" },
                        { "Telegram", "telegram" },
                        { "Discord", "discord" },
                        { "Reddit", "reddit" }
                    }
                }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 1,
                MinimalApproveThreshold = 1,
                MaximalRejectionThreshold = 2,
                MaximalAbstentionThreshold = 2
            },
            GovernanceMechanism = governanceMechanism
        };
        if (enableHighCouncil)
        {
            input.HighCouncilInput = new HighCouncilInput
            {
                GovernanceSchemeThreshold = new GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MinimalApproveThreshold = 1,
                    MaximalRejectionThreshold = 2,
                    MaximalAbstentionThreshold = 2
                },
                HighCouncilConfig = new HighCouncilConfig
                {
                    MaxHighCouncilMemberCount = 21,
                    MaxHighCouncilCandidateCount = 105,
                    ElectionPeriod = 7,
                    StakingAmount = 100000000
                }
            };
        }

        var result = await DAOContractStub.CreateDAO.SendAsync(input);

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);

        return log.DaoId;
    }

    private async Task SetSubsistStatusAsync(Hash daoId, bool status)
    {
        await DAOContractStub.SetSubsistStatus.SendAsync(new SetSubsistStatusInput
        {
            DaoId = daoId,
            Status = status
        });
    }

    private string GenerateRandomString(int length)
    {
        if (length <= 0) return "";

        const string chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-*/!@#$%^&*()_+{}|:<>?[];',./`~";

        var random = new Random();

        var stringBuilder = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }

    private Dictionary<string, string> GenerateRandomMap(int length, int keyLength, int valueLength)
    {
        var map = new Dictionary<string, string>();
        for (var i = 0; i < length; i++)
        {
            var key = GenerateRandomString(keyLength);
            if (map.ContainsKey(key))
            {
                i--;
                continue;
            }

            var value = GenerateRandomString(valueLength);

            map.Add(key, value);
        }

        return map;
    }

    private File GenerateFile(string cid, string name, string url)
    {
        return new File
        {
            Cid = cid,
            Name = name,
            Url = url
        };
    }

    private async Task<IExecutionResult<Empty>> CreateProposalAndVote(Hash daoId, ExecuteTransaction executeTransaction)
    {
        var executionResult = await CreateProposalAsync(daoId, false, executeTransaction);
        var proposalId = executionResult.Output;
        await ApproveElf(OneElf * 100, VoteContractAddress);
        //Vote 10s
        BlockTimeProvider.SetBlockTime(10000);
        await VoteProposalAsync(proposalId, OneElf, VoteOption.Approved);

        //add 7d
        BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000);
        return await GovernanceContractStub.ExecuteProposal.SendAsync(proposalId);
    }

    private async Task<IExecutionResult<Hash>> CreateProposalAsync(Hash daoId, bool withException,
        ExecuteTransaction executeTransaction)
    {
        var addressList = await GovernanceContractStub.GetDaoGovernanceSchemeAddressList.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        //addressList.Value.Count.ShouldBe(2);
        var schemeAddress = addressList.Value.FirstOrDefault();
        await MockVoteScheme();
        var voteMechanismId = await GetVoteSchemeId(VoteMechanism.TokenBallot);

        var input = MockCreateProposalInput(executeTransaction);

        input.ProposalBasicInfo.DaoId = daoId;
        input.ProposalBasicInfo.SchemeAddress = schemeAddress;
        input.ProposalBasicInfo.VoteSchemeId = voteMechanismId;

        return withException
            ? await GovernanceContractStub.CreateProposal.SendWithExceptionAsync(input)
            : await GovernanceContractStub.CreateProposal.SendAsync(input);
    }

    private async Task MockVoteScheme()
    {
        var voteSchemeId = await GetVoteSchemeId(VoteMechanism.UniqueVote);
        var voteScheme = await VoteContractStub.GetVoteScheme.CallAsync(voteSchemeId);
        if (voteScheme == null || voteScheme.SchemeId == null)
        {
            await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.UniqueVote
            });
        }

        voteSchemeId = await GetVoteSchemeId(VoteMechanism.TokenBallot);
        voteScheme = await VoteContractStub.GetVoteScheme.CallAsync(voteSchemeId);
        if (voteScheme == null || voteScheme.SchemeId == null)
        {
            await VoteContractStub.CreateVoteScheme.SendAsync(new CreateVoteSchemeInput
            {
                VoteMechanism = VoteMechanism.TokenBallot
            });
        }
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

    internal CreateProposalInput MockCreateProposalInput(ExecuteTransaction executeTransaction)
    {
        var proposalBasicInfo = new ProposalBasicInfo
        {
            DaoId = null,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "ProposalDescription",
            ForumUrl = "https://www.ForumUrl.com",
            SchemeAddress = null,
            VoteSchemeId = null,
            ActiveTimePeriod = ActiveTimePeriod
        };

        var input = new CreateProposalInput
        {
            ProposalBasicInfo = proposalBasicInfo,
            ProposalType = (int)ProposalType.Governance,
            Transaction = executeTransaction
        };
        return input;
    }

    private async Task<IExecutionResult<Empty>> VoteProposalAsync(Hash proposalId, long amount, VoteOption voteOption)
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
}