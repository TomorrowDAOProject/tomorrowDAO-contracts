using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    private void ProcessMetadata(Hash daoId, Metadata metadata)
    {
        Assert(metadata != null, "Invalid metadata.");
        Assert(IsStringValid(metadata.Name) && metadata.Name.Length <= DAOContractConstants.NameMaxLength,
            "Invalid metadata name.");
        Assert(State.DAONameMap[metadata.Name] == null, "DAO name already exists.");
        Assert(IsStringValid(metadata.LogoUrl) && metadata.LogoUrl.Length <= DAOContractConstants.LogoUrlMaxLength,
            "Invalid metadata logo url.");
        Assert(IsStringValid(metadata.Description)
               && metadata.Description.Length <= DAOContractConstants.DescriptionMaxLength,
            "Invalid metadata description.");

        Assert(metadata.SocialMedia?.Count <= DAOContractConstants.SocialMediaListMaxCount,
            "Invalid metadata social media count.");

        if (metadata.SocialMedia?.Count > 0)
        {
            foreach (var socialMedia in metadata.SocialMedia.Keys)
            {
                Assert(
                    IsStringValid(socialMedia) && socialMedia.Length <= DAOContractConstants.SocialMediaNameMaxLength,
                    "Invalid metadata social media name.");
                Assert(
                    IsStringValid(metadata.SocialMedia[socialMedia])
                    && metadata.SocialMedia[socialMedia].Length <= DAOContractConstants.SocialMediaUrlMaxLength,
                    "Invalid metadata social media url.");
            }
        }
        
        State.MetadataMap[daoId] = metadata;
        State.DAONameMap[metadata.Name] = daoId;
    }
    
    public override Empty UpdateMetadata(UpdateMetadataInput input)
    {
        Assert(input != null, "Invalid input.");
        CheckDAOExistsAndSubsist(input.DaoId);
        AssertPermission(input.DaoId, nameof(UpdateMetadata));

        var currentMetadata = State.MetadataMap[input.DaoId];
        var inputMetadata = input.Metadata;
        Assert(string.IsNullOrWhiteSpace(inputMetadata.Name), "Invalid name.");
        
        if (IsStringValid(inputMetadata.LogoUrl))
        {
            Assert(inputMetadata.LogoUrl.Length <= DAOContractConstants.LogoUrlMaxLength, "Invalid metadata logo url.");
            currentMetadata.LogoUrl = inputMetadata.LogoUrl;       
        }

        if (IsStringValid(inputMetadata.Description))
        {
            Assert(inputMetadata.Description.Length <= DAOContractConstants.DescriptionMaxLength, "Invalid metadata description.");
            currentMetadata.Description = inputMetadata.Description;
        }

        currentMetadata.SocialMedia.Clear();

        if (inputMetadata.SocialMedia?.Count > 0)
        {
            Assert(inputMetadata.SocialMedia.Count <= DAOContractConstants.SocialMediaListMaxCount, "Invalid metadata social media count.");
            foreach (var name in inputMetadata.SocialMedia.Keys)
            {
                Assert(IsStringValid(name) && name.Length <= DAOContractConstants.SocialMediaNameMaxLength, "Invalid metadata social media name.");
                var url = inputMetadata.SocialMedia[name];
                Assert(IsStringValid(url) && url.Length <= DAOContractConstants.SocialMediaUrlMaxLength, "Invalid metadata social media url.");
                currentMetadata.SocialMedia.Add(name, url);
            }
        }

        State.MetadataMap[input.DaoId] = currentMetadata;
        Context.Fire(new MetadataUpdated
        {
            DaoId = input.DaoId,
            Metadata = currentMetadata
        });
        return new Empty();
    }
}