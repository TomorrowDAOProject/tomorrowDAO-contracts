using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using TomorrowDAO.Contracts.DAO;

namespace TomorrowDAO.Contracts.Election;

public partial class ElectionContract
{
    //Assert Helper
    private void AssertInitialized()
    {
        Assert(State.Initialized.Value, "Contract not initialized.");
    }

    private void AssertSenderDaoContract()
    {
        Assert(State.DaoContract.Value == Context.Sender, "No permission.");
    }

    private DAOInfo AssertSenderIsDaoContractOrProposal(Hash daoId)
    {
        var daoInfo = State.DaoContract.GetDAOInfo.Call(daoId);
        Assert(daoId != null, $"Dao {daoId} not exist.");
        Assert(daoInfo.SubsistStatus, "DAO is not in subsistence.");
        var referendumAddress = State.DaoContract.GetReferendumAddress.Call(daoId);
        var highCouncilAddress = State.DaoContract.GetHighCouncilAddress.Call(daoId);
        Assert(
            State.DaoContract.Value == Context.Sender || Context.Sender == referendumAddress ||
            Context.Sender == highCouncilAddress, "No permission.");
        return daoInfo;
    }

    private void AssertNotNullOrEmpty(object input, string message = null)
    {
        message = string.IsNullOrWhiteSpace(message) ? "Invalid input." : $"{message} cannot be null or empty.";
        Assert(input != null, message);
        switch (input)
        {
            case Address address:
                Assert(address.Value.Any(), message);
                break;
            case Hash hash:
                Assert(hash != Hash.Empty, message);
                break;
            case string s:
                Assert(!string.IsNullOrEmpty(s), message);
                break;
        }
    }

    private HighCouncilConfig GetAndValidateHighCouncilConfig(Hash daoId)
    {
        var hCouncilConfig = State.HighCouncilConfig[daoId];
        Assert(hCouncilConfig != null, $"Dao {daoId} High Council Config not exists.");
        return hCouncilConfig;
    }

    private CandidateInformation GetAndValidateCandidateInformation(Hash daoId, Address candidate)
    {
        var candidateInformation = State.CandidateInformationMap[daoId][candidate];
        Assert(candidateInformation != null, $"Candidate not exists.");
        return candidateInformation;
    }

    private VotingItem GetAndValidateVotingItemByDaoId(Hash daoId)
    {
        var votingItemId = State.HighCouncilElectionVotingItemId[daoId];
        Assert(votingItemId != null, "Voting item not exists");
        var votingItem = State.VotingItems[votingItemId];
        Assert(votingItem != null, "Voting item not exists");
        return votingItem;
    }

    //Generate Hash
    private static Hash GetVotingItemHash(Hash daoId, Address sponsorAddress)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(daoId), HashHelper.ComputeFrom(sponsorAddress));
    }

    private Hash GetVotingResultHash(Hash votingItemId, long snapshotNumber)
    {
        return new VotingResult
        {
            VotingItemId = votingItemId,
            SnapshotNumber = snapshotNumber
        }.GetHash();
    }

    private TokenInfo GetTokenInfo(string symbol)
    {
        return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = symbol
        });
    }
}