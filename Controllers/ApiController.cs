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
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Newtonsoft.Json;
using CTSWeb.Models;
using CTSWeb.Util;



namespace CTSWeb.Controllers
{
    // Main API controller
    //
    // TODO: Add license-controlled access to verbs  https://github.com/rubicon-oss/LicenseHeaderManager/wiki/License-Header-Definitions
    // Standard.Licensing

    public class ApiController : Controller
    {
        // JsonResult with NewtonSoft JSON, giving more control than MS
        private class PrJsonResult : ContentResult
        {
            public PrJsonResult(Object roObj)
            {
                this.ContentEncoding = System.Text.Encoding.UTF8;
                this.ContentType = "application/json";
                JsonSerializerSettings oSettings = new JsonSerializerSettings
                {
                    DateFormatHandling         = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling       = DateTimeZoneHandling.Utc,          // Force time zone info
                    PreserveReferencesHandling = PreserveReferencesHandling.None,   // Same object is serialized multiple times
                    ReferenceLoopHandling      = ReferenceLoopHandling.Ignore
                };
                this.Content = JsonConvert.SerializeObject(roObj, oSettings);
            }
        }

        // Helper function to use in controllers
        // Handles exceptions without crashing the server
        // Returns the exception text in an HTTP message
        private static ActionResult PrSafeResult(Func<ActionResult> roDelegate)
        {
            ActionResult oRet;
            try
            {
                oRet = roDelegate();
            }
            catch (Exception e)
            {
                oRet = new HttpStatusCodeResult(HttpStatusCode.BadRequest, e.Message);  // Adding the stack trace creates an invalid argument error (too long, probably)
            }
            return oRet;
        }


        public ActionResult Help() => PrSafeResult(() => new PrJsonResult(Models.Help.Commands()));


        public ActionResult Reportings() => PrSafeResult(() =>
        {
            using (Context oSession = new Context(this.HttpContext))
            {
                return new PrJsonResult(oSession.GetAll<ReportingModel>());
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                return new PrJsonResult(oManager.GetReportings());
            }
        }
        );


        public ActionResult Reporting(int id) => PrSafeResult(() =>
        {
            using (Context oSession = new Context(this.HttpContext))
            {
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                return new PrJsonResult(oManager.GetReporting(id));
            }
        }
        );

        [HttpPost]
        public ActionResult Reporting() => PrSafeResult(() =>
        {
            Stream oBody = this.Request.InputStream;
            oBody.Seek(0, SeekOrigin.Begin);
            JsonTextReader oJReader = new JsonTextReader(new StreamReader(oBody));
            // JObject o = JObject.Load(oJReader);
            DataSet oSet = new JsonSerializer().Deserialize<DataSet>(oJReader);
            // Need the test because the exception stops production of meaningfull reslut
            if (!(oSet?.Tables["Table"] is null))
            {
                ActionResult oRet = new PrJsonResult((from row in oSet.Tables["Table"].AsEnumerable() select row["Phase"]).Distinct().ToList());
                return oRet;
            }
            else throw new KeyNotFoundException("Table not found in dataset");
        }
        );

        public ActionResult Languages() => PrSafeResult(() =>
        {
            using (Context oContext = new Context(this.HttpContext))
            {
                return new PrJsonResult(oContext.GetActiveLanguages());
            }
        }
        );


    }
}