using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    public override Empty AnnounceElection(AnnounceElectionInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.CandidateAdmin, "CandidateAdmin");
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        var highCouncilConfig = State.HighCouncilConfig[input.DaoId];
        Assert(highCouncilConfig != null, "HighCouncilConfig not exists");
        Assert(State.ManagedCandidateMap[input.DaoId][Context.Sender] == null, "Candidate cannot be others' admin.");

        AnnounceElection(input.DaoId, Context.Sender, input.CandidateAdmin);
        State.CandidateAdmins[input.DaoId][Context.Sender] = input.CandidateAdmin;

        LockCandidateAnnounceToken(input.DaoId, highCouncilConfig);

        Context.Fire(new CandidateAdded
        {
            DaoId = input.DaoId,
            Candidate = Context.Sender,
            CandidateAdmin = input.CandidateAdmin,
            Amount = highCouncilConfig!.StakeThreshold
        });

        return new Empty();
    }

    public override Empty AnnounceElectionFor(AnnounceElectionForInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Candidate, "Candidate");
        AssertNotNullOrEmpty(input.CandidateAdmin, "CandidateAdmin");
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        var highCouncilConfig = State.HighCouncilConfig[input.DaoId];
        Assert(highCouncilConfig != null, "HighCouncilConfig not exists");
        Assert(State.ManagedCandidateMap[input.DaoId][input.Candidate] == null, "Candidate cannot be others' admin.");

        AnnounceElection(input.DaoId, input.Candidate, input.CandidateAdmin);
        State.CandidateAdmins[input.DaoId][input.Candidate] = input.CandidateAdmin;

        LockCandidateAnnounceToken(input.DaoId, highCouncilConfig);

        State.CandidateSponsorMap[input.DaoId][input.Candidate] = Context.Sender;

        Context.Fire(new CandidateAdded
        {
            DaoId = input.DaoId,
            Candidate = input.Candidate,
            CandidateAdmin = input.CandidateAdmin,
            Sponsor = Context.Sender,
            Amount = highCouncilConfig!.StakeThreshold
        });
        return new Empty();
    }

    public override Empty QuitElection(QuitElectionInput input)
    {
        AssertValidAndQuitElection(input);

        var hCouncilConfig = GetAndValidateHighCouncilConfig(input.DaoId);
        var candidateInformation = GetAndValidateCandidateInformation(input.DaoId, input.Candidate);

        // Unlock candidate's native token.
        var lockId = candidateInformation.AnnouncementTransactionId;
        State.TokenContract.Transfer.VirtualSend(lockId, 
            new TransferInput
            {
                To = State.CandidateSponsorMap[input.DaoId][input.Candidate] ?? input.Candidate,
                Symbol = hCouncilConfig.GovernanceToken,
                Amount = hCouncilConfig.StakeThreshold,
                Memo = $"Quit election."
                
            });

        // Update candidate information.
        candidateInformation.IsCurrentCandidate = false;
        candidateInformation.AnnouncementTransactionId = Hash.Empty;
        State.CandidateInformationMap[input.DaoId][input.Candidate] = candidateInformation;

        //Update candidate sponsor map
        State.CandidateSponsorMap[input.DaoId].Remove(input.Candidate);

        //Update managed candidate map
        var managedCandidate = State.ManagedCandidateMap[input.DaoId][Context.Sender];
        managedCandidate.Value.Remove(input.Candidate);
        if (managedCandidate.Value.Any())
            State.ManagedCandidateMap[input.DaoId][Context.Sender] = managedCandidate;
        else
            State.ManagedCandidateMap[input.DaoId].Remove(Context.Sender);

        //Update candidate admins map
        State.CandidateAdmins[input.DaoId].Remove(input.Candidate);

        Context.Fire(new CandidateRemoved
        {
            DaoId = input.DaoId,
            Candidate = input.Candidate
        });

        return new Empty();
    }

    public override Empty SetCandidateAdmin(SetCandidateAdminInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoId, "DaoId");
        AssertNotNullOrEmpty(input.Candidate, "Candidate");
        AssertNotNullOrEmpty(input.NewAdmin, "NewAdmin");
        var oldAdminAddress = State.CandidateAdmins[input.DaoId][input.Candidate];
        Assert(oldAdminAddress == Context.Sender, "No permission.");
        State.CandidateAdmins[input.DaoId][input.Candidate] = input.NewAdmin;
        
        var oldAdminManagedCandidates = State.ManagedCandidateMap[input.DaoId][oldAdminAddress] ?? new AddressList();
        if (oldAdminManagedCandidates.Value.Contains(input.Candidate))
        {
            oldAdminManagedCandidates.Value.Remove(input.Candidate);
            State.ManagedCandidateMap[input.DaoId][oldAdminAddress] = oldAdminManagedCandidates;
        }

        var newAdminManagedCandidates = State.ManagedCandidateMap[input.DaoId][input.NewAdmin] ?? new AddressList();
        if (!newAdminManagedCandidates.Value.Contains(input.Candidate))
        {
            newAdminManagedCandidates.Value.Add(input.Candidate);
            State.ManagedCandidateMap[input.DaoId][input.NewAdmin] = newAdminManagedCandidates;
        }

        return new Empty();
    }

    private void AnnounceElection(Hash daoId, Address candidateAddress, Address candidateAdmin)
    {
        var highCouncil = State.HighCouncilConfig[daoId];
        Assert(highCouncil != null, $"Dao {daoId} not exists.");
        var candidateInformation = State.CandidateInformationMap[daoId][candidateAddress];

        if (candidateInformation != null)
        {
            Assert(!candidateInformation.IsCurrentCandidate,
                $"This address already announced election. {candidateAddress}");
            candidateInformation.AnnouncementTransactionId = Context.OriginTransactionId;
            candidateInformation.IsCurrentCandidate = true;
            // In this way we can keep history of current candidate, like terms, missed time slots, etc.
            State.CandidateInformationMap[daoId][candidateAddress] = candidateInformation;
        }
        else
        {
            Assert(!IsAddressBanned(daoId, candidateAddress), "This candidate already banned before.");
            State.CandidateInformationMap[daoId][candidateAddress] = new CandidateInformation
            {
                Address = candidateAddress,
                AnnouncementTransactionId = Context.OriginTransactionId,
                IsCurrentCandidate = true
            };
        }

        var addresses = State.Candidates[daoId] ?? new AddressList();
        Assert(addresses.Value.Count < highCouncil.MaxHighCouncilCandidateCount,
            $"The number of candidates cannot exceed {highCouncil.MaxHighCouncilCandidateCount}");
        addresses.Value.Add(candidateAddress);
        State.Candidates[daoId] = addresses;

        var managedAddresses = State.ManagedCandidateMap[daoId][candidateAdmin] ?? new AddressList();
        if (!managedAddresses.Value.Contains(Context.Sender))
        {
            managedAddresses.Value.Add(Context.Sender);
            State.ManagedCandidateMap[daoId][candidateAdmin] = managedAddresses;
        }
    }

    private bool IsAddressBanned(Hash daoId, Address address)
    {
        return State.BannedAddressMap[daoId][address];
    }

    private void LockCandidateAnnounceToken(Hash daoId, HighCouncilConfig highCouncilConfig)
    {
        var lockId = Context.OriginTransactionId;
        var lockVirtualAddress = Context.ConvertVirtualAddressToContractAddress(lockId);
        var sponsorAddress = Context.Sender;
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = sponsorAddress,
            To = lockVirtualAddress,
            Symbol = highCouncilConfig.GovernanceToken,
            Amount = highCouncilConfig.StakeThreshold,
            Memo = "Lock for dao announcing election."
        });
    }

    private void AssertValidAndQuitElection(QuitElectionInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Candidate, "Candidate");
        Assert(State.Candidates[input.DaoId].Value.Contains(input.Candidate), "Target is not a candidate.");
        var managedCandidates = State.CandidateAdmins[input.DaoId][input.Candidate];
        Assert(Context.Sender == managedCandidates, "Only admin can quit election.");
        
        State.Candidates[input.DaoId].Value.Remove(input.Candidate);
    }
}