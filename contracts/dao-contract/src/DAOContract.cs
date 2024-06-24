using System.Collections.Generic;
using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Treasury;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract : DAOContractContainer.DAOContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender,
            "No permission.");

        InitializeContract(input);

        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.Initialized.Value = true;
        return new Empty();
    }

    private void InitializeContract(InitializeInput input)
    {
        Assert(IsAddressValid(input.GovernanceContractAddress), "Invalid governance contract address.");
        State.GovernanceContract.Value = input.GovernanceContractAddress;

        Assert(IsAddressValid(input.ElectionContractAddress), "Invalid election contract address.");
        State.ElectionContract.Value = input.ElectionContractAddress;

        // todo do not have TimelockContract and TreasuryContract this version, so remove check and assignment temporarily
        
        // Assert(IsAddressValid(input.TimelockContractAddress), "Invalid timelock contract address.");
        // State.TimelockContract.Value = input.TimelockContractAddress;

        Assert(IsAddressValid(input.VoteContractAddress), "Invalid vote contract address.");
        State.VoteContract.Value = input.VoteContractAddress;
    }

    public override Empty CreateDAO(CreateDAOInput input)
    {
        CheckInitialized();

        var daoId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input),
            HashHelper.ComputeFrom(Context.Sender));

        Assert(State.DAOInfoMap[daoId] == null, "DAO already exists.");
        if (GovernanceMechanism.Organization == (GovernanceMechanism)input.GovernanceMechanism)
        {
            Assert(string.IsNullOrEmpty(input.GovernanceToken), "Invalid governance token.");
        }

        ProcessDAOBaseInfo(daoId, input);
        ProcessDAOComponents(daoId, input);

        var daoInfo = State.DAOInfoMap[daoId];
        var treasuryAddress = State.TreasuryContract?.GetTreasuryAccountAddress?.Call(daoId);
        Context.Fire(new DAOCreated
        {
            DaoId = daoId,
            Metadata = input.Metadata,
            Creator = Context.Sender,
            GovernanceToken = input.GovernanceToken,
            ContractAddressList = daoInfo.ContractAddressList,
            IsNetworkDao = input.IsNetworkDao,
            TreasuryAddress = treasuryAddress,
            GovernanceMechanism = daoInfo.GovernanceMechanism,
        });

        return new Empty();
    }

    private void ProcessDAOBaseInfo(Hash daoId, CreateDAOInput input)
    {
        var daoInfo = new DAOInfo
        {
            DaoId = daoId,
            Creator = Context.Sender,
            SubsistStatus = true,
            ContractAddressList = new ContractAddressList
            {
                GovernanceContractAddress = State.GovernanceContract.Value,
                ElectionContractAddress = State.ElectionContract.Value,
                TimelockContractAddress = State.TimelockContract.Value,
                TreasuryContractAddress = State.TreasuryContract.Value,
                VoteContractAddress = State.VoteContract.Value
            },
            IsNetworkDao = input.IsNetworkDao,
            GovernanceMechanism = (GovernanceMechanism)input.GovernanceMechanism
        };

        State.DAOInfoMap[daoId] = daoInfo;

        ProcessMetadata(daoId, input.Metadata);
        ProcessGovernanceToken(daoId, input.GovernanceToken);
    }

    private void ProcessGovernanceToken(Hash daoId, string governanceToken)
    {
        if (!IsStringValid(governanceToken)) return;
        AssertToken(governanceToken);

        State.DAOInfoMap[daoId].GovernanceToken = governanceToken;
    }

    private void ProcessDAOComponents(Hash daoId, CreateDAOInput input)
    {
        var governanceMechanism = (GovernanceMechanism)input.GovernanceMechanism;
        if (GovernanceMechanism.Organization == governanceMechanism)
        {
            ProcessOrganization(daoId, input);
        }
        else
        {
            ProcessReferendumGovernanceMechanism(daoId, input.GovernanceSchemeThreshold);
            ProcessHighCouncil(daoId, input.HighCouncilInput);
        }
        ProcessTreasuryContract(daoId, input.IsTreasuryNeeded);
        ProcessFileUploads(daoId, input.Files);
        ProcessDefaultPermissions(daoId, new List<string>
            { DAOContractConstants.UploadFileInfos, DAOContractConstants.RemoveFileInfos, DAOContractConstants.UpdateMetadata });
    }

    private void ProcessTreasuryContract(Hash daoId, bool isTreasuryNeeded)
    {
        if (!isTreasuryNeeded) return;
        
        State.TreasuryContract.CreateTreasury.Send(new CreateTreasuryInput
        {
            DaoId = daoId
        });
    }

    public override Empty SetSubsistStatus(SetSubsistStatusInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
        CheckDAOExists(input.DaoId);
        AssertPermission(input.DaoId, nameof(SetSubsistStatus));

        if (State.DAOInfoMap[input.DaoId].SubsistStatus == input.Status) return new Empty();

        State.DAOInfoMap[input.DaoId].SubsistStatus = input.Status;

        Context.Fire(new SubsistStatusSet
        {
            DaoId = input.DaoId,
            Status = input.Status
        });

        return new Empty();
    }
    
    public override Empty SetTreasuryContractAddress(Address input)
    {
        Assert(IsAddressValid(input), "Invalid treasury contract address.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender,
            "No permission.");
        
        if (State.TreasuryContract.Value == null)
        {
            State.TreasuryContract.Value = input;
        }

        return new Empty();
    }
}