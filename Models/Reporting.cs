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
using System.Collections.Generic;
using CTREPORTINGMODULELib;
using CTCORELib;

using System.Linq;
using System.Text;
using CTCOMDEFAULTLib;
using CTCLIENTSERVERLib;
using CTCOMMONMODULELib;

using CTKREFLib;
using CTTRANSFERLib;
using CTSWeb.Util;


namespace CTSWeb.Models
{

    public class Reporting : ManagedObjectWithDescAndSecurity // Inherits ID and Name
    {
        static Reporting() 
        {
            // Comment seems to bug the second time it's called
            Manager.Register<Reporting>((int)CtReportingManagers.CT_REPORTING_MANAGER, (int)(TranslatableField.ShortDesc | TranslatableField.LongDesc));
        }

        public string Phase;
        public string UpdatePeriod;
        public string FrameworkVersion;

        public int Status;
        public DateTime ReportingStartDate;
        public DateTime ReportingEndDate;
        public Framework Framework;
        public ExchangeRate ExchangeRate;
        public ExchangeRateUpdatePeriod ExchangeRateUpdatePeriod;
        public ExchangeRateVersion ExchangeRateVersion;
        public ExchangeRateType ExchangeRateType;
        public List<Period> Periods;
        public uint ReportingModifyComment;
        public DateTime ReportingHierarchyDate;
        public List<RelatedEntityReportingCollection> RelatedEntityReportingCollection;

        public Reporting()
        {
        }

        public override ManagedObject CreateFrom(ICtObjectBase roObj, Language roLang)
        {
            Reporting oNewObj = new Reporting();

            _oLanguage = roLang;

            PrReportingModel(oNewObj, (ICtReporting)roObj, false);
            return oNewObj;
        }

        public Reporting(ICtReporting reporting, bool details = false)
        {
            PrReportingModel(this, reporting, details);
        }

        private void PrReportingModel(Reporting roObj, ICtReporting reporting, bool details)
        {
            Manager.LoadFromFC<Reporting>(roObj, reporting, _oLanguage);
            roObj.Phase = reporting.Phase.Name;
            roObj.UpdatePeriod = reporting.UpdatePeriod.Name;
            roObj.FrameworkVersion = reporting.FrameworkVersion.Name;
            roObj.Status = (int)reporting.Status;
            roObj.ReportingStartDate = reporting.ReportingStartDate;
            roObj.ReportingEndDate = reporting.ReportingEndDate;
            if (details)
            {
                roObj.Framework = new Framework(reporting.Framework);
                roObj.ExchangeRate = new ExchangeRate(reporting.ExchangeRate);
                roObj.ExchangeRateUpdatePeriod = new ExchangeRateUpdatePeriod(reporting.ExchangeRateUpdatePeriod);
                roObj.ExchangeRateVersion = new ExchangeRateVersion(reporting.ExchangeRateVersion);
                roObj.RelatedEntityReportingCollection = new List<RelatedEntityReportingCollection>();

                if (reporting.ExchangeRateType != null)
                {
                    roObj.ExchangeRateType = new ExchangeRateType()
                    {

                        ID = reporting.ExchangeRateType.ID,
                        Name = reporting.ExchangeRateType.Name
                    };
                }
                else
                {
                    roObj.ExchangeRateType = new ExchangeRateType();
                }

                foreach (ICtEntityReporting reporting1 in reporting.RelatedEntityReportingCollection)
                {
                    roObj.RelatedEntityReportingCollection.Add(new RelatedEntityReportingCollection(reporting1));
                }

                roObj.Periods = new List<Period>();
                foreach (ICtRefValue period in reporting.Periods)
                {
                    roObj.Periods.Add(new Period() { ID = period.ID, Name = period.Name });
                }
                roObj.ReportingModifyComment = reporting.ReportingModifyComment;
                roObj.ReportingHierarchyDate = reporting.ReportingHierarchyDate;
            }
        }

    }
}



