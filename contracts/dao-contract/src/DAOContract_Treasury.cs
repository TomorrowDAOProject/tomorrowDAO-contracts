using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override Empty CreateTreasury(CreateTreasuryInput input)
    {
        Assert(input != null, "Invalid input.");
        CheckDAOExistsAndSubsist(input.DaoId);
        var daoInfo = State.DAOInfoMap[input.DaoId];
        Assert(daoInfo.Creator == Context.Sender, "No permission.");
        Assert(daoInfo.TreasuryAddress == null, $"Dao {input.DaoId} treasury has been created.");

        var treasuryAddress = GenerateTreasuryAddressFromDaoId(input.DaoId);

        var daoId = State.TreasuryAccountMap[treasuryAddress];
        Assert(daoId == null || daoId == Hash.Empty, "Treasury address already exists.");
        State.TreasuryAccountMap[treasuryAddress] = daoId;

        daoInfo.TreasuryAddress = treasuryAddress;
        State.DAOInfoMap[input.DaoId] = daoInfo;

        Context.Fire(new TreasuryCreated
        {
            DaoId = input.DaoId,
            TreasuryAccountAddress = treasuryAddress
        });
        return new Empty();
    }

    public override Empty Deposit(DepositInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsStringValid(input!.Symbol), "Invalid Symbol.");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        CheckDAOExistsAndSubsist(input.DaoId);

        var daoInfo = State.DAOInfoMap[input.DaoId];
        var treasuryAddress = daoInfo.TreasuryAddress;
        Assert(IsAddressValid(treasuryAddress), "Treasury has not bean created yet.");

        var symbols = daoInfo.Symbols;
        if (!symbols.Contains(input.Symbol))
        {
            symbols.Add(input.Symbol);
            Assert(symbols.Count <= DAOContractConstants.MaxSupportedTokenTypes,
                $"The staked token cannot be exceed {DAOContractConstants.MaxSupportedTokenTypes} types");
        }

        State.DAOInfoMap[input.DaoId] = daoInfo;

        TransferTokenToTreasury(treasuryAddress, input.Symbol, input.Amount, false);

        var fundInfo = State.FundInfoMap[treasuryAddress][input.Symbol] ?? new FundInfo();
        fundInfo.Amount += input.Amount;
        State.FundInfoMap[treasuryAddress][input.Symbol] = fundInfo;

        var totalFundInfo = State.TotalFundInfoMap[input.Symbol] ?? new FundInfo();
        totalFundInfo.Amount += input.Amount;
        State.TotalFundInfoMap[input.Symbol] = totalFundInfo;

        Context.Fire(new DepositReceived
        {
            DaoId = input.DaoId,
            TreasuryAddress = treasuryAddress,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Donor = Context.Sender,
            DonationTime = Context.CurrentBlockTime
        });

        return new Empty();
    }

    public override Empty Transfer(TransferInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsStringValid(input!.Symbol), "Invalid Symbol.");
        Assert(input.Amount > 0, "Amount must be greater than 0.");
        Assert(IsAddressValid(input.Recipient), "Invalid Recipient.");
        CheckDAOExistsAndSubsist(input.DaoId);

        var daoInfo = State.DAOInfoMap[input.DaoId];
        var treasuryAddress = daoInfo.TreasuryAddress;
        Assert(IsAddressValid(treasuryAddress), "Treasury has not bean created yet.");
        AssertPermission(input.DaoId, nameof(Transfer));

        //update funds
        UpdateFundInfo(treasuryAddress, daoInfo, input);
        UpdateTotalFundInfo(input);

        TransferFromTreasury(input);

        Context.Fire(new TreasuryTransferred
        {
            DaoId = input.DaoId,
            TreasuryAddress = treasuryAddress,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Recipient = input.Recipient,
            Memo = input.Memo,
            Executor = Context.Sender,
            ProposalId = input.ProposalId
        });
        return new Empty();
    }

    private void UpdateTotalFundInfo(TransferInput input)
    {
        var totalFundInfo = State.TotalFundInfoMap[input.Symbol];
        Assert(totalFundInfo != null, "TotalFundInfo not exist.");
        totalFundInfo!.Amount -= input.Amount;
        State.TotalFundInfoMap[input.Symbol] = totalFundInfo;
    }

    private void UpdateFundInfo(Address treasuryAddress, DAOInfo daoInfo, TransferInput input)
    {
        var fundInfo = State.FundInfoMap[treasuryAddress][input.Symbol];
        Assert(fundInfo != null, "FundInfo not exist.");
        Assert(fundInfo!.Amount >= input.Amount, "Insufficient treasury funds.");
        fundInfo!.Amount -= input.Amount;
        if (fundInfo.Amount == 0)
        {
            State.FundInfoMap[treasuryAddress].Remove(input.Symbol);
            daoInfo.Symbols.Remove(input.Symbol);
            State.DAOInfoMap[input.DaoId] = daoInfo;
        }
        else
        {
            State.FundInfoMap[treasuryAddress][input.Symbol] = fundInfo;
        }
    }


    private void TransferTokenToTreasury(Address treasuryAddress, string symbol, long amount, bool isStakingToken)
    {
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = treasuryAddress,
            Symbol = symbol,
            Amount = amount,
            Memo = "Deposit funds into the treasury"
        });
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