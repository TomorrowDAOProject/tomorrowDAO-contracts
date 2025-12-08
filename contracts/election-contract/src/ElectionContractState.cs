using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.Contracts.Election;

// The state class is access the blockchain state
public partial class ElectionContractState : ContractState
{
    public BoolState Initialized { get; set; }

    // dao id -> Time unit: seconds
    public Int64State MinimumLockTime { get; set; }

    // dao id -> Time unit: seconds
    public Int64State MaximumLockTime { get; set; }

    // dao id -> HighCouncilConfig
    public MappedState<Hash, HighCouncilConfig> HighCouncilConfig { get; set; }

    //dao id -> voting item id
    public MappedState<Hash, Hash> HighCouncilElectionVotingItemId { get; set; }

    //voting item id -> voting item
    public MappedState<Hash, VotingItem> VotingItems { get; set; }

    //voting result hash -> voting result
    public MappedState<Hash, VotingResult> VotingResults { get; set; }

    // dao -> Current term
    public MappedState<Hash, long> CurrentTermNumber { get; set; }

    // dao id -> candidate address -> Candidate Information
    public MappedState<Hash, Address, CandidateInformation> CandidateInformationMap { get; set; }

    // dao id -> banned address
    public MappedState<Hash, Address, bool> BannedAddressMap { get; set; }

    // dao id -> candidates
    public MappedState<Hash, AddressList> Candidates { get; set; }

    //dao id -> elected candidate
    public MappedState<Hash, AddressList> ElectedCandidates { get; set; }

    // dao id -> candidate admin map -> candidates
    public MappedState<Hash, Address, AddressList> ManagedCandidateMap { get; set; }

    // dao id -> candidate address -> candidate admin Address
    public MappedState<Hash, Address, Address> CandidateAdmins { get; set; }

    // dao id -> candidate address -> Sponsor (who will pay announce election fee for this candidate)
    public MappedState<Hash, Address, Address> CandidateSponsorMap { get; set; }

    // dao -> Address-> Voting information of candidates
    public MappedState<Hash, Address, CandidateVote> CandidateVotes { get; set; }

    //vote id -> lock time(seconds)
    public MappedState<Hash, long> LockTimeMap { get; set; }

    // dao -> elector address-> Voter's Voting Information
    public MappedState<Hash, Address, ElectorVote> ElectorVotes { get; set; }

    // vote id -> voting record
    public MappedState<Hash, VotingRecord> VotingRecords { get; set; }

    // dao -> termId -> snapshot
    public MappedState<Hash, long, TermSnapshot> Snapshots { get; set; }
    
    // dao id -> bool
    public MappedState<Hash, bool> VotingEventEnabledStatus { get; set; }
    
    // dao -> Whether HC voting is turned on
    public MappedState<Hash, bool> ElectionEnabledStatus { get; set; }
    
    //dao id -> high council member list
    public MappedState<Hash, AddressList> InitialHighCouncilMembers { get; set; }
}