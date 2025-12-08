using AElf;
using AElf.Types;

namespace TomorrowDAO.Contracts.Election;

public static class ElectionExtensions
{
    public static Hash GetHash(this VotingResult votingResult)
    {
        return HashHelper.ComputeFrom(new VotingResult
        {
            VotingItemId = votingResult.VotingItemId,
            SnapshotNumber = votingResult.SnapshotNumber
        });
    }
}