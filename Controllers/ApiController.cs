using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Newtonsoft.Json;
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
            FCSession oSession = new FCSession(this.HttpContext);
            ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);

            return new S_JsonResult(oManager.GetReportings());
        }
    }
}