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
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CTSWeb.Models
{
    public static class Help
    {

        //----------------------------------
        //      Public interface
        //----------------------------------

        public static object[] Commands()
        {
            return _aoCommands;
        }


        //----------------------------------
        //      Implementation
        //----------------------------------

        private static readonly object[] _aoCommands;
        private static readonly bool _bIsInit;

        static Help()
        {
            if (!_bIsInit)
            {
                _bIsInit = true;
                SortedDictionary<string, SortedDictionary<string, string>> oList = new SortedDictionary<string, SortedDictionary<string, string>>();

                PrAddCommand(oList, "Reportings", "GET", "Returns the list of reportings");
                PrAddCommand(oList, "Reporting", "POST", "Creates or updates 1 or many reportings");
                PrAddCommand(oList, "Reporting/ID", "GET", "Returns the content of 1 reporting");

                _aoCommands = PrSort(oList);
            }
        }


        private static void PrAddCommand(SortedDictionary<string, SortedDictionary<string, string>> roList, string rsCommand, string rsAction, string rsDesc)
        {
            SortedDictionary<string, string> oDict;

            if (roList.ContainsKey(rsCommand))
            {
                oDict = roList[rsCommand];
                oDict.Add(rsAction, rsDesc);        // Exception if duplicate
            }
            else
            {
                oDict = new SortedDictionary<string, string>
                {
                    { rsAction, rsDesc }
                };
                roList.Add(rsCommand, oDict);
            }
        }

        private static object[] PrSort(SortedDictionary<string, SortedDictionary<string, string>> roList)
        {
            List<object> aoList = new List<object>();

            foreach (string sCommand in roList.Keys)
            {
                foreach (string sAction in roList[sCommand].Keys)
                {
                    aoList.Add(new { Command = sCommand, Action = sAction, Description = roList[sCommand][sAction] });
                }
            }
            return aoList.ToArray();
        }
    }
}