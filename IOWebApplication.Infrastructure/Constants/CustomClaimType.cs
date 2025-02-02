﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

namespace IOWebApplication.Infrastructure.Constants
{
    public static class CustomClaimType
    {
        public static class IdStampit
        {
            public static string PersonalId = "urn:stampit:pid";

            public static string Organization = "urn:stampit:organization";

            public static string PublicKey = "urn:stampit:public_key";

            public static string Certificate = "urn:stampit:certificate";

            public static string CertificateNumber = "urn:stampit:certno";
        }

        public static string Email = "email";

        public static string FullName = "full_name";

        public static string Uic = "uic";
        public static string CourtId = "court_id";
        public static string CourtList = "court_list";
        public static string CourtName = "court_name";
        public static string CourtTypeId = "court_type_id";
        public static string InstanceList = "instance_list";
        public static string OrganizationList = "org_list";
        public static string SubDocRegistry = "sub_doc_reg";
        public static string SubDepartments = "sub_deps";
        public static string LawUnitId = "law_unit_id";
    }
}
