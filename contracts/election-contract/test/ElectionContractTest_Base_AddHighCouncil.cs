using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAO.Contracts.DAO;
using Xunit;

namespace TomorrowDAO.Contracts.Election;

public class ElectionContractTestBaseAddHighCouncil : ElectionContractTestBase
{

    [Fact]
    public async Task AddHighCouncilTest()
    {
        await InitializeAllContract();
        
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
                },
                HighCouncilMembers = new DAO.AddressList()
                {
                    Value = { UserAddress, DefaultAddress }
                },
                IsHighCouncilElectionClose = false
            },
            IsTreasuryNeeded = false,
            IsNetworkDao = false
        });

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        var daoId =  log.DaoId;

        var addressList = await ElectionContractStub.GetInitialHighCouncilMembers.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);

        addressList = await ElectionContractStub.GetHighCouncilMembers.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(0);

        await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput
        {
            DaoId = daoId,
            TermNumber = 1
        });
        
        addressList = await ElectionContractStub.GetHighCouncilMembers.CallAsync(daoId);
        addressList.ShouldNotBeNull();
        addressList.Value.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task AddHighCouncilTest_Subsistence()
    {
        await InitializeAllContract();

        var result =
            await ElectionContractStub.AddHighCouncil.SendWithExceptionAsync(new AddHighCouncilInput
            {
                DaoId = DefaultDaoId,
                AddHighCouncils = new AddressList
                {
                    Value = { UserAddress, DefaultAddress }
                }
            });
        
        result.TransactionResult.Error.ShouldContain("DAO is not in subsistence");
    }
    
    [Fact]
    public async Task AddHighCouncilTest_NoPermission()
    {
        await InitializeAllContract();
        
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
            HighCouncilInput = null,
            IsTreasuryNeeded = false,
            IsNetworkDao = false
        });

        var log = GetLogEvent<DAOCreated>(result.TransactionResult);
        var daoId = log.DaoId;

        result =
            await ElectionContractStub.AddHighCouncil.SendWithExceptionAsync(new AddHighCouncilInput
            {
                DaoId = daoId,
                AddHighCouncils = new AddressList
                {
                    Value = { UserAddress, DefaultAddress }
                }
            });
        
        result.TransactionResult.Error.ShouldContain("No permission");
    }
}