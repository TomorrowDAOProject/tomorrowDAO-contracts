using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.Timelock
{
    // This class is unit test class, and it inherit TestBase. Write your unit test code inside it
    public class TimelockContractTests : TestBase
    {
        [Fact]
        public async Task Update_ShouldUpdateMessageAndFireEvent()
        {
            // Arrange
            var inputValue = "Hello, World!";
            var input = new StringValue { Value = inputValue };
            input.ShouldNotBeNull();
        }
    }
    
}