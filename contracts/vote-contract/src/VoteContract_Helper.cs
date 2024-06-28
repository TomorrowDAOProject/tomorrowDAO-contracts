using System;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract
{
    private static bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }
    
    private static bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private void AssertCommon<T>(T input)
    {
        Assert(State.Initialized.Value, "Not initialized yet.");
        Assert(input != null, "Input is null.");
    }

    private DAOInfo AssertDao(Hash daoId)
    {
        Assert(IsHashValid(daoId), "Invalid daoId.");
        var daoInfo = State.DaoContract.GetDAOInfo.Call(daoId);
        Assert(daoInfo != null && daoInfo.DaoId == daoId, "DAO not exists.");
        return daoInfo;
    }
    
    private DAOInfo AssertDaoSubsist(Hash daoId)
    {
        var daoInfo = AssertDao(daoId);
        Assert(daoInfo.SubsistStatus, "DAO not subsist.");
        return daoInfo;
    }
    
    private ProposalInfoOutput AssertProposal(Hash proposalId)
    {
        Assert(IsHashValid(proposalId) && State.VotingItems[proposalId] == null, "Invalid proposalId.");
        var proposalInfo = State.GovernanceContract.GetProposalInfo.Call(proposalId);
        Assert(proposalInfo != null && proposalInfo.ProposalId == proposalId, "Proposal not exists.");
        return proposalInfo;
    }

    private GovernanceScheme AssertGovernanceScheme(Address schemeAddress)
    {
        Assert(IsAddressValid(schemeAddress), "Invalid schemeAddress.");
        var governanceScheme = State.GovernanceContract.GetGovernanceScheme.Call(schemeAddress);
        Assert(governanceScheme != null && governanceScheme.SchemeAddress == schemeAddress, "governanceScheme not exists.");
        return governanceScheme;
    }

    private VoteScheme AssertVoteScheme(Hash voteSchemeId)
    {
        Assert(IsHashValid(voteSchemeId) && State.VoteSchemes[voteSchemeId] != null, "Invalid voteSchemeId.");
        return State.VoteSchemes[voteSchemeId];
    }
    
    private VotingItem AssertVotingItem(Hash votingItemId)
    {
        Assert(IsHashValid(votingItemId) && State.VotingItems[votingItemId] != null, "Invalid votingItemId.");
        return State.VotingItems[votingItemId];
    }

    private void AssertVotingRecord(Hash votingItemId, Address voter)
    {
        Assert(IsHashValid(votingItemId), "Invalid votingItemId.");
        Assert(State.VotingRecords[votingItemId][voter] == null, "Voter already voted.");
    }

    private TokenInfo AssertToken(string token)
    {
        Assert(!string.IsNullOrEmpty(token), "Token is null.");
        var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = token
        });
        Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), "Token not exists.");
        return tokenInfo;
    }

    private void AssertHighCouncil(Hash daoId, Address voter)
    {
        var addressList = State.ElectionContract.GetHighCouncilMembers.Call(daoId);
        Assert(addressList.Value.Contains(voter), "Invalid voter: not HC.");
    }
    
    private void AssertOrganizationMember(Hash daoId, Address voter)
    {
        var isMember = State.DaoContract.GetIsMember.Call(new GetIsMemberInput { DaoId = daoId, Member = voter }).Value;
        Assert(isMember, "Invalid voter: not member.");
    }
    
    private void AssertBP(Address voter)
    {
        var minerList = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty());
        var minerAddressList = minerList.Pubkeys.Select(x => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(x.ToHex()))).ToList();
        Assert(minerAddressList.Contains(voter), "Invalid voter: not BP.");
    }

    private long AssertWithdraw(Address user, WithdrawInput input)
    {
        Assert(IsAddressValid(user), "Invalid withdraw user.");
        Assert(State.DaoRemainAmounts[user][input.DaoId] > 0, "Invalid dao, no amount to withdraw.");
        Assert(input.VotingItemIdList.Value.Count <= VoteContractConstants.MaxWithdrawProposalCount, "Invalid VotingItemIdList, count too much");
        Assert(input.VotingItemIdList.Value.Count > 0, "Invalid VotingItemIdList, count is zero");
        var withdrawAmount = 0L;
        foreach (var votingItemId in input.VotingItemIdList.Value)
        {
            var votingItem = State.VotingItems[votingItemId];
            Assert(votingItem != null && votingItem.EndTimestamp < Context.CurrentBlockTime, $"VotingItem not end {votingItemId}");
            var daoProposalAmount = State.DaoProposalRemainAmounts[user][GetDaoProposalId(input.DaoId,votingItemId)];
            Assert(daoProposalAmount > 0, $"Invalid proposal, no amount to withdraw {votingItemId}");
            withdrawAmount += daoProposalAmount;
        }
        Assert(withdrawAmount == input.WithdrawAmount, $"Invalid withdraw amount. withdrawAmount is {withdrawAmount} input.WithdrawAmount is {input.WithdrawAmount}");
        return withdrawAmount;
    }

    private Hash GetVirtualAddressHash(Address user, Hash daoId)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(user), HashHelper.ComputeFrom(daoId));
    }
    
    private Address GetVirtualAddress(Address user, Hash daoId)
    {
        return Context.ConvertVirtualAddressToContractAddress(GetVirtualAddressHash(user, daoId));
    }
    
    private Hash GetDaoProposalId(Hash daoId, Hash proposalId)
    {
        return HashHelper.ConcatAndCompute(daoId, proposalId);
    }
}