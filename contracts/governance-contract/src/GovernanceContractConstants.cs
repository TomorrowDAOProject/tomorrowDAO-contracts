namespace TomorrowDAO.Contracts.Governance;

public static class GovernanceContractConstants
{
    public const int AbstractVoteTotal = 10000;
    public const int MinActiveTimePeriod = 7; // days
    public const int MaxActiveTimePeriod = 15; // days
    public const int MinPendingTimePeriod = 5; // days
    public const int MaxPendingTimePeriod = 7; // days
    public const int MinExecuteTimePeriod = 3; // days
    public const int MaxExecuteTimePeriod = 5; // days
    public const int MinVetoActiveTimePeriod = 3; // days
    public const int MaxVetoActiveTimePeriod = 5; // days
    public const int MinVetoExecuteTimePeriod = 1; // days
    public const int MaxVetoExecuteTimePeriod = 3; // days
    public const int MaxProposalDescriptionUrlLength = 256;
    
    public const int MemoMaxLength = 64;
    public const string TransferMethodName = "Transfer";
}