using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Governance;

public partial class GovernanceContract : GovernanceContractContainer.GovernanceContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        State.AEDPoSContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender, "No permission.");
        AssertParams(input.DaoContractAddress);
        State.DaoContract.Value = input.DaoContractAddress;
        State.VoteContract.Value = input.VoteContractAddress;
        State.ElectionContract.Value = input.ElectionContractAddress;
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty SetTokenContract(Empty input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        if (State.TokenContract.Value == null)
        {
            State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        }
        return new Empty();
    }
}