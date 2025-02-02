﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace IOWebApplication.Infrastructure.Models.ViewModels.Report
{
    public class LawUnitLoadSprVM
    {
        public int LawUnitId { get; set; }
        public string LawUnitLabel { get; set; }
        public decimal CaseLoadIndex { get; set; }
        public decimal LoadIndex { get; set; }
        public decimal SumLoadIndex { get; set; }
    }
}
