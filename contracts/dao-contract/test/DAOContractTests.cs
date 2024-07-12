using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.ContractTestKit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

// This class is unit test class, and it inherit TestBase. Write your unit test code inside it
public partial class DAOContractTests : DAOContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        var result = await DAOContractStub.Initialize.SendAsync(new InitializeInput
        {
            GovernanceContractAddress = DefaultAddress,
            ElectionContractAddress = DefaultAddress,
            TreasuryContractAddress = DefaultAddress,
            VoteContractAddress = DefaultAddress,
            TimelockContractAddress = DefaultAddress
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var output = await DAOContractStub.GetInitializedContracts.CallAsync(new Empty());
        output.GovernanceContractAddress.ShouldBe(DefaultAddress);
        output.ElectionContractAddress.ShouldBe(DefaultAddress);
        output.TreasuryContractAddress.ShouldBe(null);
        output.VoteContractAddress.ShouldBe(DefaultAddress);
        output.TimelockContractAddress.ShouldBe(null);
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await UserDAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Invalid governance contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid governance contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid election contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid election contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress,
                TreasuryContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress,
                TreasuryContractAddress = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }
        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                GovernanceContractAddress = DefaultAddress,
                ElectionContractAddress = DefaultAddress,
                TimelockContractAddress = DefaultAddress,
                TreasuryContractAddress = DefaultAddress,
                VoteContractAddress = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid vote contract address.");
        }

        await InitializeAsync();

        {
            var result = await DAOContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Fact]
    public async Task CreateDAOTests()
    {
        var daoName = GenerateRandomString(50);
        var daoLogoUrl = GenerateRandomString(256);
        var daoDescription = GenerateRandomString(240);
        var socialMedias = GenerateRandomMap(20, 16, 64);
        var file = GenerateFile("cid", "name", "url");
        var file2 = GenerateFile("cid2", "name2", "url2");

        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = daoName,
                LogoUrl = daoLogoUrl,
                Description = daoDescription,
                SocialMedia = { socialMedias }
            },
            GovernanceToken = "",
            Files =
            {
                file, file2
            },
            GovernanceSchemeThreshold = new GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 1,
                MinimalApproveThreshold = 1,
                MaximalRejectionThreshold = 2,
                MaximalAbstentionThreshold = 2
            }
        });

        Hash daoId;

        {
            var log = GetLogEvent<DAOCreated>(result.TransactionResult);
            log.DaoId.ShouldNotBeNull();
            log.Metadata.Name.ShouldBe(daoName);
            log.Metadata.LogoUrl.ShouldBe(daoLogoUrl);
            log.Metadata.Description.ShouldBe(daoDescription);
            log.Metadata.SocialMedia.Count.ShouldBe(socialMedias.Count);
            log.Metadata.SocialMedia.ShouldBe(socialMedias);
            log.GovernanceToken.ShouldBe("");
            log.Creator.ShouldBe(DefaultAddress);
            log.ContractAddressList.GovernanceContractAddress.ShouldBe(GovernanceContractAddress);
            log.ContractAddressList.ElectionContractAddress.ShouldBe(ElectionContractAddress);
            log.ContractAddressList.TreasuryContractAddress.ShouldBe(TreasuryContractAddress);
            log.ContractAddressList.VoteContractAddress.ShouldBe(VoteContractAddress);
            log.ContractAddressList.TimelockContractAddress.ShouldBe(null);

            daoId = log.DaoId;
        }
        {
            var log = GetLogEvent<FileInfosUploaded>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.UploadedFiles.Data.Count.ShouldBe(2);
            log.UploadedFiles.Data[file.Cid].File.ShouldBe(file);
            log.UploadedFiles.Data[file2.Cid].File.ShouldBe(file2);
        }
        {
            var output = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
            output.Creator.ShouldBe(DefaultAddress);
            output.SubsistStatus.ShouldBeTrue();
            output.ContractAddressList.GovernanceContractAddress.ShouldBe(GovernanceContractAddress);
            output.ContractAddressList.ElectionContractAddress.ShouldBe(ElectionContractAddress);
            output.ContractAddressList.TreasuryContractAddress.ShouldBe(TreasuryContractAddress);
            output.ContractAddressList.VoteContractAddress.ShouldBe(VoteContractAddress);
            output.ContractAddressList.TimelockContractAddress.ShouldBe(null);
            output.DaoId.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetDAOIdByName.CallAsync(new StringValue
            {
                Value = daoName
            });
            output.Value.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(2);
            output.Data[file.Cid].File.ShouldBe(file);
            output.Data[file.Cid].Uploader.ShouldBe(DefaultAddress);
            output.Data[file.Cid].UploadTime.ShouldNotBeNull();
            output.Data[file2.Cid].File.ShouldBe(file2);
            output.Data[file2.Cid].Uploader.ShouldBe(DefaultAddress);
            output.Data[file2.Cid].UploadTime.ShouldNotBeNull();
        }
        {
            var output = await DAOContractStub.GetMetadata.CallAsync(daoId);
            output.Name.ShouldBe(daoName);
            output.LogoUrl.ShouldBe(daoLogoUrl);
            output.Description.ShouldBe(daoDescription);
            output.SocialMedia.Count.ShouldBe(socialMedias.Count);
            output.SocialMedia.ShouldBe(socialMedias);
        }
        {
            var output = await DAOContractStub.GetGovernanceToken.CallAsync(daoId);
            output.Value.ShouldBeEmpty();
        }
        {
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }

        daoName = GenerateRandomString(49);
        daoLogoUrl = GenerateRandomString(255);
        daoDescription = GenerateRandomString(239);
        socialMedias = GenerateRandomMap(19, 15, 63);

        result = await DAOContractStub.CreateDAO.SendAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = daoName,
                LogoUrl = daoLogoUrl,
                Description = daoDescription,
                SocialMedia = { socialMedias }
            },
            GovernanceToken = "ELF",
            GovernanceSchemeThreshold = new GovernanceSchemeThreshold
            {
                MinimalRequiredThreshold = 1,
                MinimalVoteThreshold = 1,
                MinimalApproveThreshold = 1,
                MaximalRejectionThreshold = 2,
                MaximalAbstentionThreshold = 2
            }
        });

        {
            var log = GetLogEvent<DAOCreated>(result.TransactionResult);
            log.DaoId.ShouldNotBeNull();
            log.Metadata.Name.ShouldBe(daoName);
            log.Metadata.LogoUrl.ShouldBe(daoLogoUrl);
            log.Metadata.Description.ShouldBe(daoDescription);
            log.Metadata.SocialMedia.Count.ShouldBe(socialMedias.Count);
            log.Metadata.SocialMedia.ShouldBe(socialMedias);
            log.GovernanceToken.ShouldBe("ELF");
            log.Creator.ShouldBe(DefaultAddress);
            log.ContractAddressList.GovernanceContractAddress.ShouldBe(GovernanceContractAddress);
            log.ContractAddressList.ElectionContractAddress.ShouldBe(ElectionContractAddress);
            log.ContractAddressList.TreasuryContractAddress.ShouldBe(TreasuryContractAddress);
            log.ContractAddressList.VoteContractAddress.ShouldBe(VoteContractAddress);
            log.ContractAddressList.TimelockContractAddress.ShouldBe(null);

            daoId = log.DaoId;
        }
        {
            var output = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
            output.Creator.ShouldBe(DefaultAddress);
            output.SubsistStatus.ShouldBeTrue();
            output.ContractAddressList.GovernanceContractAddress.ShouldBe(GovernanceContractAddress);
            output.ContractAddressList.ElectionContractAddress.ShouldBe(ElectionContractAddress);
            output.ContractAddressList.TreasuryContractAddress.ShouldBe(TreasuryContractAddress);
            output.ContractAddressList.VoteContractAddress.ShouldBe(VoteContractAddress);
            output.ContractAddressList.TimelockContractAddress.ShouldBe(null);
            output.DaoId.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetDAOIdByName.CallAsync(new StringValue
            {
                Value = daoName
            });
            output.Value.ShouldBe(daoId);
        }
        {
            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.ShouldBeEmpty();
        }
        {
            var output = await DAOContractStub.GetMetadata.CallAsync(daoId);
            output.Name.ShouldBe(daoName);
            output.LogoUrl.ShouldBe(daoLogoUrl);
            output.Description.ShouldBe(daoDescription);
            output.SocialMedia.Count.ShouldBe(socialMedias.Count);
            output.SocialMedia.ShouldBe(socialMedias);
        }
        {
            var output = await DAOContractStub.GetGovernanceToken.CallAsync(daoId);
            output.Value.ShouldBe("ELF");
        }
        {
            var output = await DAOContractStub.GetSubsistStatus.CallAsync(daoId);
            output.Value.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task CreateDAOTests_Fail()
    {
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        await InitializeAsync();

        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput());
            result.TransactionResult.Error.ShouldContain("Invalid metadata.");
        }
        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata()
            });
            result.TransactionResult.Error.ShouldContain("Invalid metadata name.");
        }

        await CreateDAOAsync();

        {
            var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
            {
                Metadata = new Metadata
                {
                    Name = "TestDAO",
                    LogoUrl = "logo_url",
                    Description = "Description",
                    SocialMedia =
                    {
                        new Dictionary<string, string>
                        {
                            { "X", "twitter" },
                            { "Facebook", "facebook" },
                            { "Telegram", "telegram" },
                            { "Discord", "discord" },
                            { "Reddit", "reddit" }
                        }
                    }
                },
                GovernanceToken = "ELF",
                GovernanceSchemeThreshold = new GovernanceSchemeThreshold
                {
                    MinimalRequiredThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MinimalApproveThreshold = 1,
                    MaximalRejectionThreshold = 2,
                    MaximalAbstentionThreshold = 2
                },
                HighCouncilInput = new HighCouncilInput
                {
                    GovernanceSchemeThreshold = new GovernanceSchemeThreshold(),
                    HighCouncilConfig = new HighCouncilConfig()
                }
                
            });
            result.TransactionResult.Error.ShouldContain("DAO name already exists.");
        }
    }

    [Theory]
    [InlineData(0, 0, 0, "Invalid metadata name.")]
    [InlineData(51, 0, 0, "Invalid metadata name.")]
    [InlineData(5, 0, 0, "Invalid metadata logo url.")]
    [InlineData(5, 257, 0, "Invalid metadata logo url.")]
    [InlineData(5, 5, 0, "Invalid metadata description.")]
    [InlineData(5, 5, 241, "Invalid metadata description.")]
    public async Task CreateDAOTests_Metadata_Fail(int nameLength, int logoUrlLength, int descriptionLength,
        string errorMessage)
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = GenerateRandomString(nameLength),
                LogoUrl = GenerateRandomString(logoUrlLength),
                Description = GenerateRandomString(descriptionLength)
            }
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
    }

    [Theory]
    [InlineData(0, 1, 1, "Invalid metadata social media count.")]
    [InlineData(21, 1, 1, "Invalid metadata social media count.")]
    [InlineData(1, 0, 0, "Invalid metadata social media name.")]
    [InlineData(1, 17, 0, "Invalid metadata social media name.")]
    [InlineData(1, 5, 0, "Invalid metadata social media url.")]
    [InlineData(1, 5, 241, "Invalid metadata social media url.")]
    public async Task CreateDAOTests_SocialMedia_Fail(int count, int keyLength, int valueLength, string errorMessage)
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "name",
                LogoUrl = "logo",
                Description = "des",
                SocialMedia = { GenerateRandomMap(count, keyLength, valueLength) }
            }
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
    }

    [Theory]
    [InlineData("ABCdE", "Token not found.")]
    [InlineData("ABCDEFGHIJK", "Token not found.")]
    [InlineData("TEST", "Token not found.")]
    [InlineData("ELF-1", "Token not found.")]
    public async Task CreateDAOTests_GovernanceToken_Fail(string symbol, string errorMessage)
    {
        await InitializeAsync();

        var result = await DAOContractStub.CreateDAO.SendWithExceptionAsync(new CreateDAOInput
        {
            Metadata = new Metadata
            {
                Name = "name",
                LogoUrl = "logo",
                Description = "des",
                SocialMedia = { GenerateRandomMap(1, 1, 1) }
            },
            GovernanceToken = symbol
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
    }

    [Fact]
    public async Task CreateDAOTests_SetTreasuryContractAddress()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync(false);

        var daoInfo = await DAOContractStub.GetDAOInfo.CallAsync(daoId);
        daoInfo.ShouldNotBeNull();
        daoInfo.ContractAddressList.TreasuryContractAddress.ShouldBe(TreasuryContractAddress);
    }
}