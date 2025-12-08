using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Treasury;

public class TreasuryContractTestsFundsTransfer : TreasuryContractTestsBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TreasuryContractTestsFundsTransfer(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task TransferTest()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        long deposit = OneElfAmount * 10;
        await CreateTreasuryAddDonateAndStaking(daoId, deposit);

        var amount = OneElfAmount * 5;
        var proposalId = await RequestTransferAndVote(daoId, amount);

        //add 7d
        BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000);
        var result = await GovernanceContractStub.ExecuteProposal.SendAsync(proposalId);

        var treasuryTransferred = GetLogEvent<TreasuryTransferred>(result.TransactionResult);
        treasuryTransferred.ShouldNotBeNull();
        treasuryTransferred.Amount.ShouldBe(amount);
        treasuryTransferred.ProposalId.ShouldBe(proposalId);
        var treasuryAddress = treasuryTransferred.TreasuryAddress;

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = DefaultGovernanceToken,
            Owner = UserAddress
        });
        balance.Balance.ShouldBe(deposit - amount);
    }
    
    [Fact]
    public async Task TransferTest_Expire()
    {
        await InitializeAllContract();
        var daoId = await MockDao();
        long deposit = OneElfAmount * 10;
        await CreateTreasuryAddDonateAndStaking(daoId, deposit);

        var amount = OneElfAmount * 5;
        var proposalId = await RequestTransferAndVote(daoId, amount, false);

        //add 7d
        BlockTimeProvider.SetBlockTime(3600 * 24 * 7 * 1000);
        var result = await GovernanceContractStub.ExecuteProposal.SendWithExceptionAsync(proposalId);
        result.ShouldNotBeNull();
        result.TransactionResult.Error.ShouldContain(" The proposal is in active or expired");
    }
}