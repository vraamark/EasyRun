using System;

namespace EasyRun
{
    internal static class GuidList
    {
        public static readonly Guid guidOutputWindowPane = new Guid("{037A3F98-80BB-4C43-BBCE-8329C48B2EB1}");

        public static readonly Guid guidCPlusPlus = new Guid("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}");
        public static readonly Guid guidWebApp = new Guid("{349c5851-65df-11da-9384-00065b846f21}");
        public static readonly Guid guidWebSite = new Guid("{e24c65dc-7377-472b-9aba-bc803b73c61a}");
        public static readonly Guid guidVsPackage = new Guid("{82b43b9b-a64c-4715-b499-d71e9ca2bd60}");
        public static readonly Guid guidDatabase = new Guid("{00d1a9c2-b5f0-4af3-8072-f6c62b433612}");

        public static readonly Guid guidSolutionFolder = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
        public static readonly Guid guidMiscFiles = new Guid(EnvDTE.Constants.vsProjectKindMisc);
    }
}