using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using TomorrowDAO.Contracts.Governance;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests
{
    [Fact]
    public async Task UploadFileInfosTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var files = new List<File>();
        for (var i = 0; i < 20; i++)
        {
            files.Add(GenerateFile("cid" + i, "name" + i, "url" + i));
        }

        var result = await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { files }
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<FileInfosUploaded>(result.TransactionResult);
        log.DaoId.ShouldBe(daoId);
        log.UploadedFiles.Data.Count.ShouldBe(20);

        var j = 0;
        foreach (var cid in log.UploadedFiles.Data.Keys)
        {
            log.UploadedFiles.Data[cid].File.Cid.ShouldBe(files[j].Cid);
            log.UploadedFiles.Data[cid].File.Name.ShouldBe(files[j].Name);
            log.UploadedFiles.Data[cid].File.Url.ShouldBe(files[j++].Url);
            log.UploadedFiles.Data[cid].Uploader.ShouldBe(DefaultAddress);
            log.UploadedFiles.Data[cid].UploadTime.ShouldNotBeNull();
        }

        j = 0;
        var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
        output.Data.Count.ShouldBe(20);
        foreach (var cid in output.Data.Keys)
        {
            output.Data[cid].File.Cid.ShouldBe(files[j].Cid);
            output.Data[cid].File.Name.ShouldBe(files[j].Name);
            output.Data[cid].File.Url.ShouldBe(files[j++].Url);
            output.Data[cid].Uploader.ShouldBe(DefaultAddress);
            output.Data[cid].UploadTime.ShouldNotBeNull();
        }
    }

    [Theory]
    [InlineData(63, 127, 255)]
    [InlineData(64, 128, 256)]
    public async Task UploadFileInfosTests_Duplicate(int cidLength, int nameLength, int urlLength)
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var file = GenerateFile(GenerateRandomString(cidLength), GenerateRandomString(nameLength),
            GenerateRandomString(urlLength));

        var result = await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { file, file }
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<FileInfosUploaded>(result.TransactionResult);
        log.DaoId.ShouldBe(daoId);
        log.UploadedFiles.Data.Count.ShouldBe(1);
        log.UploadedFiles.Data[file.Cid].File.Cid.ShouldBe(file.Cid);
        log.UploadedFiles.Data[file.Cid].File.Name.ShouldBe(file.Name);
        log.UploadedFiles.Data[file.Cid].File.Url.ShouldBe(file.Url);
        log.UploadedFiles.Data[file.Cid].Uploader.ShouldBe(DefaultAddress);
        log.UploadedFiles.Data[file.Cid].UploadTime.ShouldNotBeNull();

        var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
        output.Data.Count.ShouldBe(1);
        output.Data[file.Cid].File.Cid.ShouldBe(file.Cid);
        output.Data[file.Cid].File.Name.ShouldBe(file.Name);
        output.Data[file.Cid].File.Url.ShouldBe(file.Url);
        output.Data[file.Cid].Uploader.ShouldBe(DefaultAddress);
        output.Data[file.Cid].UploadTime.ShouldNotBeNull();
    }

    [Fact]
    public async Task UploadFileInfosTests_Fail()
    {
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = HashHelper.ComputeFrom("test")
            });
            result.TransactionResult.Error.ShouldContain("DAO not existed.");
        }

        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        {
            var result = await UserDAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Permission of UploadFileInfos is not granted");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input files.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input files.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { new File() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cid.");
        }
        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { GenerateFile("cid", "name", "url"), new File() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cid.");
        }
        {
            var files = new List<File>();
            for (var i = 0; i < 21; i++)
            {
                files.Add(GenerateFile("cid" + i, "name" + i, "url" + i));
            }

            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { files }
            });
            result.TransactionResult.Error.ShouldContain("Too many files.");
        }
        {
            var files = new List<File>();
            for (var i = 0; i < 11; i++)
            {
                files.Add(GenerateFile("cid" + i, "name" + i, "url" + i));
            }

            await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { files }
            });

            files = new List<File>();
            for (var i = 0; i < 10; i++)
            {
                files.Add(GenerateFile("cids" + i, "names" + i, "urls" + i));
            }

            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { files }
            });
            result.TransactionResult.Error.ShouldContain("Too many files.");
        }
        
        await CreateProposalAndVote(daoId, new ExecuteTransaction
        {
            ContractMethodName = nameof(DAOContractStub.SetSubsistStatus),
            ToAddress = DAOContractAddress,
            Params = new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = false
            }.ToByteString()
        });

        {
            var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("DAO not subsisted.");
        }
    }

    [Theory]
    [InlineData(0, 0, 0, "Invalid input file cid.")]
    [InlineData(5, 0, 0, "Invalid input file name.")]
    [InlineData(5, 5, 0, "Invalid input file url.")]
    [InlineData(65, 0, 0, "Invalid input file cid.")]
    [InlineData(64, 129, 0, "Invalid input file name.")]
    [InlineData(64, 128, 257, "Invalid input file url.")]
    public async Task UploadFileInfosTests_FileInfo_Fail(int cidLength, int nameLength, int urlLength,
        string errorMessage)
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var file = GenerateFile(GenerateRandomString(cidLength), GenerateRandomString(nameLength),
            GenerateRandomString(urlLength));

        var result = await DAOContractStub.UploadFileInfos.SendWithExceptionAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { file }
        });
        result.TransactionResult.Error.ShouldContain(errorMessage);
    }

    [Fact]
    public async Task RemoveFileInfosTests()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        {
            var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { "cid" }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(FileInfosRemoved)));
            log.ShouldBeNull();
        }

        var file = GenerateFile("cid", "name", "url");

        await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { file }
        });

        // all not exist
        {
            var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { "cid2" }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(FileInfosRemoved)));
            log.ShouldBeNull();
        }
        // some not exist
        {
            var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { "cid", "cid2" }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<FileInfosRemoved>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.RemovedFiles.Data.Count.ShouldBe(1);
            log.RemovedFiles.Data["cid"].File.Cid.ShouldBe("cid");
            log.RemovedFiles.Data["cid"].File.Name.ShouldBe("name");
            log.RemovedFiles.Data["cid"].File.Url.ShouldBe("url");

            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(0);
        }

        await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { file }
        });

        {
            var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { "cid", "cid" }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<FileInfosRemoved>(result.TransactionResult);
            log.DaoId.ShouldBe(daoId);
            log.RemovedFiles.Data.Count.ShouldBe(1);
            log.RemovedFiles.Data["cid"].File.Cid.ShouldBe("cid");
            log.RemovedFiles.Data["cid"].File.Name.ShouldBe("name");
            log.RemovedFiles.Data["cid"].File.Url.ShouldBe("url");

            var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
            output.Data.Count.ShouldBe(0);
        }
    }

    [Fact]
    public async Task RemoveFileInfosTests_Fail()
    {
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput());
            result.TransactionResult.Error.ShouldContain("Invalid input dao id.");
        }
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = HashHelper.ComputeFrom("test")
            });
            result.TransactionResult.Error.ShouldContain("DAO not existed.");
        }

        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        {
            var result = await UserDAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Permission of RemoveFileInfos is not granted");
        }
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
        }
        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
        }
        {
            var cids = new List<string>();
            for (int i = 0; i < 21; i++)
            {
                cids.Add(i.ToString());
            }

            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { cids }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
        }
        {
            await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
            {
                DaoId = daoId,
                Files = { GenerateFile("cid", "name", "url") }
            });

            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId,
                FileCids = { GenerateRandomString(65) }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input file cid.");
        }
        
        await CreateProposalAndVote(daoId, new ExecuteTransaction
        {
            ContractMethodName = nameof(DAOContractStub.SetSubsistStatus),
            ToAddress = DAOContractAddress,
            Params = new SetSubsistStatusInput
            {
                DaoId = daoId,
                Status = false
            }.ToByteString()
        });

        {
            var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
            {
                DaoId = daoId
            });
            result.TransactionResult.Error.ShouldContain("DAO not subsisted.");
        }
    }

    [Theory]
    [InlineData(19, 64)]
    [InlineData(20, 63)]
    public async Task RemoveFileInfosTests_MultipleFiles(int count, int cidLength)
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var files = new List<File>();
        for (var i = 0; i < count - 1; i++)
        {
            files.Add(GenerateFile("cid" + i, "name", "url"));
        }

        files.Add(GenerateFile(GenerateRandomString(cidLength), "name", "url"));

        await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { files }
        });

        var result = await DAOContractStub.RemoveFileInfos.SendAsync(new RemoveFileInfosInput
        {
            DaoId = daoId,
            FileCids = { files.Select(f => f.Cid) }
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<FileInfosRemoved>(result.TransactionResult);
        log.DaoId.ShouldBe(daoId);
        log.RemovedFiles.Data.Values.Select(f => new File
        {
            Cid = f.File.Cid,
            Name = f.File.Name,
            Url = f.File.Url
        }).ShouldBe(files);

        var output = await DAOContractStub.GetFileInfos.CallAsync(daoId);
        output.Data.Count.ShouldBe(0);
    }

    [Fact]
    public async Task RemoveFileInfosTests_TooManyFiles_Fail()
    {
        await InitializeAsync();
        var daoId = await CreateDAOAsync();

        var files = new List<File>();
        for (var i = 0; i < 20; i++)
        {
            files.Add(GenerateFile("cid" + i, "name", "url"));
        }

        await DAOContractStub.UploadFileInfos.SendAsync(new UploadFileInfosInput
        {
            DaoId = daoId,
            Files = { files }
        });

        var cids = files.Select(f => f.Cid).ToList();
        cids.Add("cid20");
        var result = await DAOContractStub.RemoveFileInfos.SendWithExceptionAsync(new RemoveFileInfosInput
        {
            DaoId = daoId,
            FileCids = { cids }
        });
        result.TransactionResult.Error.ShouldContain("Invalid input file cids.");
    }
}