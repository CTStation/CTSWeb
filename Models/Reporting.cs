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

        public override bool IsValid(Context roContext, MessageList roMess)
        {
            return base.IsValid(roContext, roMess);
        }


        public override bool Exists(Context roContext)
        {
            bool bRet;
            if (ID != 0)
            {
                bRet = Manager.TryGetFCObject(roContext, ID, GetType(), out _);
            }
            else
            {
                if (String.IsNullOrEmpty(Phase) || string.IsNullOrEmpty(UpdatePeriod))
                {
                    bRet = false;
                }
                else
                {
                    bRet = Manager.TryGetFCObject(roContext, Phase, UpdatePeriod, GetType(), out _);
                }
            }
            return bRet;
        }

    }


   
    public class Reporting : ReportingLight
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        static Reporting() 
        {
            Manager.Register<Reporting>((int)CtReportingManagers.CT_REPORTING_MANAGER, (int)(LanguageMasks.ShortDesc | LanguageMasks.LongDesc | LanguageMasks.Comment)); // TranslatableField.None
            // Get reporting from phase and updper
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

        public Package DefaultPackage;
        public Restriction DefaultRestriction;
        public Operation DefaultOperation;
        public List<EntityReporting> RelatedEntityReportingCollection;


        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);

            if(!(roObject is null))
            {
                ICtReporting oFC = (ICtReporting)roObject;

                Status = (int)oFC.Status;
                Framework = new Framework();
                Framework.ReadFrom((ICtObject)oFC.Framework, roLang);


                //ExchangeRate = new ExchangeRate(reporting.ExchangeRate);
                //ExchangeRateUpdatePeriod = new ExchangeRateUpdatePeriod(reporting.ExchangeRateUpdatePeriod);
                //ExchangeRateVersion = new ExchangeRateVersion(reporting.ExchangeRateVersion);

                //DefaultPackage = new Package() {

                //};


                DefaultOperation = new Operation()
                {
                    PackPublishingCutOffDate = oFC.PropVal[(int)CtReportingProperties.CT_PROP_PACK_PUBLISHING_CUTOFF_DATE],
                    AllowEarlyPublishing = (0 != oFC.PropVal[(int)CtReportingProperties.CT_PROP_ALLOW_EARLY_PUBLISHING]),
                    IntegrateAfterPublication = oFC.PropVal[(int)CtReportingProperties.CT_PROP_INTEGRATE_AFTER_PUB],
                    ControlLevelReachedAfterPublication = Framework.GetControlLevel(oFC.RelVal[(int)CtReportingRelationships.CT_REL_REPORTING_CTRL_LEVEL_REACHED_PUB]?.Rank),
                    IntegrateAfterTransfer = oFC.PropVal[(int)CtReportingProperties.CT_PROP_INTEGRATE_AFTER_TRANSFER],
                    ControlLevelReachedAfterTransfer = Framework.GetControlLevel(oFC.RelVal[(int)CtReportingRelationships.CT_REL_REPORTING_CTRL_LEVEL_REACHED_TRANSFER]?.Rank)
                };

                RelatedEntityReportingCollection = new List<EntityReporting>();

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

                EntityReporting o;
                foreach (ICtEntityReporting oFCDetail in oFC.RelatedEntityReportingCollection)
                {
                    o = new EntityReporting();
                    o.ReadFrom(oFCDetail, roLang);
                    RelatedEntityReportingCollection.Add(o);
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



        public override bool IsValid(Context roContext, MessageList roMess)
        {
            return base.IsValid(roContext, roMess);
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
                        if (!roContext.TryGet<Reporting>((string)o["Phase"], (string)o["UpdatePeriod"], out oFullRep))
                        {
                            // New reporting to create
                            oFullRep = new Reporting();
                            oFullRep.ReadFrom(null, roContext.Language);    // sets up the object, equivalent to constructor TODO: maybe a Construct method?
                        } // Else Update existing reporting

                        oFullRep.Phase = (string)o["Phase"];
                        oFullRep.UpdatePeriod = (string)o["UpdatePeriod"];
                        oFullRep.Name = oFullRep.Phase + " - " + oFullRep.UpdatePeriod;
                        oFullRep.FrameworkVersion = (string)o["FrameworkVersion"];
                        // Check framework is published
                        if (!roContext.TryGet<Framework>(oFullRep.Phase, oFullRep.FrameworkVersion, out oFullRep.Framework))
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
                                oFullRep.ReportingStartDate = (DateTime)o["ReportingStartDate"];
                                oFullRep.ReportingEndDate = (DateTime)o["ReportingEndDate"];
                                // Desc is set automatically from Phase in all languages
                                // Should not save each time. Maybe get the rporting from the list
                                // TODO: see why doesn t work
                                // Save only once and not per line
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
    }



    public class EntityReporting : ManagedObject
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool _bDontSaveName = true;

        static EntityReporting()
        {
            // No manager, read from reporting
            //Manager.Register<ReportingLight>((int)CtReportingManagers.CT_REPORTING_MANAGER, (int)LanguageMasks.LongDesc); // TranslatableField.None
        }

        // Argument-less constructor
        public EntityReporting() { }

        public string Entity;
        public string InputCurrency;
        public bool IsInputSiteLocal;

        public Operation PackOperation;

        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);


            if (!(roObject is null))
            {
                ICtEntityReporting oFC = (ICtEntityReporting)roObject;

                Entity = oFC.Entity.Name;
                InputCurrency = oFC.InputCurrency.Name;
                IsInputSiteLocal = oFC.IsInputSiteLocal;     //TODO InputRecipient; TransferRecipient PublishingRecipient
                PackOperation = new Operation() {
                    PackPublishingCutOffDate = oFC.DefaultPackOperation.PackPublishingCutOffDate,
                    AllowEarlyPublishing = oFC.DefaultPackOperation.AllowEarlyPublishing,
                    IntegrateAfterPublication = oFC.DefaultPackOperation.IntegrateAfterPublication,
                    IntegrateAfterTransfer = oFC.DefaultPackOperation.IntegrateAfterTranfer
                };
            }
        }

        // TODO
        public override void WriteInto(ICtObject roObject, MessageList roMess, Context roContext)
        {
            base.WriteInto(roObject, roMess, roContext);

            // Not used
            // _oLog.Debug($"Writen  {this.GetType().Name} {Name}");
        }

        //TODO
        public override bool IsValid(Context roContext, MessageList roMess)
        {
            return base.IsValid(roContext, roMess);
        }
    }



    public class Package
    {
    }



    public class Restriction
    {
    }



    public class Operation
    {
        public DateTime PackPublishingCutOffDate;
        public bool AllowEarlyPublishing;
        public int IntegrateAfterPublication;
        public ControlLevel ControlLevelReachedAfterPublication;
        public int IntegrateAfterTransfer;
        public ControlLevel ControlLevelReachedAfterTransfer;

        public const int StandardMask = 0x10000;
        public const int SpecialMask = 0x20000;
        public const int AdvancedMask = 0x40000;

        public bool IsStandard(int viFlags) => (viFlags & StandardMask) != 0;
        public int SetStandard(int viFlags, bool vbMode) => vbMode ? viFlags | StandardMask : viFlags & ~StandardMask;
        public bool IsSpecial(int viFlags) => (viFlags & SpecialMask) != 0;
        public int SetSpecial(int viFlags, bool vbMode) => vbMode ? viFlags | SpecialMask : viFlags & ~SpecialMask;
        public bool IsAdvanced(int viFlags) => (viFlags & AdvancedMask) != 0;
        public int SetAdvanced(int viFlags, bool vbMode) => vbMode ? viFlags | AdvancedMask : viFlags & ~AdvancedMask;
    }

}

