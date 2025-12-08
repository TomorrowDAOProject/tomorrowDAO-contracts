using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Timelock;
using TomorrowDAO.Contracts.Treasury;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal GovernanceContractContainer.GovernanceContractReferenceState GovernanceContract { get; set; }
    internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
    internal TimelockContractContainer.TimelockContractReferenceState TimelockContract { get; set; }
    internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
    internal VoteContractContainer.VoteContractReferenceState VoteContract { get; set; }
}