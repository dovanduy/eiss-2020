﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System.ComponentModel.DataAnnotations.Schema;

namespace IOWebApplication.Infrastructure.Data.Models.Nomenclatures
{
    /// <summary>
    /// Статус на заседание
    /// </summary>
    [Table("nom_session_state")]
    public class SessionState : BaseCommonNomenclature
    {
        [Column("is_initial_state")]
        public bool? IsInitialState { get; set; }
    }
}
