using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private void ProcessReferendumGovernanceMechanism(Hash daoId, GovernanceSchemeThreshold threshold)
    {
        Assert(threshold != null, "Invalid input governance scheme threshold.");

        var governanceSchemeThreshold = ConvertToGovernanceSchemeThreshold(threshold);
        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Referendum,
            SchemeThreshold = governanceSchemeThreshold,
            GovernanceToken = State.DAOInfoMap[daoId].GovernanceToken,
        });

        State.ReferendumAddressMap[daoId] = State.GovernanceContract.CalculateGovernanceSchemeAddress.Call(
            new CalculateGovernanceSchemeAddressInput
            {
                DaoId = daoId,
                GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Referendum
            });
    }

    public override Empty UpdateGovernanceSchemeThreshold(UpdateGovernanceSchemeThresholdInput input)
    {
        Assert(input is { SchemeThreshold: not null }, "Invalid input.");
        CheckDAOExistsAndSubsist(input.DaoId);
        AssertPermission(input.DaoId, nameof(UpdateGovernanceSchemeThreshold));
        
        State.GovernanceContract.UpdateGovernanceSchemeThreshold.Send(new Governance.UpdateGovernanceSchemeThresholdInput
        {
            DaoId = input.DaoId,
            SchemeAddress = input.SchemeAddress,
            SchemeThreshold = ConvertToGovernanceSchemeThreshold(input.SchemeThreshold)
        });
        return new Empty();
    }

    public override Empty RemoveGovernanceScheme(RemoveGovernanceSchemeInput input)
    {
        Assert(input != null, "Invalid input.");
        CheckDAOExistsAndSubsist(input!.DaoId);
        AssertPermission(input.DaoId, nameof(RemoveGovernanceScheme));
        
        State.GovernanceContract.RemoveGovernanceScheme.Send(new Governance.RemoveGovernanceSchemeInput
        {
            SchemeAddress = input.SchemeAddress,
            DaoId = input.DaoId
        });
        return new Empty();
    }

    // todo can hc support governance token change?
    public override Empty SetGovernanceToken(SetGovernanceTokenInput input)
    {
        Assert(input != null, "Invalid input.");
        CheckDAOExistsAndSubsist(input!.DaoId);
        AssertPermission(input.DaoId, nameof(SetGovernanceToken));
        AssertToken(input.GovernanceToken);
        State.DAOInfoMap[input.DaoId].GovernanceToken = input.GovernanceToken;
        
        State.GovernanceContract.SetGovernanceToken.Send(new Governance.SetGovernanceTokenInput
        {
            GovernanceToken = input.GovernanceToken,
            DaoId = input.DaoId
        });
        return new Empty();
    }

    public override Empty SetProposalTimePeriod(SetProposalTimePeriodInput input)
    {
        Assert(input is { ProposalTimePeriod: not null }, "Invalid input.");
        CheckDAOExistsAndSubsist(input!.DaoId);
        AssertPermission(input.DaoId, nameof(SetProposalTimePeriod));
        
        State.GovernanceContract.SetProposalTimePeriod.Send(new Governance.SetProposalTimePeriodInput
        {
            DaoId = input.DaoId,
            ProposalTimePeriod = ConvertToProposalTimePeriod(input.ProposalTimePeriod)
        });
        return new Empty();
    }
}