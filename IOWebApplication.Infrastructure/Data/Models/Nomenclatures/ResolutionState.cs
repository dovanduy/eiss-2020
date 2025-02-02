﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace IOWebApplication.Infrastructure.Data.Models.Nomenclatures
{
    /// <summary>
    /// Номенклатура статус на  Разпореждания по документи
    /// </summary>
    [Table("nom_resolution_state")]
    public class ResolutionState : BaseCommonNomenclature
    {
    }
}
