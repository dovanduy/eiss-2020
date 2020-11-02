﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IOWebApplicationService.Infrastructure.Contracts
{
    public interface IISPNCaseService : IBaseMQService
    {
        Task TestMQ();
    }
}

