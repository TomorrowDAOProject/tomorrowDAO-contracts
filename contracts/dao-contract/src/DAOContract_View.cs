using System.Linq;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override DAOInfo GetDAOInfo(Hash input)
    {
        var daoInfo = State.DAOInfoMap[input];
        if (daoInfo != null && daoInfo.ContractAddressList != null)
        {
            var treasuryContractAddress = daoInfo.ContractAddressList.TreasuryContractAddress;
            if (treasuryContractAddress == null || !treasuryContractAddress.Value.Any())
            {
                daoInfo.ContractAddressList.TreasuryContractAddress = State.TreasuryContract.Value;
            }
        }
        return daoInfo;
    }

    public override ContractAddressList GetInitializedContracts(Empty input)
    {
        return new ContractAddressList
        {
            GovernanceContractAddress = State.GovernanceContract.Value,
            ElectionContractAddress = State.ElectionContract.Value,
            TreasuryContractAddress = State.TreasuryContract.Value,
            VoteContractAddress = State.VoteContract.Value,
            TimelockContractAddress = State.TimelockContract.Value
        };
    }

    public override StringValue GetGovernanceToken(Hash input)
    {
        return new StringValue
        {
            Value = State.DAOInfoMap[input].GovernanceToken
        };
    }

    public override Hash GetDAOIdByName(StringValue input)
    {
        return State.DAONameMap[input.Value];
    }

    public override BoolValue GetSubsistStatus(Hash input)
    {
        if (State.DAOInfoMap[input] == null)
        {
            return new BoolValue
            {
                Value = false
            };
        }

        return new BoolValue
        {
            Value = State.DAOInfoMap[input].SubsistStatus
        };
    }

    public override Metadata GetMetadata(Hash input)
    {
        return State.MetadataMap[input];
    }

    public override FileInfoList GetFileInfos(Hash input)
    {
        return State.FilesMap[input];
    }

    public override BoolValue HasPermission(HasPermissionInput input)
    {
        return new BoolValue
        {
            Value = IsGranted(input.DaoId, input.Where, input.What.ToLower(), input.Who)
        };
    }

    public override BoolValue GetHighCouncilStatus(Hash input)
    {
        return new BoolValue { Value = State.HighCouncilEnabledStatusMap[input] };
    }

    public override Address GetHighCouncilAddress(Hash input)
    {
        return !State.HighCouncilEnabledStatusMap[input] ? new Address() : State.HighCouncilAddressMap[input];
    }

    public override Address GetReferendumAddress(Hash input)
    {
        return State.ReferendumAddressMap[input];
    }

    public override Address GetTreasuryAddress(Hash input)
    {
        return State.TreasuryContract.GetTreasuryAccountAddress.Call(input);
    }

    public override BoolValue GetIsMember(GetIsMemberInput input)
    {
        return new BoolValue { Value = GetMember(input.DaoId).Value.Contains(input.Member) };
    }

    public override AddressList GetMember(Hash input)
    {
        return State.OrganizationMemberMap[input] == null ? new AddressList() : State.OrganizationMemberMap[input];
    }
}