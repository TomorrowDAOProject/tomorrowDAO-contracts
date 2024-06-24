using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }

    // DAO id -> DAOInfo
    public MappedState<Hash, DAOInfo> DAOInfoMap { get; set; }

    public MappedState<Hash, Address> ReferendumAddressMap { get; set; }
    
    public MappedState<Hash, Address> OrganizationAddressMap { get; set; }

    public MappedState<string, Hash> DAONameMap { get; set; }

    // DAO id -> Metadata
    public MappedState<Hash, Metadata> MetadataMap { get; set; }

    // high council
    public MappedState<Hash, bool> HighCouncilEnabledStatusMap { get; set; }
    public MappedState<Hash, Address> HighCouncilAddressMap { get; set; }

    // file
    // DAO id -> FileInfoList
    public MappedState<Hash, FileInfoList> FilesMap { get; set; }

    // permission
    // DAO id -> PermissionHash -> PermissionType
    public MappedState<Hash, Hash, PermissionType> PermissionTypeMap { get; set; }
    
    public MappedState<Hash, AddressList> OrganizationMemberMap { get; set; }
}