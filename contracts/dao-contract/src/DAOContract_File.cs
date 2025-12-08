using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContract
{
    public override Empty UploadFileInfos(UploadFileInfosInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.DaoId), "Invalid input dao id.");

        CheckDAOExistsAndSubsist(input.DaoId);

        AssertPermission(input.DaoId, nameof(UploadFileInfos));
        Assert(input.Files != null && input.Files.Count > 0, "Invalid input files.");

        ProcessFileUploads(input.DaoId, input.Files);

        return new Empty();
    }

    private void ValidateInputFile(File input)
    {
        Assert(input != null, "Invalid input file.");
        Assert(IsStringValid(input.Cid) && input.Cid.Length <= DAOContractConstants.FileCidMaxLength,
            "Invalid input file cid.");
        Assert(IsStringValid(input.Name) && input.Name.Length <= DAOContractConstants.FileNameMaxLength,
            "Invalid input file name.");
        Assert(IsStringValid(input.Url) && input.Url.Length <= DAOContractConstants.FileUrlMaxLength,
            "Invalid input file url.");
    }

    private void ProcessFileUploads(Hash daoId, RepeatedField<File> files)
    {
        if (files.Count == 0) return;

        Assert(files.Count <= DAOContractConstants.FileMaxCount, "Too many files.");

        var distinctFiles = files.Distinct();
        var newFiles = new FileInfoList();
        var existingFiles = State.FilesMap[daoId] ?? new FileInfoList();

        Assert(files.Count + existingFiles.Data.Count <= DAOContractConstants.FileMaxCount, "Too many files.");

        foreach (var file in distinctFiles)
        {
            ValidateInputFile(file);

            existingFiles.Data.TryGetValue(file.Cid, out var existFile);
            Assert(existFile == null, "File already exists.");

            var fileInfo = new FileInfo
            {
                File = file,
                UploadTime = Context.CurrentBlockTime,
                Uploader = Context.Sender
            };

            existingFiles.Data[file.Cid] = fileInfo;
            newFiles.Data[file.Cid] = fileInfo;
        }

        State.FilesMap[daoId] = existingFiles;

        if (newFiles.Data.Any())
        {
            Context.Fire(new FileInfosUploaded
            {
                DaoId = daoId,
                UploadedFiles = newFiles
            });
        }
    }

    public override Empty RemoveFileInfos(RemoveFileInfosInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.DaoId), "Invalid input dao id.");

        CheckDAOExistsAndSubsist(input.DaoId);

        AssertPermission(input.DaoId, nameof(RemoveFileInfos));
        Assert(
            input.FileCids != null && input.FileCids.Count > 0 &&
            input.FileCids.Count <= DAOContractConstants.FileMaxCount, "Invalid input file cids.");

        ProcessRemoveFileInfos(input.DaoId, input.FileCids);

        return new Empty();
    }

    private void ProcessRemoveFileInfos(Hash daoId, RepeatedField<string> fileCids)
    {
        if (fileCids.Count == 0) return;

        var distinctFileCids = fileCids.Distinct();

        var removedFiles = new FileInfoList();

        var existingFiles = State.FilesMap[daoId] ??= new FileInfoList();
        if (existingFiles.Data.Count == 0) return;

        foreach (var fileCid in distinctFileCids)
        {
            Assert(IsStringValid(fileCid) && fileCid.Length <= DAOContractConstants.FileCidMaxLength,
                "Invalid input file cid.");

            if (!existingFiles.Data.ContainsKey(fileCid)) continue;

            removedFiles.Data[fileCid] = existingFiles.Data[fileCid].Clone();
            existingFiles.Data.Remove(fileCid);
        }

        if (removedFiles.Data.Any())
        {
            Context.Fire(new FileInfosRemoved
            {
                DaoId = daoId,
                RemovedFiles = removedFiles
            });
        }
    }
}