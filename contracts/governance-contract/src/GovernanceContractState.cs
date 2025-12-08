using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.Contracts.Governance
{
    // The state class is access the blockchain state
    public partial class GovernanceContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        /// <summary>
        /// scheme address -> scheme
        /// </summary>
        public MappedState<Address, GovernanceScheme> GovernanceSchemeMap { get; set; }

        /// <summary>
        /// dao id -> dao scheme address list (high council/referendum)
        /// </summary>
        public MappedState<Hash, AddressList> DaoSchemeAddressList { get; set; }

        /// <summary>
        /// proposal id -> proposal info
        /// </summary>
        public MappedState<Hash, ProposalInfo> Proposals { get; set; }

        /// <summary>
        /// proposal id -> proposal scheme threshold
        /// </summary>
        public MappedState<Hash, GovernanceSchemeThreshold> ProposalGovernanceSchemeSnapShot { get; set; }

        /// <summary>
        /// dao id -> dao proposal time period
        /// </summary>
        public MappedState<Hash, DaoProposalTimePeriod> DaoProposalTimePeriods { get; set; }
        
    }
}