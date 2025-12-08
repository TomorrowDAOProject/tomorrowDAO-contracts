using AElf.Sdk.CSharp.State;

namespace TomorrowDAO.Contracts.Timelock
{
    // The state class is access the blockchain state
    public class TimelockContractState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}