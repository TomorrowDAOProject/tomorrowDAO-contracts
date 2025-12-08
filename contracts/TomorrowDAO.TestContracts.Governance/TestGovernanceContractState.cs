using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.TestContracts.Governance;

public class TestGovernanceContractState : ContractState
{
    public SingletonState<Address> Referendum { get; set; }
    public SingletonState<Address> HighCouncil { get; set; }
}