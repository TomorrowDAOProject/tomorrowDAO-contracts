using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Fact]
    public async Task EnableHighCouncilTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync(false);

        {
            var result = await CreateProposalAndVote(daoId, new ExecuteTransaction
            {
                ContractMethodName = nameof(DAOContractStub.EnableHighCouncil),
                ToAddress = DAOContractAddress,
                Params = new EnableHighCouncilInput
                {
                    DaoId = daoId,
                    HighCouncilInput = new HighCouncilInput
                    {
                        HighCouncilConfig = new HighCouncilConfig
                        {
                            MaxHighCouncilMemberCount = 21,
                            MaxHighCouncilCandidateCount = 105,
                            ElectionPeriod = 7,
                            StakingAmount = 100000000
                        },
                        GovernanceSchemeThreshold = new GovernanceSchemeThreshold
                        {
                            MinimalRequiredThreshold = 75,
                            MinimalVoteThreshold = 10,
                            MinimalApproveThreshold = 10,
                            MaximalRejectionThreshold = 10,
                            MaximalAbstentionThreshold = 10
                        }
                    },
                }.ToByteString()
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var log = GetLogEvent<HighCouncilEnabled>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.HighCouncilAddress.ShouldNotBeNull();

            {
                var output = await DAOContractStub.GetHighCouncilStatus.CallAsync(daoId);
                output.Value.ShouldBeTrue();
            }

            {
                var output = await DAOContractStub.GetHighCouncilAddress.CallAsync(daoId);
                output.Value.ShouldNotBeNull();
            }
        }
    }

    [Fact]
    public async Task EnableHighCouncilTests_NoPermission()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync(false);

        {
            var result = await DAOContractStub.EnableHighCouncil.SendWithExceptionAsync(new EnableHighCouncilInput
            {
                DaoId = daoId,
                HighCouncilInput = new HighCouncilInput
                {
                    HighCouncilConfig = new HighCouncilConfig
                    {
                        MaxHighCouncilMemberCount = 21,
                        MaxHighCouncilCandidateCount = 105,
                        ElectionPeriod = 7,
                        StakingAmount = 100000000
                    },
                    GovernanceSchemeThreshold = new GovernanceSchemeThreshold
                    {
                        MinimalRequiredThreshold = 75,
                        MinimalVoteThreshold = 10,
                        MinimalApproveThreshold = 10,
                        MaximalRejectionThreshold = 10,
                        MaximalAbstentionThreshold = 10
                    }
                },
            });
            result.TransactionResult.Error.ShouldContain("Permission of EnableHighCouncil is not granted for");
        }
    }

    [Fact]
    public async Task EnableHighCouncilTests_Already()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();
        {
            var result = await DAOContractStub.EnableHighCouncil.SendWithExceptionAsync(new EnableHighCouncilInput
            {
                DaoId = daoId,
                HighCouncilInput = new HighCouncilInput
                {
                    HighCouncilConfig = new HighCouncilConfig
                    {
                        MaxHighCouncilMemberCount = 21,
                        MaxHighCouncilCandidateCount = 105,
                        ElectionPeriod = 7,
                        StakingAmount = 100000000
                    },
                    GovernanceSchemeThreshold = new GovernanceSchemeThreshold
                    {
                        MinimalRequiredThreshold = 75,
                        MinimalVoteThreshold = 10,
                        MinimalApproveThreshold = 10,
                        MaximalRejectionThreshold = 10,
                        MaximalAbstentionThreshold = 10
                    }
                },
            });
            result.TransactionResult.Error.ShouldContain("High council already enabled.");
        }
    }

    [Fact]
    public async Task DisableHighCouncilTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var result = await CreateProposalAndVote(daoId, new ExecuteTransaction
        {
            ContractMethodName = nameof(DAOContractStub.DisableHighCouncil),
            ToAddress = DAOContractAddress,
            Params = daoId.ToByteString()
        });

        {
            var output = await DAOContractStub.GetHighCouncilStatus.CallAsync(daoId);
            output.Value.ShouldBeFalse();
        }
        {
            var output = await DAOContractStub.GetHighCouncilAddress.CallAsync(daoId);
            output.ShouldNotBeNull();
        }
    }
    
    [Fact]
    public async Task DisableHighCouncilTests_NoPermission()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var result = await DAOContractStub.DisableHighCouncil.SendWithExceptionAsync(daoId);
        result.TransactionResult.Error.ShouldContain("Permission of DisableHighCouncil is not granted for");
    }

    [Fact]
    public async Task AddHighCouncilMembersTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync(enableHighCouncil: true);
        
        var result = await CreateProposalAndVote(daoId, new ExecuteTransaction
        {
            ContractMethodName = nameof(DAOContractStub.AddHighCouncilMembers),
            ToAddress = DAOContractAddress,
            Params = new AddHighCouncilMembersInput
            {
                DaoId = daoId,
                AddHighCouncils = new AddressList()
                {
                    Value = { UserAddress }
                }
            }.ToByteString()
        });
        var log = GetLogEvent<HighCouncilAdded>(result.TransactionResult);
        log.DaoId.ShouldBe(daoId);
        log.AddHighCouncils.Value.ShouldContain(UserAddress);
    }
    
    [Fact]
    public async Task RemoveHighCouncilMembersTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync(enableHighCouncil: true);
        
        var result = await CreateProposalAndVote(daoId, new ExecuteTransaction
        {
            ContractMethodName = nameof(DAOContractStub.AddHighCouncilMembers),
            ToAddress = DAOContractAddress,
            Params = new AddHighCouncilMembersInput
            {
                DaoId = daoId,
                AddHighCouncils = new AddressList()
                {
                    Value = { UserAddress, DefaultAddress }
                }
            }.ToByteString()
        });
        var log = GetLogEvent<HighCouncilAdded>(result.TransactionResult);
        log.DaoId.ShouldBe(daoId);
        log.AddHighCouncils.Value.ShouldContain(UserAddress);
        log.AddHighCouncils.Value.ShouldContain(DefaultAddress);
        
        result = await CreateProposalAndVote(daoId, new ExecuteTransaction
        {
            ContractMethodName = nameof(DAOContractStub.RemoveHighCouncilMembers),
            ToAddress = DAOContractAddress,
            Params = new RemoveHighCouncilInput()
            {
                DaoId = daoId,
                RemoveHighCouncils = new Election.AddressList()
                {
                    Value = { UserAddress }
                }
            }.ToByteString()
        });
        var removeLog = GetLogEvent<HighCouncilRemoved>(result.TransactionResult);
        removeLog.DaoId.ShouldBe(daoId);
        removeLog.RemoveHighCouncils.Value.ShouldContain(UserAddress);
        removeLog.RemoveHighCouncils.Value.ShouldNotContain(DefaultAddress);

        var addressList = await ElectionContractStub.GetInitialHighCouncilMembers.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.ShouldContain(DefaultAddress);
        addressList.Value.ShouldNotContain(UserAddress);
    }
}