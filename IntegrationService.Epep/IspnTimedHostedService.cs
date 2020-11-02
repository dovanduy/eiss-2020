﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using IOWebApplicationService.Infrastructure.Contracts;
using IOWebApplicationService.Infrastructure.Services;

namespace IntegrationService.Epep
{
    public class IspnTimedHostedService : BaseTimedHostedService
    {
        public IspnTimedHostedService(
            IConfiguration configuration,
            IHostingEnvironment environment,
            ILogger<IspnTimedHostedService> logger,
            IApplicationLifetime appLifetime,
            IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.appLifetime = appLifetime;
            this.environment = environment;
            this.interval = configuration.GetValue<double>("TimerInterval");
            this.stopServiceTimeout = configuration.GetValue<int>("StopServiceTimeout");
            this.serviceProvider = serviceProvider;
        }

        public override void TimerElapsedAction()
        {
            using (var scope = serviceProvider.CreateScope())
            {
                this.logger.LogDebug("ispnService started");
                var ispnService = scope.ServiceProvider.GetService<IISPNCaseService>();
                ispnService.PushMQ().Wait();
                this.logger.LogDebug("ispnService ended");
            }
        }
    }
}
