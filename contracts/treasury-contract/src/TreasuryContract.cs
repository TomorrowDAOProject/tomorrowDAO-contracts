using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

// Contract class must inherit the base class generated from the proto file
public partial class TreasuryContract : TreasuryContractContainer.TreasuryContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        var contractInfo = State.GenesisContract.GetContractInfo.Call(Context.Self);
        Assert(contractInfo.Deployer == Context.Sender, "No permission.");
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.DaoContractAddress, "DaoContract");
        AssertNotNullOrEmpty(input.GovernanceContractAddress, "GovernanceContract");
        State.DaoContract.Value = input.DaoContractAddress;
        State.GovernanceContract.Value = input.GovernanceContractAddress;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.Initialized.Value = true;
        return new Empty();
    }
}