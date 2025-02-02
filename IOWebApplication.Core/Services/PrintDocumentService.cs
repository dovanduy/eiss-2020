﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IOWebApplication.Core.Contracts;
using IOWebApplication.Core.Extensions;
using IOWebApplication.Core.Helper.GlobalConstants;
using IOWebApplication.Infrastructure.Constants;
using IOWebApplication.Infrastructure.Contracts;
using IOWebApplication.Infrastructure.Data.Common;
using IOWebApplication.Infrastructure.Data.Models;
using IOWebApplication.Infrastructure.Data.Models.Cases;
using IOWebApplication.Infrastructure.Data.Models.Common;
using IOWebApplication.Infrastructure.Data.Models.Documents;
using IOWebApplication.Infrastructure.Data.Models.Identity;
using IOWebApplication.Infrastructure.Data.Models.Money;
using IOWebApplication.Infrastructure.Data.Models.Nomenclatures;
using IOWebApplication.Infrastructure.Models;
using IOWebApplication.Infrastructure.Models.ViewModels;
using IOWebApplication.Infrastructure.Models.ViewModels.Case;
using IOWebApplication.Infrastructure.Models.ViewModels.Common;
using IOWebApplication.Infrastructure.Models.ViewModels.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZXing;
using ZXing.QrCode;

namespace IOWebApplication.Core.Services
{
    public class PrintDocumentService : BaseService, IPrintDocumentService
    {
        private readonly ICourtLawUnitService lawUnitService;
        private readonly ICommonService commonService;
        private readonly ICaseMigrationService caseMigrationService;
        private readonly ICaseNotificationService caseNotificationService;
        private readonly ICaseFastProcessService caseFastProcessService;
        public PrintDocumentService(
        ILogger<PrintDocumentService> _logger,
        IRepository _repo,
        IUserContext _userContext,
        ICourtLawUnitService _lawUnitService,
        ICaseMigrationService _caseMigrationService,
        ICommonService _commonService,
        ICaseNotificationService _caseNotificationService,
        ICaseFastProcessService _caseFastProcessService)
        {
            logger = _logger;
            repo = _repo;
            userContext = _userContext;
            lawUnitService = _lawUnitService;
            commonService = _commonService;
            caseMigrationService = _caseMigrationService;
            caseNotificationService = _caseNotificationService;
            caseFastProcessService = _caseFastProcessService;
        }

        #region Fill Key Value Pair

