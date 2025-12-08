using System.Linq;
using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Vote;

public partial class VoteContract : VoteContractContainer.VoteContractBase
{
    public override Empty SetEmergencyStatus(SetEmergencyStatusInput input)
    {
        Assert(Context.Sender == State.DaoContract.Value, "No permission.");
        AssertCommon(input);
        AssertDao(input.DaoId);
        Assert(input.IsEnable, "Invalid emergencyStatus isEnable");
        State.EmergencyStatusMap[input.DaoId] = input.IsEnable;
        
        return new Empty();
    }
}