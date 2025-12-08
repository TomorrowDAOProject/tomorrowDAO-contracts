using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private Hash CalculatePermissionHash(Address where, string what)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(where), HashHelper.ComputeFrom(what));
    }

    private void ProcessPermission(Hash daoId, PermissionInfo info, PermissionType type)
    {
        var permissionHash = CalculatePermissionHash(info.Where, info.What);
        State.PermissionTypeMap[daoId][permissionHash] = type;
    }

    private bool IsGranted(Hash daoId, Address where, string what, Address who)
    {
        var permissionHash = CalculatePermissionHash(where, what);
        var permissionType = State.PermissionTypeMap[daoId][permissionHash];

        return permissionType switch
        {
            PermissionType.Default => State.ReferendumAddressMap[daoId] == who ||
                                      State.HighCouncilAddressMap[daoId] == who || 
                                      State.OrganizationAddressMap[daoId] == who,
            PermissionType.Creator => State.DAOInfoMap[daoId].Creator == who,
            _ => false
        };
    }

    private void AssertPermission(Hash daoId, string what)
    {
        Assert(IsGranted(daoId, Context.Self, what.ToLower(), Context.Sender),
            $"Permission of {what} is not granted for {Context.Sender}.");
    }
    
    private void ProcessDefaultPermissions(Hash daoId, List<string> methodNames)
    {
        foreach (var method in methodNames)
        {
            ProcessPermission(daoId, new PermissionInfo
            {
                Where = Context.Self,
                Who = Context.Sender,
                What = method
            }, PermissionType.Creator);
        }
    }
    
    public override Empty AddCreatorPermissions(AddCreatorPermissionsInput input)
    {
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");
        Assert(input is { MethodNames: not null }, "Invalid input.");
        CheckDAOExistsAndSubsist(input.DaoId);
        ProcessDefaultPermissions(input.DaoId, input!.MethodNames!.ToList());
        return new Empty();
    }
}