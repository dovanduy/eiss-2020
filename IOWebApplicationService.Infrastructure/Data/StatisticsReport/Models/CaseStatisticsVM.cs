﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IOWebApplication.Infrastructure.Data.Models.Nomenclatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace IOWebApplicationService.Infrastructure.Data.StatisticsReport.Models
{
    public class CaseStatisticsVM
    {
        public int CourtId { get; set; }

        public int Count { get; set; }

        public int ExcelCol { get; set; }

        public int ExcelRow { get; set; }

        public string LawUnitName 
        { 
            get 
            {
                string result = "";
                if (string.IsNullOrEmpty(LawUnitData) == false)
                {
                    string[] name = LawUnitData.Split(",,");
                    if (name.Length == 2)
                        result = name[1];
                }
                return result;
            } 
        }

        public int LawUnitId
        {
            get
            {
                int result = 0;
                if (string.IsNullOrEmpty(LawUnitData) == false)
                {
                    string[] name = LawUnitData.Split(",,");
                    if (name.Length == 2)
                    {
                        if (int.TryParse(name[0], out result) == false)
                            result = 0;
                    }
                }
                return result;
            }
        }

        public string LawUnitData { get; set; }

        public string FromCourtData { get; set; }

        public int FromCourtId
        {
            get
            {
                int result = 0;
                if (string.IsNullOrEmpty(FromCourtData) == false)
                {
                    string[] name = FromCourtData.Split(",,");
                    if (name.Length == 3)
                    {
                        if (int.TryParse(name[0], out result) == false)
                            result = 0;
                    }
                }
                return result;
            }
        }        
        
        public string FromCourtName
        {
            get
            {
                string result = "";
                if (string.IsNullOrEmpty(FromCourtData) == false)
                {
                    string[] name = FromCourtData.Split(",,");
                    if (name.Length == 3)
                        result = name[1];
                }
                return result;
            }
        }

        public int FromCourtIsParent
        {
            get
            {
                int result = 0;
                if (string.IsNullOrEmpty(FromCourtData) == false)
                {
                    string[] name = FromCourtData.Split(",,");
                    if (name.Length == 3)
                    {
                        if (int.TryParse(name[2], out result) == false)
                            result = 0;
                    }
                }
                return result;
            }
        }
    }

    public class StatisticsCourtTypeCaseTypeVM
    {
        public int CourtTypeId { get; set; }

        public int CaseTypeId { get; set; }

        public int ReportTypeId { get; set; }

        public int Col { get; set; }
    }

    public class StatisticsExcelReportIndexVM
    {
        public int CourtTypeId { get; set; }

        public int? CaseGroupId { get; set; }
        public List<int> CaseGroupCaseTypeIds { get; set; }

        public string CaseTypeIds { get; set; }

        public string ActTypeIds { get; set; }

        public string ActComplainIndexIds { get; set; }

        public List<int> CaseTypes 
        { 
            get 
            {
                return CaseGroupId != null ? CaseGroupCaseTypeIds :
                    CaseTypeIds.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
            } 
        }

        public List<int> ActTypes
        {
            get
            {
                return ActTypeIds.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
            }
        }


        public List<int> ActComplainIndex
        {
            get
            {
                return ActComplainIndexIds.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
            }
        }


        public int Col { get; set; }
    }

    public class StatisticsExcelReportComplainIndexVM
    {
        public int CourtTypeId { get; set; }

        public int SheetIndex { get; set; }

        public string ActComplainResultIds { get; set; }


        public List<int> ActComplainResult
        {
            get
            {
                return ActComplainResultIds.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
            }
        }


        public int Col { get; set; }
    }

    public class StatisticsExcelReportCaseCodeRowVM
    {
        public string CaseCodeIds { get; set; }

        public int SheetIndex { get; set; }

        public int RowIndex { get; set; }

        public int CourtTypeId { get; set; }

        public List<int> CaseCode
        {
            get
            {
                return CaseCodeIds.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
            }
        }
    }

    public class StatisticsNomDataVM
    {
        public List<StatisticsCourtTypeCaseTypeVM> courtTypeCaseTypesCols { get; set; }

        public List<StatisticsExcelReportIndexVM> excelReportIndexCols { get; set; }

        public List<StatisticsExcelReportCaseCodeRowVM> excelReportCaseCodeRows { get; set; }

        public List<StatisticsExcelReportComplainIndexVM> excelReportComplainResults { get; set; }
    }
}
