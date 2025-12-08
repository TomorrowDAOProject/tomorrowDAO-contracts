using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Election;

// The state class is access the blockchain state
public partial class ElectionContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal DAOContractContainer.DAOContractReferenceState DaoContract { get; set; }
    internal GovernanceContractContainer.GovernanceContractReferenceState GovernanceContract { get; set; }

    
}