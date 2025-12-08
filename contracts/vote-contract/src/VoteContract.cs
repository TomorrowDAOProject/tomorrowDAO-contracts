using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract : VoteContractContainer.VoteContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        State.AEDPoSContract.Value = Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(input != null, "Input is null.");
        
        InitializeContract(input);
        return new Empty();
    }
        
    private void InitializeContract(InitializeInput input)
    {
        Assert(IsAddressValid(input.GovernanceContractAddress), "Invalid governance contract address.");
        State.GovernanceContract.Value = input.GovernanceContractAddress;
            
        Assert(IsAddressValid(input.DaoContractAddress), "Invalid dao contract address.");
        State.DaoContract.Value = input.DaoContractAddress;
        
        Assert(IsAddressValid(input.ElectionContractAddress), "Invalid election contract address.");
        State.ElectionContract.Value = input.ElectionContractAddress;
        
        State.Initialized.Value = true;
    }
}