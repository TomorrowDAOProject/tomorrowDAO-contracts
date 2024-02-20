using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Timelock
{
    public partial class TimelockContract : TimelockContractContainer.TimelockContractBase
    {
        public override Empty Initialized(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            
            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            var author = State.GenesisContract.GetContractAuthor.Call(Context.Self);
            Assert(author == Context.Sender, "No permission.");
            Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid admin");
            
            var admin = input.Admin == null ? Context.Sender : input.Admin;
            
            State.Initialized.Value = true;
            State.Admin.Value = admin;
            return new Empty();
        }

        public override Address GetAdmin(Empty input)
        {
            AssertInitialized();
            return State.Admin.Value;
        }

        public override Hash QueueOperation(OperationInput input)
        {
            AssertInitialized();
            Assert(VerifySignature(input), "Invalid signature");

            var hash = HashHelper.ComputeFrom(input);
            Assert(State.OperationQueue[hash] == null, "Operation exists");
            Assert(input.Delay <= TimelockContractConstants.MAX_DELAY, "Delay must not exceed maximum delay");
            Assert(input.Delay >= TimelockContractConstants.MIN_DELAY, "Delay must exceed minimum delay");

            State.OperationQueue[hash] = input;
            State.OperationTime[hash] = Context.CurrentBlockTime.AddSeconds(input.Delay);

            Context.Fire(new OperationQueued
            {
                TxHash = hash,
                Target = input.Target,
                Method = input.Method,
                Param = input.Param,
                QueueTime = State.OperationTime[hash]
            });

            return hash;
        }

        public override Empty ExecuteOperation(OperationInput input)
        {
            AssertInitialized();
            Assert(VerifySignature(input), "Invalid signature");

            var operationHash = HashHelper.ComputeFrom(input);
            Assert(State.OperationExecuteTime[operationHash] != null, "Operation executed");

            var queuedOperation = State.OperationQueue[operationHash];
            Assert(queuedOperation != null, "Queued operation not found");
            Assert(State.OperationTime[operationHash] < Context.CurrentBlockTime, "Not up to operation time");

            Context.SendInline(queuedOperation!.Target, queuedOperation.Method, queuedOperation.Param);
            State.OperationQueue.Remove(operationHash);
            State.OperationTime.Remove(operationHash);
            State.OperationExecuteTime[operationHash] = Context.CurrentBlockTime;

            Context.Fire(new OperationExecuted
            {
                TxHash = Context.TransactionId,
                Target = queuedOperation.Target,
                Method = queuedOperation.Method,
                Param = queuedOperation.Param,
                ExecuteTime = Context.CurrentBlockTime
            });
            return new Empty();
        }

        public override Empty CancelOperation(OperationInput input)
        {
            AssertInitialized();
            AssertAdmin();

            var operationHash = HashHelper.ComputeFrom(input);
            Assert(State.OperationQueue[operationHash] != null, "Operation hasn't been queued");

            var operationTime = State.OperationTime[operationHash];
            Assert(operationTime != null, "Queued time not exists");
            Assert(operationTime < Context.CurrentBlockTime, "Queued time not reached");

            return new Empty();
        }

        public override GetOperationStatusOutput GetOperationStatus(Hash input)
        {
            AssertInitialized();
            Assert(input.Value.IsNullOrEmpty(), "Invalid input");

            var operationTime = State.OperationTime[input];
            var operationState = State.OperationExecuteTime[input] != null ? OperationState.Done :
                operationTime == null ? OperationState.Unset :
                operationTime > Context.CurrentBlockTime ? OperationState.Waiting : OperationState.Ready;
            Assert(operationState != OperationState.Unset, "Operation not exists");
            
            return new GetOperationStatusOutput
            {
                State = operationState
            };
        }
    }
}