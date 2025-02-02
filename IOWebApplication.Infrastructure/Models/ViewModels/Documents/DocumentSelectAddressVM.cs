﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace IOWebApplication.Infrastructure.Models.ViewModels.Documents
{
    public class DocumentSelectAddressVM
    {
        public long Id { get; set; }
        public bool IsChecked { get; set; }
        public string AddressTypeName { get; set; }
        public string FullAddress { get; set; }
    }
}
