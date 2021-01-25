using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CTSWeb.Controllers
{
    public class ApiController : Controller
    {
        public ActionResult Help()
        {
            return Json( Models.Help.Commands(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Reportings()
        {
            JsonResult oRet;

            oRet = new JsonResult();

            return Json(Models.Help.Commands(), JsonRequestBehavior.AllowGet);
        }

    }
}