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


    // JsonResult with NewtonSoft JSON, giving more control than MS
    public class CTS_JsonResult : ContentResult
    {
        public CTS_JsonResult(Object voObj, MessageList voMessages = null)
        {
            this.ContentEncoding = System.Text.Encoding.UTF8;
            this.ContentType = "application/json";
            JsonSerializerSettings oSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,          // Force time zone info
                PreserveReferencesHandling = PreserveReferencesHandling.None,   // Same object is serialized multiple times
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            this.Content = JsonConvert.SerializeObject(new { Data = voObj, Messages = voMessages }, oSettings);
        }
    }


    public class ApiController : Controller
    {
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


        public ActionResult Help() => PrSafeResult(() => new CTS_JsonResult(Models.Help.Commands()));


        public ActionResult Reportings() => PrSafeResult(() =>
        {
            using (Context oSession = new Context(this.HttpContext))
            {
                return new CTS_JsonResult(oSession.GetAll<ReportingLight>());
            }
        }
        );


        public ActionResult Reporting(int id) => PrSafeResult(() =>
        {
            using (Context oContext = new Context(this.HttpContext))
            {
                MessageList oMessages = oContext.NewMessageList();
                return new CTS_JsonResult(oContext.Get<Reporting>(id, oMessages), oMessages);
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
            using (Context oContext = new Context(this.HttpContext))
            {
                MessageList oMessages = oContext.NewMessageList();

                List<Reporting> oReportings = Models.Reporting.LoadFromDataSet(oSet, oContext, oMessages);
                //foreach (Reporting oObj in oReportings)
                //{
                //    oContext.Save<Reporting>(oObj, oMessages);
                //}
                //return new CTS_JsonResult(null, oMessages);
                return new CTS_JsonResult(oReportings, oMessages);
            }
        }
        );

        public ActionResult Languages() => PrSafeResult(() =>
        {
            using (Context oContext = new Context(this.HttpContext))
            {
                return new CTS_JsonResult(oContext.GetActiveLanguages());
            }
        }
        );

        public ActionResult Users() => PrSafeResult(() =>
        {
            using (Context oContext = new Context(this.HttpContext))
            {
                return new CTS_JsonResult(oContext.GetAll<UserLight>());
            }
        }
        );


        public ActionResult RefTables() => PrSafeResult(() =>
        {
            using (Context oContext = new Context(this.HttpContext))
            {
                return new CTS_JsonResult(oContext.GetAll<RefTable>());
            }
        }
        );
    }
}