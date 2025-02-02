﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace IOWebApplication.Infrastructure.Data.Models.Money
{
    /// <summary>
    /// Ид-та на задълженията, влезнали към Изпълнителен лист
    /// </summary>
    [Table("money_exec_list_obligation")]
    public class ExecListObligation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("obligation_id")]
        public int ObligationId { get; set; }

        [Column("exec_list_id")]
        public int ExecListId { get; set; }

        [ForeignKey(nameof(ObligationId))]
        public virtual Obligation Obligation { get; set; }

        [ForeignKey(nameof(ExecListId))]
        public virtual ExecList ExecList { get; set; }
    }
}
