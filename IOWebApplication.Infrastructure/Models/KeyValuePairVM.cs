﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace IOWebApplication.Infrastructure.Models
{
    public class KeyValuePairVM
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Label { get; set; }

        public override string ToString()  {
            return Key+":" + Value; 
        }
    }
}
