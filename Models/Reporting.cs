#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using log4net;
using CTREPORTINGMODULELib;
using CTCORELib;
using CTCLIENTSERVERLib;
using CTSWeb.Util;


namespace CTSWeb.Models
{
    public class ReportingLight : ManagedObjectWithDescAndSecurity // Inherits ID and Name
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // TODO use attribute rather than a full field
        public static bool _bDontSaveName = true;

        static ReportingLight()
        {
            Manager.Register<ReportingLight>((int)CtReportingManagers.CT_REPORTING_MANAGER, (int)LanguageMasks.LongDesc ); // TranslatableField.None
            Manager.RegisterDelegate<ReportingLight>((Context roContext, ICtObjectManager voMgr, string vsID1, string vsID2) => 
                                                        (ICtObject)((ICtReportingManager)voMgr).Reporting[
                                                            roContext.GetRefValue("Phase", vsID1).FCRefValue(), 
                                                            roContext.GetRefValue("UpdPer", vsID2).FCRefValue()]
                                                    );
        }

        // Argument-less constructor
        public ReportingLight() { }

        public string Phase;
        public string UpdatePeriod;
        public string FrameworkVersion;

        public DateTime ReportingStartDate;
        public DateTime ReportingEndDate;

        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);


            if(!(roObject is null)) 
            {
                ICtReporting reporting = (ICtReporting)roObject;

                Phase = reporting.Phase.Name;
                UpdatePeriod = reporting.UpdatePeriod.Name;
                FrameworkVersion = reporting.FrameworkVersion.Name;
                ReportingStartDate = reporting.ReportingStartDate;
                ReportingEndDate = reporting.ReportingEndDate;
            }
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess, Context roContext)
        {
            base.WriteInto(roObject, roMess, roContext);

            // Not used
            // _oLog.Debug($"Writen  {this.GetType().Name} {Name}");
        }

        public bool Equals(ReportingLight voRep)
        {
            return ID == voRep?.ID;
        }
    }


    public class Reporting : ReportingLight
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        static Reporting() 
        {
            Manager.Register<Reporting>((int)CtReportingManagers.CT_REPORTING_MANAGER, (int)(LanguageMasks.ShortDesc | LanguageMasks.LongDesc | LanguageMasks.Comment)); // TranslatableField.None
            Manager.RegisterDelegate<Reporting>((Context roContext, ICtObjectManager voMgr, string vsID1, string vsID2) =>
                                                        (ICtObject)((ICtReportingManager)voMgr).Reporting[
                                                            roContext.GetRefValue("Phase", vsID1).FCRefValue(),
                                                            roContext.GetRefValue("UpdPer", vsID2).FCRefValue()]
                                                    );
        }

        // Argument-less constructor
        public Reporting() { }


        public int Status;
        public Framework Framework;
        //public ExchangeRate ExchangeRate;
        //public ExchangeRateUpdatePeriod ExchangeRateUpdatePeriod;
        //public ExchangeRateVersion ExchangeRateVersion;
        //public ExchangeRateType ExchangeRateType;
        //public List<Period> Periods;
        //public uint ReportingModifyComment;
        //public DateTime ReportingHierarchyDate;
        public List<RelatedEntityReportingCollection> RelatedEntityReportingCollection;


        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);
            if(!(roObject is null))
            {

                ICtReporting reporting = (ICtReporting)roObject;

                Status = (int)reporting.Status;
                Framework = new Framework();
                Framework.ReadFrom((ICtObject)reporting.Framework, roLang);
                //ExchangeRate = new ExchangeRate(reporting.ExchangeRate);
                //ExchangeRateUpdatePeriod = new ExchangeRateUpdatePeriod(reporting.ExchangeRateUpdatePeriod);
                //ExchangeRateVersion = new ExchangeRateVersion(reporting.ExchangeRateVersion);
                RelatedEntityReportingCollection = new List<RelatedEntityReportingCollection>();

                //if (reporting.ExchangeRateType != null)
                //{
                //    ExchangeRateType = new ExchangeRateType()
                //    {

                //        ID = reporting.ExchangeRateType.ID,
                //        Name = reporting.ExchangeRateType.Name
                //    };
                //}
                //else
                //{
                //    ExchangeRateType = new ExchangeRateType();
                //}

                foreach (ICtEntityReporting reporting1 in reporting.RelatedEntityReportingCollection)
                {
                    RelatedEntityReportingCollection.Add(new RelatedEntityReportingCollection(reporting1));
                }

                //Periods = new List<Period>();
                //foreach (ICtRefValue period in reporting.Periods)
                //{
                //    Periods.Add(new Period() { ID = period.ID, Name = period.Name });
                //}
                //ReportingModifyComment = reporting.ReportingModifyComment;
                //ReportingHierarchyDate = reporting.ReportingHierarchyDate;
                _oLog.Debug($"Read {Phase} - {UpdatePeriod}");
            }
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess, Context roContext)
        {
            base.WriteInto(roObject, roMess, roContext);

            ICtReporting oRep = (ICtReporting)roObject;

            RefValue oUpdPer = roContext.GetRefValue("UpdPer", UpdatePeriod);
            Framework = roContext.Get<Framework>(Phase, FrameworkVersion);

            oRep.UpdatePeriod = oUpdPer.FCRefValue();
            oRep.Framework = Framework.FCValue();

            // TODO: Make it clean
            //oRep.set_PropVal((int)CtReportingProperties.CT_PROP_PACK_PUBLISHING_CUTOFF_DATE, ReportingEndDate);
            //byte iAdvancedPub = 1;
            //oRep.set_PropVal((int)CtReportingProperties.CT_PROP_ALLOW_EARLY_PUBLISHING, iAdvancedPub);
            //int iIntegAfterPub = (int)(1 * Math.Pow(2, 16));
            //oRep.set_PropVal((int)CtReportingProperties.CT_PROP_INTEGRATE_AFTER_PUB, iIntegAfterPub);
            //int iIntegAfterTrans = (int)(1 * Math.Pow(2, 16));
            //oRep.set_PropVal((int)CtReportingProperties.CT_PROP_INTEGRATE_AFTER_TRANSFER, iIntegAfterTrans);

            _oLog.Debug($"Writen {this.GetType().Name} {Phase} - {UpdatePeriod}");
        }


        public static List<Reporting> LoadFromDataSet(DataSet voData, Context roContext, MessageList roMessages)
        {
            List<Reporting> oRet = new List<Reporting>();

            IControl oCtrl = new ControlColumnsExist() { TableName = "Table", RequiredColumns = new List<string> 
                                                                    { "Phase", "UpdatePeriod", "FrameworkVersion", "ReportingStartDate", "ReportingEndDate" } };
            if (oCtrl.Pass(voData, roMessages))
            {
                HashSet<int> oInvalidRows = new HashSet<int>();
                new ControlValidateColumn("Table", "Phase", roContext.GetRefValues("Phase")).Pass(voData, roMessages, oInvalidRows, true, null);
                new ControlValidateColumn("Table", "UpdatePeriod", Context.GetPeriodValidator).Pass(voData, roMessages, oInvalidRows, true, null);
                new ControlValidateColumn("Table", "FrameworkVersion", roContext.GetRefValues("FrameworkVersion")).Pass(voData, roMessages, oInvalidRows, true, null);

                Reporting oFullRep;
                int c = 0;
                foreach (DataRow o in voData.Tables["Table"].Rows) 
                {
                    if (!oInvalidRows.Contains(c))
                    { 
                        if (roContext.Exists<Reporting>((string)o["Phase"], (string)o["UpdatePeriod"]))
                        {
                            // New reporting to create
                            oFullRep = new Reporting();
                            oFullRep.ReadFrom(null, roContext.Language);    // sets up the object, equivalent to constructor TODO: maybe a Construct method?
                        }
                        else
                        {
                            // Update existing reporting
                            oFullRep = roContext.Get<Reporting>((string)o["Phase"], (string)o["UpdatePeriod"]);
                        }

                        oFullRep.Phase = (string)o["Phase"];
                        oFullRep.UpdatePeriod = (string)o["UpdatePeriod"];
                        oFullRep.Name = oFullRep.Phase + " - " + oFullRep.UpdatePeriod;
                        oFullRep.FrameworkVersion = (string)o["FrameworkVersion"];
                        // Check framework is published
                        if (roContext.Exists<Framework>(oFullRep.Phase, oFullRep.FrameworkVersion))
                        {
                            roMessages.Add("RF0010", oFullRep.Phase, oFullRep.FrameworkVersion);
                        }
                        else
                        {
                            oFullRep.Framework = roContext.Get<Framework>(oFullRep.Phase, oFullRep.FrameworkVersion);
                            if (oFullRep.Framework.Status != CTKREFLib.kref_framework_status.FRMK_STATUS_PUBLISHED)
                            {
                                roMessages.Add("RF0010", oFullRep.Phase, oFullRep.FrameworkVersion);
                            }
                            else
                            {
                                oFullRep.ReportingStartDate = (DateTime)o["ReportingStartDate"];
                                oFullRep.ReportingEndDate = (DateTime)o["ReportingEndDate"];
                                // Desc is set automatically from Phase in all languages
                                roContext.Save<Reporting>(oFullRep, roMessages);
                                oRet.Add(oFullRep);
                            }
                        }
                    }
                    c++;
                }
            }
            return oRet;
        }

        // Manage complex identifier: users choose framework from Phase and version, not from name
        // Avoid burdening Manager with a complex identifier concept, deal with the lists here
        // List of frameworks is usualy short
        // TODO Move this to Manage with a delegate that builds the identifier as string[], then buld the delegate from attributes
        public static List<(string, string)> BuildPickupList(List<ReportingLight> voList)
        {
            List<(string, string)> oRet = new List<(string, string)>();
            foreach (ReportingLight o in voList) oRet.Add((o.Phase, o.UpdatePeriod));
            return oRet;
        }


        public static ReportingLight GetFromUserID(List<ReportingLight> voList, string[] vasUserID) 
        {
            ReportingLight oRet = null;
            foreach (ReportingLight o in voList) if (o.Phase == vasUserID[0] && o.UpdatePeriod == vasUserID[1]) { oRet = o; break; }
            return oRet;
        }

    }
}



