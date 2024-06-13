using System.Linq;
using AElf;
using AElf.Types;
using TomorrowDAO.Contracts.DAO;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
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

    private DAOInfo AssertSenderIsDaoContractOrDaoAdmin(Hash daoId)
    {
        var daoInfo = State.DaoContract.GetDAOInfo.Call(daoId);
        Assert(daoId != null, $"Dao {daoId} not exist.");
        Assert(State.DaoContract.Value == Context.Sender || daoInfo.Creator == Context.Sender, "No permission.");
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

    private void AssertDaoSubsists(Hash daoId)
    {
        AssertNotNullOrEmpty(daoId, "DaoId");
        var boolValue = State.DaoContract.GetSubsistStatus.Call(daoId);
        Assert(boolValue.Value, "DAO does not exist or is not in subsistence.");
    }

    private TreasuryInfo AssertDaoSubsistAndTreasuryStatus(Hash daoId)
    {
        AssertDaoSubsists(daoId);

        var treasuryInfo = State.TreasuryInfoMap[daoId];
        Assert(treasuryInfo != null, "Treasury has not bean created yet.");

        return treasuryInfo;
    }

    private static Hash GenerateTreasuryHash(Hash daoId, Address treasuryContractAddress)
    {
        return HashHelper.ConcatAndCompute(daoId, HashHelper.ComputeFrom(treasuryContractAddress));
    }

    private Address GenerateTreasuryAddress(Hash treasuryHash)
    {
        return Context.ConvertVirtualAddressToContractAddress(treasuryHash);
    }
}