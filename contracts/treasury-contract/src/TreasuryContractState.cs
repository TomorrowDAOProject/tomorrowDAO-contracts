using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.Contracts.Treasury;

// The state class is access the blockchain state
public partial class TreasuryContractState : ContractState
{
    public BoolState Initialized { get; set; }
    
    //<DAO id -> TreasuryInfo>
    public MappedState<Hash, TreasuryInfo> TreasuryInfoMap { get; set; }

    //DAO id, <TreasuryAccountAddress -> DAO id>
    public MappedState<Address, Hash> TreasuryAccountMap { get; set; }
}