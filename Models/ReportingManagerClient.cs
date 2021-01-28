using System;
using System.Collections.Generic;
using CTCLIENTSERVERLib;
using CTREPORTINGMODULELib;
using CTSWeb.Util;


namespace CTSWeb.Models
{
    public class ReportingManagerClient
    {

        private readonly ConfigClass _config;



        public ReportingManagerClient(ConfigClass config)
        {
            this._config = config;
        }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<ReportingModel> GetReportings()
        {
            List<ReportingModel> reportings = new List<ReportingModel>();

            log.Debug("In GetReporting");

            ICtProviderContainer providerContainer = (ICtProviderContainer)this._config.Session;
            ICtObjectManager reportingManager = (ICtObjectManager)providerContainer.get_Provider(1, -523588);
            CTCLIENTSERVERLib.ICtGenCollection reportingsCol = reportingManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);

            foreach (ICtReporting reporting in reportingsCol)
            {
                ReportingModel mod = new ReportingModel(reporting);

                reportings.Add(mod);
            }



            return reportings;
        }



        public List<ReportingModel> GetReporting(int id)
        {
            List<ReportingModel> reportings = new List<ReportingModel>();

            log.Debug("In GetReporting");

            ICtProviderContainer providerContainer = (ICtProviderContainer)this._config.Session;
            ICtObjectManager reportingManager = (ICtObjectManager)providerContainer.get_Provider(1, -523588);
            CTCLIENTSERVERLib.ICtGenCollection reportingsCol = reportingManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);

            foreach (ICtReporting reporting in reportingsCol)
            {
                if (reporting.ID == id)
                {
                    ReportingModel mod = new ReportingModel(reporting, true);
                    reportings.Add(mod);
                }
            }



            return reportings;
        }

        public int CreateReporting(String rsCategName, string rsCategVersionName, string rsUpdPerName, DateTime roStartDate, DateTime roEndDate, DateTime roDeadline)
        {
            ReportingModel oRep = new ReportingModel();
            return 0;
        }
    }
}