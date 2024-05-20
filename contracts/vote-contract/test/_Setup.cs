﻿using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Standards.ACS0;
using AElf.Testing.TestBase;
using AElf.Types;
using Google.Protobuf;
using TomorrowDAO.Contracts.DAO;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace TomorrowDAO.Contracts.Vote
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<VoteContract>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        internal ACS0Container.ACS0Stub GenesisContractStub;
        public static Address VoteContractAddress { get; set; }
        internal VoteContractContainer.VoteContractStub VoteContractStub;
        internal VoteContractContainer.VoteContractStub VoteContractStubOther;
        internal Address GovernanceContractAddress { get; set; }
        internal GovernanceContractContainer.GovernanceContractStub GovernanceContractStub;
        internal Address DAOContractAddress { get; set; }
        internal DAOContractContainer.DAOContractStub DAOContractStub;
        internal Address ElectionContractAddress { get; set; }
        internal ElectionContractContainer.ElectionContractStub ElectionContractStub;
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        protected Address DefaultAddress => Accounts[0].Address;
        internal ECKeyPair UserKeyPair => Accounts[1].KeyPair;
        internal Address UserAddress => Accounts[1].Address;

        protected TestBase()
        {
            GenesisContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);
            DeployVoteContract();
            DeployGovernanceContract();
            DeployDaoContract();
            DeployElectionContract();
        }

        private T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair) where T : ContractStubBase, new()
        {
            return GetTester<T>(contractAddress, senderKeyPair);
        }
        
        private ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
        {
            var dataHash = HashHelper.ComputeFrom(contractOperation);
            var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
            return ByteStringHelper.FromHexString(signature.ToHex());
        }

        private ContractOperation GetContractOperation(byte[] code, string saltStr)
        {
            return new ContractOperation
            {
                ChainId = 9992731,
                CodeHash = HashHelper.ComputeFrom(code),
                Deployer = DefaultAddress,
                Salt = HashHelper.ComputeFrom(saltStr),
                Version = 1
            };
        }

        private void DeployVoteContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(VoteContract).Assembly.Location);
            var contractOperation = GetContractOperation(System.IO.File.ReadAllBytes(typeof(VoteContract).Assembly.Location), "vote");
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));
            VoteContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            VoteContractStub = GetContractStub<VoteContractContainer.VoteContractStub>(VoteContractAddress, DefaultKeyPair);
            VoteContractStubOther = GetContractStub<VoteContractContainer.VoteContractStub>(VoteContractAddress, UserKeyPair);
        }
        
        private void DeployGovernanceContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(GovernanceContract).Assembly.Location);
            var contractOperation = GetContractOperation(System.IO.File.ReadAllBytes(typeof(GovernanceContract).Assembly.Location), "governance");
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));
            GovernanceContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            GovernanceContractStub = GetContractStub<GovernanceContractContainer.GovernanceContractStub>(GovernanceContractAddress, DefaultKeyPair);
        }

        private void DeployDaoContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(DAOContract).Assembly.Location);
            var contractOperation = GetContractOperation(System.IO.File.ReadAllBytes(typeof(VoteContract).Assembly.Location), "dao");
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));
            DAOContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            DAOContractStub = GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, DefaultKeyPair);
        }
        
        private void DeployElectionContract()
        {
            var code = System.IO.File.ReadAllBytes(typeof(ElectionContract).Assembly.Location);
            var contractOperation = GetContractOperation(System.IO.File.ReadAllBytes(typeof(ElectionContract).Assembly.Location), "election");
            contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);
            var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
                new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(code),
                    ContractOperation = contractOperation
                }));
            ElectionContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
            ElectionContractStub = GetContractStub<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, DefaultKeyPair);
        }
    }
}