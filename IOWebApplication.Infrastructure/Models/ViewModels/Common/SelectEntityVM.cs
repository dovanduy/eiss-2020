﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace IOWebApplication.Infrastructure.Models.ViewModels.Common
{
    public class SelectEntityVM
    {
        public int[] SourceTypes { get; set; }

        public string SelectEntityCallback { get; set; }

        public SelectEntityVM()
        {

        }
    }
}
