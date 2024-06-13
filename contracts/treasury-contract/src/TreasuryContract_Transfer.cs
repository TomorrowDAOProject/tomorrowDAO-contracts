using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Empty Transfer(TransferInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertNotNullOrEmpty(input.Symbol, "Symbol");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        AssertNotNullOrEmpty(input.Recipient, "Recipient");

        var treasuryInfo = AssertDaoSubsistAndTreasuryStatus(input.DaoId);

        var referendumAddress = State.DaoContract.GetReferendumAddress.Call(input.DaoId);
        var highCouncilAddress = State.DaoContract.GetHighCouncilAddress.Call(input.DaoId);
        Assert(Context.Sender == referendumAddress || Context.Sender == highCouncilAddress, "No permission.");

        TransferFromTreasury(input);
        
        Context.Fire(new TreasuryTransferred
        {
            DaoId = input.DaoId,
            TreasuryAddress = treasuryInfo.TreasuryAddress,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Recipient = input.Recipient,
            Memo = input.Memo,
            Executor = Context.Sender,
            ProposalId = input.ProposalId
        });
        return new Empty();
    }

    private void TransferFromTreasury(TransferInput input)
    {
        var treasuryHash = GenerateTreasuryHash(input.DaoId, Context.Self);
        State.TokenContract.Transfer.VirtualSend(treasuryHash, new AElf.Contracts.MultiToken.TransferInput
        {
            To = input.Recipient,
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = input.Memo
        });
    }
}