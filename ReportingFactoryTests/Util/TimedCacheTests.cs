#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CTSWeb.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CTSWeb.Util.Tests
{
    [TestClass()]
    public class TimedCacheTests
    {
        static List<string> _oOut;

        [TestMethod()]
        public void TimedCacheTest()
        {
            _oOut = new List<string>();

            _oOut.Add("Hello World!");

            TimedCache<String, String> oCache = new TimedCache<String, String>((String vs) =>
            {
                if (vs is null)
                {
                    throw new ArgumentNullException(nameof(vs));
                }

                _oOut.Add($"Destroyed {vs}");
            }, 1000);

            oCache.Push("A", "1");
            System.Threading.Thread.Sleep(20);
            oCache.Push("A", "2");
            System.Threading.Thread.Sleep(20);
            oCache.Push("B", "11");
            System.Threading.Thread.Sleep(20);
            oCache.Push("B", "22");
            System.Threading.Thread.Sleep(800);
            
            string s;
            if (oCache.TryPop("A", out s)) _oOut.Add("A" + s); else _oOut.Add("No more A");
            if (oCache.TryPop("A", out s)) _oOut.Add("A" + s); else _oOut.Add("No more A");
            if (oCache.TryPop("A", out s)) _oOut.Add("A" + s); else _oOut.Add("No more A");
            oCache.Push("A", "3");
            System.Threading.Thread.Sleep(180);
            if (oCache.TryPop("A", out s)) _oOut.Add("A" + s); else _oOut.Add("No more A");
            oCache.Push("A", "4");
            System.Threading.Thread.Sleep(20);

            if (oCache.TryPop("B", out s)) _oOut.Add("B" + s); else _oOut.Add("No more B");
            System.Threading.Thread.Sleep(20);
            if (oCache.TryPop("B", out s)) _oOut.Add("B" + s); else _oOut.Add("No more B");
            System.Threading.Thread.Sleep(20);
            if (oCache.TryPop("B", out s)) _oOut.Add("B" + s); else _oOut.Add("No more B");
            System.Threading.Thread.Sleep(1000);

            List<string> oWanted = new List<string>() { "Hello World!", "A1", "A2", "No more A", "A3", "Destroyed 11", "B22", "No more B", "No more B", "Destroyed 4" };
            List<string> oErrors = new List<string>();
            int c = 0;
            foreach(string sLine in _oOut)
            {
                if (sLine != oWanted[c]) oErrors.Add($"Expected '{oWanted[c]}' as line {c + 1}, but got '{sLine}'");
                c++;
            }
            if (0 < oErrors.Count)
            {
                Debug.WriteLine(oErrors.Aggregate<string>((string sMain, string sCur) => sMain + "\n" + sCur));
            }
            else
            {
                Debug.WriteLine("All GOOD!!");
            }
            Assert.IsTrue(oErrors.Count == 0);
        }
    }
}