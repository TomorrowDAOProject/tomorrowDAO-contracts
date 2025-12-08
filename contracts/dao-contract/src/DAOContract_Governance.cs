using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using TomorrowDAO.Contracts.Governance;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private void ProcessReferendumGovernanceMechanism(Hash daoId, GovernanceSchemeThreshold threshold)
    {
        Assert(threshold != null, "Invalid input governance scheme threshold.");

        var daoInfo = State.DAOInfoMap[daoId];
        Assert(daoInfo != null, "Dao information not found.");

        var governanceSchemeThreshold = ConvertToGovernanceSchemeThreshold(threshold);
        governanceSchemeThreshold.ProposalThreshold = daoInfo!.ProposalThreshold;
        State.GovernanceContract.AddGovernanceScheme.Send(new AddGovernanceSchemeInput
        {
            DaoId = daoId,
            GovernanceMechanism = TomorrowDAO.Contracts.Governance.GovernanceMechanism.Referendum,
            SchemeThreshold = governanceSchemeThreshold,
            GovernanceToken = daoInfo.GovernanceToken,
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

        var daoInfo = State.DAOInfoMap[input.DaoId];
        var schemeThreshold = ConvertToGovernanceSchemeThreshold(input.SchemeThreshold);
        schemeThreshold.ProposalThreshold = daoInfo.ProposalThreshold;
        
        State.GovernanceContract.UpdateGovernanceSchemeThreshold.Send(new Governance.UpdateGovernanceSchemeThresholdInput
        {
            DaoId = input.DaoId,
            SchemeAddress = input.SchemeAddress,
            SchemeThreshold = schemeThreshold
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

    public override Empty SetGovernanceToken(SetGovernanceTokenInput input)
    {
        Assert(input != null, "Invalid input.");
        CheckDAOExistsAndSubsist(input!.DaoId);
        AssertPermission(input.DaoId, nameof(SetGovernanceToken));
        AssertToken(input.GovernanceToken);
        var daoInfo = State.DAOInfoMap[input.DaoId];
        daoInfo.GovernanceToken = input.GovernanceToken;
        daoInfo.ProposalThreshold = input.ProposalThreshold;
        State.DAOInfoMap[input.DaoId] = daoInfo;
        
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