using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private void ProcessOrganization(Hash daoId, CreateDAOInput input)
    {
        ProcessOrganizationGovernanceMechanism(daoId, input.GovernanceSchemeThreshold);
        AddMember(daoId, input.Members);
    }

    private void ProcessOrganizationGovernanceMechanism(Hash daoId, GovernanceSchemeThreshold threshold)
    {
        Assert(threshold != null, "Invalid input governance scheme threshold.");

        var governanceSchemeThreshold = ConvertToGovernanceSchemeThreshold(threshold);
        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Organization,
            SchemeThreshold = governanceSchemeThreshold,
        });

        State.OrganizationAddressMap[daoId] = State.GovernanceContract.CalculateGovernanceSchemeAddress.Call(
            new CalculateGovernanceSchemeAddressInput
            {
                DaoId = daoId,
                GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Organization
            });
    }

    public override Empty AddMember(AddMemberInput input)
    {
        CheckDAOExistsAndSubsist(input.DaoId);
        AssertPermission(input.DaoId, nameof(AddMember));
        AddMember(input.DaoId, input.AddMembers);
        return new Empty();
    }

    private void AddMember(Hash daoId, AddressList members)
    {
        Assert(members != null && members!.Value.Count <= DAOContractConstants.OnceOrganizationMemberMaxCount, 
            "Invalid add members.");
        var toAddMembers = new AddressList();
        foreach (var member in members!.Value)
        {
            if (!State.OrganizationMemberMap[daoId][member])
            {
                toAddMembers.Value.Add(member);
                State.OrganizationMemberMap[daoId][member] = true;
            }
        }
        State.OrganizationMemberCountMap[daoId] += toAddMembers.Value.Count;
        Context.Fire(new MemberAdded
        {
            DaoId = daoId,
            AddMembers = toAddMembers
        });
    }

    public override Empty RemoveMember(RemoveMemberInput input)
    {
        Assert(input is { RemoveMembers: not null } && input.RemoveMembers!.Value.Count <= DAOContractConstants.OnceOrganizationMemberMaxCount, 
            "Invalid remove members.");
        CheckDAOExistsAndSubsist(input.DaoId);
        AssertPermission(input.DaoId, nameof(RemoveMember));

        var toRemoveMembers = new AddressList();
        foreach (var member in input.RemoveMembers!.Value)
        {
            if (State.OrganizationMemberMap[input.DaoId][member])
            {
                toRemoveMembers.Value.Add(member);
                State.OrganizationMemberMap[input.DaoId].Remove(member);
            }
        }
        Assert(State.OrganizationMemberCountMap[input.DaoId] > toRemoveMembers.Value.Count, 
            "members after remove will be less than 0.");
        State.OrganizationMemberCountMap[input.DaoId] -= toRemoveMembers.Value.Count;
        
        Context.Fire(new MemberRemoved
        {
            DaoId = input.DaoId,
            RemoveMembers = toRemoveMembers
        });
        return new Empty();
    }
}