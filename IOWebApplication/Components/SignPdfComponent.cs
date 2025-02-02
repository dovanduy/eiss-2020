﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IO.SignTools.Contracts;
using IOWebApplication.Core.Models;
using IOWebApplication.Infrastructure.Contracts;
using IOWebApplication.Infrastructure.Models.Cdn;
using IOWebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IOWebApplication.Components
{
    public class SignPdfComponent : ViewComponent
    {
        private readonly IIOSignToolsService signTools;
        private readonly ILogger logger;
        private readonly ICdnService cdn;

        public SignPdfComponent(
            IIOSignToolsService signTools,
            ILogger<SignPdfComponent> logger,
            ICdnService cdn)
        {
            this.signTools = signTools;
            this.logger = logger;
            this.cdn = cdn;
        }

        public async Task<IViewComponentResult> InvokeAsync(SignPdfInfo info, string viewName = "")
        {
            SignPdfViewModel model = new SignPdfViewModel()
            {
                SuccessUrl = info.SuccessUrl.ToString(),
                ErrorUrl = info.ErrorUrl.ToString(),
                CancelUrl = info.CancelUrl.ToString(),
                SourceId = info.SourceId,
                SourceType = info.DestinationType,
                SignerName = info.SignerName,
                SignerUic = info.SignerUic
            };

            try
            {
                var pdf = await cdn.MongoCdn_Download(new CdnFileSelect() { FileId = info.FileId, SourceId = info.SourceId, SourceType = info.SourceType });

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(pdf.FileContentBase64)))
                {
                    var (hash, tempPdfId) = await signTools.GetPdfHash(ms, info.Reason, info.Location);

                    model.PdfUrl = Url.Action("GetFile", "PDF", new { pdfContentId = pdf.FileId });
                    model.PdfId = tempPdfId;
                    model.PdfHash = hash;
                    model.FileName = pdf.FileName;
                    model.FileTitle = pdf.FileTitle;
                }
            }
            catch (ArgumentException aex)
            {
                logger.LogError(aex, "SignPdf Error");
                string message = "Невалиден файл за подписване";

                return await Task.FromResult<IViewComponentResult>(View("Error", message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SignPdf Error");

                return await Task.FromResult<IViewComponentResult>(View("Error", ex.Message));
            }

            return await Task.FromResult<IViewComponentResult>(View(viewName, model));
        }
    }
}