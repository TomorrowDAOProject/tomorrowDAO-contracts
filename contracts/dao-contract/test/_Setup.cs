using AElf;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAO.Contracts.Election;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAO.Contracts.Treasury;
using TomorrowDAO.Contracts.Vote;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace TomorrowDAO.Contracts.DAO;

public class Module : AElf.Testing.TestBase.ContractTestModule<DAOContract>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        context.Services.AddSingleton<IRefBlockInfoProvider, RefBlockInfoProvider>();
        context.Services.AddSingleton<ITestTransactionExecutor, TestTransactionExecutor>();
        context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProvider>();
        context.Services.AddSingleton<IContractTesterFactory, ContractTesterFactory>();

        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
    }
}

public class TestBase : AElf.Testing.TestBase.ContractTestBase<Module>
{
    internal ACS0Container.ACS0Stub GenesisContractStub;
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal DAOContractContainer.DAOContractStub DAOContractStub;
    internal DAOContractContainer.DAOContractStub UserDAOContractStub;
    internal DAOContractContainer.DAOContractStub OtherDAOContractStub;
    internal GovernanceContractContainer.GovernanceContractStub GovernanceContractStub;
    internal ElectionContractContainer.ElectionContractStub ElectionContractStub;
    internal VoteContractContainer.VoteContractStub VoteContractStub;
    internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub;

    internal ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    internal Address DefaultAddress => Accounts[0].Address;

    internal ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    internal Address UserAddress => Accounts[1].Address;

    internal Address ReferendumAddress => Accounts[10].Address;
    internal Address HighCouncilAddress => Accounts[11].Address;
    internal ECKeyPair OtherKeyPair => Accounts[12].KeyPair;
    internal Address OtherAddress => Accounts[12].Address;

    internal Address DAOContractAddress { get; set; }
    internal Address GovernanceContractAddress { get; set; }
    internal Address ElectionContractAddress { get; set; }
    internal Address VoteContractAddress { get; set; }
    
    internal Address TreasuryContractAddress { get; set; }
    
    internal IBlockTimeProvider BlockTimeProvider;

    public TestBase()
    {
        GenesisContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);
        TokenContractStub =
            GetContractStub<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);

        BlockTimeProvider = Application.ServiceProvider.GetService<IBlockTimeProvider>();
        
        // deploy dao contract
        var code = System.IO.File.ReadAllBytes(typeof(DAOContract).Assembly.Location);
        var contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("dao"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        var result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(code),
                ContractOperation = contractOperation
            }));

        DAOContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        // deploy test governance contract
        code = System.IO.File.ReadAllBytes(typeof(GovernanceContract).Assembly.Location);
        contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("test_governance"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(code),
                ContractOperation = contractOperation
            }));

        GovernanceContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);


        // deploy election contract
        code = System.IO.File.ReadAllBytes(typeof(ElectionContract).Assembly.Location);
        contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("election"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(code),
                ContractOperation = contractOperation
            }));
        ElectionContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        // deploy vote contract
        code = System.IO.File.ReadAllBytes(typeof(VoteContract).Assembly.Location);
        contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("vote"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(code),
                ContractOperation = contractOperation
            }));
        VoteContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);
        
        // deploy test treasury contract
        code = System.IO.File.ReadAllBytes(typeof(TreasuryContract).Assembly.Location);
        contractOperation = new ContractOperation
        {
            ChainId = 9992731,
            CodeHash = HashHelper.ComputeFrom(code),
            Deployer = DefaultAddress,
            Salt = HashHelper.ComputeFrom("treasury"),
            Version = 1
        };
        contractOperation.Signature = GenerateContractSignature(DefaultKeyPair.PrivateKey, contractOperation);

        result = AsyncHelper.RunSync(async () => await GenesisContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(code),
                ContractOperation = contractOperation
            }));

        TreasuryContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);


        DAOContractStub = GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, DefaultKeyPair);
        UserDAOContractStub =
            GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, UserKeyPair);
        OtherDAOContractStub = GetContractStub<DAOContractContainer.DAOContractStub>(DAOContractAddress, OtherKeyPair);
        GovernanceContractStub =
            GetContractStub<GovernanceContractContainer.GovernanceContractStub>(GovernanceContractAddress,
                DefaultKeyPair);
        ElectionContractStub = GetContractStub<ElectionContractContainer.ElectionContractStub>(
            ElectionContractAddress,
            DefaultKeyPair);
        VoteContractStub = GetContractStub<VoteContractContainer.VoteContractStub>(
            VoteContractAddress,
            DefaultKeyPair);
        TreasuryContractStub = GetContractStub<TreasuryContractContainer.TreasuryContractStub>(
            TreasuryContractAddress,
            DefaultKeyPair);
    }

    private T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair)
        where T : ContractStubBase, new()
    {
        var contractTesterFactory = this.Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
        return contractTesterFactory.Create<T>(contractAddress, senderKeyPair);
        // return GetTester<T>(contractAddress, senderKeyPair);
    }

    private ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
    {
        var dataHash = HashHelper.ComputeFrom(contractOperation);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}