        private IList<KeyValuePairVM> fillList_CaseNotificationDELIVERER(CaseNotification notification)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var delivererName = notification.ToCourtId == notification.CourtId ? notification.LawUnit?.FullName_MiddleNameInitials ?? "" : "";
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DELIVERER}", Label = "Име П. Фамилия на съдебния призовкар", Value = delivererName });
            return keyValuePairs;
        }

        private void AddConnectsEisspNumber(Case model, List<KeyValuePairVM> keyValuePairs)
        {
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_CONNECTS}",
                Label = "Вид, номер , година на служебно / свързано дело, наименование на съдебната инстация",
                Value = model.CaseType?.Label + " №" + model.RegNumber + "/" + model.RegDate.Year.ToString() + "г." +
                         (string.IsNullOrEmpty(model.EISSPNumber) == false ? (", " + model.EISSPNumber) : "")
            });
        }

        private IList<KeyValuePairVM> fillList_Case(Case model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            AddConnectsEisspNumber(model, keyValuePairs);
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_KIND}", Label = "Вид на делото (точен)", Value = model.CaseType?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_NO}", Label = "Номер на делото", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_YEAR}", Label = "Година на делото", Value = model.RegDate.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.Court?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_ADDRESS}", Label = "Адрес на съда", Value = model.Court?.Address });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_PHONE}", Label = "< Телефонен номер на съда>", Value = model.Court?.PhoneNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_EMAIL}", Label = "E-mail адрес на съд", Value = model.Court?.Email });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_UCLP}", Label = "Населено място, в което се намира съда", Value = model.Court?.CityName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_QUICK}", Label = "Бързо производство", Value = (model.ProcessPriorityId == NomenclatureConstants.ProcessPriority.Quick) ? "Бързо производство" : "" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_TYPE}", Label = "Точен вид дело", Value = model.CaseType.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUBJECT}", Label = "< Статистически код / описание на предмета на делото>", Value = model.CaseCode?.Code + " / " + model.CaseCode?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_TEXT}", Label = "< Допълнителен текст към делото>", Value = model.Description });

            if (model.Document != null)
            {
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TRANSCRIPT_DESC}", Label = "Вид на входящия документ(съпровождащ документ)", Value = model.Document.DocumentGroup.Label });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TRANSCRIPT_NO}", Label = "Номер на входящия документ", Value = model.Document.DocumentNumber });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TRANSCRIPT_DATE}", Label = "Дата на входящия документ(искова молба)", Value = model.Document.DocumentDate.ToString(FormattingConstant.NormalDateFormat) });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TRANSCRIPT_YEAR}", Label = "Година на входящия документ", Value = model.Document.DocumentDate.Year.ToString() });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GENERAL_REQUEST_KIND}", Label = "Точен вид на входящ документ за образуване на дело", Value = model.Document.DocumentType.Label });
                string F_TRANSCRIPT_SENDERS = "";
                if (model.Document?.DocumentPersons != null)
                    F_TRANSCRIPT_SENDERS = string.Join(" ", model.Document.DocumentPersons.Select(x => x.FullName));
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TRANSCRIPT_SENDERS}", Label = "Подател на входящия документ", Value = F_TRANSCRIPT_SENDERS });

                string caseAttourney = "";
                if (model.Document.DocumentInstitutionCaseInfo != null)
                {
                    caseAttourney = string.Join(", ", model.Document
                               .DocumentInstitutionCaseInfo
                               .Where(x => x.Institution.InstitutionTypeId == NomenclatureConstants.InstitutionTypes.Attourney)
                               .Select(x => x.InstitutionCaseType.Label + " " + x.CaseNumber + "/" + x.CaseYear)
                               .ToList());
                }
                
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FLOOR_LAWSUIT}", Label = "Дела на прокуратура", Value = caseAttourney });
            }
            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_CaseXORIGIN(Case model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_XORIGIN_KIND}", Label = "Вид на делото (точен)", Value = model.CaseType?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_XORIGIN_NO}", Label = "Номер на делото", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_XORIGIN_YEAR}", Label = "Година на делото", Value = model.RegDate.Year.ToString() });
            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_CaseLower(int caseId)
        {
            var model = repo.AllReadonly<Case>()
                            .Include(x => x.Document)
                            .ThenInclude(x => x.DocumentCaseInfo)
                            .Where(x => x.Id == caseId)
                            .FirstOrDefault();
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            int? sessionActId = model.Document?.DocumentCaseInfo?.FirstOrDefault()?.SessionActId;
            int? courtId = model.Document?.DocumentCaseInfo?.FirstOrDefault()?.CourtId;
            int? caseId2 = model.Document?.DocumentCaseInfo?.FirstOrDefault()?.CaseId;
            string F_LOWER_DECISION_KIND = "";
            string F_LOWER_COURT = "";
            string F_LOWER_LAWSUIT = "";
            if (sessionActId != null)
            {
                var sessionAct = repo.AllReadonly<CaseSessionAct>()
                                     .Include(x => x.ActType)
                                     .Where(x => x.Id == sessionActId)
                                     .FirstOrDefault();
                F_LOWER_DECISION_KIND = sessionAct?.ActType?.Label ?? "";
            }
            if (courtId != null)
            {
                var court = repo.AllReadonly<Court>()
                                .Where(x => x.Id == courtId)
                                .FirstOrDefault();
                F_LOWER_COURT = court?.Label ?? "";
            }
            if (caseId2 != null)
            {
                var case2 = repo.AllReadonly<Case>()
                                .Where(x => x.Id == caseId2)
                                 .FirstOrDefault();

                F_LOWER_LAWSUIT = case2?.RegNumber;
            }
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_DECISION_KIND}", Label = "Вид на съдебния акт на подчинения съд, който се обжалва в текущата съдебна инстанция", Value = F_LOWER_DECISION_KIND });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_COURT}", Label = "<Наименовение на съда>", Value = F_LOWER_COURT });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_LAWSUIT}", Label = "<Номер и година на първоинстанционното/ въззивно дело>", Value = F_LOWER_LAWSUIT });
            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_BankAccounts(int courtId)
        {
            var bankAccounts = repo.AllReadonly<CourtBankAccount>()
                                   .Where(x => x.IsActive && x.CourtId == courtId)
                                   .OrderBy(x => x.Id)
                                   .ToList();
            var baBUDGET = bankAccounts.FirstOrDefault(x => x.MoneyGroupId == NomenclatureConstants.MoneyGroups.Budget);
            var baDEPOSIT = bankAccounts.FirstOrDefault(x => x.MoneyGroupId == NomenclatureConstants.MoneyGroups.Deposit);
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEPOSITS_BIC}", Label = "<BIC код на банка>", Value = baDEPOSIT?.Code });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEPOSITS_BANK_NAME}", Label = "<Наименование на банка>", Value = baDEPOSIT?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEPOSITS_ACCOUNT}", Label = "<Номер на депозитната сметка на съда>", Value = baDEPOSIT?.Iban });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BUDGET_BIC}", Label = "<BIC код на банка>", Value = baBUDGET?.Code });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BUDGET_BANK_NAME}", Label = "<Наименование на банка>", Value = baBUDGET?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BUDGET_ACCOUNT}", Label = "<Банкова сметка - бюджет>", Value = baBUDGET?.Iban });

            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_UpperCourt(int courtId)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            Court court = null;
            var courtRegion = repo.AllReadonly<Court>()
                                  .Where(x => x.Id == courtId)
                                  .Select(x => x.CourtRegion.ParentRegion)
                                  .FirstOrDefault();
            if (courtRegion != null)
                court = repo.AllReadonly<Court>()
                                  .Where(x => x.CourtRegionId == courtRegion.Id &&
                                              x.CourtTypeId != NomenclatureConstants.CourtType.Аdministrative &&
                                              x.CourtTypeId != NomenclatureConstants.CourtType.VAS)
                                  .FirstOrDefault();
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_RECEIVER}", Label = "<Наименовение на висшата инстанция, компетентна да разгледа жалбата>", Value = court?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INSTANCE_III}", Label = "<Наименовение на висшата инстанция>", Value = court?.Label });

            return keyValuePairs;

        }
        private IList<KeyValuePairVM> fillList_CaseMigration(List<CaseMigrationVM> model)
        {
            var caseMigration = string.Empty;

            foreach (var migrationVM in model)
            {
                caseMigration += migrationVM.MigrationTypeName + ": " + migrationVM.SentToName + " " + migrationVM.CaseRegNumber + "/" + migrationVM.CaseRegDate.ToString("dd.MM.yyyy") + "; ";
            }

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RENEW_LAWSUITS}", Label = "Списък на всички свързани дела", Value = caseMigration });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_Document(Document model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OUTREG_NO}", Label = "Изходящ номер", Value = model.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OUTREG_DATE}", Label = "Дата", Value = model.DocumentDate.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TODAY}", Label = "Дата", Value = model.DocumentDate.ToString("dd.MM.yyyy") });

            //този параметър го има само за Европейското наследство и е F_TODAY + 6 месеца. Ако трябва да го има и другаде с различна стойност да е друг параметър
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_VALID_DATE}", Label = "Дата валидност", Value = model.DocumentDate.AddMonths(6).ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_PRESENSE_REQUIRED}", Label = "< Допълнителен текст към писмото>", Value = model.Description });
            var firstPerson = model.DocumentPersons.FirstOrDefault();
            if (firstPerson != null)
            {
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Получател", Value = firstPerson?.FullName });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LRECEIVER}", Label = "Получател", Value = firstPerson?.FullName });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_UCN}", Label = "ЕГН Получател", Value = firstPerson?.Uic });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INVOLVEMENT}", Label = "Качество", Value = firstPerson?.PersonRole?.Label });
                if (firstPerson.Addresses.Any())
                {
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_ADDRESS}", Label = "Адрес на получателя", Value = firstPerson.Addresses.FirstOrDefault()?.Address.FullAddress });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LRECEIVER_ADDRESS}", Label = "Адрес на получателя", Value = firstPerson.Addresses.FirstOrDefault()?.Address.FullAddress });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAST_KNOWN_ADDRESS}", Label = "Адрес на получателя", Value = firstPerson.Addresses.FirstOrDefault()?.Address.FullAddress });
                }
            }

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_CasePersons(ICollection<CasePerson> models, int? casePersonId, int caseTypeId)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            bool? isLeftSide = null;
            string _leftSide = string.Empty;
            foreach (var casePerson in models.Where(x => x.PersonRole.RoleKindId == NomenclatureConstants.PersonKinds.LeftSide))
            {
                _leftSide += (!string.IsNullOrEmpty(_leftSide)) ? ", " : string.Empty;
                _leftSide += casePerson.FullName;
                if (casePerson.Id == casePersonId)
                    isLeftSide = true;
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUITORS}", Label = "Име на ищците", Value = _leftSide });

            //Ако е АНД Административно наказващ орган се брои за дясна страна
            string _rightSide = string.Empty;
            foreach (var casePerson in models.Where(x => (x.PersonRole.RoleKindId == NomenclatureConstants.PersonKinds.RightSide)
                                        || (caseTypeId == NomenclatureConstants.CaseTypes.AND && 
                                           x.PersonRoleId == NomenclatureConstants.PersonRole.AdministrativeMember)
                                                   ))
            {
                _rightSide += (!string.IsNullOrEmpty(_rightSide)) ? ", " : string.Empty;
                _rightSide += casePerson.FullName;
                if (casePerson.Id == casePersonId)
                    isLeftSide = false;
            }

            switch (isLeftSide)
            {
                case true:
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OPPOSITE_SIDES}", Label = "Ответни страни", Value = _rightSide });
                    break;
                case false:
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OPPOSITE_SIDES}", Label = "Ответни страни", Value = _leftSide });
                    break;
                default:
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OPPOSITE_SIDES}", Label = "Ответни страни", Value = "" });
                    break;
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEFENDANTS}", Label = "Име П.Фамилия на ответник (дясна страна)", Value = _rightSide });

            var debtor = models.Where(x => x.PersonRoleId == NomenclatureConstants.PersonRole.Debtor).FirstOrDefault();

            if (debtor != null)
            {
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TARGET_BULSTAT}", Label = "ЕИК/БУЛСТАТ на фирмата длъжник", Value = debtor.Uic });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TARGET_DEBTOR}", Label = "Име на фирмата длъжни", Value = debtor.FullName });
            }

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_CaseLawUnitFull(ICollection<CaseLawUnit> models, bool setFullNameFJudge)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            var judgeReporter = models.Where(x => x.JudgeRoleId == NomenclatureConstants.JudgeRole.JudgeReporter).Select(x => x.LawUnit).FirstOrDefault();
            string nameReporter = "";
            string nameReporterFirstNameInitial = "";
            CourtDepartment courtDepartment = null;
            if (judgeReporter != null)
            {
                nameReporter = judgeReporter.FullName_MiddleNameInitials;
                nameReporterFirstNameInitial = setFullNameFJudge == true ? judgeReporter.FullName : judgeReporter.FullName_MiddleNameInitials;
                courtDepartment = models.Where(x => x.LawUnitId == judgeReporter.Id && x.CourtDepartmentId != null).Select(x => x.CourtDepartment).FirstOrDefault();
            }
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COMPOSITION}", Label = "Съдебен състав", Value = courtDepartment?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JUDGE_NAME}", Label = "Имена на съдията-докладчик по делото", Value = nameReporter });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JUDGE}", Label = "Имена на съдията-докладчик по делото", Value = nameReporterFirstNameInitial });

            keyValuePairs.AddRange(fillList_CourtDepartment(courtDepartment?.ParentDepartment));

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_Notification(CaseNotification model, List<CaseSessionNotificationListVM> caseSessionNotificationLists, int courtId)
        {
            string position = lawUnitService.GetLawUnitPosition(courtId, model.User?.LawUnitId ?? 0);
            if (string.IsNullOrEmpty(position))
                position = "Служител";
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            string F_RECEIVER = model.NotificationPersonName;
            if (!string.IsNullOrEmpty(model.NotificationPersonDuty))
                F_RECEIVER += " като (" + model.NotificationPersonDuty + ")";
            if (model.IsMultiLink == true)
            {
                F_RECEIVER = "";
                foreach (var link in model.CaseNotificationMLinks.Where(x => x.IsActive && x.IsChecked == true))
                    F_RECEIVER += link.PersonSummonedName + (string.IsNullOrEmpty(link.PersonSummonedRole) ? " " : $" ({link.PersonSummonedRole}) ");
                F_RECEIVER += $"чрез {model.NotificationPersonName} ({model.NotificationPersonDuty})";
            }

            var caseSessionNotifications = caseSessionNotificationLists != null ? caseSessionNotificationLists.Where(x => x.PersonId == (model.NotificationPersonType == NomenclatureConstants.NotificationPersonType.CasePerson ? model.CasePersonId : model.CaseLawUnitId)).FirstOrDefault() : null;

            string F_SESSION_RESULT = "";
            if (model.CaseSessionId > 0)
            {
                var sessionResults = repo.AllReadonly<CaseSessionResult>()
                                         .Where(x => x.CaseSessionId == model.CaseSessionId)
                                         .Include(x => x.SessionResult)
                                         .ToList();
                if (sessionResults.Any())
                    F_SESSION_RESULT = string.Join(" ", sessionResults.Select(x => x.SessionResult.Label + " " + x.Description));
            }
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Име Презиме Фамилия на Получателя", Value = F_RECEIVER });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_INVOLVEMENT}", Label = "Процесуално качество на получателя", Value = model.NotificationPersonDuty });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INVOLVEMENT}", Label = "Качеството на лицето в съдебния процес", Value = model.NotificationPersonDuty });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SIDE_NO}", Label = "Номер на страната в списъка на лицата за призоваване", Value = (caseSessionNotifications != null ? caseSessionNotifications.RowNumber.ToString() : string.Empty) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_NOTIFIEDS}", Label = "Получатели (Име Презиме Фамилия на получатели)", Value = F_RECEIVER });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_ADDRESS}", Label = "Адрес на връчване", Value = model.NotificationAddress?.FullAddressNotification() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ADDITIONAL_TEXT}", Label = "Допълнителен текст - указания", Value = model.Description });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_POST}", Label = "Длъжност на лице, което съставя документа в съда", Value = position });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_NAME}", Label = "И.(инициал на първо име)Фамилия", Value = model.User?.LawUnit?.FullName_MiddleNameInitials });

            var F_USER_INITIALS = model.User?.LawUnit?.FullName_Initials ?? "";

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_INITIALS}", Label = "Инициали на потребителя - И(ме)Ф(амилия)", Value = F_USER_INITIALS });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BARCODE}", Label = "Баркод - линеен", Value = "<div id='barcode-holder' name='barcode-holder'></div>" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BARDIGIT}", Label = "Баркод - цифров формат", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SESSION_RESULT}", Label = "Допълнителен текст - указани", Value = F_SESSION_RESULT });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EXPERT_REPORT}", Label = "Описание на предмета и задачата на експеризата", Value = model.ExpertReport });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EXPERT_DEADATE}", Label = "Крайна дата за изговяне на експертиза", Value = model.ExpertDeadDate?.ToString(FormattingConstant.NormalDateFormat) });

            var barcode_css = "#barcode-holder { margin-bottom: 10px; width: 200px;height: 40px; background: url('data:image/png;base64," + GenerateBarcode(model.RegNumber) + "') 100% 100% no-repeat;}";
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BARCODE_CSS}", Label = "Баркод - линеен", Value = barcode_css });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_Notification_ForLetter(CaseNotification model)
        {
            var keyValuePairs = new List<KeyValuePairVM>();
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Име Презиме Фамилия на Получателя", Value = model.NotificationPersonName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_INVOLVEMENT}", Label = "Процесуално качество на получателя", Value = model.NotificationPersonDuty });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INVOLVEMENT}", Label = "Качеството на лицето в съдебния процес", Value = model.NotificationPersonDuty });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REGARD_ADDRESS}", Label = "Адрес на връчване", Value = model.NotificationAddress?.FullAddressNotification() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REGARD_SUBPOENA_DATE}", Label = "Дата на призовката", Value = model.RegDate.ToString(FormattingConstant.NormalDateFormat) });
            var casePerson = repo.AllReadonly<CasePerson>()
                                 .Where(x => x.Id == model.CasePersonId)
                                 .FirstOrDefault();
            if (casePerson != null)
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_UCN}", Label = "ЕГН Осъден", Value = casePerson.Uic });
            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_SessionNotificationList(CaseSessionNotificationList model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SIDE_NO}", Label = "Номер на страната в списъка на лицата за призоваване", Value = model?.RowNumber.ToString() ?? "" });
            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_Session(CaseSession model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SESSION_DATE}", Label = "Дата на заседание", Value = model.DateFrom.Date.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SESSION_TIME}", Label = "Час на насрочване", Value = model.DateFrom.ToString("HH:mm") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FLOOR}", Label = "Номер на етаж", Value = model.CourtHall?.Location });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_HALL}", Label = "Наименование  на съдебна зала", Value = model.CourtHall?.Name });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_MULTI_DATE_TIME}", Label = "Дата и начален час на многодневното заседание", Value = model.DateFrom.ToString("dd.MM.yyyy HH:mm") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_HALL_ADDRESS}", Label = "Адрес на съдебна зала", Value = model.CourtHall?.Location });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_SessionAct(CaseSessionAct model, ActComplainResult actComplainResult)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_NO}", Label = "<Номер на съдебен акт>", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATE}", Label = "<Дата насъдебен акт>", Value = model.RegDate?.ToString(FormattingConstant.NormalDateFormat) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_KIND}", Label = "<Вид на съдебен акт>", Value = model.ActType?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RETURNED}", Label = "<Дата на обявяване на съдебния акт>", Value = model.ActDeclaredDate?.ToString(FormattingConstant.NormalDateFormat) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RETURNED_MOTIVES}", Label = "<Дата на обявяване на мотивите>", Value = model.ActMotivesDeclaredDate?.ToString(FormattingConstant.NormalDateFormat) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_APPEAL}", Label = "<Указания за възможността за обжалване>", Value = model.CanAppeal == true ? " подлежи на обжалване" : " не подлежи на обжалване" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_PERSONAL_FORCE_DATE}", Label = "Дата на влизане в сила", Value = model.ActInforcedDate?.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FIRST_SET_NO_YEAR}", Label = "<Номер на съдебен акт>", Value = $"{model.RegNumber}/{model.RegDate?.Year}" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_TEXT}", Label = "<Диспозитив на събния акт>", Value = model.Description });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FORCE_DATE}", Label = "<Дата на влизане в законна сила>", Value = model.ActInforcedDate?.ToString(FormattingConstant.NormalDateFormat) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_STATUS}", Label = "<Резултат от разпорежне  по докумет от входящ регистър>", Value = actComplainResult?.Label });
            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_SessionActComplain(CaseSessionActComplain model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_KIND}", Label = "Вид на документа от входящ регистър", Value = model.ComplainDocument.DocumentType.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_NO}/{F_INREG_YEAR}г.", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_NO}/{F_INREG_YEAR}г.", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_NO}/{F_INREG_YEAR} г.", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_NO}/{F_INREG_YEAR}  г.", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_NO}/{F_INREG_YEAR}   г.", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_NO}", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_YEAR}", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentDate.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REQUEST_NO}", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REQUEST_YEAR}", Label = "Номер на документа от входящ регистър", Value = model.ComplainDocument.DocumentDate.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_DATE}", Label = "Дата на постъпване на входящ документ", Value = model.ComplainDocument.DocumentDate.ToString(FormattingConstant.NormalDateFormat) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_RESULT}", Label = "Резултат от разпорежне  по документ от входящ регистър>", Value = model.ComplainState.Label });

            string F_INREG_SENDER = string.Join(" ", model.CasePersons.Select(x => x.CasePerson.FullName));
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_SENDER}", Label = "Подател на входящия документ", Value = F_INREG_SENDER });

            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_CaseFastProcess(int caseId)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var model = caseFastProcessService.CaseFastProcess_Select(caseId);
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TAX_SUM}", Label = "<Сума на държавната такса>", Value = model?.TaxAmount.ToString("0.00") });
            return keyValuePairs;
        }
        private IList<KeyValuePairVM> fillList_CourtDepartment(CourtDepartment model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            string initials = "";
            if (model != null)
            {
                string[] list = model.Label.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                initials = string.Join("", list.Select(x => x.Substring(0, 1).ToUpper()));
            }
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COLLEGE}", Label = "Колегия/отделение", Value = model?.Label ?? "" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COLLEGE_INITIALS}", Label = "Колегия/отделение", Value = initials });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_All()
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TODAY}", Label = "Дата на изготвяне на призовката - Текуща дата", Value = DateTime.Now.ToString("dd.MM.yyyy") });

            return keyValuePairs;
        }

        #endregion

        #region Fill Blank

        #region Fill String From Alias And List Key Value Pair

        public List<KeyValuePairVM> KeyValuePairVMFromNotification(CaseNotification caseNotification, HtmlTemplate htmlTemplate)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var caseCase = Read_Case(caseNotification.CaseId);

            List<CaseSessionNotificationListVM> caseSessionNotificationLists = null;
            if (caseNotification.CaseSessionId > 0)
                caseSessionNotificationLists = caseNotificationService.CaseSessionNotificationList_Select(caseNotification.CaseSessionId ?? 0, (caseNotification.NotificationTypeId == NomenclatureConstants.NotificationType.Subpoena ? SourceTypeSelectVM.CaseSessionNotificationList : (caseNotification.NotificationTypeId == NomenclatureConstants.NotificationType.Message ? SourceTypeSelectVM.CaseSessionNotificationListMessage : SourceTypeSelectVM.CaseSessionNotificationListNotification))).ToList();

            keyValuePairs.AddRange(fillList_Notification(caseNotification, caseSessionNotificationLists, caseCase?.CourtId ?? 0));
            string html = "";
            if (htmlTemplate != null)
                html = HtmlTemplateContentAsString(htmlTemplate);

            if (caseNotification.CaseId > 0)
            {
                keyValuePairs.AddRange(fillList_Case(caseCase));

                var caseMigrations = Read_CaseMigration(caseNotification.CaseId);
                keyValuePairs.AddRange(fillList_CaseMigration(caseMigrations));

                if (html == "" || html.Contains("{F_BUDGET_") || html.Contains("{F_DEPOSITS_"))
                    keyValuePairs.AddRange(fillList_BankAccounts(caseCase.CourtId));
                if (html == "" || html.Contains("{F_TAX_SUM}"))
                    keyValuePairs.AddRange(fillList_CaseFastProcess(caseNotification.CaseId));
                if (html == "" || html.Contains("{F_INREG_RECEIVER}") || html.Contains("{F_INSTANCE_III}"))
                    keyValuePairs.AddRange(fillList_UpperCourt(caseCase.CourtId));
                if (html == "" || html.Contains("{F_LOWER_"))
                    keyValuePairs.AddRange(fillList_CaseLower(caseNotification.CaseId));
            }


            if ((caseNotification.CaseSessionId ?? 0) != 0)
            {
                var caseSession = Read_CaseSession(caseNotification.CaseSessionId ?? 0);
                keyValuePairs.AddRange(fillList_Session(caseSession));
            }

            if ((caseNotification.CaseSessionActId ?? 0) != 0)
            {
                var caseSessionAct = Read_CaseSessionAct(caseNotification.CaseSessionActId ?? 0);
                var actComplainResult = Read_ActComplainResult(caseSessionAct.ActComplainResultId ?? 0);
                keyValuePairs.AddRange(fillList_SessionAct(caseSessionAct, actComplainResult));
            }

            if ((caseNotification.CaseSessionActComplainId ?? 0) != 0)
            {
                var caseSessionActComplain = Read_CaseSessionActComplain(caseNotification.CaseSessionActComplainId ?? 0);
                keyValuePairs.AddRange(fillList_SessionActComplain(caseSessionActComplain));
            }

            var casePersons = Read_CasePersons(caseNotification.CaseId, caseNotification.CaseSessionId);
            keyValuePairs.AddRange(fillList_CasePersons(casePersons, caseNotification.CasePersonId, caseCase.CaseTypeId));

            var caseLawUnits = Read_CaseLawUnit(caseNotification.CaseId, caseNotification.CaseSessionId);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));

            if ((caseNotification.CaseSessionId ?? 0) != 0)
            {
                var caseSessionNotificationList = Read_CaseSessionNotificationList((caseNotification.CaseSessionId ?? 0), caseNotification.NotificationPersonType, (caseNotification.NotificationPersonType == NomenclatureConstants.NotificationPersonType.CasePerson) ? (caseNotification.CasePersonId ?? 0) : (caseNotification.CaseLawUnitId ?? 0));
                keyValuePairs.AddRange(fillList_SessionNotificationList(caseSessionNotificationList));
            }

            keyValuePairs.AddRange(fillList_All());
            keyValuePairs.AddRange(fillList_CaseNotificationDELIVERER(caseNotification));
            return keyValuePairs;
        }

        public List<KeyValuePairVM> KeyValuePairVMFromNotification_ForLetter(CaseNotification caseNotification, List<KeyValuePairVM> kvFromDoc)
        {
            var keyValuePairs = new List<KeyValuePairVM>();
            var caseLawUnits = Read_CaseLawUnit(caseNotification.CaseId, caseNotification.CaseSessionId);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));
            keyValuePairs.AddRange(fillList_Court(caseNotification.CourtId ?? 0));

            var caseCase = Read_Case(caseNotification.CaseId);
            keyValuePairs.AddRange(fillList_CaseXORIGIN(caseCase));
            keyValuePairs.AddRange(fillList_Notification_ForLetter(caseNotification));
            if ((caseNotification.CaseSessionId ?? 0) != 0)
            {
                var caseSession = Read_CaseSession(caseNotification.CaseSessionId ?? 0);
                keyValuePairs.AddRange(fillList_Session(caseSession));
            }
            if ((caseNotification.CaseSessionActId ?? 0) != 0)
            {
                var caseSessionAct = Read_CaseSessionAct(caseNotification.CaseSessionActId ?? 0);
                var actComplainResult = Read_ActComplainResult(caseSessionAct.ActComplainResultId ?? 0);
                keyValuePairs.AddRange(fillList_SessionAct(caseSessionAct, actComplainResult));
            }
            keyValuePairs.AddRange(fillList_UpperCourt(caseCase.CourtId));
            kvFromDoc = kvFromDoc.Where(d => !keyValuePairs.Any(x => x.Key == d.Key)).ToList();
            kvFromDoc.AddRange(keyValuePairs);
            return kvFromDoc;
        }
        #endregion

        #region Read Data From Data Base

        private Case Read_Case(int caseId)
        {
            return repo.AllReadonly<Case>()
                .Include(x => x.CaseType)
                .Include(x => x.Court)
                .Include(x => x.Document)
                .ThenInclude(x => x.DocumentGroup)
                .Include(x => x.Document)
                .ThenInclude(x => x.DocumentType)
                .Include(x => x.Document)
                .ThenInclude(x => x.DocumentPersons)
                .ThenInclude(x => x.Person)
                .Include(x => x.Document)
                .ThenInclude(x => x.DocumentInstitutionCaseInfo)
                .ThenInclude(x => x.Institution)
                .Include(x => x.Document)
                .ThenInclude(x => x.DocumentInstitutionCaseInfo)
                .ThenInclude(x => x.InstitutionCaseType)
                .Where(x => x.Id == caseId)
                .FirstOrDefault();

        }

        private List<CaseMigrationVM> Read_CaseMigration(int CaseId)
        {
            return caseMigrationService.Select(CaseId).ToList();
        }

        private ICollection<CaseLawUnit> Read_CaseLawUnit(int caseId, int? caseSessionId)
        {
            return repo.AllReadonly<CaseLawUnit>()
                .Include(x => x.LawUnit)
                .Include(x => x.JudgeRole)
                .Include(x => x.CourtDepartment)
                .Include(x => x.CourtDepartment.ParentDepartment)
                .Where(x => x.CaseId == caseId && ((x.CaseSessionId ?? 0) == (caseSessionId ?? 0)) &&
                            !NomenclatureConstants.JudgeRole.ManualRoles.Contains(x.JudgeRoleId))
                .Where(x => (x.DateTo ?? DateTime.MaxValue) >= (x.CaseSessionId == null ? DateTime.Now : x.CaseSession.DateFrom))
                .ToList();
        }

        private ICollection<CasePerson> Read_CasePersons(int caseId, int? caseSessionId)
        {
            return repo.AllReadonly<CasePerson>()
                .Include(x => x.PersonRole)
                .Include(x => x.Addresses)
                .Where(x => x.CaseId == caseId && ((x.CaseSessionId ?? 0) == (caseSessionId ?? 0)))
                .ToList();
        }
        
        private CaseSession Read_CaseSession(int caseSessionId)
        {
            return repo.AllReadonly<CaseSession>()
                .Include(x => x.CourtHall)
                .Where(x => x.Id == caseSessionId)
                .FirstOrDefault();
        }
        private CaseSessionAct Read_CaseSessionAct(int caseSessionActId)
        {
            return repo.AllReadonly<CaseSessionAct>()
                .Include(x => x.ActType)
                .Where(x => x.Id == caseSessionActId)
                .FirstOrDefault();
        }
        private ActComplainResult Read_ActComplainResult(int actComplainResultId)
        {
            return repo.AllReadonly<ActComplainResult>()
                .Where(x => x.Id == actComplainResultId)
                .FirstOrDefault();
        }

        private CaseSessionActComplain Read_CaseSessionActComplain(int caseSessionActComplainId)
        {
            return repo.AllReadonly<CaseSessionActComplain>()
                .Include(x => x.ComplainDocument)
                .ThenInclude(x => x.DocumentType)
                .Include(x => x.ComplainState)
                .Include(x => x.CasePersons)
                .ThenInclude(x => x.CasePerson)
                .Where(x => x.Id == caseSessionActComplainId)
                .FirstOrDefault();
        }

        private CaseNotification Read_CaseNotification(int caseNotificationId)
        {
            return repo.AllReadonly<CaseNotification>()
                .Include(x => x.NotificationAddress)
                .Include(x => x.HtmlTemplate)
                .Include(x => x.CaseNotificationMLinks)
                .Include(x => x.User)
                .ThenInclude(x => x.LawUnit)
                .Include(x => x.LawUnit)
                .Where(x => x.Id == caseNotificationId)
                .FirstOrDefault();
        }

        private List<CaseSessionNotificationListVM> Read_CaseNotificationList(int CaseSessionId)
        {
            return caseNotificationService.CaseSessionNotificationList_Select(CaseSessionId, SourceTypeSelectVM.CaseSessionNotificationList).ToList();
        }

        private CaseSessionNotificationList Read_CaseSessionNotificationList(int caseSessionId, int NotificationPersonType, int PersonId)
        {
            return repo.AllReadonly<CaseSessionNotificationList>()
                .Include(x => x.CaseLawUnit)
                .Include(x => x.CasePerson)
                .Where(x => x.CaseSessionId == caseSessionId &&
                            x.NotificationPersonType == NotificationPersonType &&
                            ((NotificationPersonType == NomenclatureConstants.NotificationPersonType.CasePerson) ? (x.CasePersonId == PersonId) : (x.CaseLawUnitId == PersonId)))
                .FirstOrDefault();
        }

        #endregion

        #endregion

        private string ReplaceString(string Source, string Find, string Replace)
        {
            return Source.Replace(Find, Replace);
        }

        public TinyMCEVM FillHtmlTemplateNotification(int caseNotificationId)
        {
            var caseNotification = Read_CaseNotification(caseNotificationId);

            int htmlTemplateId = caseNotification.HtmlTemplateId ?? 0;
            var html = repo.AllReadonly<HtmlTemplate>().Where(x => x.Id == htmlTemplateId).DefaultIfEmpty(null).FirstOrDefault();

            var keyValuePairs = KeyValuePairVMFromNotification(caseNotification, html);
            if ((caseNotification.HtmlTemplateId ?? 0) == 0)
                return null;
            return FillHtmlTmplateToTinyMCEVM(html, keyValuePairs);
        }
        public TinyMCEVM FillHtmlTemplateNotificationTestOne(int caseNotificationId, int htmlTemplateId)
        {
            var caseNotification = Read_CaseNotification(caseNotificationId);

            var html = repo.AllReadonly<HtmlTemplate>().Where(x => x.Id == htmlTemplateId).DefaultIfEmpty(null).FirstOrDefault();

            var keyValuePairs = KeyValuePairVMFromNotification(caseNotification, html);
            if (html == null)
                return null;
            return FillHtmlTmplateToTinyMCEVM(html, keyValuePairs);
        }

        public TinyMCEVM FillHtmlTemplateNotificationTest(int caseNotificationId)
        {
            var caseNotification = Read_CaseNotification(caseNotificationId);
            var keyValuePairs = KeyValuePairVMFromNotification(caseNotification, null);
            // 11 призовка 
            // 22 Съобщение
            // 23 Уведомление
            var htmls = repo.AllReadonly<HtmlTemplate>()
                .Where(x => x.HtmlTemplateTypeId == 11 || x.HtmlTemplateTypeId == 22 || x.HtmlTemplateTypeId == 23)
                .OrderBy(x => x.HtmlTemplateTypeId)
                .ThenBy(x => x.Id)
                .ToList();
            string errors = "";
            //Dictionary<string>
            foreach (var item in htmls)
            {
                if (item.Id == 9) // Граждански брак
                    continue;
                var tiny = GetTinyMCEVMFromHtmlTemplates(item.Id, keyValuePairs);
                string str = tiny.Text;
                errors += $"{item.Label} {item.FileName} {item.Id} <br>" + Environment.NewLine;
                int idx = str.IndexOf("{");
                while (idx > 0)
                {
                    int idxTo = str.IndexOf("}", idx);
                    if (idxTo >= 0)
                    {
                        errors += str.Substring(idx, idxTo - idx + 1) + "<br>" + Environment.NewLine;
                        idx = str.IndexOf("{", idxTo + 1);
                    }
                    else
                    {
                        errors += str.Substring(idx, str.Length - idx + 1) + "<br>" + Environment.NewLine;
                        break;
                    }

                }
            }
            TinyMCEVM result = new TinyMCEVM();
            result.Text = errors;
            return result;
        }
        public void FillHtmlTemplateNotificationHaveSaveTest(int caseNotificationId)
        {
            var caseNotification = Read_CaseNotification(caseNotificationId);
            // var keyValuePairs = KeyValuePairVMFromNotification(caseNotification);
            // 11 призовка 
            // 22 Съобщение
            // 23 Уведомление
            var htmls = repo.AllReadonly<HtmlTemplate>().Where(x => x.HtmlTemplateTypeId == 11 || x.HtmlTemplateTypeId == 22 || x.HtmlTemplateTypeId == 23).ToList();
            foreach (var item in htmls)
            {
                var tiny = ConvertToTinyMCVM(item);
                if (tiny.Text.Contains("{F_EVENT_NO}") || tiny.Text.Contains("{F_EVENT_DATE}"))
                    item.HaveSessionAct = true;
                if (tiny.Text.Contains("{F_INREG_NO}") || tiny.Text.Contains("{F_INREG_YEAR}"))
                    item.HaveSessionActComplain = true;
                if (tiny.Text.Contains("{F_EXPERT_REPORT}") || tiny.Text.Contains("{F_EXPERT_DEADATE}"))
                    item.HaveExpertReport = true;
                if (item.HaveSessionAct == true || item.HaveSessionActComplain == true || item.HaveExpertReport == true)
                {
                    repo.Update(item);

                }
            }
            repo.SaveChanges();
        }
        private string GenerateBarcode(string barcode)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_39,
                Options = new QrCodeEncodingOptions { Width = 200, Height = 40, Margin = 1 }
            };

            var result = writer.Write(barcode);
            var base64str = string.Empty;

            using (var bitmap = new System.Drawing.Bitmap(result.Width, result.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, result.Width, result.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    try
                    {
                        // we assume that the row stride of the bitmap is aligned to 4 byte multiplied by the width of the image   
                        System.Runtime.InteropServices.Marshal.Copy(result.Pixels, 0, bitmapData.Scan0, result.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    // PNG or JPEG or whatever you want
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    base64str = Convert.ToBase64String(ms.ToArray());
                }
            }

            return base64str;
        }

        private string HtmlTemplateContentAsString(HtmlTemplate htmlTemplate)
        {
            string htmlText;
            Stream stream = new MemoryStream(htmlTemplate.Content);
            using (StreamReader sr = new StreamReader(stream))
                htmlText = sr.ReadToEnd();
            if (string.IsNullOrEmpty(htmlText))
                htmlText = "";
            return htmlText;
        }
        private string ReplaceImageHr(string html, string imageName, string lineName)
        {
            int posImg = html.IndexOf(imageName);
            if (posImg > 0)
            {
                int posImgStart = 0;
                int newPosStart = 0;
                while (newPosStart >= 0 && newPosStart < posImg)
                {
                    posImgStart = newPosStart;
                    newPosStart = html.IndexOf("<img", posImgStart + 1);
                }
                int posImgEnd = html.IndexOf(">", posImg);
                if (posImgEnd > 0 && posImgStart > 0)
                {
                    string html1 = html.Substring(0, posImgStart);
                    string html2 = html.Substring(posImgEnd + 1, html.Length - posImgEnd - 1);
                    html = html1 + lineName + html2;
                }
            }
            return html;
        }
        private string replacePageBreak(string htmlText)
        {
            //"<div style = 'page-break-before:always' /> ";
            if (htmlText.IndexOf("page-break-before") > 0)
            {
                int toPos = htmlText.IndexOf("page-break-before");
                int spanPos = htmlText.IndexOf("<span");
                if (spanPos > 0)
                    while (spanPos < toPos)
                    {
                        int spanPos2 = htmlText.IndexOf("<span", spanPos + 1);
                        if (spanPos2 <= 0)
                            break;
                        if (spanPos2 > toPos)
                            break;
                        spanPos = spanPos2;
                    }
                if (spanPos > 0 && (spanPos < toPos))
                {
                    htmlText = htmlText.Substring(0, spanPos) + "<div style='page-break-before:always'/>" + htmlText.Substring(spanPos, htmlText.Length - spanPos);
                }
            }
            return htmlText;
        }

        /// <summary>
        /// Създава обект за преглед и редакция на съдържание в пълнотекстов редактор
        /// </summary>
        /// <param name="htmlTemplate"></param>
        /// <param name="preparedBlank"></param>
        /// <returns></returns>
        public TinyMCEVM ConvertToTinyMCVM(HtmlTemplate htmlTemplate, string preparedBlank = null)
        {
            if (htmlTemplate == null)
            {
                return null;
            }
            HtmlTemplate htmlTemplateFrame = null;
            if (htmlTemplate.FrameTemplateId != null)
                htmlTemplateFrame = repo.GetById<HtmlTemplate>(htmlTemplate.FrameTemplateId);

            HtmlTemplate htmlTemplateStyle = null;
            if (htmlTemplate.StyleTemplateId != null)
                htmlTemplateStyle = repo.GetById<HtmlTemplate>(htmlTemplate.StyleTemplateId);

            TinyMCEVM htmlModel = new TinyMCEVM();
            htmlModel.Id = htmlTemplate.Id;
            htmlModel.SmartShrinkingPDF = htmlTemplate.SmartShrinkingPDF == true;
            string htmlText = string.IsNullOrEmpty(preparedBlank) ? HtmlTemplateContentAsString(htmlTemplate) : preparedBlank;
            string htmlCSS = "";

            int styleStart = htmlText.IndexOf("<style>");
            int styleEnd = htmlText.IndexOf("</style>");
            if ((styleStart >= 0) && (styleEnd >= 0))
            {
                htmlCSS = htmlText.Substring(styleStart, styleEnd - styleStart + "</style>".Length);
                htmlText = htmlText.Substring(styleEnd + "</style>".Length);
            }
            htmlCSS = htmlCSS.Replace("<style>", "");
            htmlCSS = htmlCSS.Replace("</style>", "");
            htmlCSS = htmlCSS.Replace("<!--", "");
            htmlCSS = htmlCSS.Replace("-->", "");

            htmlCSS = htmlCSS + Environment.NewLine + "{F_BARCODE_CSS}" + Environment.NewLine;
            htmlCSS = htmlCSS + Environment.NewLine + "{F_COURT_LOGO_CSS}" + Environment.NewLine;
            int bodyStart = htmlText.IndexOf("<body");
            if (bodyStart >= 0)
                bodyStart = htmlText.IndexOf(">", bodyStart);
            int bodyEnd = htmlText.IndexOf("</body>");
            if ((bodyStart >= 0) && (bodyEnd >= 0))
            {
                htmlText = htmlText.Substring(bodyStart + ">".Length, bodyEnd - bodyStart - ">".Length);
            }
            int pStart = htmlText.IndexOf("<p class=MsoNormal><span lang=EN-GB");
            if (pStart >= 0)
            {
                int pEnd = htmlText.IndexOf("</p>", pStart);
                if (pEnd >= 0)
                    htmlText = htmlText.Substring(0, pStart) + htmlText.Substring(pEnd + "</p>".Length, htmlText.Length - pEnd - "</p>".Length);
            }

            htmlText = htmlText.Replace("{F_LAWSUIT_NO}/{F_LAWSUIT_YEAR}г.", "{F_LAWSUIT_NO}");
            htmlText = ReplaceImageHr(htmlText, "image001.gif", "<hr class=\"hr1line\"/>");
            htmlText = ReplaceImageHr(htmlText, "image002.gif", "<hr class=\"hr2line\"/>");
            //htmlText = replacePageBreak(htmlText);
            htmlModel.Text = htmlText;
            if (htmlTemplateFrame != null)
            {
                htmlModel.Text = HtmlTemplateContentAsString(htmlTemplateFrame).Replace("{Document}", htmlModel.Text);
            }
            htmlModel.Style = htmlCSS;
            htmlModel.PageOrientation = 1;
            if (htmlTemplateStyle != null)
            {
                string lineHeight = "";
                if (htmlTemplate.LineHeight != null && htmlTemplate.LineHeight > 0.1m)
                    lineHeight = Environment.NewLine + "body { line-height: " + (htmlTemplate.LineHeight ?? 0.8m).ToString("0.00").Replace(",", ".") + "; }";
                htmlModel.Style = HtmlTemplateContentAsString(htmlTemplateStyle) + lineHeight + Environment.NewLine + htmlModel.Style;
                if (htmlTemplateStyle.Label.IndexOf("Landscape") > 0)
                    htmlModel.PageOrientation = 0;
            }
            else
            {
                htmlModel.Style = "body { line-height: 0.80; } " + Environment.NewLine +
                                   "@media print { tr { page-break-inside: avoid; } }" + Environment.NewLine + htmlModel.Style;
            }
            return htmlModel;
        }
        private TinyMCEVM FillHtmlTmplateToTinyMCEVM(HtmlTemplate html, IList<KeyValuePairVM> keyValuePairs, string preparedBlank = null)
        {
            var tinyMCEVM = ConvertToTinyMCVM(html, preparedBlank);
            if (keyValuePairs != null)
            {
                foreach (var keyValue in keyValuePairs)
                    tinyMCEVM.Text = ReplaceString(tinyMCEVM.Text, keyValue.Key, keyValue.Value);
                foreach (var keyValue in keyValuePairs)
                    tinyMCEVM.Style = ReplaceString(tinyMCEVM.Style, keyValue.Key, keyValue.Value);
            }
            return tinyMCEVM;
        }
        public TinyMCEVM GetTinyMCEVMFromHtmlTemplates(string alias, IList<KeyValuePairVM> keyValuePairs, string preparedBlank = null)
        {
            var html = repo.AllReadonly<HtmlTemplate>().Where(x => x.Alias.ToUpper() == alias.ToUpper()).DefaultIfEmpty(null).FirstOrDefault();
            return FillHtmlTmplateToTinyMCEVM(html, keyValuePairs, preparedBlank);
        }
        public TinyMCEVM GetTinyMCEVMFromHtmlTemplates(int htmlTemplateId, IList<KeyValuePairVM> keyValuePairs, string preparedBlank = null)
        {
            var html = repo.AllReadonly<HtmlTemplate>().Where(x => x.Id == htmlTemplateId).DefaultIfEmpty(null).FirstOrDefault();
            return FillHtmlTmplateToTinyMCEVM(html, keyValuePairs, preparedBlank);
        }

        private TinyMCEVM FillHtmlTemplatePaymentPos(int paymentId)
        {
            var model = repo.AllReadonly<Payment>()
                .Include(x => x.Court)
                .Include(x => x.User)
                .Include(x => x.User.LawUnit)
                .Include(x => x.CourtBankAccount)
                .Include(x => x.PosPaymentResults)
                .Include(x => x.ObligationPayments)
                .ThenInclude(x => x.Obligation)
                .Where(x => x.Id == paymentId)
                .FirstOrDefault();
            string keyData = "";
            string moneyTypeData = "";
            var activeObligationPayments = model.ObligationPayments.Where(x => x.IsActive == true).ToList();
            if (activeObligationPayments.Count > 0)
            {
                var documentIds = activeObligationPayments.Where(x => x.Obligation.DocumentId != null).Select(x => x.Obligation.DocumentId).Distinct().ToList();
                if (documentIds.Count > 0)
                {
                    var documentData = repo.AllReadonly<Document>()
                            .Include(x => x.DocumentType)
                            .Where(x => documentIds.Contains(x.Id))
                            .Select(x => x.DocumentType.Label + " " + x.DocumentNumber)
                            .ToList();
                    keyData = String.Join(", ", documentData);
                }
                else
                {
                    var actIds = activeObligationPayments.Where(x => x.Obligation.CaseSessionActId != null).Select(x => x.Obligation.CaseSessionActId).Distinct().ToList();
                    var caseData = repo.AllReadonly<CaseSessionAct>()
                            .Include(x => x.CaseSession)
                            .Include(x => x.CaseSession.Case)
                            .Include(x => x.CaseSession.Case.CaseType)
                            .Where(x => actIds.Contains(x.Id))
                            .Select(x => x.CaseSession.Case.CaseType.Label + " " + x.CaseSession.Case.RegNumber)
                            .Distinct()
                            .ToList();
                    keyData = String.Join(", ", caseData);
                }

                var moneyTypeIds = activeObligationPayments.Select(x => x.Obligation.MoneyTypeId).Distinct().ToList();
                moneyTypeData = String.Join(", ", repo.AllReadonly<MoneyType>()
                                                        .Where(x => moneyTypeIds.Contains(x.Id))
                                                        .Select(x => x.Label)
                                                        .Distinct()
                                                        .ToList()
                                            );

            }

            string position = lawUnitService.GetLawUnitPosition(model.CourtId, model.User.LawUnitId);

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            PosPaymentResult posData = model.PosPaymentResults.Where(x => x.Status == MoneyConstants.PosPaymentResultStatus.StatusOk).DefaultIfEmpty(new PosPaymentResult()).FirstOrDefault();
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.Court.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REGNUMBER}", Label = "Разписка №", Value = model.PaymentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_STAMP}", Label = "Дата и час", Value = model.PaidDate.ToString("dd.MM.yyyy HH:mm") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TERM_ID}", Label = "Терминал", Value = posData.Tid });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_AUTH_ID}", Label = "Авторизационен код", Value = posData.Authcode });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ACCOUNT}", Label = "Сметка", Value = model.CourtBankAccount.Iban });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM}", Label = "Сума", Value = model.Amount.ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_PERSONS}", Label = "Платец", Value = model.SenderName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_CARD_ID}", Label = "Номер на карта", Value = posData.Cardid });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_KEY}", Label = "Вид дело/документ, номер, година", Value = keyData });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_TYPE}", Label = "Вид сума", Value = moneyTypeData });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TEXT}", Label = "Пояснение", Value = model.Description });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_NAME}", Label = "Потребител", Value = model.User.LawUnit.FullName_MiddleNameInitials });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_POST}", Label = "Длъжност", Value = position });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_WORDS}", Label = "Словом", Value = MoneyExtensions.MoneyToString(model.Amount) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TRANS_ID}", Label = "Транзакция", Value = posData.Rrn });

            return GetTinyMCEVMFromHtmlTemplates("Payments", keyValuePairs);
        }

        public TinyMCEVM FillHtmlTemplatePayment(int paymentId)
        {
            var model = repo.AllReadonly<Payment>()
                .Where(x => x.Id == paymentId)
                .FirstOrDefault();
            if (model.PaymentTypeId == NomenclatureConstants.PaymentType.Pos)
            {
                return FillHtmlTemplatePaymentPos(paymentId);
            }
            else if (model.PaymentTypeId == NomenclatureConstants.PaymentType.Bank)
            {
                return null; //FillHtmlTemplatePaymentEarningsBank(paymentId);
            }
            else
            {
                return null;
            }
        }

        private TinyMCEVM FillHtmlTemplatePaymentEarningsBank(int paymentId)
        {
            var model = repo.AllReadonly<Payment>()
                .Include(x => x.User)
                .Include(x => x.User.LawUnit)
                .Include(x => x.CourtBankAccount)
                .Include(x => x.ObligationPayments)
                .ThenInclude(x => x.Obligation)
                .Include(x => x.Court)
                .Where(x => x.Id == paymentId)
                .FirstOrDefault();

            int[] moneytypes = { NomenclatureConstants.MoneyType.Earnings, NomenclatureConstants.MoneyType.Transport };
            var activeObligationPayments = model.ObligationPayments
                   .Where(x => x.IsActive == true)
                   .Where(x => moneytypes.Contains(x.Obligation.MoneyTypeId))
                   .ToList();
            string judge = "";
            if (activeObligationPayments.Count > 0)
            {
                var sessions = activeObligationPayments.Where(x => x.Obligation.CaseSessionId != null).Select(x => x.Obligation.CaseSessionId).Distinct().ToList();
                var caseLawUnit = repo.AllReadonly<CaseLawUnit>()
                                            .Include(x => x.CaseSession)
                                            .Where(x => sessions.Contains(x.CaseSessionId))
                                            .Where(x => x.DateFrom <= x.CaseSession.DateFrom && (x.DateTo ?? x.CaseSession.DateFrom.AddDays(1)) >= x.CaseSession.DateFrom)
                                            .Where(x => x.JudgeRoleId == NomenclatureConstants.JudgeRole.JudgeReporter)
                                            .Select(x => x.LawUnitId)
                                            .Distinct()
                                            .ToList();
                if (caseLawUnit.Count == 1)
                {
                    judge = repo.AllReadonly<LawUnit>().Where(x => x.Id == caseLawUnit[0]).FirstOrDefault()?.FullName_MiddleNameInitials ?? "";
                }
                else if (caseLawUnit.Count > 1)
                {
                    var generalJudge = lawUnitService.GetGeneralJudgeCourtLawUnit(model.CourtId);
                    judge = generalJudge?.LawUnit?.FullName_MiddleNameInitials ?? "";
                }
            }
            else
            {
                return null;
            }

            string persons = String.Join(", ", activeObligationPayments.Select(x => x.Obligation.FullName).Distinct().ToList());
            decimal sum1 = activeObligationPayments.Where(x => x.Obligation.MoneyTypeId == NomenclatureConstants.MoneyType.Earnings).Select(x => x.Amount).DefaultIfEmpty(0).Sum();
            decimal sum2 = activeObligationPayments.Where(x => x.Obligation.MoneyTypeId == NomenclatureConstants.MoneyType.Transport).Select(x => x.Amount).DefaultIfEmpty(0).Sum();
            var courtLawUnit = lawUnitService.GetCourtLawUnitAllDatabyLawUnitId(model.CourtId, model.User.LawUnitId);
            string college = "";
            if (courtLawUnit != null && courtLawUnit.CourtOrganization != null)
                college = courtLawUnit.CourtOrganization.Label;

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_PAID_NOTE}", Label = "Пояснение", Value = model.Description });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.Court.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COLLEGE}", Label = "Структура", Value = college });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATE}", Label = "Дата", Value = model.PaidDate.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ACCOUNT}", Label = "Сметка", Value = model.CourtBankAccount.Iban });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Работник", Value = persons });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_1}", Label = "Сума", Value = sum1.ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_2}", Label = "Сума", Value = sum2.ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM}", Label = "Сума", Value = (sum1 + sum2).ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_NAME}", Label = "Потребител", Value = model.User.LawUnit.FullName_MiddleNameInitials });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JUDGE}", Label = "Съдия", Value = judge });

            return GetTinyMCEVMFromHtmlTemplates("RKO2", keyValuePairs);
        }

        public TinyMCEVM FillHtmlTemplateExpenseOrder(int orderId)
        {
            var model = repo.AllReadonly<ExpenseOrder>()
                .Include(x => x.User)
                .Include(x => x.User.LawUnit)
                .Include(x => x.Court)
                .Include(x => x.ExpenseOrderObligations)
                .ThenInclude(x => x.Obligation)
                .ThenInclude(x => x.CaseSessionAct)
                .Where(x => x.Id == orderId)
                .FirstOrDefault();

            string judge = "";
            int judgeId = 0;
            LawUnit lawUnitJudge = null;
            string courtBankAccountName = "";
            string sessionData = "";
            string courtDepartmentName = "";
            if (model != null)
            {
                var sessions = model.ExpenseOrderObligations
                                        .Where(x => x.Obligation.CaseSessionId != null || x.Obligation.CaseSessionActId != null)
                                        .Select(x => x.Obligation.CaseSessionId ?? x.Obligation.CaseSessionAct.CaseSessionId).Distinct().ToList();

                var caseLawUnit = repo.AllReadonly<CaseLawUnit>()
                                            .Include(x => x.CaseSession)
                                            .Where(x => sessions.Contains(x.CaseSessionId ?? 0))
                                            .Where(x => x.DateFrom <= x.CaseSession.DateFrom && (x.DateTo ?? x.CaseSession.DateFrom.AddDays(1)) >= x.CaseSession.DateFrom)
                                            .Where(x => x.JudgeRoleId == NomenclatureConstants.JudgeRole.JudgeReporter)
                                            .Select(x => x.LawUnitId)
                                            .Distinct()
                                            .ToList();

                sessionData = String.Join(", ", repo.AllReadonly<CaseSession>()
                                            .Include(x => x.SessionType)
                                            .Include(x => x.Case)
                                            .Include(x => x.Case.CaseType)
                                            .Where(x => sessions.Contains(x.Id))
                                            .Select(x => x.SessionType.Label + "/" + x.DateFrom.ToString("dd.MM.yyyy HH:mm") + " г. по " +
                                                    x.Case.CaseType.Label + "  № " + x.Case.RegNumber + "/" + x.Case.RegDate.ToString("dd.MM.yyyy"))
                                            .Distinct()
                                            .ToList());
                if (caseLawUnit.Count == 1)
                {
                    lawUnitJudge = repo.AllReadonly<LawUnit>().Where(x => x.Id == caseLawUnit[0]).FirstOrDefault();
                    judge = lawUnitJudge.FullName;
                    judgeId = caseLawUnit[0];
                }
                else if (caseLawUnit.Count > 1)
                {
                    var generalJudge = lawUnitService.GetGeneralJudgeCourtLawUnit(model.CourtId);
                    if (generalJudge != null)
                    {
                        lawUnitJudge = generalJudge.LawUnit;
                        judge = generalJudge.LawUnit.FullName;
                        judgeId = generalJudge.LawUnitId;
                    }
                }

                if (sessions.Count == 1)
                {
                    courtDepartmentName = repo.AllReadonly<CaseLawUnit>()
                                                .Include(x => x.CaseSession)
                                                .Include(x => x.CourtDepartment)
                                                .Include(x => x.CourtDepartment.DepartmentType)
                                                .Where(x => sessions.Contains(x.CaseSessionId ?? 0))
                                                .Where(x => x.DateFrom <= x.CaseSession.DateFrom && (x.DateTo ?? x.CaseSession.DateFrom.AddDays(1)) >= x.CaseSession.DateFrom)
                                                .Where(x => x.CourtDepartmentId != null)
                                                .Where(x => !NomenclatureConstants.JudgeRole.ManualRoles.Contains(x.JudgeRoleId))
                                                .Select(x => x.CourtDepartment.DepartmentType.Label + " " + x.CourtDepartment.Label)
                                                .DefaultIfEmpty("")
                                                .FirstOrDefault();
                }

                int moneyTypeId = model.ExpenseOrderObligations.Select(x => x.Obligation.MoneyTypeId).FirstOrDefault();
                var courtBankAccount = commonService.GetCourtBankAccountForMoneyType(moneyTypeId);
                courtBankAccountName = courtBankAccount == null ? courtBankAccount.Iban : "";
            }
            else
            {
                return null;
            }

            string personName = model.ExpenseOrderObligations.Select(x => x.Obligation.FullName).DefaultIfEmpty("").FirstOrDefault();
            decimal sum1 = model.ExpenseOrderObligations
                         .Where(x => NomenclatureConstants.MoneyType.EarningList.Contains(x.Obligation.MoneyTypeId))
                         .Select(x => x.Obligation.Amount).DefaultIfEmpty(0).Sum();
            decimal sum2 = model.ExpenseOrderObligations.Where(x => x.Obligation.MoneyTypeId == NomenclatureConstants.MoneyType.Transport).Select(x => x.Obligation.Amount).DefaultIfEmpty(0).Sum();

            string college = "";
            if (judgeId > 0)
            {
                var courtLawUnit = lawUnitService.GetCourtLawUnitAllDatabyLawUnitId(model.CourtId, model.User.LawUnitId);
                if (courtLawUnit != null && courtLawUnit.CourtOrganization != null)
                    college = courtLawUnit.CourtOrganization.Label;
            }

            string userInitials = "";
            if (string.IsNullOrEmpty(model.User.LawUnit.FirstName) == false && string.IsNullOrEmpty(model.User.LawUnit.FamilyName) == false)
                userInitials = model.User.LawUnit.FirstName.First() + "." + model.User.LawUnit.FamilyName.First() + ".";

            string judgeInitials = "";
            if (lawUnitJudge != null && string.IsNullOrEmpty(lawUnitJudge.FirstName) == false && string.IsNullOrEmpty(lawUnitJudge.FamilyName) == false)
                judgeInitials = lawUnitJudge.FirstName.First() + "." + lawUnitJudge.FamilyName.First() + ".";

            string iban = model.BankName + ", IBAN: " + (model.Iban ?? "") + ", BIC: " + (model.BIC ?? "");

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_PAID_NOTE}", Label = "По вн. документ No", Value = model.PaidNote });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.Court.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COLLEGE}", Label = "Структура", Value = college });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COLLEGE_DATA}", Label = "Състав", Value = courtDepartmentName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATE}", Label = "Дата", Value = model.RegDate.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_NUMBER}", Label = "Дата", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATA}", Label = "Заседание", Value = sessionData });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ACCOUNT}", Label = "Сметка", Value = courtBankAccountName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Работник", Value = personName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_1}", Label = "Сума", Value = sum1.ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_2}", Label = "Сума", Value = sum2.ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM}", Label = "Сума", Value = (sum1 + sum2).ToString("0.00") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_NAME}", Label = "Потребител", Value = model.User.LawUnit.FullName_MiddleNameInitials });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JUDGE}", Label = "Съдия", Value = judge });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_INITIALS}", Label = "Инициали потребител", Value = userInitials });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JUDGE_INITIALS}", Label = "Инициали съдия", Value = judgeInitials });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUM_TEXT}", Label = "Словом", Value = MoneyExtensions.MoneyToString(sum1 + sum2) });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REGION}", Label = "Регион", Value = model.RegionName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FIRM_NAME}", Label = "Служител при", Value = model.FirmName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FIRM_CITY}", Label = "Служител при населено място", Value = model.FirmCity });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_IBAN}", Label = "Банкова сметка", Value = iban });

            return GetTinyMCEVMFromHtmlTemplates("RKO2", keyValuePairs);
        }

        private IList<KeyValuePairVM> fillList_DocumentGeneral(DocumentTemplate model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.AddRange(fillList_Court(model.CourtId));

            Case caseModel = null;
            if (model.CaseId != null)
            {
                caseModel = Read_Case(model.CaseId ?? 0);
                keyValuePairs.AddRange(fillList_Case(caseModel));

                var casePersons = Read_CasePersons(model.CaseId ?? 0, null);
                keyValuePairs.AddRange(fillList_CasePersons(casePersons, null, caseModel.CaseTypeId));

                var caseSession = repo.AllReadonly<CaseSession>()
                                   .Where(x => x.CaseId == model.CaseId && x.DateExpired == null)
                                   .Where(x => x.SessionType.SessionTypeGroup == NomenclatureConstants.CaseSessionTypeGroup.PublicSession)
                                   .OrderBy(x => x.DateFrom)
                                   .FirstOrDefault();

                if (caseSession != null)
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SCHEDULE_SESSION}", Label = "Дата на насрочване", Value = caseSession.DateFrom.ToString(FormattingConstant.NormalDateFormatHHMM) });
            }

            Document documentModel = null;
            if (model.DocumentId > 0)
            {
                documentModel = repo.AllReadonly<Document>()
                                            .Include(x => x.DocumentPersons)
                                            .ThenInclude(x => x.Addresses)
                                            .ThenInclude(x => x.Address)
                                            .Include(x => x.DocumentPersons)
                                            .ThenInclude(x => x.PersonRole)
                                            .Include(x => x.User)
                                            .Include(x => x.User.LawUnit)
                                            .Where(x => x.Id == model.DocumentId)
                                            .FirstOrDefault();
                keyValuePairs.AddRange(fillList_Document(documentModel));

                if (!string.IsNullOrEmpty(model.AuthorId))
                {
                    var authorUser = repo.AllReadonly<ApplicationUser>()
                                            .Include(x => x.LawUnit)
                                            .Where(x => x.Id == model.AuthorId)
                                            .FirstOrDefault();
                    keyValuePairs.AddRange(fillList_UserData(authorUser, documentModel.CourtId));
                }
                else
                {
                    keyValuePairs.AddRange(fillList_UserData(documentModel.User, documentModel.CourtId));
                }
            }

            if (NomenclatureConstants.HtmlTemplateAlias.HeritageLetters.Contains(model.HtmlTemplate.Alias))
                keyValuePairs.AddRange(fillList_DocumentHeritage(model.CaseId ?? 0));


            // Ako писмото е закачено към призовка/съобщение
            if (model.SourceType == SourceTypeSelectVM.CaseNotification)
            {
                var caseNotification = Read_CaseNotification((int)model.SourceId);
                if (caseNotification != null)
                {
                    keyValuePairs = KeyValuePairVMFromNotification_ForLetter(caseNotification, keyValuePairs);
                }
            }

            bool addCaseLawUnit = false;
            if (model.SourceType == SourceTypeSelectVM.CaseSessionAct)
            {
                var caseSessionAct = Read_CaseSessionAct((int)model.SourceId);
                if (caseSessionAct != null)
                {
                    keyValuePairs.AddRange(fillList_SessionAct(caseSessionAct, null));

                    var caseSession = Read_CaseSession(caseSessionAct.CaseSessionId);
                    keyValuePairs.AddRange(fillList_Session(caseSession));

                    var caseLawUnits = Read_CaseLawUnit(caseSessionAct.CaseId ?? 0, caseSessionAct.CaseSessionId);
                    keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));
                    addCaseLawUnit = true;
                }
            }

            if (model.CaseId > 0 && addCaseLawUnit == false)
            {
                var caseLawUnits = Read_CaseLawUnit(model.CaseId ?? 0, null);
                keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));
            }

            return keyValuePairs;
        }
        public TinyMCEVM FillHtmlTemplateDocumentTemplate(int id, string preparedBlank = null)
        {
            var model = repo.AllReadonly<DocumentTemplate>()
                             .Include(x => x.HtmlTemplate)
                             .Include(x => x.Author)
                             .Include(x => x.Author.LawUnit)
                             .Where(x => x.Id == id)
                             .FirstOrDefault();

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            switch (model.SourceType)
            {
                case SourceTypeSelectVM.CasePersonBulletin:
                    keyValuePairs.AddRange(fillList_DocumentBulletin(model));
                    break;
                case SourceTypeSelectVM.ExecList:
                    keyValuePairs.AddRange(fillList_DocumentExecList(model));
                    break;
                case SourceTypeSelectVM.CaseMigration:
                    keyValuePairs.AddRange(fillList_CaseMigration(model));
                    break;
                case SourceTypeSelectVM.CasePersonSentence:
                    keyValuePairs.AddRange(fillList_DocumentSentence(model));
                    break;
                case SourceTypeSelectVM.CaseSessionActDivorce:
                    keyValuePairs.AddRange(fillList_DocumentDivorce(model));
                    break;
                default:
                    keyValuePairs.AddRange(fillList_DocumentGeneral(model));
                    break;
            }

            int htmlTemplateId = model.HtmlTemplateId ?? 0;
            // 73, 74, 75, 76, 77, 78
            // htmlTemplateId = 77;
            return GetTinyMCEVMFromHtmlTemplates(htmlTemplateId, keyValuePairs, preparedBlank);
        }

        public TinyMCEVM FillHtmlTemplateCaseSessionActDivorce(int divorceId)
        {
            var model = repo.AllReadonly<CaseSessionActDivorce>()
                .Include(x => x.CaseSessionAct)
                .Include(x => x.CaseSessionAct.CaseSession)
                .Include(x => x.CaseSessionAct.CaseSession.Case)
                .Include(x => x.CaseSessionAct.CaseSession.Case.Court)
                .Include(x => x.CaseSessionAct.CaseSession.Case.CaseType)
                .Include(x => x.CasePersonMan)
                .Include(x => x.CasePersonMan.Addresses)
                .ThenInclude(x => x.Address)
                .Include(x => x.CasePersonWoman)
                .Include(x => x.CasePersonWoman.Addresses)
                .ThenInclude(x => x.Address)
                .Include(x => x.User)
                .Include(x => x.User.LawUnit)
                .Where(x => x.Id == divorceId)
                .FirstOrDefault();

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            string countryCode = "";
            if (string.IsNullOrEmpty(model.CountryCode) == false)
            {
                countryCode = repo.AllReadonly<EkCountry>().Where(x => x.Code == model.CountryCode).Select(x => x.Name).DefaultIfEmpty("").FirstOrDefault();
            }

            var cityName = "";
            var regionName = "";
            var areaName = "";
            var city = repo.AllReadonly<EkEkatte>()
                  .Include(x => x.District)
                  .Include(x => x.Munincipality)
                  .Where(x => x.Ekatte == model.CaseSessionAct.CaseSession.Case.Court.CityCode)
                  .FirstOrDefault();
            if (city != null)
            {
                cityName = city.Name;
                regionName = city.Munincipality.Name;
                areaName = city.District.Name;
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Наименование на текущия съд", Value = model.CaseSessionAct.CaseSession.Case.Court.Label });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_FORCE_DATE}",
                Label = "Дата на влизане в законна сила на съдебния акт",
                Value = model.CaseSessionAct.ActInforcedDate != null ? ((DateTime)model.CaseSessionAct.ActInforcedDate).ToString("dd.MM.yyyy") : ""
            });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_TYPE}", Label = "Точен вид дело", Value = model.CaseSessionAct.CaseSession.Case.CaseType.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_NO}", Label = "Номер на делото", Value = model.CaseSessionAct.CaseSession.Case.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_YEAR}", Label = "година на образуване на делото", Value = model.CaseSessionAct.CaseSession.Case.RegDate.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{FREG_NUMBER}", Label = "Номер", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_UCLP}", Label = "наименование на населеното място", Value = cityName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_REGION}", Label = "наименование на общината", Value = regionName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_AREA}", Label = "наименование на областта", Value = regionName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_NO}", Label = "номер на съдебния акт", Value = model.CaseSessionAct.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_EVENT_DATE}",
                Label = "дата на съдебния акт ",
                Value = model.CaseSessionAct.ActDate != null ? ((DateTime)model.CaseSessionAct.ActDate).ToString("dd.MM.yyyy") : ""
            });


            keyValuePairs.Add(new KeyValuePairVM() { Key = "{CountryCode}", Label = "Държава при прекратяване на брака в чужбина", Value = countryCode });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{CountryCodeDate}",
                Label = "Дата",
                Value = model.CountryCodeDate != null ? ((DateTime)model.CountryCodeDate).ToString("dd.MM.yyyy") : ""
            });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriageNumber}", Label = "Акт за сключен граждански брак №", Value = model.MarriageNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriageDate}", Label = "Акт за сключен граждански брак дата", Value = model.MarriageDate.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriagePlace}", Label = "Място на съставяне", Value = model.MarriagePlace });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriageFault}", Label = "По чия вина се прекратява брака", Value = model.MarriageFault });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriageFaultDescription}", Label = "Причина за прекратяване на брака", Value = model.MarriageFaultDescription });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{ChildrenUnder18}", Label = "Деца под 18 г.", Value = model.ChildrenUnder18.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{ChildrenOver18}", Label = "Деца над 18 г.", Value = model.ChildrenOver18.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_MAN_UCN}", Label = "ЕГН", Value = model.CasePersonMan.Uic });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_MAN_BIRTHDAY}", Label = "Дата на раждане", Value = model.BirthDayMan.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_MAN_NAME}", Label = "Име преди бракоразвода", Value = model.CasePersonMan.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{NameAfterMarriageMan}", Label = "Име след брака", Value = model.NameAfterMarriageMan });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_MAN_ADDRESS_1}",
                Label = "Постоянен адрес",
                Value = model.CasePersonMan.Addresses.Where(x => x.Address.AddressTypeId == NomenclatureConstants.AddressType.Permanent)
                           .Select(x => x.Address.FullAddress).DefaultIfEmpty("").FirstOrDefault()
            });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_MAN_ADDRESS_2}",
                Label = "Настоящ адрес",
                Value = model.CasePersonMan.Addresses.Where(x => x.Address.AddressTypeId == NomenclatureConstants.AddressType.Current)
                           .Select(x => x.Address.FullAddress).DefaultIfEmpty("").FirstOrDefault()
            });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriedStatusBeforeMan}", Label = "Сем. положение при сключване на брака", Value = model.MarriedStatusBeforeMan });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriageCountMan}", Label = "Поредност на брака", Value = model.MarriageCountMan.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{DivorceCountMan}", Label = "Поредност на развода", Value = model.DivorceCountMan.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{NationalityMan}", Label = "Гражданство", Value = model.NationalityMan });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{EducationMan}", Label = "Степен на образование", Value = model.EducationMan });


            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_WOMAN_UCN}", Label = "ЕГН", Value = model.CasePersonWoman.Uic });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_WOMAN_BIRTHDAY}", Label = "Дата на раждане", Value = model.BirthDayWoman.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_WOMAN_NAME}", Label = "Име преди бракоразвода", Value = model.CasePersonWoman.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{NameAfterMarriageWoman}", Label = "Име след брака", Value = model.NameAfterMarriageWoman });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_WOMAN_ADDRESS_1}",
                Label = "Постоянен адрес",
                Value = model.CasePersonWoman.Addresses.Where(x => x.Address.AddressTypeId == NomenclatureConstants.AddressType.Permanent)
                           .Select(x => x.Address.FullAddress).DefaultIfEmpty("").FirstOrDefault()
            });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_WOMAN_ADDRESS_2}",
                Label = "Настоящ адрес",
                Value = model.CasePersonWoman.Addresses.Where(x => x.Address.AddressTypeId == NomenclatureConstants.AddressType.Current)
                           .Select(x => x.Address.FullAddress).DefaultIfEmpty("").FirstOrDefault()
            });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriedStatusBeforeWoman}", Label = "Сем. положение при сключване на брака", Value = model.MarriedStatusBeforeWoman });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{MarriageCountWoman}", Label = "Поредност на брака", Value = model.MarriageCountWoman.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{DivorceCountWoman}", Label = "Поредност на развода", Value = model.DivorceCountWoman.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{NationalityWoman}", Label = "Гражданство", Value = model.NationalityWoman });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{EducationWoman}", Label = "Степен на образование", Value = model.EducationWoman });


            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_UCN}", Label = "ЕГН", Value = model.User.LawUnit.Uic });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FULL_USER_NAME}", Label = "Имена", Value = model.User.LawUnit.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TODAY}", Label = "Дата", Value = model.RegDate.ToString("dd.MM.yyyy") });

            return GetTinyMCEVMFromHtmlTemplates("ACTEX_M_R", keyValuePairs);
        }

        private IList<KeyValuePairVM> fillList_DocumentDivorce(DocumentTemplate documentTemplate)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var model = repo.AllReadonly<CaseSessionActDivorce>()
                               .Include(x => x.CaseSessionAct)
                               .Include(x => x.Case)
                               .Include(x => x.CasePersonMan)
                               .Include(x => x.CasePersonWoman)
                               .Where(x => x.Id == documentTemplate.SourceId)
                               .FirstOrDefault();

            keyValuePairs.AddRange(fillList_Court(model.CourtId ?? 0));

            keyValuePairs.AddRange(fillList_OnlyCaseData(model.Case));

            var caseLawUnits = Read_CaseLawUnit(model.CaseId ?? 0, model.CaseSessionAct.CaseSessionId);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));

            var caseSessionAct = Read_CaseSessionAct(model.CaseSessionActId);
            keyValuePairs.AddRange(fillList_SessionAct(caseSessionAct, null));

            keyValuePairs.AddRange(fillList_DocumentUser(documentTemplate));
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUITOR}", Label = "Ищец", Value = model.CasePersonMan.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_SUITOR_UCN}", Label = "Ищец", Value = model.CasePersonMan.Uic});

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEFENDANT}", Label = "Ответник", Value = model.CasePersonWoman.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEFENDANT_UCN}", Label = "Ответник", Value = model.CasePersonWoman.Uic });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_ExecList(ExecList model)
        {
            string debtors = string.Join(", ", model.ExecListObligations.Select(x => x.Obligation.FullName).Distinct());
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EXEC_LIST_NO}", Label = "Номер на ИЛ", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REGARD_SUBPOENA_DATE}", Label = "Дата на ИЛ", Value = model.RegDate.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ADDITIONAL_TEXT}", Label = "Допълнителен текст", Value = model.Description });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_DEBTORS}", Label = "Длъжници", Value = debtors });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_REGARD_SUBPOENA_KIND}", Label = "Тип на ИЛ", Value = model.ExecListType?.Label });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_EXEC_SUMS}",
                Label = "Сума",
                Value = model.ExecListObligations.Select(x => x.Obligation.Amount).Sum().ToString("0.00")
            });
            return keyValuePairs;
        }

        private void AddEditKey(string key, string label, string value, List<KeyValuePairVM> keyValuePairs)
        {
            var keyData = keyValuePairs.Where(x => x.Key == key).FirstOrDefault();
            if (keyData == null)
            {
                keyValuePairs.Add(new KeyValuePairVM()
                {
                    Key = key,
                    Label = label,
                    Value = value
                });
            }
            else
            {
                keyData.Value = value;
            }
        }

        public TinyMCEVM FillHtmlTemplateExecList(int execListId)
        {
            var model = repo.AllReadonly<ExecList>()
                              .Include(x => x.Court)
                              .Include(x => x.ExecListLawBase)
                              .Include(x => x.ExecListObligations)
                              .ThenInclude(x => x.Obligation)
                              .Include(x => x.LawUnitSign)
                              .Where(x => x.Id == execListId)
                              .FirstOrDefault();

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            var obligations = repo.AllReadonly<Obligation>()
                              .Include(x => x.CaseSessionAct)
                              .Include(x => x.CaseSessionAct.ActType)
                              .Include(x => x.ObligationReceives)
                              .ThenInclude(x => x.UicType)
                              .Where(x => model.ExecListObligations.Select(a => a.ObligationId).Contains(x.Id))
                              .ToList();

            int caseId = obligations.Select(x => x.CaseSessionAct.CaseId ?? 0).FirstOrDefault();
            int caseSessionId = obligations.Select(x => x.CaseSessionAct.CaseSessionId).FirstOrDefault();
            var caseModel = repo.AllReadonly<Case>()
                              .Include(x => x.CaseType)
                              .Where(x => x.Id == caseId)
                              .FirstOrDefault();

            var cityName = repo.AllReadonly<EkEkatte>()
                             .Where(x => x.Ekatte == model.Court.CityCode)
                             .Select(x => ((x.TVM ?? "") + x.Name).ToUpper())
                             .FirstOrDefault();

            string actData = string.Join(", ", obligations.Where(x => x.CaseSessionActId != null)
                             .Select(x => x.CaseSessionAct.ActType.Label + " №" + x.CaseSessionAct.RegNumber + "/" +
                                    (x.CaseSessionAct.ActDate != null ? ((DateTime)x.CaseSessionAct.ActDate).ToString("dd.MM.yyyy") : ""))
                             .Distinct());
            string actForce = string.Join(", ", obligations.Where(x => x.CaseSessionActId != null)
                             .Select(x => x.CaseSessionAct.ActInforcedDate != null ? ((DateTime)x.CaseSessionAct.ActInforcedDate).ToString("dd.MM.yyyy") : "")
                             .Distinct());
            string receiveName = "";
            var receive = obligations.Where(x => x.ObligationReceives.Any()).Select(x => x.ObligationReceives.FirstOrDefault()).FirstOrDefault();
            if (model.ExecListTypeId == NomenclatureConstants.ExecListTypes.Country)
            {
                if (receive != null)
                {
                    if (receive.Person_SourceType == SourceTypeSelectVM.Court)
                        receiveName = "бюджета на съдебната власт";
                    else if (receive.Person_SourceType == SourceTypeSelectVM.Instutution)
                        receiveName = receive.FullName;
                }
            }
            else if (model.ExecListTypeId == NomenclatureConstants.ExecListTypes.ThirdPerson)
            {
                if (receive != null)
                {
                    receiveName = receive.FullName + ", " + receive.UicType.Label + " " + receive.Uic + " ";
                    if (receive.CasePersonId != null)
                    {
                        var casePersonReceive = repo.AllReadonly<CasePerson>()
                                                        .Include(x => x.Addresses)
                                                        .ThenInclude(x => x.Address)
                                                        .Include(x => x.PersonRole)
                                                        .Where(x => x.Id == receive.CasePersonId)
                                                        .FirstOrDefault();
                        if (casePersonReceive != null)
                            receiveName += casePersonReceive.Addresses.Where(x => x.DateExpired == null)
                                                 .Select(x => x.Address.FullAddress).FirstOrDefault() + ", " + casePersonReceive.PersonRole.Label;
                    }

                }
            }

            var caseLawUnits = Read_CaseLawUnit(caseId, caseSessionId);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));
            keyValuePairs.AddRange(fillList_ExecList(model));
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_UCLP}", Label = "Населено място", Value = cityName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TODAY}", Label = "Дата на ИЛ", Value = model.RegDate.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.Court.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATA}", Label = "Акт", Value = actData });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FORCE_DATE}", Label = "Влязал в сила", Value = actForce });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FORCE_DATE_TEXT}", Label = "Влязал в сила", Value = ", влязло в законна сила на " + actForce });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_KIND}", Label = "Вид на делото (точен)", Value = caseModel.CaseType.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_NO}", Label = "Номер на делото", Value = caseModel.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_YEAR}", Label = "Година на делото", Value = caseModel.RegDate.Year.ToString() });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECIPIENT}", Label = "В полза на", Value = receiveName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECIPIENTS}", Label = "В полза на", Value = receiveName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JUDGE_TYPE}", Label = "Съдия", Value = "СЪДИЯ при " + model.Court.Label.ToUpper() });
            if (model.LawUnitSignId != null)
            {
                AddEditKey("{F_JUDGE}", "Съдия", model.LawUnitSign.FullName_MiddleNameInitials, keyValuePairs);
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EXECUTION}", Label = "Съдия", Value = "изпълнение" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ACCOMPLY}", Label = "Основание", Value = model.ExecListLawBase?.Label });

            string alias = model.ExecListTypeId == NomenclatureConstants.ExecListTypes.Country ? "ACTEX_E__" : "ACTEX_E_";
            return GetTinyMCEVMFromHtmlTemplates(alias, keyValuePairs);
        }

        public TinyMCEVM FillHtmlTemplateSentenceBulletin(int id)
        {
            var model = repo.AllReadonly<CasePersonSentenceBulletin>()
                              .Include(x => x.CasePerson)
                              .Include(x => x.CasePerson.Case)
                              .Include(x => x.CasePerson.Case.Court)
                              .Include(x => x.CasePerson.Case.CaseType)
                              .Include(x => x.User)
                              .Include(x => x.User.LawUnit)
                              .Include(x => x.LawUnitSign)
                              .Where(x => x.Id == id)
                              .FirstOrDefault();

            var casepersonSentence = repo.AllReadonly<CasePersonSentence>()
                                    .Include(x => x.CaseSessionAct)
                                    .Include(x => x.CaseSessionAct.CaseSession)
                                    .Include(x => x.CaseSessionAct.ActType)
                                    .Where(x => x.CasePersonId == model.CasePersonId)
                                    .Where(x => x.IsActive == true)
                                    .OrderByDescending(x => x.Id)
                                    .FirstOrDefault();

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            var sessionDate = casepersonSentence.CaseSessionAct.CaseSession.DateFrom;
            var caseLawUnits = repo.AllReadonly<CaseLawUnit>()
                            .Include(x => x.LawUnit)
                            .Include(x => x.CourtDepartment)
                            .Where(x => x.CaseSessionId == casepersonSentence.CaseSessionAct.CaseSessionId)
                            .Where(x => (x.DateTo ?? sessionDate) >= sessionDate)
                            .Where(x => !NomenclatureConstants.JudgeRole.ManualRoles.Contains(x.JudgeRoleId))
                            .ToList();

            var actComplainResult = Read_ActComplainResult(casepersonSentence.CaseSessionAct.ActComplainResultId ?? 0);
            string position = lawUnitService.GetLawUnitPosition(model.CourtId ?? 0, model.User?.LawUnitId ?? 0);
            if (string.IsNullOrEmpty(position))
                position = "Служител";

            string lawBase = (model.IsAdministrativePunishment ?? false) ? "за наложено административно наказание по чл.78а НК" : "";
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, true));
            keyValuePairs.AddRange(fillList_SessionAct(casepersonSentence.CaseSessionAct, actComplainResult));
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{LawBase}", Label = "Чл.", Value = lawBase });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Лице", Value = model.CasePerson.FullName.ToUpper() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FAMILY_MARRIAGE}", Label = "Фамилно име, придобито при сключване на граждански брак", Value = model.FamilyMarriage });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_BIRTHDAY_PLACE}", Label = "Месторождение", Value = model.BirthDayPlace });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_BIRTHDAY}", Label = "Ден, месец и година на раждане", Value = model.BirthDay.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_UCN}", Label = "ЕГН/ЛНЧ", Value = model.CasePerson.Uic });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_COUNTRY}", Label = "Гражданство", Value = model.Nationality });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_FATHER}", Label = "Баща", Value = model.FatherName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_MOTHER}", Label = "Майка", Value = model.MotherName });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_KIND}", Label = "Вид на делото (точен)", Value = model.CasePerson.Case.CaseType.Code });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_NO}", Label = "Номер на делото", Value = model.CasePerson.Case.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_YEAR}", Label = "Година на делото", Value = model.CasePerson.Case.RegDate.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.CasePerson.Case.Court.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_ADDITIONAL_TEXT}", Label = "Присъда", Value = model.SentenceDescription });

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TODAY}", Label = "Дата", Value = model.DateWrt.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_FULL_USER_NAME}", Label = "Потребител", Value = model.User.LawUnit.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_POST}", Label = "Длъжност", Value = position });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_CONVICTED}", Label = "Осъждан", 
                                                      Value = (model.IsConvicted ?? false) == true ? "Осъждан" : "" });

            var forceDate = keyValuePairs.Where(x => x.Key == "{F_PERSONAL_FORCE_DATE}").FirstOrDefault();
            if (forceDate == null)
            {
                keyValuePairs.Add(new KeyValuePairVM()
                {
                    Key = "{F_PERSONAL_FORCE_DATE}",
                    Label = "Дата на влизане в сила",
                    Value = casepersonSentence.InforcedDate?.ToString("dd.MM.yyyy")
                });
            }
            else
            {
                forceDate.Value = casepersonSentence.InforcedDate?.ToString("dd.MM.yyyy");
            }

            if (model.LawUnitSignId != null)
            {
                AddEditKey("{F_JUDGE}", "Съдия", model.LawUnitSign.FullName_MiddleNameInitials, keyValuePairs);
            }


            return GetTinyMCEVMFromHtmlTemplates("BULETIN", keyValuePairs);
        }

        private IList<KeyValuePairVM> fillList_DocumentBulletin(DocumentTemplate documentTemplate)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var model = repo.AllReadonly<CasePersonSentenceBulletin>()
                               .Include(x => x.CasePerson)
                               .Include(x => x.CasePerson.Case)
                               .Include(x => x.CasePerson.Case.CaseType)
                               .Where(x => x.Id == documentTemplate.SourceId)
                               .FirstOrDefault();

            keyValuePairs.AddRange(fillList_Court(model.CasePerson.Case.CourtId));
            keyValuePairs.AddRange(fillList_OnlyCaseData(model.CasePerson.Case));

            var caseLawUnits = Read_CaseLawUnit(model.CasePerson.CaseId, null);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });
            if (documentTemplate.DocumentId != null)
            {
                var modelDocument = repo.AllReadonly<Document>()
                                   .Include(x => x.DocumentPersons)
                                   .ThenInclude(x => x.Addresses)
                                   .ThenInclude(x => x.Address)
                                   .Include(x => x.User)
                                   .Include(x => x.User.LawUnit)
                                   .Where(x => x.Id == documentTemplate.DocumentId)
                                   .FirstOrDefault();

                keyValuePairs.AddRange(fillList_UserData(documentTemplate.AuthorId != null ? documentTemplate.Author : modelDocument.User, model.CasePerson.Case.CourtId));

                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OUTREG_NO}", Label = "Изходящ номер", Value = modelDocument.DocumentNumber });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OUTREG_DATE}", Label = "Дата", Value = modelDocument.DocumentDate.ToString("dd.MM.yyyy") });

                var documentPerson = modelDocument.DocumentPersons.FirstOrDefault();
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LRECEIVER}", Label = "Получател", Value = documentPerson.FullName });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_ADDRESS}", Label = "Адрес Получател", Value = documentPerson.Addresses.FirstOrDefault()?.Address.FullAddress });
            }
            else
            {
                keyValuePairs.AddRange(fillList_UserData(documentTemplate.Author, model.CasePerson.Case.CourtId));
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER}", Label = "Осъден", Value = model.CasePerson.FullName });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_RECEIVER_UCN}", Label = "ЕГН Осъден", Value = model.CasePerson.Uic });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_Court(int courtId)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            var model = repo.AllReadonly<Court>()
                         .Where(x => x.Id == courtId)
                         .FirstOrDefault();

            var cityName = repo.AllReadonly<EkEkatte>()
                             .Where(x => x.Ekatte == model.CityCode)
                             .Select(x => ((x.TVM ?? "") + x.Name).ToUpper())
                             .FirstOrDefault();

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT}", Label = "Съд", Value = model.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_LOGO}", Label = "Лого", Value = "<div id='courtlogo-holder' name='courtlogo-holder'></div>" });
            var courtLogo_css = "#courtlogo-holder { margin - bottom: 10px; width: 200px;height: 100px; background: url('" + model.CourtLogo + "') 100% 100% no-repeat;}";
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_LOGO_CSS}", Label = "Лого", Value = courtLogo_css });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_ADDRESS}", Label = "Адрес на съда", Value = model.Address });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_ADDRESS_1}", Label = "Седалище – адрес на съда:", Value = (model.CityName ?? "") + " " + (model.Address ?? "") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_ADDRESS_2}", Label = "Други данни за адрес на текущия съд:", Value = "" });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_COURT_UCLP}", Label = "наименование на населеното място", Value = cityName });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_OnlyCaseData(Case model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            AddConnectsEisspNumber(model, keyValuePairs);
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_KIND}", Label = "Вид на делото (точен)", Value = model.CaseType?.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_NO}", Label = "Номер на делото", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LAWSUIT_YEAR}", Label = "Година на делото", Value = model.RegDate.Year.ToString() });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_UserData(ApplicationUser model, int courtId)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            if (model != null)
            {
                string position = lawUnitService.GetLawUnitPosition(courtId, model.LawUnitId);

                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_NAME}", Label = "Потребител", Value = model.LawUnit.FullName_MiddleNameInitials });
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_USER_POST}", Label = "Длъжност", Value = position });
            }

            return keyValuePairs;
        }

        private string PrintRowExchangeDoc(int autogen, string debtor, string code, string debtorAddress,
                         string lawsuitKind, string lawsuitNo, string composition, string eventData, string sum)
        {
            string result = @"<tr style='height:44.6pt'>
<!-- tr1 autogen -->
  <td width=106 style='width:35.45pt;border:solid windowtext 1.0pt;border-top:
  none;padding:0cm 5.4pt 0cm 5.4pt;height:44.6pt'>
  <p class=MsoNormal><span lang=EN-US style='color:black'>" + autogen + @"</span></p>
  </td>
<!-- tr1 debtor -->
  <td width=104 style='width:212.65pt;border-top:none;border-left:none;
  border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;
  padding:0cm 5.4pt 0cm 5.4pt;height:44.6pt'>
  <p class=MsoNormal><span>" + debtor + @"</span></p>
  </td>
  <td width=80 style='width:92.1pt;border-top:none;border-left:none;border-bottom:
  solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;padding:0cm 5.4pt 0cm 5.4pt;
  height:44.6pt'>
  <p class=MsoNormal><span>" + code + @"</span></p>
  </td>
  <td width=162 style='width:155.95pt;border-top:none;border-left:none;
  border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;
  padding:0cm 5.4pt 0cm 5.4pt;height:44.6pt'>
  <p class=MsoNormal><span lang=EN-US>" + debtorAddress + @"</span></p>
  </td>
<!-- tr1 event -->
  <td width=266 style='width:134.65pt;border-top:none;border-left:none;
  border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;
  padding:0cm 5.4pt 0cm 5.4pt;height:44.6pt'>
  <p class=MsoNormal><b><span lang=SR-CYRL-RS>" + lawsuitKind + @"</span></b></p>
  <p class=MsoNormal><b><span lang=EN-US>№
  " + lawsuitNo + @"</span></b></p>
  <p class=MsoNormal><span lang=EN-US style='font-size:9.0pt'>" + composition + @"</span></p>
  <p class=MsoNormal><span lang=EN-US style='font-size:9.0pt'>&nbsp;</span></p>
  <p class=MsoNormal><span lang=EN-US>" + eventData + @"</span></p>
  </td>
  <td width=115 style='width:127.6pt;border-top:none;border-left:none;
  border-bottom:solid windowtext 1.0pt;border-right:solid windowtext 1.0pt;
  padding:0cm 5.4pt 0cm 5.4pt;height:44.6pt'>
  <p class=MsoNormal style='margin-right:1.0cm'><span lang=EN-US><!-- tr1 sums -->" + sum + @"</span><span
  lang=EN-US> </span></p>
  </td>
 </tr>";
            return result;
        }

        public TinyMCEVM FillHtmlTemplateExchangeDoc(int id)
        {
            var model = repo.AllReadonly<ExchangeDoc>()
                              .Include(x => x.OutDocument)
                              .Include(x => x.OutDocument.DocumentPersons)
                              .Where(x => x.Id == id)
                              .FirstOrDefault();

            int[] permanentAddress = { NomenclatureConstants.AddressType.Permanent, NomenclatureConstants.AddressType.CompanyPermanent };
            var execList = repo.AllReadonly<ExchangeDocExecList>()
                              .Where(x => x.ExchangeDocId == id)
                              .Select(x => new ExecListPrintVM()
                              {
                                  Id = x.ExecListId,
                                  Debtor = string.Join("<br>", x.ExecList.ExecListObligations.Select(a => a.Obligation.FullName).Distinct()),
                                  DebtorAddress = string.Join("<br>", repo.AllReadonly<CasePerson>()
                                                  .Where(a => a.CaseId == x.ExecList.ExecListObligations.Select(b => b.Obligation.CaseSessionAct.CaseId).FirstOrDefault() &&
                                                    x.ExecList.ExecListObligations.Select(b => b.Obligation.Person_SourceId).Contains(a.Id))
                                                  .Select(a => a.Addresses.Where(b => permanentAddress.Contains(b.Address.AddressTypeId))
                                                              .Select(b => b.Address.FullAddress).FirstOrDefault()).Distinct()),
                                  Uic = string.Join("<br>", x.ExecList.ExecListObligations
                                                    .Where(a => a.Obligation.Uic != null).Select(a => a.Obligation.Uic).Distinct()),
                                  CaseTypeName = x.ExecList.ExecListObligations
                                                   .Select(a => a.Obligation.CaseSessionAct.Case.CaseType.Label).FirstOrDefault(),
                                  CaseNumber = x.ExecList.ExecListObligations
                                                   .Select(a => a.Obligation.CaseSessionAct.Case.RegNumber).FirstOrDefault(),
                                  Composition = x.ExecList.ExecListObligations
                                                   .Where(a => a.Obligation.CaseSessionAct.CaseSession.CaseLawUnits
                                                          .Where(b => (b.DateTo ?? a.Obligation.CaseSessionAct.CaseSession.DateFrom) >= a.Obligation.CaseSessionAct.CaseSession.DateFrom &&
                                                                     b.CourtDepartmentId != null).Any())
                                                   .Select(a => a.Obligation.CaseSessionAct.CaseSession.CaseLawUnits.Select(c => c.CourtDepartment.Label).FirstOrDefault()).FirstOrDefault(),
                                  EventData = string.Join("<br>", x.ExecList.ExecListObligations
                                                    .Select(a => a.Obligation.CaseSessionAct.ActType.Label + "<br>" +
                                                            a.Obligation.CaseSessionAct.RegNumber + " от " +
                                                            (a.Obligation.CaseSessionAct.ActDate != null ?
                                                            ((DateTime)a.Obligation.CaseSessionAct.ActDate).ToString("dd.MM.yyyy") : "") +
                                                            "<br>" +
                                                            (a.Obligation.CaseSessionAct.ActInforcedDate != null ?
                                                            "В законна сила от " + ((DateTime)a.Obligation.CaseSessionAct.ActInforcedDate).ToString("dd.MM.yyyy") : ""))
                                                    .Distinct()),
                                  Amount = string.Join("<br>", x.ExecList.ExecListObligations.Select(a => a.Obligation.Amount.ToString("0.00") + " лв.")),
                              }).ToList();

            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            keyValuePairs.AddRange(fillList_Court(model.CourtId));

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OUT_NO}", Label = "Изходящ номер", Value = model.OutDocument?.DocumentNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_OUT_YEAR}", Label = "Дата", Value = model.OutDocument?.DocumentDate.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_NO}", Label = "Протокол номер", Value = model.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_YEAR}", Label = "Дата", Value = model.RegDate?.Year.ToString() });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_TODAY}", Label = "Дата", Value = model.RegDate?.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM()
            {
                Key = "{F_SERV_TAX}",
                Label = "Дата",
                Value = model.OutDocument?.DocumentPersons.Select(x => x.FullName).FirstOrDefault()
            });

            string rowData = "";
            int index = 1;
            foreach (var item in execList)
            {
                rowData += PrintRowExchangeDoc(index, item.Debtor, item.Uic, item.DebtorAddress, item.CaseTypeName,
                           item.CaseNumber, item.Composition, item.EventData, item.Amount);
                index++;
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{FROW_DATA}", Label = "ИЛ", Value = rowData });

            return GetTinyMCEVMFromHtmlTemplates("PROTEXLIST", keyValuePairs);
        }

        private IList<KeyValuePairVM> fillList_DocumentExecList(DocumentTemplate documentTemplate)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var model = repo.AllReadonly<ExecList>()
                               .Include(x => x.ExecListType)
                               .Include(x => x.ExecListObligations)
                               .ThenInclude(x => x.Obligation)
                               .ThenInclude(x => x.CaseSessionAct)
                               .ThenInclude(x => x.ActType)
                               .Where(x => x.Id == documentTemplate.SourceId)
                               .FirstOrDefault();

            var caseId = model.ExecListObligations.Select(x => x.Obligation.CaseSessionAct.CaseId).FirstOrDefault();
            var modelCase = repo.AllReadonly<Case>()
                           .Include(x => x.CaseType)
                           .Where(x => x.Id == caseId)
                           .FirstOrDefault();

            var bankAccounts = repo.AllReadonly<CourtBankAccount>()
                                   .Where(x => x.IsActive && x.CourtId == model.CourtId)
                                   .OrderBy(x => x.Id)
                                   .ToList();
            var baBUDGET = bankAccounts.FirstOrDefault(x => x.MoneyGroupId == NomenclatureConstants.MoneyGroups.Budget);

            if (documentTemplate.DocumentId != null)
            {
                var modelDocument = repo.AllReadonly<Document>()
                                   .Include(x => x.DocumentPersons)
                                   .ThenInclude(x => x.Addresses)
                                   .ThenInclude(x => x.Address)
                                   .Include(x => x.User)
                                   .Include(x => x.User.LawUnit)
                                   .Where(x => x.Id == documentTemplate.DocumentId)
                                   .FirstOrDefault();

                string institutionCode = "";
                var firstPerson = modelDocument.DocumentPersons.FirstOrDefault();
                if (firstPerson.Person_SourceType == SourceTypeSelectVM.Instutution)
                {
                    institutionCode = repo.AllReadonly<Institution>()
                                         .Where(x => x.Id == firstPerson.Person_SourceId)
                                         .Select(x => x.Code)
                                         .FirstOrDefault();
                }
                keyValuePairs.AddRange(fillList_Document(modelDocument));
                keyValuePairs.AddRange(fillList_UserData(documentTemplate.AuthorId != null ? documentTemplate.Author : modelDocument.User, documentTemplate.CourtId));
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_CSJID}", Label = "Код на ЧСИ", Value = institutionCode });
            }
            else
            {
                keyValuePairs.AddRange(fillList_UserData(documentTemplate.Author, documentTemplate.CourtId));
            }

            keyValuePairs.AddRange(fillList_Court(model.CourtId));
            keyValuePairs.AddRange(fillList_OnlyCaseData(modelCase));

            var caseLawUnits = Read_CaseLawUnit(modelCase.Id, null);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));

            keyValuePairs.AddRange(fillList_ExecList(model));

            string actData = string.Join(", ", model.ExecListObligations.Where(x => x.Obligation.CaseSessionActId != null)
                             .Select(x => x.Obligation.CaseSessionAct.ActType.Label + " №" + x.Obligation.CaseSessionAct.RegNumber + "/" +
                                    (x.Obligation.CaseSessionAct.ActDate != null ? ((DateTime)x.Obligation.CaseSessionAct.ActDate).ToString("dd.MM.yyyy") : ""))
                             .Distinct());

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATA}", Label = "Акт", Value = actData });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_BANK_ACCOUNTS}", Label = "Банкова сметка", Value = baBUDGET.Iban });

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_CaseMigration(DocumentTemplate documentTemplate)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();
            var model = repo.AllReadonly<CaseMigration>()
                               .Include(x => x.CaseSessionAct)
                               .Include(x => x.CaseSessionAct.ActType)
                               .Where(x => x.Id == documentTemplate.SourceId)
                               .FirstOrDefault();

            if (model == null)
                return keyValuePairs;

            var modelCase = repo.AllReadonly<Case>()
                           .Include(x => x.CaseType)
                           .Where(x => x.Id == model.CaseId)
                           .FirstOrDefault();

            string allCases = string.Join(", ", repo.AllReadonly<CaseMigration>()
                               .Where(x => x.InitialCaseId == model.InitialCaseId)
                               .Where(x => x.CaseId != model.CaseId)
                               .Select(x => x.Case.RegNumber)
                               .Distinct());

            string allInstitutionCases = string.Join(", ", repo.AllReadonly<CaseMigration>()
                               .Where(x => x.InitialCaseId == model.InitialCaseId)
                               .Where(x => x.Case.Document.DocumentInstitutionCaseInfo.Any())
                               .Select(x => string.Join(", ", x.Case.Document.DocumentInstitutionCaseInfo
                                            .Select(a => a.CaseNumber + "/" + a.CaseYear).Distinct()))
                               .Distinct());
            if (string.IsNullOrEmpty(allCases) == false && string.IsNullOrEmpty(allInstitutionCases) == false)
                allCases += ", " + allInstitutionCases;
            else
                allCases += allInstitutionCases;

            if (documentTemplate.DocumentId != null)
            {
                var modelDocument = repo.AllReadonly<Document>()
                                   .Include(x => x.DocumentPersons)
                                   .ThenInclude(x => x.Addresses)
                                   .ThenInclude(x => x.Address)
                                   .Include(x => x.User)
                                   .Include(x => x.User.LawUnit)
                                   .Where(x => x.Id == documentTemplate.DocumentId)
                                   .FirstOrDefault();

                keyValuePairs.AddRange(fillList_UserData(documentTemplate.AuthorId != null ? documentTemplate.Author : modelDocument.User, documentTemplate.CourtId));
                keyValuePairs.AddRange(fillList_Document(modelDocument));
            }
            else
            {
                keyValuePairs.AddRange(fillList_UserData(documentTemplate.Author, documentTemplate.CourtId));
            }

            keyValuePairs.AddRange(fillList_Court(modelCase.CourtId));
            keyValuePairs.AddRange(fillList_OnlyCaseData(modelCase));

            var caseLawUnits = Read_CaseLawUnit(modelCase.Id, null);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));


            var casePersons = Read_CasePersons(modelCase.Id, null);
            keyValuePairs.AddRange(fillList_CasePersons(casePersons, null, 0));

            string actDocs = "";
            if (model.CaseSessionActId != null)
            {
                actDocs = string.Join(", ", repo.AllReadonly<CaseSessionActComplain>()
                             .Where(x => x.CaseSessionActId == model.CaseSessionActId && x.DateExpired == null)
                             .Select(x => x.ComplainDocument.DocumentNumber + "/" + x.ComplainDocument.DocumentDate.ToString("dd.MM.yyyy"))
                             .Distinct());
            }

            //Ако е връщане - да се попълнят променливите за делото от което е дошло
            if (NomenclatureConstants.CaseMigrationTypes.ReturnCaseTypes.Contains(model.CaseMigrationTypeId))
            {
                var prevCase = repo.AllReadonly<CaseMigration>()
                                 .Include(x => x.Case)
                                 .Include(x => x.Case.CaseType)
                                 .Include(x => x.OutDocument)
                                 .Where(x => x.InitialCaseId == model.InitialCaseId)
                                 .Where(x => x.CaseId == model.ReturnCaseId)
                                 .Where(x => x.SendToCourtId == modelCase.CourtId)
                                 .Where(x => x.Id < model.Id)
                                 .OrderByDescending(x => x.Id)
                                 .FirstOrDefault();
                if (prevCase != null)
                {
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_OUTREG_NO}", Label = "Документ", Value = prevCase.OutDocument?.DocumentNumber });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_OUTREG_YEAR}", Label = "Документ", Value = prevCase.OutDocument?.DocumentDate.Year.ToString() });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_KIND}", Label = "Вид дело", Value = prevCase.Case.CaseType.Label });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_NO}", Label = "Номер дело", Value = prevCase.Case.RegNumber });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LOWER_YEAR}", Label = "дело", Value = prevCase.Case.RegDate.Year.ToString() });
                }
            }

            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_GOD}", Label = "г.", Value = "г." });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EISPP_NMR}", Label = "EISPP_NMR", Value = modelCase.EISSPNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_KIND}", Label = "Акт", Value = model.CaseSessionAct?.ActType.Label });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_NO}", Label = "Акт", Value = model.CaseSessionAct?.RegNumber });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_EVENT_DATE}", Label = "Акт", Value = model.CaseSessionAct?.RegDate?.ToString("dd.MM.yyyy") });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_INREG_DOCS}", Label = "Документи за обжалване", Value = actDocs });
            keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_CUTDOWN}", Label = "Свързани дела", Value = allCases });
            AddConnectsEisspNumber(modelCase, keyValuePairs);

            if (model.CaseMigrationTypeId == NomenclatureConstants.CaseMigrationTypes.SendJurisdiction)
            {
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JURICONF}", Label = "Вид", Value = "подсъдност" });
            }
            else if (model.CaseMigrationTypeId == NomenclatureConstants.CaseMigrationTypes.SendCompetence)
            {
                keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_JURICONF}", Label = "Вид", Value = "компетентност" });
            }

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_DocumentHeritage(int caseId)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            if (caseId > 0)
            {
                var model = repo.AllReadonly<CasePerson>()
                                            .Include(x => x.Addresses)
                                            .ThenInclude(x => x.Address)
                                            .Where(x => x.CaseId == caseId)
                                            .Where(x => x.CaseSessionId == null)
                                            .Where(x => x.DateExpired == null)
                                            .Where(x => x.PersonRoleId == NomenclatureConstants.PersonRole.Legator)
                                            .FirstOrDefault();

                if (model != null)
                {
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LEGACY_LEAVER}", Label = "Наследодател", Value = model.FullName });
                    keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LEGACY_LEAVER_UCN}", Label = "ЕГн Наследодател", Value = model.Uic });

                    var address = model.Addresses.Where(x => x.DateExpired == null &&
                           x.Address.AddressTypeId == NomenclatureConstants.AddressType.Permanent).FirstOrDefault();
                    if (address != null)
                    {
                        var city = repo.AllReadonly<EkEkatte>().Where(x => x.Ekatte == address.Address.CityCode).Select(x => x.Name).FirstOrDefault();
                        keyValuePairs.Add(new KeyValuePairVM() { Key = "{F_LEGACY_LEAVER_UCLP}", Label = "Град Наследодател", Value = city });
                    }
                }
            }

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_DocumentSentence(DocumentTemplate model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            var sentence = repo.AllReadonly<CasePersonSentence>()
                            .Include(x => x.CaseSessionAct)
                            .Include(x => x.CaseSessionAct.Case)
                            .Include(x => x.CaseSessionAct.ActType)
                            .Include(x => x.CaseSessionAct.Case.CaseType)
                            .Where(x => x.Id == model.SourceId)
                            .FirstOrDefault();



            keyValuePairs.AddRange(fillList_DocumentUser(model));
            keyValuePairs.AddRange(fillList_Court(model.CourtId));
            keyValuePairs.AddRange(fillList_OnlyCaseData(sentence.CaseSessionAct.Case));
            keyValuePairs.AddRange(fillList_SessionAct(sentence.CaseSessionAct, null));

            var caseLawUnits = Read_CaseLawUnit(model.CaseId ?? 0, null);
            keyValuePairs.AddRange(fillList_CaseLawUnitFull(caseLawUnits, false));

            return keyValuePairs;
        }

        private IList<KeyValuePairVM> fillList_DocumentUser(DocumentTemplate model)
        {
            List<KeyValuePairVM> keyValuePairs = new List<KeyValuePairVM>();

            if (model.DocumentId != null)
            {
                var modelDocument = repo.AllReadonly<Document>()
                                   .Include(x => x.DocumentPersons)
                                   .ThenInclude(x => x.Addresses)
                                   .ThenInclude(x => x.Address)
                                   .Include(x => x.User)
                                   .Include(x => x.User.LawUnit)
                                   .Where(x => x.Id == model.DocumentId)
                                   .FirstOrDefault();

                keyValuePairs.AddRange(fillList_UserData(model.AuthorId != null ? model.Author : modelDocument.User, model.CourtId));
                keyValuePairs.AddRange(fillList_Document(modelDocument));
            }
            else
            {
                keyValuePairs.AddRange(fillList_UserData(model.Author, model.CourtId));
            }

            return keyValuePairs;
        }
    }
}
