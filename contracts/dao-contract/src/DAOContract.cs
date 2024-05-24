using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

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
        
        // Assert(IsAddressValid(input.TreasuryContractAddress), "Invalid treasury contract address.");
        // State.TreasuryContract.Value = input.TreasuryContractAddress;

        Assert(IsAddressValid(input.VoteContractAddress), "Invalid vote contract address.");
        State.VoteContract.Value = input.VoteContractAddress;
    }

    public override Empty CreateDAO(CreateDAOInput input)
    {
        CheckInitialized();

        var daoId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input),
            HashHelper.ComputeFrom(Context.Sender));

        Assert(State.DAOInfoMap[daoId] == null, "DAO already exists.");

        ProcessDAOBaseInfo(daoId, input);
        ProcessDAOComponents(daoId, input);

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
            IsNetworkDao = input.IsNetworkDao
        };

        State.DAOInfoMap[daoId] = daoInfo;

        ProcessMetadata(daoId, input.Metadata);
        ProcessGovernanceToken(daoId, input.GovernanceToken);

        Context.Fire(new DAOCreated
        {
            DaoId = daoId,
            Metadata = input.Metadata,
            Creator = Context.Sender,
            GovernanceToken = input.GovernanceToken,
            ContractAddressList = daoInfo.ContractAddressList,
            IsNetworkDao = input.IsNetworkDao
        });
    }

    private void ProcessMetadata(Hash daoId, Metadata metadata)
    {
        Assert(metadata != null, "Invalid metadata.");
        Assert(IsStringValid(metadata.Name) && metadata.Name.Length <= DAOContractConstants.NameMaxLength,
            "Invalid metadata name.");
        Assert(State.DAONameMap[metadata.Name] == null, "DAO name already exists.");
        Assert(IsStringValid(metadata.LogoUrl) && metadata.LogoUrl.Length <= DAOContractConstants.LogoUrlMaxLength,
            "Invalid metadata logo url.");
        Assert(IsStringValid(metadata.Description)
               && metadata.Description.Length <= DAOContractConstants.DescriptionMaxLength,
            "Invalid metadata description.");

        Assert(
            metadata.SocialMedia.Count > 0 &&
            metadata.SocialMedia.Count <= DAOContractConstants.SocialMediaListMaxCount,
            "Invalid metadata social media count.");

        foreach (var socialMedia in metadata.SocialMedia.Keys)
        {
            Assert(
                IsStringValid(socialMedia) && socialMedia.Length <= DAOContractConstants.SocialMediaNameMaxLength,
                "Invalid metadata social media name.");
            Assert(
                IsStringValid(metadata.SocialMedia[socialMedia])
                && metadata.SocialMedia[socialMedia].Length <= DAOContractConstants.SocialMediaUrlMaxLength,
                "Invalid metadata social media url.");
        }

        State.MetadataMap[daoId] = metadata;
        State.DAONameMap[metadata.Name] = daoId;
    }

    private void ProcessGovernanceToken(Hash daoId, string governanceToken)
    {
        if (!IsStringValid(governanceToken)) return;
        Assert(governanceToken.Length <= DAOContractConstants.SymbolMaxLength &&
               governanceToken.All(IsValidTokenChar), "Invalid token symbol.");

        var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = governanceToken
        });

        Assert(!string.IsNullOrWhiteSpace(tokenInfo.Symbol), "Token not found.");

        State.DAOInfoMap[daoId].GovernanceToken = governanceToken;
    }

    private void ProcessDAOComponents(Hash daoId, CreateDAOInput input)
    {
        ProcessReferendumGovernanceMechanism(daoId, input.GovernanceSchemeThreshold);
        ProcessHighCouncil(daoId, input.HighCouncilInput);
        ProcessTreasuryContract(daoId, input.IsTreasuryContractNeeded);
        ProcessFileUploads(daoId, input.Files);
        ProcessDefaultPermissions(daoId);
    }

    private void ProcessReferendumGovernanceMechanism(Hash daoId, GovernanceSchemeThreshold threshold)
    {
        Assert(threshold != null, "Invalid input governance scheme threshold.");

        var governanceSchemeThreshold = ConvertToGovernanceSchemeThreshold(threshold);
        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = GovernanceMechanism.Referendum,
            SchemeThreshold = governanceSchemeThreshold,
            GovernanceToken = State.DAOInfoMap[daoId].GovernanceToken
        });

        State.ReferendumAddressMap[daoId] = State.GovernanceContract.CalculateGovernanceSchemeAddress.Call(
            new CalculateGovernanceSchemeAddressInput
            {
                DaoId = daoId,
                GovernanceMechanism = GovernanceMechanism.Referendum
            });
    }

    private void ProcessHighCouncil(Hash daoId, HighCouncilInput input)
    {
        if (input == null || !IsStringValid(State.DAOInfoMap[daoId].GovernanceToken)) return;

        ProcessEnableHighCouncil(daoId, input.HighCouncilConfig, input.GovernanceSchemeThreshold);
    }

    private void ProcessTreasuryContract(Hash daoId, bool isTreasuryNeeded)
    {
        if (!isTreasuryNeeded) return;

        // TODO
    }

    private void ProcessDefaultPermissions(Hash daoId)
    {
        var defaultMethodNames = new List<string>
            { DAOContractConstants.UploadFileInfos, DAOContractConstants.RemoveFileInfos };

        foreach (var method in defaultMethodNames)
        {
            ProcessPermission(daoId, new PermissionInfo
            {
                Where = Context.Self,
                Who = Context.Sender,
                What = method
            }, PermissionType.Creator);
        }
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
}