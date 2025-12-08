using AElf.Cryptography.ECDSA;
using AElf.Testing.TestBase;

namespace TomorrowDAO.Contracts.Timelock
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<TimelockContract>
    {
        
    }
    
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly TimelockContractContainer.TimelockContractStub TimelockContractStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            TimelockContractStub = GetTimelockContractStub(DefaultKeyPair);
        }

        private TimelockContractContainer.TimelockContractStub GetTimelockContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TimelockContractContainer.TimelockContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}