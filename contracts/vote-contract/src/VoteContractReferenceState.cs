using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractReferenceState AEDPoSContract { get; set; }
    internal DAOContractContainer.DAOContractReferenceState DaoContract { get; set; }
    internal GovernanceContractContainer.GovernanceContractReferenceState GovernanceContract { get; set; }
    internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
}