﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IOWebApplication.Infrastructure.Models.ViewModels.Report
{
    public class CaseAlphabeticalFilterVM
    {
        [Display(Name = "От дата")]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "До дата")]
        public DateTime? DateTo { get; set; }

        [Display(Name = "От номер")]
        public int? NumberFrom { get; set; }

        [Display(Name = "До номер")]
        public int? NumberTo { get; set; }

        [Display(Name = "Основен вид дело")]
        public int CaseGroupId { get; set; }

        [Display(Name = "Буква/букви")]
        public string Alphabet { get; set; }

        [Display(Name = "С обезличени данни")]
        public bool ReplaceEgn { get; set; }
    }
}
