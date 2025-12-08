using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

partial class DAOContractTests
{
    [Fact]
    public async Task SetSubsistStatusTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        {
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }

        // already subsist
        {
            var result = await DAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = true
            });
            result.TransactionResult.Error.ShouldContain("Permission of SetSubsistStatus is not granted");

            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }

        // already subsist
        {
            var result = await CreateProposalAndVote(daoId, new ExecuteTransaction
            {
                ContractMethodName = nameof(DAOContractStub.SetSubsistStatus),
                ToAddress = DAOContractAddress,
                Params = new SetSubsistStatusInput
                {
                    DaoId = daoId,
                    Status = true
                }.ToByteString()
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains("SubsistStatusSet"));
            log.ShouldBeNull();
        }

        {
            var result = await CreateProposalAndVote(daoId, new ExecuteTransaction
            {
                ContractMethodName = nameof(DAOContractStub.SetSubsistStatus),
                ToAddress = DAOContractAddress,
                Params = new SetSubsistStatusInput
                {
                    DaoId = daoId,
                    Status = false
                }.ToByteString()
            });

            var log = GetLogEvent<SubsistStatusSet>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.Status.ShouldBeFalse();

            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeFalse();
        }
    }
    
    [Fact]
    public async Task SetSubsistStatusTests_Fail()
    {
        await InitializeAsync();
        
        {
            var result = await DAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput
            {
                DaoId = HashHelper.ComputeFrom("test"),
                Status = false
            });
            result.TransactionResult.Error.ShouldContain("DAO not existed.");
        }

        var daoId = await CreateDAOAsync();

        {
            var result = await OtherDAOContractStub.SetSubsistStatus.SendWithExceptionAsync(new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = false
            });
            result.TransactionResult.Error.ShouldContain("Permission of SetSubsistStatus is not granted");
        }
    }
}