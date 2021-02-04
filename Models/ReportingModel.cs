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

    public class ReportingModel : ManagedObject
    {
        public int ID;
        public string Name;
        public string UpdatePeriod;
        public string FrameworkVersion;
        public int Type;
        public string TypeName;
        public string TypeDesc;
        public bool Dirty;
        public bool IsNew;
        public bool IsReadOnly;
        public int LoadedCategoryMask;
        public int ScalarDirtyCategoryMask;
        public int Categories;
        public int Icon;
        public int LargeIcon;
        public int ImplementationAddress;

        public int OwnerSite;
        public int OwnerWorkgroup;
        public int VisibilityMode;
        public DateTime CreationDate;
        public int Author;
        public DateTime UpdateDate;
        public int UpdateAuthor;
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
        public uint Locked;
        public List<RelatedEntityReportingCollection> RelatedEntityReportingCollection;

        public ReportingModel()
        {
            // Create empty
            Dirty = true;
        }

        public override ManagedObject Factory(ICtObjectBase roObj)
        {
            ReportingModel oNewObj = new ReportingModel();

            PrReportingModel(oNewObj, (ICtReporting)roObj, false);
            return oNewObj;
        }

        public ReportingModel(ICtReporting reporting, bool details = false)
        {
            PrReportingModel(this, reporting, details);
        }

        private void PrReportingModel(ReportingModel roObj, ICtReporting reporting, bool details)
        { 
            roObj.ID = reporting.ID;
            roObj.Name = reporting.Name;
            //reporting.Phase
            roObj.UpdatePeriod = reporting.UpdatePeriod.Name;
            roObj.FrameworkVersion = reporting.FrameworkVersion.Name;
            roObj.Type = reporting.Type;
            roObj.TypeName = reporting.TypeName; ;
            roObj.TypeDesc = reporting.TypeDesc;
            roObj.Dirty = reporting.Dirty;
            roObj.IsNew = reporting.IsNew;
            roObj.IsReadOnly = reporting.IsReadOnly;
            roObj.LoadedCategoryMask = reporting.LoadedCategoryMask;
            roObj.ScalarDirtyCategoryMask = reporting.ScalarDirtyCategoryMask;
            roObj.Categories = reporting.Categories;
            roObj.Icon = reporting.Icon;
            roObj.LargeIcon = reporting.LargeIcon;
            roObj.ImplementationAddress = reporting.ImplementationAddress;
            roObj.OwnerSite = reporting.OwnerSite;
            roObj.OwnerWorkgroup = reporting.OwnerWorkgroup;
            roObj.VisibilityMode = (int)reporting.VisibilityMode;
            roObj.CreationDate = reporting.CreationDate;
            roObj.Author = reporting.Author;
            roObj.UpdateDate = reporting.UpdateDate;
            roObj.UpdateAuthor = reporting.UpdateAuthor;
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
            roObj.Locked = reporting.Locked;

        }

    }
}



