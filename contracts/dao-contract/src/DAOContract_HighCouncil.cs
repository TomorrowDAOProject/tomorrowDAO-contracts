using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override Empty EnableHighCouncil(EnableHighCouncilInput input)
    {
        // Assert(input != null, "Invalid input.");
        // Assert(IsHashValid(input.DaoId), "Invalid input dao id.");
        // Assert(State.HighCouncilEnabledStatusMap[input.DaoId] == false, "High council already enabled.");
        //
        // CheckDAOExists(input.DaoId);
        // CheckDaoSubsistStatus(input.DaoId);
        // AssertPermission(input.DaoId, nameof(EnableHighCouncil));
        //
        // ProcessEnableHighCouncil(input.DaoId, input.HighCouncilConfig, input.ExecutionConfig);

        return new Empty();
    }

    private void ProcessEnableHighCouncil(Hash daoId, HighCouncilConfig highCouncilConfig,
        GovernanceSchemeThreshold threshold)
    {
        State.HighCouncilEnabledStatusMap[daoId] = true;

        var governanceSchemeThreshold = ConvertToGovernanceSchemeThreshold(threshold);

        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = GovernanceMechanism.HighCouncil,
            SchemeThreshold = governanceSchemeThreshold,
            GovernanceToken = State.DAOInfoMap[daoId].GovernanceToken
        });

        State.HighCouncilAddressMap[daoId] = State.GovernanceContract.CalculateGovernanceSchemeAddress.Call(
            new CalculateGovernanceSchemeAddressInput
            {
                DaoId = daoId,
                GovernanceMechanism = GovernanceMechanism.HighCouncil
            });

        // TODO call election contract

        Context.Fire(new HighCouncilEnabled
        {
            DaoId = daoId,
            HighCouncilAddress = State.HighCouncilAddressMap[daoId]
        });
    }

    public override Empty DisableHighCouncil(Hash input)
    {
        // Assert(input != null, "Invalid input.");
        // Assert(IsHashValid(input), "Invalid input dao id.");
        // Assert(State.HighCouncilEnabledStatusMap[input] == true, "High council already disabled.");
        //
        // CheckDAOExists(input);
        // CheckDaoSubsistStatus(input);
        // AssertPermission(input, nameof(EnableHighCouncil));
        //
        // State.HighCouncilEnabledStatusMap[input] = false;
        //
        // Context.Fire(new HighCouncilDisabled
        // {
        //     DaoId = input
        // });
        //
        //
        return new Empty();
    }
}