﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IOWebApplication.Infrastructure.Data.Models.Documents;
using IOWebApplication.Infrastructure.Models;
using IOWebApplication.Infrastructure.Models.Documents;
using IOWebApplication.Infrastructure.Models.ViewModels;
using IOWebApplication.Infrastructure.Models.ViewModels.Common;
using IOWebApplication.Infrastructure.Models.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IOWebApplication.Core.Contracts
{
    public interface IDocumentService : IBaseService
    {
        DocumentVM Document_Init(int documentDirection, int templateId = 0);

        DocumentVM Document_GetById(long id);
        bool Document_SaveData(DocumentVM model);
        bool Document_CorrectData(DocumentVM model);
        IQueryable<DocumentListVM> Document_Select(DocumentFilterVM model);

        List<SelectListItem> GetDocumentRegistratures(bool appendallItem = false);

        List<SelectListItem> GetDeliveryGroups(int documentDirection);
        IEnumerable<LabelValueVM> GetDocument(int courtId, string documentNumber);
        bool CheckDocumentOldNumber(int courtId, int docDirectionId, string documentNumber, DateTime documentDate);
        LabelValueVM GetDocumentById(int id);
        List<SelectListItem> DocumentPerson_SelectForDropDownList(long documentId);
        string GetDataInstitutionCaseInfoForDocument(long documentId);
        DocumentSelectPersonsVM Case_SelectPersons(int caseId);
        List<DocumentPersonVM> SelectDocumentPersonsFromCase(DocumentSelectPersonsVM model, int index);
        IQueryable<DocumentSelectAddressVM> SelectAddressListByPerson(string uic, int uicTypeId, int? personSourceType,
                        long? personSourceId);
        (bool result, string errorMessage) DocumentDecision_SaveData(DocumentDecision model);
        DocumentDecision DocumentDecision_SelectForDocument(long documentId);
        IQueryable<DocumentDecisionListVM> DocumentDecision_Select(int courtId, DocumentDecisionFilterVM model);
        IQueryable<DocumentDecisionCaseListVM> DocumentDecisionCase_Select(long documentDecisionId);
        (bool result, string errorMessage) DocumentDecisionCase_SaveData(DocumentDecisionCase model);
        DocumentSelectPersonsVM Document_SelectPersons(long documentId);
        List<DocumentPersonVM> SelectDocumentPersonsFromDocument(DocumentSelectPersonsVM model, int index);
        IQueryable<DocumentCaseInfoSprVM> DocumentCaseInfoSpr_Select(DocumentCaseInfoSprFilterVM model);
        Document GetByIdWithData(long id);
        bool IsCanExpireCompliantDocument(long id);
        bool DocumentExpire(ExpiredInfoVM model);

        bool Reactivate(DocumentReactivateVM model);

        IQueryable<DocumentInstitutionCaseInfoListVM> DocumentInstitutionCaseInfo_Select(long documentId);
        bool DocumentInstitutionCaseInfo_SaveData(DocumentInstitutionCaseInfoEditVM model);
        DocumentInstitutionCaseInfoEditVM GetById_InstitutionCaseInfoEditVM(long Id);
    }
}
