﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IOWebApplication.Infrastructure.Models.ViewModels.Case
{
    public class CaseForArchiveVM
    {
        public int Id { get; set; }

        [Display(Name = "Точен вид дело")]
        public string CaseTypeLabel { get; set; }

        [Display(Name = "Статус")]
        public string CaseStateLabel { get; set; }

        [Display(Name = "Регистрационен номер")]
        public string RegNumber { get; set; }

        [Display(Name = "Кратък номер")]
        public string ShortRegNumber { get; set; }

        [Display(Name = "Дата на регистрация")]
        public DateTime RegDate { get; set; }
    }
}
