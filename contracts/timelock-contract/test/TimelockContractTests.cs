using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.CSharp.Core.Extension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Timelock
{
    public partial class TimelockContractTests : TestBase
    {
        private readonly IBlockTimeProvider _blockTimeProvider;

        public TimelockContractTests()
        {
            _blockTimeProvider = ServiceProvider.GetService<IBlockTimeProvider>();
        }

        [Fact]
        public async Task InitializeTest()
        {
            await AdminTimelockStub.Initialize.SendAsync(new InitializeInput());
            var admin = await AdminTimelockStub.GetAdmin.CallAsync(new Empty());
            admin.ShouldBe(Admin.Address);
        }


        [Fact]
        public async Task<OperationInput> EnQueueTest()
        {
            await InitializeTest();

            // blockTime
            var blockTime = _blockTimeProvider.GetBlockTime();

            // build and sign
            var operationInput = new OperationInput
            {
                Target = ContractAddress,
                Method = "GetAdmin",
                Param = new Empty().ToByteString(),
                Delay = 24 * 3600,
            };
            operationInput.Signature =
                ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(Admin.KeyPair.PrivateKey,
                    HashHelper.ComputeFrom(operationInput.ToByteArray()).ToByteArray()));

            // enqueue
            var enqueueResult = await AdminTimelockStub.QueueOperation.SendAsync(operationInput);
            var queuedLog =
                enqueueResult.TransactionResult.Logs.FirstOrDefault(log => log.Name == nameof(OperationQueued));
            queuedLog.ShouldNotBeNull();
            var operationQueued = OperationQueued.Parser.ParseFrom(queuedLog.NonIndexed.ToByteArray());
            operationQueued.OperationHash.ShouldNotBeNull();
            operationQueued.Target.ShouldBe(ContractAddress);
            operationQueued.Method.ShouldBe(operationInput.Method);
            operationQueued.Param.ShouldBe(operationInput.Param);
            operationQueued.QueueTime.ShouldBe(blockTime.AddSeconds(operationInput.Delay));

            // operation should be waiting
            var getOperationOutput =
                await AdminTimelockStub.GetOperationStatus.CallAsync(operationQueued.OperationHash);
            getOperationOutput.ShouldNotBeNull();
            getOperationOutput.State.ShouldBe(OperationState.Waiting);

            // set time two days after
            _blockTimeProvider.SetBlockTime(blockTime.AddDays(2));

            // operation should be ready
            getOperationOutput = await AdminTimelockStub.GetOperationStatus.CallAsync(operationQueued.OperationHash);
            getOperationOutput.ShouldNotBeNull();
            getOperationOutput.State.ShouldBe(OperationState.Ready);

            return operationInput;
        }


        [Fact]
        public async Task ExecuteTest()
        {
            var operationInput = await EnQueueTest();
            var hash = HashHelper.ComputeFrom(operationInput.ToByteArray());

            var executeResult = await AdminTimelockStub.ExecuteOperation.SendAsync(operationInput);
            var executeLog =
                executeResult.TransactionResult.Logs.FirstOrDefault(log => log.Name == nameof(OperationExecuted));
            executeLog.ShouldNotBeNull();

            var operationExecuted = OperationExecuted.Parser.ParseFrom(executeLog.NonIndexed);
            operationExecuted.OperationHash.ShouldBe(hash);
            operationExecuted.Method.ShouldBe(operationInput.Method);
            operationExecuted.Param.ShouldBe(operationInput.Param);
            operationExecuted.ExecuteTime.ShouldBe(_blockTimeProvider.GetBlockTime());

            var operationState = await AdminTimelockStub.GetOperationStatus.CallAsync(hash);
            operationState.ShouldNotBeNull();
            operationState.State.ShouldBe(OperationState.Done);
        }


        [Fact]
        public async Task CancelTest()
        {
            var operationInput = await EnQueueTest();
            var hash = HashHelper.ComputeFrom(operationInput.ToByteArray());

            var cancelResult = await AdminTimelockStub.CancelOperation.SendAsync(operationInput);
            var canceledLog =
                cancelResult.TransactionResult.Logs.FirstOrDefault(log => log.Name == nameof(OperationCanceled));
            canceledLog.ShouldNotBeNull();

            var operationCanceled = OperationCanceled.Parser.ParseFrom(canceledLog.NonIndexed);
            operationCanceled.OperationHash.ShouldBe(hash);
            operationCanceled.Target.ShouldBe(operationInput.Target);
            operationCanceled.Method.ShouldBe(operationInput.Method);
            operationCanceled.Param.ShouldBe(operationInput.Param);
            operationCanceled.CancelTime.ShouldBe(_blockTimeProvider.GetBlockTime());

            var operationState = await AdminTimelockStub.GetOperationStatus.CallAsync(hash);
            operationState.ShouldNotBeNull();
            operationState.State.ShouldBe(OperationState.NotExists);
        }
    }
}