using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender, "No permission.");
        AssertNotNullOrEmpty(input.DaoContractAddress, "DaoContractAddress");
        AssertNotNullOrEmpty(input.GovernanceContractAddress, "GovernanceContractAddress");
        State.DaoContract.Value = input.DaoContractAddress;
        State.GovernanceContract.Value = input.GovernanceContractAddress;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.Initialized.Value = true;

        State.MinimumLockTime.Value = input.MinimumLockTime;
        State.MaximumLockTime.Value = input.MaximumLockTime;

        return new Empty();
    }

    public override Empty SetHighCouncilConfig(SetHighCouncilConfigInput input)
    {
        AssertValidAndSetHighCouncilConfig(input);

        Context.Fire(new HighCouncilConfigSet
        {
            DaoId = input.DaoId,
            HighCouncilConfig = State.HighCouncilConfig[input.DaoId]
        });

        return new Empty();
    }

    public override Empty RegisterElectionVotingEvent(RegisterElectionVotingEventInput input)
    {
        AssertInitialized();
        AssertSenderDaoContract();
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        Assert(State.HighCouncilElectionVotingItemId[input.DaoId] == null, "Already registered.");

        InitHighCouncilConfig(input);
        var highCouncilConfig = State.HighCouncilConfig[input.DaoId];
        Assert(highCouncilConfig != null, "HighCouncilConfig not initialize");

        var votingItemId = RegisterVoteItem(input.DaoId, highCouncilConfig);
        var votingItem = State.VotingItems[votingItemId];

        State.HighCouncilElectionVotingItemId[input.DaoId] = votingItemId;
        State.VotingEventEnabledStatus[input.DaoId] = true;
        State.CurrentTermNumber[input.DaoId] = 1;
        State.ElectedCandidates[input.DaoId] = new AddressList();

        Context.Fire(new ElectionVotingEventRegistered
        {
            DaoId = input.DaoId,
            Config = highCouncilConfig,
            VotingItem = votingItem
        });
        return new Empty();
    }

    public override Empty AddHighCouncil(AddHighCouncilInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.AddHighCouncils);
        AssertNotNullOrEmpty(input.DaoId);
        Assert(input.AddHighCouncils.Value is { Count: > 0 }, "Invalid input.");
        AssertSenderIsDaoContractOrProposal(input.DaoId);

        var validAddressList = new AddressList();
        var addressList = State.InitialHighCouncilMembers[input.DaoId] ?? new AddressList();
        foreach (var address in input.AddHighCouncils.Value!)
        {
            if (!addressList.Value.Contains(address))
            {
                addressList.Value.Add(address);
                validAddressList.Value.Add(address);
            }
        }

        Assert(addressList.Value.Count <= ElectionContractConstants.MaxInitialHighCouncilMemberCount,
            $"The number of High Council members cannot exceed {ElectionContractConstants.MaxInitialHighCouncilMemberCount}.");

        State.InitialHighCouncilMembers[input.DaoId] = addressList;

        Context.Fire(new HighCouncilAdded
        {
            DaoId = input.DaoId,
            AddHighCouncils = validAddressList
        });
        return new Empty();
    }

    public override Empty RemoveHighCouncil(RemoveHighCouncilInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.RemoveHighCouncils);
        AssertNotNullOrEmpty(input.DaoId);
        Assert(input.RemoveHighCouncils.Value is { Count: > 0 }, "Invalid input.");
        AssertSenderIsDaoContractOrProposal(input.DaoId);

        var validAddressList = new AddressList();
        var addressList = State.InitialHighCouncilMembers[input.DaoId] ?? new AddressList();
        foreach (var address in input.RemoveHighCouncils.Value)
        {
            if (addressList.Value.Contains(address))
            {
                addressList.Value.Remove(address);
                validAddressList.Value.Add(address);
            }
        }

        if (addressList.Value.Count == 0)
        {
            State.InitialHighCouncilMembers.Remove(input.DaoId);
        }
        else
        {
            State.InitialHighCouncilMembers[input.DaoId] = addressList;
        }

        Context.Fire(new HighCouncilRemoved
        {
            DaoId = input.DaoId,
            RemoveHighCouncils = validAddressList
        });
        return new Empty();
    }

    private Hash RegisterVoteItem(Hash daoId, HighCouncilConfig config)
    {
        var votingItemId = AssertValidNewVotingItem(daoId);

        // Initialize voting event.
        var votingItem = new VotingItem
        {
            Sponsor = Context.Sender,
            VotingItemId = votingItemId,
            AcceptedCurrency = config.GovernanceToken,
            IsLockToken = true,
            TotalSnapshotNumber = long.MaxValue,
            CurrentSnapshotNumber = 1,
            CurrentSnapshotStartTimestamp = TimestampHelper.MinValue,
            StartTimestamp = TimestampHelper.MinValue,
            EndTimestamp = TimestampHelper.MaxValue,
            RegisterTimestamp = Context.CurrentBlockTime,
            IsQuadratic = false,
            TicketCost = 0
        };

        State.VotingItems[votingItemId] = votingItem;

        // Initialize first voting going information of registered voting event.
        var votingResultHash = GetVotingResultHash(votingItemId, 1);
        State.VotingResults[votingResultHash] = new VotingResult
        {
            VotingItemId = votingItemId,
            SnapshotNumber = 1,
            SnapshotStartTimestamp = TimestampHelper.MinValue
        };
        State.CurrentTermNumber[daoId] = 1;
        return votingItemId;
    }

    private void InitHighCouncilConfig(RegisterElectionVotingEventInput input)
    {
        AssertValidAndSetHighCouncilConfig(new SetHighCouncilConfigInput
        {
            DaoId = input.DaoId,
            MaxHighCouncilMemberCount = input.MaxHighCouncilMemberCount,
            MaxHighCouncilCandidateCount = input.MaxHighCouncilCandidateCount,
            StakeThreshold = input.StakeThreshold,
            ElectionPeriod = input.ElectionPeriod,
            IsRequireHighCouncilForExecution = input.IsRequireHighCouncilForExecution,
            GovernanceToken = input.GovernanceToken
        });
    }

    private void AssertValidAndSetHighCouncilConfig(SetHighCouncilConfigInput input)
    {
        AssertInitialized();
        AssertSenderDaoContract();
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        Assert(input.MaxHighCouncilMemberCount > 0, "Invalid MaxHighCouncilMemberCount.");
        Assert(input.MaxHighCouncilCandidateCount > 0, "Invalid MaxHighCouncilCandidateCount.");
        Assert(input.StakeThreshold > 0, "Invalid StakeThreshold.");
        Assert(input.ElectionPeriod >= 0, "Invalid ElectionPeriod.");
        AssertNotNullOrEmpty(input.GovernanceToken, "GovernanceToken");

        //var governanceTokenInfo = GetTokenInfo(input.GovernanceToken);
        //Assert(governanceTokenInfo != null && governanceTokenInfo.Symbol.Length > 0, "Invalid governanceToken");

        var highCouncilConfig = State.HighCouncilConfig[input.DaoId] ?? new HighCouncilConfig();
        highCouncilConfig.MaxHighCouncilMemberCount = input.MaxHighCouncilMemberCount;
        highCouncilConfig.MaxHighCouncilCandidateCount = input.MaxHighCouncilCandidateCount;
        highCouncilConfig.StakeThreshold = input.StakeThreshold;
        highCouncilConfig.GovernanceToken = input.GovernanceToken;
        highCouncilConfig.ElectionPeriod = input.ElectionPeriod;
        highCouncilConfig.IsRequireHighCouncilForExecution = input.IsRequireHighCouncilForExecution;
        State.HighCouncilConfig[input.DaoId] = highCouncilConfig;
    }

    private Hash AssertValidNewVotingItem(Hash daoId)
    {
        var votingItemId = GetVotingItemHash(daoId, Context.Sender);
        Assert(State.VotingItems[votingItemId] == null, "Voting item already exists.");
        return votingItemId;
    }
}