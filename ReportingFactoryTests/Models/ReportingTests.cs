#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CTSWeb.Models;
using CTSWeb.Util;
using CTCOMDEFAULTLib;
using CTCLIENTSERVERLib;
using CTCOMMONMODULELib;
using CTCORELib;
using CTKREFLib;
using CTREPORTINGMODULELib;
using CTTRANSFERLib;
using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Web;
using System.Web.SessionState;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTSWeb.Models.Tests
{

    [TestClass()]
    public class ReportingTests
    {
        //private HttpRequestBase PrContext(List<(string, string)> voHeaders)
        //{
        //    var httpRequest = new HttpRequest("", "http://someurl/", "");
        //    foreach (var o in voHeaders)
        //    {
        //        httpRequest.Headers.Add(o.Item1, o.Item2);
        //    }
        //    var stringWriter = new StringWriter();
        //    var httpResponce = new HttpResponse(stringWriter);
        //    var httpContext = new HttpContext(httpRequest, httpResponce);

        //    var sessionContainer = new HttpSessionStateContainer(
        //        "id",
        //        new SessionStateItemCollection(),
        //        new HttpStaticObjectsCollection(),
        //        10,
        //        true,
        //        HttpCookieMode.AutoDetect,
        //        SessionStateMode.InProc,
        //        false);

        //    httpContext.Items["AspSession"] =
        //        typeof(HttpSessionState).GetConstructor(
        //        BindingFlags.NonPublic | BindingFlags.Instance,
        //        null,
        //        CallingConventions.Standard,
        //        new[] { typeof(HttpSessionStateContainer) },
        //        null).Invoke(new object[] { sessionContainer });

        //    return httpRequest.RequestContext.HttpContext.Request;
        //}

        private static NameValueCollection PrContext(List<(string, string)> voHeaders)
        {
            var oRet = new NameValueCollection(voHeaders.Count);
            foreach (var o in voHeaders)
            {
                oRet.Add(o.Item1, o.Item2);
            }
            return oRet;
        }

        private static List<(string, string)> _oParams = new List<(string, string)>()
            {
                ("P001.ctstation.fr", "172.31.38.85"),
                ("P002.ctstation.fr", "SAPFCSQLSERVER"),
                ("P003.ctstation.fr", ""),
                ("P004.ctstation.fr", "ADMIN"),
                ("P005.ctstation.fr", ""),
                ("P007.ctstation.fr", "fr-FR"),
                ("P008.ctstation.fr", "en-US, fr-FR, de-DE"),
            };

        private static NameValueCollection _oHeaders = PrContext(_oParams);



        [TestMethod()]
        public void CreateReportingTest()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                MessageList oMessages = oContext.NewMessageList();

                //PrPlain(oContext, "A", "2002.03", "C");
                PrFull(oContext, "A", "2001.11", "C");
            }
        }


        private void PrFull(Context roContext, string vsPhase, string vsUpdPer, string vsVersion)
        {
            DateTime oNow = DateTime.Now.AddMonths(2);

            RefValue oUpdPer = Manager.GetRefValue(roContext, Dims.UpdPer, vsUpdPer);

            if (oUpdPer is null)
            {
                // Skip line. Error already signaled
            }
            else
            {
                ReportingLight oRep;
                Reporting oFullRep;
                if (!roContext.Exists<ReportingLight>(vsPhase, vsUpdPer))
                {
                    // New reporting to create
                    oFullRep = new Reporting();
                    oFullRep.ReadFrom(null, roContext);
                }
                else
                {
                    // Update existing reporting
                    oRep = roContext.Get<ReportingLight>(vsPhase, vsUpdPer);
                    oFullRep = roContext.Get<Reporting>(oRep.ID);
                }

                oFullRep.Phase = vsPhase;
                oFullRep.UpdatePeriod = vsUpdPer;
                oFullRep.Name = oFullRep.Phase + " - " + oFullRep.UpdatePeriod;
                oFullRep.FrameworkVersion = vsVersion;
                // Check framework is published
                if (!roContext.Exists<Framework>(oFullRep.Phase, oFullRep.FrameworkVersion))
                {
                    throw new Exception();
                }
                else
                {
                    oFullRep.Framework = roContext.Get<Framework>(oFullRep.Phase, oFullRep.FrameworkVersion);
                    if (oFullRep.Framework.Status != CTKREFLib.kref_framework_status.FRMK_STATUS_PUBLISHED)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        oFullRep.ReportingEndDate = oNow;
                        oFullRep.DefaultOperation.AllowEarlyPublishing = true;
                        oFullRep.DefaultOperation.AfterTransfer.Special = true;
                        oFullRep.DefaultPackage.UseDefaultLock = false;
                        oFullRep.DefaultPackage.UseDefaultOpbal = false;
                        oFullRep.DefaultPackage.HasOpBal = true;
                        oFullRep.DefaultPackage.OpbPhase = "A";
                        oFullRep.DefaultPackage.OpbUpdatePeriod = "2010.12";
                        oFullRep.DefaultPackage.OpbScope = "CORPORATE";
                        oFullRep.DefaultPackage.OpbVariant = "IFRSPER";
                        oFullRep.DefaultPackage.OpbConsCurrency = "EUR";

                        MessageList oMess = roContext.NewMessageList();
                        Assert.IsTrue(oFullRep.IsValid(roContext, oMess));
                        roContext.Save<Reporting>(oFullRep, oMess);

                        Assert.IsTrue(roContext.TryGet<ReportingLight>(vsPhase, vsUpdPer, out oRep));
                        oFullRep = roContext.Get<Reporting>(oRep.ID);
                        // Simple equality is not obtained, probably because of the conversion to and from ISO, so tolerate some distance
                        TimeSpan oT = oNow - oFullRep.ReportingEndDate;
                        Assert.IsTrue(Math.Abs(oT.Milliseconds) < 1000);
                    }

                }
            }
        }



        private void PrPlain(Context roContext, string vsPhase, string vsUpdPer, string vsVersion)
        {
            var oContainer = (ICtProviderContainer)roContext.Config.Session;

            var oDimManager = (ICtDimensionManager)oContainer.get_Provider(1, -524261);

            ICtRefValue oPhase = oDimManager.get_Dimension((int)ct_dimension.DIM_PHASE).get_RefValueFromName(vsPhase, 0);
            ICtRefValue oPeriod = oDimManager.get_Dimension((int)ct_dimension.DIM_UPDPER).get_RefValueFromName(vsUpdPer, 0);

            var oRepManager = (ICtObjectManager)oContainer.get_Provider(1, (int)CtReportingManagers.CT_REPORTING_MANAGER);
            ICtReporting oReporting = (ICtReporting)oRepManager.NewObject(1);


            var oRefTableManager = (ICtRefTableManager)oContainer.get_Provider(1, -524262);
            ICtRefValue oVersion = oRefTableManager.get_RefTable((int)ct_reftable.REFTABLE_FRAMEWORKVERSION).get_RefValueFromName(vsVersion, 0);

            var oFrameworkManager = (ICtFrameworkManager)oContainer.get_Provider(1, -524238);
            IRefObjRef oFramework = oFrameworkManager.get_FrameworkFromPhaseVersion(oPhase, oVersion);

            //oReporting.set_Desc(ct_desctype.ctdesc_short, lang_t.lang_1, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_long, lang_t.lang_1, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_short, lang_t.lang_2, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_long, lang_t.lang_2, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_short, lang_t.lang_3, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_long, lang_t.lang_3, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_short, lang_t.lang_4, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_long, lang_t.lang_4, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_short, lang_t.lang_5, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_long, lang_t.lang_5, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_short, lang_t.lang_6, "a");
            //oReporting.set_Desc(ct_desctype.ctdesc_long, lang_t.lang_6, "a");


            //oReporting.ReportingStartDate = (DateTime);
            //oReporting.ReportingEndDate = (DateTime);
            //oReporting.set_PropVal((int)CtReportingProperties.CT_PROP_PACK_PUBLISHING_CUTOFF_DATE, (DateTime));

            //SByte iByte = 1;
            //oReporting.set_PropVal((int)CtReportingProperties.CT_PROP_ALLOW_EARLY_PUBLISHING, iByte);

            //oReporting.Phase = oPhase;
            oReporting.UpdatePeriod = oPeriod;
            oReporting.Framework = oFramework;
            //oReporting.FrameworkVersion = oVersion;

            oRepManager.SaveObject(oReporting);
        }


        [TestMethod()]
        public void ReadReportingTest()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                Reporting oRep = oContext.Get<Reporting>("A", "2003.12");
                Assert.IsNotNull(oRep);
            }
        }


        [TestMethod()]
        public void ListRepProperties()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                var oContainer = (ICtProviderContainer)oContext.Config.Session;
                var oRepManager = (ICtObjectManager)oContainer.get_Provider(1, (int)CtReportingManagers.CT_REPORTING_MANAGER);
                ICtReporting oFC = (ICtReporting)oRepManager.GetObject(79, ACCESSFLAGS.OM_READ, 0);

                Assert.IsNotNull(oFC);
                Debug.WriteLine($"Loaded reporting {oFC.Name}");

                PrTestProps<CtReportingProperties>(oFC, true);
                PrTestProps<CtReportingRelationships>(oFC, false);
            }
        }

        [TestMethod()]
        public void ListRepEntityRepProperties()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                var oContainer = (ICtProviderContainer)oContext.Config.Session;
                var oRepManager = (ICtObjectManager)oContainer.get_Provider(1, (int)CtReportingManagers.CT_REPORTING_MANAGER);
                ICtReporting oFC = (ICtReporting)oRepManager.GetObject(71, ACCESSFLAGS.OM_READ, 0);

                Assert.IsNotNull(oFC);
                Debug.WriteLine($"Loaded reporting {oFC.Name}");

                ICtEntityReporting oEntRep = (ICtEntityReporting)oFC.RelatedEntityReportingCollection.GetAt(1);
                PrTestProps<CtReportingProperties>(oEntRep, true);
                PrTestProps<CtReportingRelationships>(oEntRep, false);
            }
        }

        private void PrTestProps<tEnum>(ICtObject voFC, bool vbProp) where tEnum : Enum
        {
            Type oType;
            bool bIsInEnum;
            dynamic oProp;
            int c = 0;
            string sName = vbProp ? "Prop" : "Rel";

            tEnum iEnum;
            Dictionary<int, tEnum> oDic = new Dictionary<int, tEnum>();
            foreach (tEnum i in Enum.GetValues(typeof(tEnum)))
                oDic.Add(i.GetHashCode(), i);

            for (int i = -0xF0000; i <= 0xF0000; i++)
            {
                bIsInEnum = oDic.TryGetValue(i, out iEnum);
                try
                {
                    oProp = vbProp ? voFC.PropVal[i] : voFC.RelVal[i];
                    if (!(oProp is null))
                    {
                        oType = oProp.GetType();
                        Debug.Write($"{sName} {i} \t 0x{i:X}: \t {oType.Name} \t'{oProp}'");
                        Debug.WriteIf(bIsInEnum, $"  \t {iEnum}");
                        Debug.WriteLine("");
                        c++;
                    }
                }
                catch (Exception e)
                {
                    Debug.Write($"{sName} {i} \t 0x{i:X}: \t {e.Message}");
                    Debug.WriteIf(bIsInEnum, $"  \t {iEnum}");
                    Debug.WriteLine("");
                }
            }
            Debug.WriteLine($"Found {c} non null {sName} for object {voFC.Name}\n");
        }



        [TestMethod()]
        public void GetRecipient()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                var oContainer = (ICtProviderContainer)oContext.Config.Session;
                var oRecepManager = (ICtObjectManager)oContainer.get_Provider(1, -524253);
                ICtRecipient oFC = (ICtRecipient)oRecepManager.GetObject(3, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL);



                Assert.IsNotNull(oFC);

                ICtReporting oRep = ((ICtReportingManager)oContainer.get_Provider(1, (int)CtReportingManagers.CT_REPORTING_MANAGER)).Reporting
                    [oContext.GetRefValue(Dims.Phase, "A").FCValue(), oContext.GetRefValue(Dims.UpdPer, "2001.12").FCValue()];

                ICtObjectManager oManager = null;
                ICtEntityReporting oEntityRep = null;


                oManager = (ICtObjectManager)oContainer.get_Provider(1, -523587); 

                oEntityRep = (ICtEntityReporting)oManager.NewObject();
                oEntityRep.Reporting = oRep;
                oEntityRep.Entity = oContext.GetRefValue(Dims.Entity, "S002").FCValue();
                oEntityRep.InputRecipient = oFC;
                oEntityRep.PublishingRecipient = oFC;
                oManager.SaveObject(oEntityRep);



            }
        }
    }
}

