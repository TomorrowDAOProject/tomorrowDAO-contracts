using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract : VoteContractContainer.VoteContractBase
{ 
    public override Empty CreateVoteScheme(CreateVoteSchemeInput input)
    {
        AssertCommon(input);
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        var voteSchemeId = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.Self), 
            HashHelper.ComputeFrom(input));
        Assert(State.VoteSchemes[voteSchemeId] == null, "VoteScheme already exists.");
        State.VoteSchemes[voteSchemeId] = new VoteScheme
        {
            SchemeId = voteSchemeId,
            VoteMechanism = input.VoteMechanism,
            WithoutLockToken = input.WithoutLockToken,
            VoteStrategy = input.VoteStrategy
        };
        Context.Fire(new VoteSchemeCreated
        {
            VoteSchemeId = voteSchemeId,
            VoteMechanism = input.VoteMechanism,
            WithoutLockToken = input.WithoutLockToken,
            VoteStrategy = input.VoteStrategy
        });
        return new Empty();
    }

    #region View
    public override VoteScheme GetVoteScheme(Hash input)
    {
        return State.VoteSchemes[input] ?? new VoteScheme();
    }
    #endregion
}