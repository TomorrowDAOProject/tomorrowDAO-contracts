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

        var governanceSchemeThreshold = ConvertToOrganizationGovernanceSchemeThreshold(threshold);
        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Oragnization,
            SchemeThreshold = governanceSchemeThreshold,
        });

        State.OrganizationAddressMap[daoId] = State.GovernanceContract.CalculateGovernanceSchemeAddress.Call(
            new CalculateGovernanceSchemeAddressInput
            {
                DaoId = daoId,
                GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Oragnization
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
        Assert(members != null, "Invalid input : no members to add.");
        var currentMembers = GetMember(daoId);
        var toAddMembers = new AddressList();
        foreach (var member in members!.Value)
        {
            if (!currentMembers.Value.Contains(member))
            {
                toAddMembers.Value.Add(member);
            }
        }
        Assert(toAddMembers!.Value.Count + currentMembers!.Value.Count <= DAOContractConstants.OrganizationMemberMaxCount, "Too many members to add.");
        currentMembers.Value.AddRange(toAddMembers.Value);
        State.OrganizationMemberMap[daoId] = currentMembers;
        Context.Fire(new MemberAdded
        {
            DaoId = daoId,
            AddMembers = toAddMembers
        });
    }

    public override Empty RemoveMember(RemoveMemberInput input)
    {
        Assert(input is { RemoveMembers: not null }, "Invalid input : no members to remove.");
        Assert(input.RemoveMembers!.Value.Count <= DAOContractConstants.OrganizationMemberMaxCount, "Too many members to remove.");
        CheckDAOExistsAndSubsist(input.DaoId);
        AssertPermission(input.DaoId, nameof(RemoveMember));
        
        var currentMembers = GetMember(input.DaoId);
        var toRemoveMembers = new AddressList();
        foreach (var member in input.RemoveMembers!.Value)
        {
            if (currentMembers.Value.Contains(member))
            {
                toRemoveMembers.Value.Add(member);
                currentMembers.Value.Remove(member);
            }
        }

        var governanceSchemeAddress = State.OrganizationAddressMap[input.DaoId];
        var governanceScheme = State.GovernanceContract.GetGovernanceScheme.Call(governanceSchemeAddress);
        var minVoter = governanceScheme.SchemeThreshold.MinimalRequiredThreshold;
        Assert(currentMembers.Value.Count >= minVoter, "members after remove will be less than minVoter.");
        State.OrganizationMemberMap[input.DaoId] = currentMembers;
        
        Context.Fire(new MemberRemoved
        {
            DaoId = input.DaoId,
            RemoveMembers = toRemoveMembers
        });
        return new Empty();
    }
}