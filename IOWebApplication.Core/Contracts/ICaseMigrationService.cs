﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IOWebApplication.Infrastructure.Data.Models.Cases;
using IOWebApplication.Infrastructure.Models.ViewModels;
using IOWebApplication.Infrastructure.Models.ViewModels.Case;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOWebApplication.Core.Contracts
{
    public interface ICaseMigrationService : IBaseService
    {
        IQueryable<CaseMigrationVM> Select(int caseId);
        CaseMigration InitNewMigration(int caseId);
        bool SaveData(CaseMigration model);
        bool UnionCase(CaseMigrationUnionVM model);
        int GetLastMigrationAcceptToUse(CaseMigrationFindCaseVM model);
        bool AcceptCaseMigration(int id, int caseId, string description = null);
        List<SelectListItem> Get_MigrationTypes(int direction);
        List<SelectListItem> GetDropDownList_Court(int caseId, bool addDefaultElement = true, bool addAllElement = false);
        List<SelectListItem> GetDropDownList_CourtCase(int caseId, bool addDefaultElement = true, bool addAllElement = false);
        List<SelectListItem> GetDropDownList_ReturnCase(int caseId, bool addDefaultElement = true, bool addAllElement = false);
        bool IsExistMigrationWithComplainWithDocumentId(long DocumentId);
        bool IsExistMigrationWithAct(long ActId);
    }
}
