using AElf;
using AElf.Types;

namespace TomorrowDAO.Contracts.Timelock;

public partial class TimelockContract
{


    private void AssertInitialized()
    {
        Assert(State.Initialized.Value, "Contract not initialized");
    }

    private void AssertAdmin()
    {
        Assert(Context.Sender == State.Admin.Value, "No permission");
    }
    
    
    private bool VerifySignature(OperationInput input)
    {
        var signature = input.Signature;
        if (signature.IsNullOrEmpty()) return false;
            
        input.Signature = null;
        var hash = HashHelper.ComputeFrom(input);
        input.Signature = signature;
            
        var publicKey = Context.RecoverPublicKey(signature.ToByteArray(), hash.ToByteArray());
        if (publicKey == null || publicKey.Length == 0) return false;
            
        return Address.FromPublicKey(publicKey) == Context.Sender;
    }

    
}