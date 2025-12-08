namespace TomorrowDAO.Contracts.DAO;

public static class DAOContractConstants
{
    // metadata
    public const int NameMaxLength = 50;
    public const int LogoUrlMaxLength = 256;
    public const int DescriptionMaxLength = 240;

    // social media
    public const int SocialMediaListMaxCount = 20;
    public const int SocialMediaNameMaxLength = 16;
    public const int SocialMediaUrlMaxLength = 64;

    // governance token
    public const char NFTSymbolSeparator = '-';
    public const string CollectionSymbolSuffix = "0";

    // file
    public const int FileMaxCount = 20;
    public const int FileCidMaxLength = 64;
    public const int FileNameMaxLength = 128;
    public const int FileUrlMaxLength = 256;

    // permission
    public const string UploadFileInfos = "uploadfileinfos";
    public const string RemoveFileInfos = "removefileinfos";
    public const string UpdateMetadata = "updatemetadata";
    
    // organization
    public const int OnceOrganizationMemberMaxCount = 10000;
}