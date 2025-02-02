﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IOWebApplication.Infrastructure.Models.Cdn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOWebApplication.Infrastructure.Contracts
{
    public interface ICdnService
    {
        Task<CdnUploadResult> MongoCdn_UploadFile(CdnUploadRequest request);
        Task<CdnDownloadResult> MongoCdn_Download(long mongoFileId);
        Task<CdnDownloadResult> MongoCdn_Download(string fileId);
        Task<CdnDownloadResult> MongoCdn_Download(CdnFileSelect request);
        Task<bool> MongoCdn_DeleteFile(string id);
        Task<bool> MongoCdn_DeleteFiles(CdnFileSelect request);

        Task<string> LoadHtmlFileTemplate(CdnFileSelect request);
        Task<bool> MongoCdn_AppendUpdate(CdnUploadRequest request);

        bool SaveMongoFileData(CdnUploadRequest file, string mongoFileId);
        bool DeleteMongoFileData(string mongoFileId);

        IEnumerable<CdnItemVM> Select(int sourceType, string sourceId, string fileId = null);
        IEnumerable<CdnItemVM> Select(int[] sourceTypes, string sourceId, string fileId = null);
    }
}
