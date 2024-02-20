using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Timelock;

// The state class is access the blockchain state
public partial class TimelockContractState : ContractState 
{
        
    public BoolState Initialized { get; set; }
        
    public SingletonState<Address> Admin { get; set; }
        
    public MappedState<Hash, Timestamp> OperationExecuteTime { get; set; }
        
    public MappedState<Hash, OperationInput> OperationQueue { get; set; }
        
    public MappedState<Hash, Timestamp> OperationTime { get; set; }
        
}