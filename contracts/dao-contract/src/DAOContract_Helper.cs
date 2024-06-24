using System.Linq;
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

    private bool IsValidTokenChar(char character)
    {
        return character >= 'A' && character <= 'Z';
    }
    
    private TokenInfo AssertToken(string token)
    {
        Assert(!string.IsNullOrEmpty(token), "Token is null.");
        Assert(token!.Length <= DAOContractConstants.SymbolMaxLength && token.All(IsValidTokenChar), "Invalid token symbol.");
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
            ProposalThreshold = input.ProposalThreshold
        };
    }
    
    private Governance.GovernanceSchemeThreshold ConvertToOrganizationGovernanceSchemeThreshold(GovernanceSchemeThreshold input)
    {
        return new Governance.GovernanceSchemeThreshold
        {
            MinimalVoteThreshold = input.MinimalVoteThreshold,
            MinimalRequiredThreshold = input.MinimalVoteThreshold,
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