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

        static ReportingLight()
        {
            Manager.Register<ReportingLight>((int)CtReportingManagers.CT_REPORTING_MANAGER, (int)(LanguageMasks.ShortDesc | LanguageMasks.LongDesc | LanguageMasks.Comment)); // TranslatableField.None
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

            ICtReporting reporting = (ICtReporting)roObject;
            Phase = reporting.Phase.Name;
            UpdatePeriod = reporting.UpdatePeriod.Name;
            FrameworkVersion = reporting.FrameworkVersion.Name;
            ReportingStartDate = reporting.ReportingStartDate;
            ReportingEndDate = reporting.ReportingEndDate;
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess)
        {
            // Not used
            _oLog.Debug($"Writen  {this.GetType().Name} {Name}");
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
        }
        
        // Argument-less constructor
        public Reporting() { }


        public int Status;
        public Framework Framework;
        private CTKREFLib.IRefObjRef _oFCFramework;
        private ICtRefValue _oFCPhase;
        private ICtRefValue _oFCVersion;
        private ICtRefValue _oFCUpdPer;
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

        public override void WriteInto(ICtObject roObject, MessageList roMess)
        {
            base.WriteInto(roObject, roMess);

            ICtReporting oRep = (ICtReporting)roObject;
            oRep.Framework = _oFCFramework;
            oRep.Name = Name;
            oRep.Phase = _oFCPhase;
            oRep.ReportingEndDate = ReportingEndDate;
            oRep.ReportingStartDate = ReportingStartDate;
            oRep.UpdatePeriod = _oFCUpdPer;
            _oLog.Debug($"Writen {this.GetType().Name} {Phase} - {UpdatePeriod}");
        }

        public static List<Reporting> LoadFromDataSet(DataSet voData, Context voContext, MessageList roMessages)
        {
            List<Reporting> oRet = new List<Reporting>();
            IControl oCtrl = new ControlColumnsExist() { TableName = "Table", RequiredColumns = new List<string> 
                                                                    { "Phase", "UpdatePeriod", "FrameworkVersion", "ReportingStartDate", "ReportingEndDate" } };
            if (oCtrl.Pass(voData, roMessages))
            {
                HashSet<int> oInvalidRows = new HashSet<int>();
                new ControlValidateColumn("Table", "Phase", voContext.GetRefValues("Phase")).Pass(voData, roMessages, oInvalidRows, true, null);
                new ControlValidateColumn("Table", "UpdatePeriod", Context.GetPeriodValidator).Pass(voData, roMessages, oInvalidRows, true, null);
                new ControlValidateColumn("Table", "FrameworkVersion", voContext.GetRefValues("FrameworkVersion")).Pass(voData, roMessages, oInvalidRows, true, null);

                List<Framework> oRefList = Manager.GetAll<Framework>(voContext);
                List<ReportingLight> oReportings = Manager.GetAll<ReportingLight>(voContext);
                ReportingLight oRep = null;
                Reporting oFullRep;
                int c = 0;
                foreach (DataRow o in voData.Tables["Table"].Rows) 
                {
                    if (!oInvalidRows.Contains(c))
                    { 
                        RefValue oPhase = Manager.GetRefValue(voContext.Config, "Phase", (string)o["Phase"], voContext.Language);
                        RefValue oUpdPer = Manager.GetRefValue(voContext.Config, "UpdPer", (string)o["UpdatePeriod"], voContext.Language);

                        if (oPhase is null || oUpdPer is null)
                        {
                            // Skip line. Error already signaled
                            _oLog.Debug("Error: invalidLine not in invalid list");
                        }
                        else
                        {
                            oRep = Reporting.GetFromUserID(oReportings, new string[2] { (string)o["Phase"], (string)o["UpdatePeriod"] });
                            if (oRep == null)
                            {
                                // New reporting to create
                                oFullRep = new Reporting();
                            }
                            else
                            {
                                // Update existing reporting
                                oFullRep = Manager.Get<Reporting>(voContext, oRep.ID, roMessages);
                            }

                            oFullRep.Phase = (string)o["Phase"];
                            oFullRep.UpdatePeriod = (string)o["UpdatePeriod"];
                            oFullRep.Name = oFullRep.Phase + " - " + oFullRep.UpdatePeriod;
                            oFullRep.FrameworkVersion = (string)o["FrameworkVersion"];
                            // Check framework is published
                            oFullRep.Framework = Framework.GetFromPhaseVersion(oRefList, oFullRep.Phase, oFullRep.FrameworkVersion, out CTKREFLib.IRefObjRef oFramekork);
                            if (oFullRep.Framework is null)
                            {
                                roMessages.Add("RF0010", oFullRep.Phase, oFullRep.FrameworkVersion);
                            }
                            else
                            {
                                if (oFullRep.Framework.Status != CTKREFLib.kref_framework_status.FRMK_STATUS_PUBLISHED)
                                {
                                    roMessages.Add("RF0010", oFullRep.Phase, oFullRep.FrameworkVersion);
                                }
                                else
                                {
                                    oFullRep._oFCUpdPer = oUpdPer.FCRefValue;
                                    oFullRep._oFCFramework = oFramekork;
                                    
                                    oFullRep.ReportingStartDate = (DateTime)o["ReportingStartDate"];
                                    oFullRep.ReportingEndDate = (DateTime)o["ReportingEndDate"];
                                    oFullRep.Descriptions = new LanguageText[oPhase.Descriptions.Length];
                                    for (int i = 0; i < oPhase.Descriptions.Length; i++)
                                    {
                                        oFullRep.Descriptions[i] = new LanguageText(voContext.WorkingLanguage, voContext.Language);
                                        oFullRep.Descriptions[i].Texts["LDesc"] = oPhase.Descriptions[i].Texts["LDesc"] + " - " + oUpdPer.Name;
                                    }
                                    Manager.Save<Reporting>(voContext.Config, oFullRep, roMessages);
                                    oRet.Add(oFullRep);
                                }
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



