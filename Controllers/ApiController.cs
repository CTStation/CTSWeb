using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CTSWeb.Models;
using CTSWeb.Util;

namespace CTSWeb.Controllers
{
    public class ApiController : Controller
    {
        // JsonResult with Newtonsoft JSON, giving more control than MS
        private class PrJsonResult : ContentResult
        {
            public PrJsonResult(Object roObj)
            {
                this.ContentEncoding = System.Text.Encoding.UTF8;
                this.ContentType = "application/json";
                JsonSerializerSettings oSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
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
            using (FCSession oSession = new FCSession(this.HttpContext))
            {
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                return new PrJsonResult(oManager.GetReportings());
            }
        }
        );


        public ActionResult Reporting(int id) => PrSafeResult(() =>
        {
            using (FCSession oSession = new FCSession(this.HttpContext))
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
    }
}