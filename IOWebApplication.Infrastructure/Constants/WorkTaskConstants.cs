﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace IOWebApplication.Infrastructure.Constants
{
    public class WorkTaskConstants
    {
        public class Types
        {
            /// <summary>
            /// Разпределение на дело
            /// </summary>
            public const int Case_SelectLawUnit = 1;

            /// <summary>
            /// Предаване за съгласуване
            /// </summary>
            public const int CaseSessionAct_SentToCoordinate = 4;

            /// <summary>
            /// Съгласуване на документ
            /// </summary>
            public const int CaseSessionAct_Coordinate = 5;

            /// <summary>
            /// Изпращане за подписване
            /// </summary>
            public const int CaseSessionAct_SentToSign = 6;
            public const int CaseSessionAct_Sign = 7;
            public const int CaseSessionActCoordination_Sign = 8;
            public const int CaseSessionActMotives_SentToSign = 9;
            public const int CaseSessionActMotives_Sign = 10;
            public const int Document_Sign = 11;
            public const int ForReport = 12;
            public const int DocumentDecision = 13;
            public const int DocumentResolution_SentToSign = 14;
            public const int DocumentResolution_Sign = 15;
            public const int ForDocumentResolution = 16;

            /// <summary>
            /// Задачи, които се приключват сами и неможе да се приключват ръчно
            /// </summary>
            public static int[] SelfCompleteTasks = { CaseSessionAct_SentToSign, CaseSessionAct_Sign, CaseSessionActCoordination_Sign, CaseSessionActMotives_Sign };
            /// <summary>
            /// задачи, които немогат да бъдат редактирани и пренасочвани
            /// </summary>
            public static int[] TaskCantUpdate = { CaseSessionAct_Coordinate, CaseSessionAct_Sign, CaseSessionActCoordination_Sign, CaseSessionActMotives_Sign };
            public static int[] TaskCanChangeUser = { CaseSessionAct_Sign, CaseSessionActCoordination_Sign, CaseSessionActMotives_Sign};
        }

        public class States
        {
            /// <summary>
            /// Нова задача
            /// </summary>
            public const int New = 1;

            /// <summary>
            /// Приета
            /// </summary>
            public const int Accepted = 2;

            /// <summary>
            /// Приключила
            /// </summary>
            public const int Completed = 3;

            /// <summary>
            /// Пренасочена
            /// </summary>
            public const int Redirected = 4;

            /// <summary>
            /// Изтрита
            /// </summary>
            public const int Deleted = 5;

            public static readonly int[] NotFinished = { New, Accepted };
            public const int NotFinishedId = -2;
        }
        public class TaskExecution
        {
            /// <summary>
            /// От потребител
            /// </summary>
            public const int ByUser = 1;

            /// <summary>
            /// От отдел
            /// </summary>
            public const int ByOrganization = 2;
        }
    }
}
