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

        private static object[] _aoCommands;
        private static bool _bIsInit;

        static Help()
        {
            if (!_bIsInit)
            {
                _bIsInit = true;
                SortedDictionary<string, SortedDictionary<string, string>> oList = new SortedDictionary<string, SortedDictionary<string, string>>();

                S_AddCommand(oList, "Reportings", "GET", "Returns the list of reportings");
                S_AddCommand(oList, "Reporting", "POST", "Creates or updates 1 or many reportings");
                S_AddCommand(oList, "Reporting/ID", "GET", "Returns the content of 1 reporting");

                _aoCommands = S_Sort(oList);
            }
        }


        private static void S_AddCommand(SortedDictionary<string, SortedDictionary<string, string>> roList, string rsCommand, string rsAction, string rsDesc)
        {
            SortedDictionary<string, string> oDict;

            if (roList.ContainsKey(rsCommand))
            {
                oDict = roList[rsCommand];
                oDict.Add(rsAction, rsDesc);        // Exception if duplicate
            }
            else
            {
                oDict = new SortedDictionary<string, string>();
                oDict.Add(rsAction, rsDesc);
                roList.Add(rsCommand, oDict);
            }
        }

        private static object[] S_Sort(SortedDictionary<string, SortedDictionary<string, string>> roList)
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