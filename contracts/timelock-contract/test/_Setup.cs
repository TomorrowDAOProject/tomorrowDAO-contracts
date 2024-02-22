using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;

namespace TomorrowDAO.Contracts.Timelock
{
    public class Module : AElf.Testing.TestBase.ContractTestModule<TimelockContract>
    {
        
    }
    
    public class TestBase : AElf.Testing.TestBase.ContractTestBase<Module>
    {
        
        internal Account Admin => Accounts[0];
        internal Account User1 => Accounts[1];
        internal Account User2 => Accounts[2];
        internal Account User3 => Accounts[3];

        internal readonly TimelockContractContainer.TimelockContractStub AdminTimelockStub;
        internal readonly TimelockContractContainer.TimelockContractStub User1TimelockStub;
        internal readonly TimelockContractContainer.TimelockContractStub User2TimelockStub;
        internal readonly TimelockContractContainer.TimelockContractStub User3TimelockStub;
        
        public TestBase()
        {
            AdminTimelockStub = GetTimelockContractStub(Admin.KeyPair);
            User1TimelockStub = GetTimelockContractStub(User1.KeyPair);
            User2TimelockStub = GetTimelockContractStub(User2.KeyPair);
            User3TimelockStub = GetTimelockContractStub(User3.KeyPair);
        }

        private TimelockContractContainer.TimelockContractStub GetTimelockContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TimelockContractContainer.TimelockContractStub>(ContractAddress, senderKeyPair);
        }
    }
    
}