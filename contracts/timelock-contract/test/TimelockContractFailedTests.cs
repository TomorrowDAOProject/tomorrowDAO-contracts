using System;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.CSharp.Core.Extension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Timelock;

public partial class TimelockContractTests
{
        
    [Fact]
    public async Task EnQueueFailed()
    {
        // build and sign
        var operationInput = new OperationInput
        {
            Target = ContractAddress,
            Method = "GetAdmin",
            Param = new Empty().ToByteString(),
            Delay = 24 * 3600,
        };
        
        // not init
        var notInitReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.QueueOperation.SendAsync(operationInput));
        notInitReason.Message.ShouldContain("Contract not initialized");


        await InitializeTest();
        
        // with error signature
        operationInput.Signature =
            ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(User1.KeyPair.PrivateKey,
                HashHelper.ComputeFrom(operationInput.ToByteArray()).ToByteArray()));
        var invalidSignReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.QueueOperation.SendAsync(operationInput));
        invalidSignReason.Message.ShouldContain("Invalid signature");

        // max_delay exceeded
        operationInput.Delay = TimelockContractConstants.MAX_DELAY + 1;
        operationInput.Signature = ByteString.Empty;
        operationInput.Signature =
            ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(Admin.KeyPair.PrivateKey,
                HashHelper.ComputeFrom(operationInput.ToByteArray()).ToByteArray()));
        var invalidDelayMaxReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.QueueOperation.SendAsync(operationInput));
        invalidDelayMaxReason.Message.ShouldContain("Delay must not exceed maximum delay");
        
        // min_delay not exceeded
        operationInput.Delay = TimelockContractConstants.MIN_DELAY - 1;
        operationInput.Signature = ByteString.Empty;
        operationInput.Signature =
            ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(Admin.KeyPair.PrivateKey,
                HashHelper.ComputeFrom(operationInput.ToByteArray()).ToByteArray()));
        var invalidDelayMinReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.QueueOperation.SendAsync(operationInput));
        invalidDelayMinReason.Message.ShouldContain("Delay must exceed minimum delay");
    }


    [Fact]
    public async Task ExecuteOperationFailed()
    {
        // build and sign
        var operationInput = new OperationInput
        {
            Target = ContractAddress,
            Method = "GetAdmin",
            Param = new Empty().ToByteString(),
            Delay = 24 * 3600,
        };
        
        // not init
        var notInitReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.ExecuteOperation.SendAsync(operationInput));
        notInitReason.Message.ShouldContain("Contract not initialized");
        
        await InitializeTest();

        // with error signature
        var invalidSignReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.ExecuteOperation.SendAsync(operationInput));
        invalidSignReason.Message.ShouldContain("Invalid signature");
        
        // with error signature
        operationInput.Signature = ByteString.CopyFrom(0x00, 0x01);
        var invalidSignReason2 = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.ExecuteOperation.SendAsync(operationInput));
        invalidSignReason2.Message.ShouldContain("Invalid signature");
        
        // not exists
        operationInput.Signature = ByteString.Empty;
        operationInput.Signature =
            ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(Admin.KeyPair.PrivateKey,
                HashHelper.ComputeFrom(operationInput.ToByteArray()).ToByteArray()));
        var notFoundReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.ExecuteOperation.SendAsync(operationInput));
        notFoundReason.Message.ShouldContain("Queued operation not exists");

        await AdminTimelockStub.QueueOperation.SendAsync(operationInput);
        
        // not up to time
        var notUpToTimeReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.ExecuteOperation.SendAsync(operationInput));
        notUpToTimeReason.Message.ShouldContain("Not up to operation time");
        
        // execute success
        _blockTimeProvider.SetBlockTime(_blockTimeProvider.GetBlockTime().AddDays(2));
        await AdminTimelockStub.ExecuteOperation.SendAsync(operationInput);
            
        // not up to time
        var executedReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.ExecuteOperation.SendAsync(operationInput));
        executedReason.Message.ShouldContain("Operation executed");
    }

    [Fact]
    public async Task CancelOperationFailed()
    {
        
        // build and sign
        var operationInput = new OperationInput
        {
            Target = ContractAddress,
            Method = "GetAdmin",
            Param = new Empty().ToByteString(),
            Delay = 24 * 3600,
        };
        
        // not init
        var notInitReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.CancelOperation.SendAsync(operationInput));
        notInitReason.Message.ShouldContain("Contract not initialized");
        
        await InitializeTest();
     
        // no permission
        var noPermissionReason = await Assert.ThrowsAnyAsync<Exception>(() => User1TimelockStub.CancelOperation.SendAsync(operationInput));
        noPermissionReason.Message.ShouldContain("No permission");
        
        // not exists
        var notExistsReason = await Assert.ThrowsAnyAsync<Exception>(() => AdminTimelockStub.CancelOperation.SendAsync(operationInput));
        notExistsReason.Message.ShouldContain("Operation hasn't been queued");
    }
        
}