using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }

    // DAO id -> DAOInfo
    public MappedState<Hash, DAOInfo> DAOInfoMap { get; set; }

    public MappedState<Hash, Address> ReferendumAddressMap { get; set; }

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

    //Treasury
    //<treasury -> symbol -> FundInfo>
    public MappedState<Address, string, FundInfo> FundInfoMap { get; set; }

    //<symbol -> FundInfo>
    public MappedState<string, FundInfo> TotalFundInfoMap { get; set; }

    //<TreasuryAccountAddress -> DAO id>
    public MappedState<Address, Hash> TreasuryAccountMap { get; set; }
}