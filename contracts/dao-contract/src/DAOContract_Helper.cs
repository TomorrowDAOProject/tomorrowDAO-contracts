using System.Linq;
using System.Text.RegularExpressions;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private void CheckInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsStringValid(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    private void CheckDAOExists(Hash input)
    {
        Assert(IsHashValid(input), "Invalid input dao id.");
        Assert(State.DAOInfoMap[input] != null, "DAO not existed.");
    }

    private void CheckDaoSubsistStatus(Hash input)
    {
        Assert(State.DAOInfoMap[input].SubsistStatus, "DAO not subsisted.");
    }

    private void CheckDAOExistsAndSubsist(Hash input)
    {
        CheckDAOExists(input);
        CheckDaoSubsistStatus(input);
    }

    private bool IsValidTokenChar(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
    }
    
    private bool IsValidItemId(char c)
    {
        return c >= '0' && c <= '9';
    }
    
    private TokenInfo AssertToken(string token)
    {
        //token
        Assert(!string.IsNullOrEmpty(token), "Token is null.");
        var words = token!.Split(DAOContractConstants.NFTSymbolSeparator);
        Assert(words[0].Length > 0 && words[0].All(IsValidTokenChar), "Invalid Symbol input");
        
        //NFT
        if (words.Length > 1)
        {
            Assert(words.Length == 2 && words[1].Length > 0 && words[1].All(IsValidItemId), "Invalid NFT Symbol input");
            //cannot be an NFT Collection
            Assert(words[1] != DAOContractConstants.CollectionSymbolSuffix, "NFT Collection is not supported.");
        }
        
        var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = token
        });
        Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), "Token not found.");
        return tokenInfo;
    }

    private Governance.GovernanceSchemeThreshold ConvertToGovernanceSchemeThreshold(GovernanceSchemeThreshold input)
    {
        return new Governance.GovernanceSchemeThreshold
        {
            MinimalVoteThreshold = input.MinimalVoteThreshold,
            MinimalRequiredThreshold = input.MinimalRequiredThreshold,
            MinimalApproveThreshold = input.MinimalApproveThreshold,
            MaximalAbstentionThreshold = input.MaximalAbstentionThreshold,
            MaximalRejectionThreshold = input.MaximalRejectionThreshold,
        };
    }
    
    private Governance.GovernanceSchemeThreshold ConvertToOrganizationGovernanceSchemeThreshold(GovernanceSchemeThreshold input)
    {
        return new Governance.GovernanceSchemeThreshold
        {
            MinimalVoteThreshold = 0,
            MinimalRequiredThreshold = input.MinimalRequiredThreshold,
            MinimalApproveThreshold = input.MinimalApproveThreshold,
            MaximalAbstentionThreshold = input.MaximalAbstentionThreshold,
            MaximalRejectionThreshold = input.MaximalRejectionThreshold,
            ProposalThreshold = 0
        };
    }
    
    private Governance.DaoProposalTimePeriod ConvertToProposalTimePeriod(DaoProposalTimePeriod input)
    {
        return new Governance.DaoProposalTimePeriod
        {
            ActiveTimePeriod = input.ActiveTimePeriod,
            VetoActiveTimePeriod = input.VetoActiveTimePeriod,
            PendingTimePeriod = input.PendingTimePeriod,
            ExecuteTimePeriod = input.ExecuteTimePeriod,
            VetoExecuteTimePeriod = input.VetoExecuteTimePeriod,
        };
    }
}