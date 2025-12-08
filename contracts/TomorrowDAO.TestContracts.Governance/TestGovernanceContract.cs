using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.TestContracts.Governance;

public class TestGovernanceContract : TestGovernanceContractContainer.TestGovernanceContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        State.Referendum.Value = input.Referendum;
        State.HighCouncil.Value = input.HighCouncil;
        return new Empty();
    }

    public override Address AddGovernanceScheme(AddGovernanceSchemeInput input)
    {
        return Context.Sender;
    }

    public override Address CalculateGovernanceSchemeAddress(CalculateGovernanceSchemeAddressInput input)
    {
        switch (input.GovernanceMechanism)
        {
            case GovernanceMechanism.Referendum:
                return State.Referendum.Value;
            case GovernanceMechanism.HighCouncil:
                return State.HighCouncil.Value;
            default:
                return new Address();
        }
    }
}