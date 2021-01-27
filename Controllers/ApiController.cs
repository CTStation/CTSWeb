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

namespace CTSWeb.Controllers
{
    public class ApiController : Controller
    {
        private class S_JsonResult : ContentResult
        {
            public S_JsonResult(Object roObj)
            {
                this.ContentEncoding = System.Text.Encoding.UTF8;
                this.ContentType = "application/json";
                JsonSerializerSettings oSettings = new JsonSerializerSettings();
                oSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                oSettings.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
                oSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                oSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                this.Content = JsonConvert.SerializeObject(roObj, oSettings);
            }
        }

        public ActionResult Help()
        {
            return  new S_JsonResult(Models.Help.Commands());
        }

        public ActionResult Reportings()
        {
            ActionResult oRet = null;

            using (FCSession oSession = new FCSession(this.HttpContext))
            {
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                oRet = new S_JsonResult(oManager.GetReportings());
            }

            return oRet;
        }


        public ActionResult Reporting(int id)
        {
            ActionResult oRet = null;
            
            using (FCSession oSession = new FCSession(this.HttpContext))
            {
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                oRet = new S_JsonResult(oManager.GetReporting(id));
            }

            return oRet;
        }

        [HttpPost]
        public ActionResult Reporting()
        {
            ActionResult oRet = null;

            Stream oText = this.Request.InputStream;
            oText.Seek(0, SeekOrigin.Begin);
            JsonTextReader oJReader = new JsonTextReader(new StreamReader(oText));
            try
            {
                // JObject o = JObject.Load(oJReader);
                DataSet oSet = new JsonSerializer().Deserialize<DataSet>(oJReader);
                oRet = new S_JsonResult( (from row in oSet.Tables["Table"].AsEnumerable() select row["Phase"]).Distinct().ToList() );
            }
            catch (Exception e)
            {
                oRet = new HttpStatusCodeResult(HttpStatusCode.BadRequest, e.Message);
            }

            return oRet;
        }
    }
}