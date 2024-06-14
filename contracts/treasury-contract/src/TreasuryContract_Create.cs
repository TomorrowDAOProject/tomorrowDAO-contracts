using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Treasury;

public partial class TreasuryContract
{
    public override Empty CreateTreasury(CreateTreasuryInput input)
    {
        AssertNotNullOrEmpty(input);
        AssertDaoSubsists(input.DaoId);
        var daoInfo = AssertSenderIsDaoContractOrDaoAdmin(input.DaoId);
        Assert(!string.IsNullOrWhiteSpace(daoInfo.GovernanceToken),
            "Governance token is not set up yet, cannot create treasury.");
        var treasuryInfo = State.TreasuryInfoMap[input.DaoId];
        Assert(treasuryInfo == null, $"Dao {input.DaoId} treasury has been created.");

        var treasuryHash = GenerateTreasuryHash(input.DaoId, Context.Self);
        var treasuryAddress = GenerateTreasuryAddress(treasuryHash);

        var daoId = State.TreasuryAccountMap[treasuryAddress];
        Assert(daoId == null || daoId == Hash.Empty, "Treasury address already exists.");

        treasuryInfo = new TreasuryInfo
        {
            TreasuryAddress = treasuryAddress
        };
        State.TreasuryInfoMap[input.DaoId] = treasuryInfo;
        State.TreasuryAccountMap[treasuryAddress] = input.DaoId;

        Context.Fire(new TreasuryCreated
        {
            DaoId = input.DaoId,
            TreasuryAccountAddress = treasuryAddress
        });
        return new Empty();
    }
}