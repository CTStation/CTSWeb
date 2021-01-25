using System;
using System.Collections.Generic;
using CTREPORTINGMODULELib;
using CTCORELib;

namespace CTSWeb.Models
{

    public class ReportingModel
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
        List<RelatedEntityReportingCollection> RelatedEntityReportingCollection;

        public ReportingModel(ICtReporting reporting, bool details = false)
        {
            ID = reporting.ID;
            Name = reporting.Name;
            //reporting.Phase
            UpdatePeriod = reporting.UpdatePeriod.Name;
            FrameworkVersion = reporting.FrameworkVersion.Name;
            Type = reporting.Type;
            TypeName = reporting.TypeName; ;
            TypeDesc = reporting.TypeDesc;
            Dirty = reporting.Dirty;
            IsNew = reporting.IsNew;
            IsReadOnly = reporting.IsReadOnly;
            LoadedCategoryMask = reporting.LoadedCategoryMask;
            ScalarDirtyCategoryMask = reporting.ScalarDirtyCategoryMask;
            Categories = reporting.Categories;
            Icon = reporting.Icon;
            LargeIcon = reporting.LargeIcon;
            ImplementationAddress = reporting.ImplementationAddress;
            OwnerSite = reporting.OwnerSite;
            OwnerWorkgroup = reporting.OwnerWorkgroup;
            VisibilityMode = (int)reporting.VisibilityMode;
            CreationDate = reporting.CreationDate;
            Author = reporting.Author;
            UpdateDate = reporting.UpdateDate;
            UpdateAuthor = reporting.UpdateAuthor;
            Status = (int)reporting.Status;
            ReportingStartDate = reporting.ReportingStartDate;
            ReportingEndDate = reporting.ReportingEndDate;
            if (details)
            {
                Framework = new Framework(reporting.Framework);
                ExchangeRate = new ExchangeRate(reporting.ExchangeRate);
                ExchangeRateUpdatePeriod = new ExchangeRateUpdatePeriod(reporting.ExchangeRateUpdatePeriod);
                ExchangeRateVersion = new ExchangeRateVersion(reporting.ExchangeRateVersion);
                RelatedEntityReportingCollection = new List<RelatedEntityReportingCollection>();

                if (reporting.ExchangeRateType != null)
                {
                    ExchangeRateType = new ExchangeRateType()
                    {

                        ID = reporting.ExchangeRateType.ID,
                        Name = reporting.ExchangeRateType.Name
                    };
                }
                else
                {
                    ExchangeRateType = new ExchangeRateType();

                }

                foreach (ICtEntityReporting reporting1 in reporting.RelatedEntityReportingCollection)
                {
                    RelatedEntityReportingCollection.Add(new RelatedEntityReportingCollection(reporting1));
                }

                Periods = new List<Period>();
                foreach (ICtRefValue period in reporting.Periods)
                {
                    Periods.Add(new Period() { ID = period.ID,Name=period.Name }) ;
                }
                ReportingModifyComment = reporting.ReportingModifyComment;
                ReportingHierarchyDate =  reporting.ReportingHierarchyDate;
            }
            Locked = reporting.Locked;

        }

    }

}
