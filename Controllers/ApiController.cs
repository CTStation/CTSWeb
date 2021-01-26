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
            ActionResult oRet = null;

            FCSession oSession = new FCSession(this.HttpContext);
            try
            {
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                oRet = new S_JsonResult(oManager.GetReportings());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                oSession.Close();
            }
            return oRet;
        }

  
        public ActionResult Reporting(int id)
        {
            ActionResult oRet = null;

            FCSession oSession = new FCSession(this.HttpContext);
            try
            {
                ReportingManagerClient oManager = new ReportingManagerClient(oSession.Config);
                oRet = new S_JsonResult(oManager.GetReporting(id));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                oSession.Close();
            }

            return oRet;
        }
    }
}