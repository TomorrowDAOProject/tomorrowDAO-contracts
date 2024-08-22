using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAO.Contracts.Election.Protobuf;

public class IssueTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public IssueTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public Task GenerateIssueInputByteString()
    {
        var input = new IssueInput
        {
            Symbol = "AGENTSS",
            Amount = 100000000000,
            Memo = "IssueTesting",
            To = Address.FromBase58("2fgbLE3pTghxVLmo5iR63pm2sZYe3vkphCG1Sungg3ed9sdsaQ")
        };
        _testOutputHelper.WriteLine("IssueInput={0}", input.ToByteString().ToBase64());
        return Task.CompletedTask;
    }
    
    [Fact]
    public Task GenerateIssueInputByteStringA()
    {
        var input = new IssueInput
        {
            Symbol = "AGENT",
            Amount = 1000000000000,
            Memo = "IssueTesting",
            To = Address.FromBase58("2fgbLE3pTghxVLmo5iR63pm2sZYe3vkphCG1Sungg3ed9sdsaQ")
        };
        _testOutputHelper.WriteLine("IssueInput={0}", input.ToByteString().ToBase64());
        return Task.CompletedTask;
    }
    
    [Fact]
    public Task GenerateIssueInputByteStringB()
    {
        var input = new IssueInput
        {
            Symbol = "TESTAGENTS",
            Amount = 1000000000000,
            Memo = "IssueTesting",
            To = Address.FromBase58("w1s8hUonC8E1v2SkaNMnjwcRadw7xD8EythpnYUABFf367XJu")
        };
        _testOutputHelper.WriteLine("IssueInput={0}", input.ToByteString().ToBase64());
        return Task.CompletedTask;
    }
    
    
}