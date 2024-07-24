namespace TomorrowDAO.Contracts.Governance;

public static class GovernanceContractConstants
{
    public const int AbstractVoteTotal = 10000;
    public const int DefaultActiveTimePeriod = 7 * 24;
    public const int MinActiveTimePeriod = 1;
    public const int MaxActiveTimePeriod = 15 * 24;
    public const int MinPendingTimePeriod = 5 * 24;
    public const int MaxPendingTimePeriod = 7 * 24;
    public const int MinExecuteTimePeriod = 3 * 24;
    public const int MaxExecuteTimePeriod = 5 * 24;
    public const int DefaultVetoActiveTimePeriod = 3 * 24;
    public const int MinVetoActiveTimePeriod = 1;
    public const int MaxVetoActiveTimePeriod = 5 * 24;
    public const int MinVetoExecuteTimePeriod = 1 * 24;
    public const int MaxVetoExecuteTimePeriod = 3 * 24;
    public const int MaxProposalDescriptionUrlLength = 256;
    
    public const int MemoMaxLength = 64;
    public const string TransferMethodName = "Transfer";
}