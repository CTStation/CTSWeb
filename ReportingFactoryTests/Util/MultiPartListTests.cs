using Microsoft.VisualStudio.TestTools.UnitTesting;
using CTSWeb.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTSWeb.Models;
using CTCLIENTSERVERLib;
using CTKREFLib;
using Newtonsoft.Json;

namespace CTSWeb.Util.Tests
{
    [TestClass()]
    public class MultiPartListTests
    {
        private static NameValueCollection PrContext(List<(string, string)> voHeaders)
        {
            var oRet = new NameValueCollection(voHeaders.Count);
            foreach (var o in voHeaders)
            {
                oRet.Add(o.Item1, o.Item2);
            }
            return oRet;
        }

        private static List<(string, string)> _oParams = new List<(string, string)>()
            {
                ("P001.ctstation.fr", "172.31.38.85"),
                ("P002.ctstation.fr", "SAPFCSQLSERVER"),
                ("P003.ctstation.fr", ""),
                ("P004.ctstation.fr", "ADMIN"),
                ("P005.ctstation.fr", ""),
                ("P007.ctstation.fr", "fr-FR"),
                ("P008.ctstation.fr", "en-US, fr-FR, de-DE"),
            };

        private static NameValueCollection _oHeaders = PrContext(_oParams);




        [TestMethod()]
        public void MultipartListTest()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                MessageList oMessages = oContext.NewMessageList();
                object o = new MultiPartID<Framework>(oContext, new Framework().GetIdentifierParts, Framework.GetIDDimensions,
                                                                    (ICtObject oFramework) => ((IRefObjRef)oFramework).RefStatus == kref_framework_status.FRMK_STATUS_PUBLISHED);
                string s = JsonConvert.SerializeObject(o);
                Assert.IsTrue(s != "");
                Debug.WriteLine(s);
            }
        }



        // TODO Supress, nothing todo here
        [TestMethod()]
        public void ControlTest()
        {
            using (Context oContext = new Context(_oHeaders))
            {
                MessageList oMessages = oContext.NewMessageList();
                object o = new MultiPartID<Framework>(oContext, new Framework().GetIdentifierParts, Framework.GetIDDimensions,
                                                                    (ICtObject oFramework) => ((IRefObjRef)oFramework).RefStatus == kref_framework_status.FRMK_STATUS_PUBLISHED);
                string s = JsonConvert.SerializeObject(o);
                Assert.IsTrue(s != "");
                Debug.WriteLine(s);
            }
        }
    }
}