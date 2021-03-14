using Microsoft.VisualStudio.TestTools.UnitTesting;
using CTSWeb.Util;
using CTSWeb.Models;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTSWeb.Util.Tests
{
    [TestClass()]
    public class SerialiserTests
    {
        private static SerConfig[] PrBuildParam(List<(SerFieldType, string, string, SerDirective)> voParam)
        {
            var oRet = new SerConfig[voParam.Count];
            int c = 0;
            foreach (var o in voParam)
            {
                oRet[c] = new SerConfig() { FieldType = o.Item1, Name = o.Item2, TypeName = o.Item3, Action = o.Item4 };
                c++;
            }
            return oRet;
        }

        private static List<(SerFieldType, string, string, SerDirective)> _oParam = new List<(SerFieldType, string, string, SerDirective)>()
        {
(SerFieldType.Field, "Rep_Framework", "Framework", SerDirective.Ignore),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultWindowsFolder", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_WindowsFolder", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultInternetFolder", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_InternetFolder", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultSetOfControls", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_SetOfControls", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultLevel", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_LevelToReach", "Nullable`1", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_Blocking", "Nullable`1", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultLock", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_LockOnPublication", "Nullable`1", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultRuleSet", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_RuleSet", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_UseDefaultOpbal", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_HasOpBal", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_OpbPhase", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_OpbUpdatePeriod", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_OpbScope", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_OpbVariant", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultPackage_OpbConsCurrency", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultOperation_UseDefaultPublish", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultOperation_PackPublishingCutOffDate", "DateTime", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultOperation_AllowEarlyPublishing", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultOperation_UseDefaultAfterPub", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultOperation_AfterPublication", "IntegrateMode", SerDirective.Flatten),
(SerFieldType.Field, "Rep_DefaultOperation_UseDefaultAfterTran", "Boolean", SerDirective.Show),
(SerFieldType.Field, "Rep_DefaultOperation_AfterTransfer", "IntegrateMode", SerDirective.Flatten),
(SerFieldType.Field, "Rep_EntityReportings", "List`1", SerDirective.FlatList),
(SerFieldType.Field, "Rep_Phase", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_UpdatePeriod", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_FrameworkVersion", "String", SerDirective.Show),
(SerFieldType.Field, "Rep_ReportingStartDate", "DateTime", SerDirective.Show),
(SerFieldType.Field, "Rep_ReportingEndDate", "DateTime", SerDirective.Show),
(SerFieldType.Field, "Rep_OwnerSite", "Int32", SerDirective.Ignore),
(SerFieldType.Field, "Rep_OwnerWorkgroup", "Int32", SerDirective.Ignore),
(SerFieldType.Field, "Rep_CreationDate", "DateTime", SerDirective.Ignore),
(SerFieldType.Field, "Rep_Author", "Int32", SerDirective.Ignore),
(SerFieldType.Field, "Rep_UpdateDate", "DateTime", SerDirective.Ignore),
(SerFieldType.Field, "Rep_UpdateAuthor", "Int32", SerDirective.Ignore),
(SerFieldType.Field, "Rep_Descriptions", "LanguageText[]", SerDirective.FlatArray),
(SerFieldType.Field, "Rep_ID", "Int32", SerDirective.Show),
(SerFieldType.Field, "Rep_LDesc", "String", SerDirective.Ignore),
(SerFieldType.Property, "Rep_Name", "String", SerDirective.Show),
 };

        [TestMethod()]
        public void SerializeTest()
        {
            Reporting oRep = new Reporting();
            Assert.IsNotNull(oRep);
        
            foreach (string s in Serialiser.Flatten(oRep.GetType(), "oRep.")) Debug.WriteLine(s);

            Debug.WriteLine("\n\n");
            EntityReporting oER = new EntityReporting();
            foreach (string s in Serialiser.Flatten(oER.GetType(), "oEntityRep.")) Debug.WriteLine(s);
        }
    }
}