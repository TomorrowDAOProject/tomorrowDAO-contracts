using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Timelock;

// The state class is access the blockchain state
public partial class TimelockContractState : ContractState 
{
    
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
        
}