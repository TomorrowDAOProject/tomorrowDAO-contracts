using AElf.Types;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Address GetTreasuryAccountAddress(Hash input)
    {
        var treasuryInfo = State.TreasuryInfoMap[input];
        return treasuryInfo?.TreasuryAddress;
    }

    public override TreasuryInfo GetTreasuryInfo(Hash input)
    {
        return State.TreasuryInfoMap[input] ?? new TreasuryInfo();
    }

    public override Hash GetDAOIdByTreasuryAccountAddress(Address input)
    {
        var daoId = State.TreasuryAccountMap[input];
        return daoId;
    }
}