﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Xml.Serialization;

namespace IOWebApplication.Infrastructure.Models.Integrations.Eispp
{
    /// <summary>
    /// Съобщение изпратено от ЕИСПП
    /// </summary>
    [Serializable]
    [XmlRoot("EISPPMessage")]
    public class EisppMessage : EisppProperties
    {
        /// <summary>
        /// EISPPPAKET
        /// Пакет за ЕИСПП
        /// </summary>
        [XmlElement("EISPPPAKET")]
        public EisppPackage EisspPackage { get; set; }
    }
}
