using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override Empty EnableHighCouncil(EnableHighCouncilInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
        Assert(State.HighCouncilEnabledStatusMap[input.DaoId] == false, "High council already enabled.");

        CheckDAOExists(input.DaoId);
        CheckDaoSubsistStatus(input.DaoId);
        AssertPermission(input.DaoId, nameof(EnableHighCouncil));

        var daoInfo = State.DAOInfoMap[input.DaoId];
        Assert(daoInfo.GovernanceMechanism != GovernanceMechanism.Organization,
            "Multi-signature governance cannot enable the High Council.");

        ProcessEnableHighCouncil(input.DaoId, input.HighCouncilInput);

        return new Empty();
    }

    public override Empty AddHighCouncilMembers(AddHighCouncilMembersInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input!.DaoId), "Invalid input dao id.");
        CheckDAOExists(input.DaoId);
        CheckDaoSubsistStatus(input.DaoId);
        AssertPermission(input.DaoId, nameof(AddHighCouncilMembers));
        
        var daoInfo = State.DAOInfoMap[input.DaoId];
        Assert(daoInfo.GovernanceMechanism != GovernanceMechanism.Organization,
            "Multi-signature governance cannot add the High Council members.");

        State.ElectionContract.AddHighCouncil.Send(new AddHighCouncilInput
        {
            DaoId = input.DaoId,
            AddHighCouncils = new Election.AddressList()
            {
                Value = { input.AddHighCouncils.Value }
            }
        });
        return new Empty();
    }

    public override Empty RemoveHighCouncilMembers(RemoveHighCouncilMembersInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input!.DaoId), "Invalid input dao id.");
        CheckDAOExists(input.DaoId);
        CheckDaoSubsistStatus(input.DaoId);
        AssertPermission(input.DaoId, nameof(RemoveHighCouncilMembers));
        
        var daoInfo = State.DAOInfoMap[input.DaoId];
        Assert(daoInfo.GovernanceMechanism != GovernanceMechanism.Organization,
            "Multi-signature governance cannot remove the High Council members.");
        
        State.ElectionContract.RemoveHighCouncil.Send(new RemoveHighCouncilInput()
        {
            DaoId = input.DaoId,
            RemoveHighCouncils = new Election.AddressList()
            {
                Value = { input.RemoveHighCouncils.Value }
            }
        });
        return new Empty();
    }

    private void ProcessEnableHighCouncil(Hash daoId, HighCouncilInput highCouncilInput)
    {
        HighCouncilConfig highCouncilConfig = highCouncilInput.HighCouncilConfig;
        GovernanceSchemeThreshold threshold = highCouncilInput.GovernanceSchemeThreshold;

        State.HighCouncilEnabledStatusMap[daoId] = true;

        var governanceSchemeThreshold = ConvertToGovernanceSchemeThreshold(threshold);

        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.HighCouncil,
            SchemeThreshold = governanceSchemeThreshold,
            GovernanceToken = State.DAOInfoMap[daoId].GovernanceToken
        });

        State.HighCouncilAddressMap[daoId] = State.GovernanceContract.CalculateGovernanceSchemeAddress.Call(
            new CalculateGovernanceSchemeAddressInput
            {
                DaoId = daoId,
                GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.HighCouncil
            });

        if (highCouncilInput.HighCouncilMembers != null && highCouncilInput.HighCouncilMembers.Value.Count > 0)
        {
            State.ElectionContract.AddHighCouncil.Send(new AddHighCouncilInput
            {
                DaoId = daoId,
                AddHighCouncils = new Election.AddressList()
                {
                    Value = { highCouncilInput.HighCouncilMembers.Value }
                }
            });
        }

        if (!highCouncilInput.IsHighCouncilElectionClose)
        {
            State.ElectionContract.RegisterElectionVotingEvent.Send(new RegisterElectionVotingEventInput
            {
                DaoId = daoId,
                ElectionPeriod = highCouncilConfig.ElectionPeriod,
                GovernanceToken = State.DAOInfoMap[daoId].GovernanceToken,
                StakeThreshold = highCouncilConfig.StakingAmount,
                MaxHighCouncilCandidateCount = highCouncilConfig.MaxHighCouncilCandidateCount,
                MaxHighCouncilMemberCount = highCouncilConfig.MaxHighCouncilMemberCount
            });
        }

        Context.Fire(new HighCouncilEnabled
        {
            DaoId = daoId,
            HighCouncilInput = new HighCouncilInput
            {
                HighCouncilConfig = highCouncilConfig
            },
            HighCouncilAddress = State.HighCouncilAddressMap[daoId]
        });
    }

    public override Empty DisableHighCouncil(Hash input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input), "Invalid input dao id.");
        Assert(State.HighCouncilEnabledStatusMap[input] == true, "High council already disabled.");

        CheckDAOExists(input);
        CheckDaoSubsistStatus(input);
        AssertPermission(input, nameof(DisableHighCouncil));

        State.HighCouncilEnabledStatusMap[input] = false;

        Context.Fire(new HighCouncilDisabled
        {
            DaoId = input
        });

        return new Empty();
    }

    private void ProcessHighCouncil(Hash daoId, HighCouncilInput input)
    {
        if (input == null || !IsStringValid(State.DAOInfoMap[daoId].GovernanceToken)) return;

        ProcessEnableHighCouncil(daoId, input);
    }
}