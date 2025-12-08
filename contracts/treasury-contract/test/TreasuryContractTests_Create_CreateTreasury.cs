using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsCreateCreateTreasury : TreasuryContractTestsBase
{
    [Fact]
    public async Task CreateTreasuryTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();

        var result = await TreasuryContractStub.CreateTreasury.SendAsync(new CreateTreasuryInput
        {
            DaoId = daoId
        });
        result.ShouldNotBeNull();

        var address = await TreasuryContractStub.GetTreasuryAccountAddress.CallAsync(daoId);
        address.ShouldNotBeNull();

        var treasuryInfo = await TreasuryContractStub.GetTreasuryInfo.CallAsync(daoId);
        treasuryInfo.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateTreasuryTest_Exists()
    {
        await InitializeAllContract();
        var daoId = await MockDao(isTreasuryNeeded: true);

        var result = await TreasuryContractStub.CreateTreasury.SendWithExceptionAsync(new CreateTreasuryInput
        {
            DaoId = daoId
        });
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain("treasury has been created");
    }
}