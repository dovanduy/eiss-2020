﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

namespace IOWebApplication.Infrastructure.Models.ViewModels.Common
{
    public class ExternalDataEditVM
    {
        public long Id { get; set; }
        public int SourceType { get; set; }
        public string SourceId { get; set; }
        public int IntegrationTypeId { get; set; }
        public string Title { get; set; }
        public string FormData { get; set; }
        public object Data { get; set; }
    }
}
