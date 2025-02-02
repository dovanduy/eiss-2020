﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace IOWebApplication.Core.Models
{
    public class ShowLogModel
    {
        public string Controller { get; set; }
        public string Action { get; set; }
        public string ObjectKey { get; set; }
        public string ButtonLabel { get; set; }
        public string Title { get; set; }
        public bool IconOnly { get; set; }
    }
}
