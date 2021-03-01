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
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using log4net;
using CTSWeb.Models;

namespace CTSWeb.Util
{
    // Translate objects to and from a DataSet
    //

    public enum SerFieldType
    {
        Field,
        Property
    }

    public enum SerDirective
    {
        Show,
        Ignore,
        Flatten,
        FlatArray,
        FlatList
    }

    public class SerConfig
    {
        public SerFieldType FieldType;
        public string Name;
        public string TypeName;
        public SerDirective Action;
    }


    public static class Serialiser
    {
        // Creates a list of strings describing the public fields and properties in an object
        public static List<string> Flatten(Type voType, string vsPrefix = "")
        {
            List<string> oRet = new List<string>();

            foreach (FieldInfo o in voType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                switch (o.FieldType.Name)
                {
                    case "Framework":
                    case "List`1":
                    case "String":
                    case "DateTime":
                    case "Int32":
                    case "LanguageText[]":
                    case "Boolean":
                    case "Nullable`1":
                        oRet.Add($"Field\t {vsPrefix}{o.Name}\t {o.FieldType.FullName}");
                        break;
                    default:
                        oRet.AddRange( Flatten(o.FieldType, vsPrefix + o.Name + "."));
                        break;
                }

            }
            foreach (PropertyInfo o in voType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                switch (o.PropertyType.Name)
                {
                    case "Framework":
                    case "List`1":
                    case "String":
                    case "DateTime":
                    case "Int32":
                    case "LanguageText[]":
                    case "Boolean":
                    case "Nullable`1":
                        oRet.Add($"Property\t {vsPrefix}{o.Name}\t {o.PropertyType.FullName}");
                        break;
                    default:
                        oRet.AddRange(Flatten(o.PropertyType, vsPrefix + o.Name + "."));
                        break;
                }
            }
            return oRet;
        }
        

        public static DataSet CreateTable()
        {
            List<(string, string, string)> oParam = new List<(string, string, string)>()
            {
("b", "System.Boolean", "1"),
("c", "System.String", "Wfold"),
("d", "System.Boolean", "1"),
("e", "System.String", "Ifold"),
("f", "System.Boolean", "1"),
("g", "System.String", "SET"),
("h", "System.Boolean", "1"),
("i", "System.Int32", "1"),
("j", "System.Boolean", "0"),
("k", "System.Boolean", "1"),
("l", "System.Int32", "2"),
("m", "System.Boolean", "1"),
("n", "System.String", ""),
("o", "System.Boolean", "1"),
("p", "System.Boolean", "0"),
("q", "System.String", ""),
("r", "System.String", ""),
("s", "System.String", ""),
("t", "System.String", ""),
("u", "System.String", ""),
("v", "System.Boolean", "1"),
("w", "System.DateTime", "2021-02-24H10:02"),
("x", "System.Boolean", "1"),
("y", "System.Boolean", "1"),
("z", "System.Boolean", "1"),
("aa", "System.Boolean", "1"),
("ab", "System.Boolean", "1"),
("ac", "System.Int32", "2"),
("ad", "System.Boolean", "0"),
("ae", "System.Boolean", "0"),
("af", "System.Boolean", "1"),
("ag", "System.Boolean", "0"),
("ah", "System.Int32", "2"),

("aj", "System.String", "A"),
("ak", "System.String", "2001.12"),
("al", "System.String", "C"),
("am", "System.DateTime", "2021-02-24H10:02"),
("an", "System.DateTime", "2021-02-24H10:02"),












("ca", "System.String", "S001"),
("cb", "System.String", "EUR"),
("cc", "System.String", "0000.TOP"),
("cd", "System.String", "0000.TOP"),
("ce", "System.Boolean", "1"),
("cf", "System.String", "Wfold"),
("cg", "System.Boolean", "1"),
("ch", "System.String", "Ifold"),
("ci", "System.Boolean", "1"),
("cj", "System.String", ""),
("ck", "System.Boolean", "1"),
("cl", "System.Int32", "0"),
("cm", "System.Boolean", "1"),
("cn", "System.Boolean", "1"),
("co", "System.Int32", "0"),
("cp", "System.Boolean", "1"),
("cq", "System.String", ""),
("cr", "System.Boolean", "1"),
("cs", "System.Boolean", ""),
("ct", "System.String", ""),
("cu", "System.String", ""),
("cv", "System.String", ""),
("cw", "System.String", ""),
("cx", "System.String", ""),
("cy", "System.Boolean", "1"),
("cz", "System.DateTime", ""),
("da", "System.Boolean", ""),
("db", "System.Boolean", "1"),
("dc", "System.Boolean", ""),
("dd", "System.Boolean", ""),
("de", "System.Boolean", ""),
("df", "System.Int32", ""),
("dg", "System.Boolean", "1"),
("dh", "System.Boolean", ""),
("di", "System.Boolean", ""),
("dj", "System.Boolean", ""),
("dk", "System.Int32", ""),
            };

            DataSet oRet = new DataSet();
            DataTable oTable = oRet.Tables.Add();
            foreach (var o in oParam)
            {
                oTable.Columns.Add(o.Item1, Type.GetType(o.Item2));
            }
            DataRow oRow = oTable.NewRow();
            foreach (var o in oParam)
            {
                oRow[o.Item1] = (o.Item2 == "System.String") ? o.Item3 : SerConvert(Type.GetType(o.Item2), o.Item3);
            }
            oTable.Rows.Add(oRow);
            return oRet;
        }

        public static object SerConvert(Type T, string vsVal)
        {
            object oRet;
            int i;
            DateTime oDate;
            switch (T.Name)
            {
                case "Boolean":
                case "Int32":
                    oRet = (Int32.TryParse(vsVal, out i)) ? i : 0;
                    break;
                case "DateTime":
                    oRet = (DateTime.TryParse(vsVal, out oDate)) ? oDate : DateTime.Now;
                    break;
                default:
                    throw new Exception();
                    //break;
            }
            return oRet;
        }


        // Checks and messages should be done before
        // May build invalid objects
        // Framework is null at the end, should be recified afterward
        public static List<Reporting> ReadReportings(DataTable voTable, HashSet<int> voInvalidRows, Context voContext)
        {
            List<Reporting> oRet = new List<Reporting>();

            Dictionary<(string, string, string), Reporting> oCurRep = new Dictionary<(string, string, string), Reporting>();
            Dictionary<(string, string, string), EntityReporting> oCurEntity = new Dictionary<(string, string, string), EntityReporting>();

            object PrNoException(DataRow voRow, string vsColName) => (voTable.Columns.Contains(vsColName) && (!(voRow[vsColName] is DBNull))) ? voRow[vsColName] : null;

            int c = 0;
            (string, string, string) sKey;
            Reporting oRep;
            EntityReporting oEntityRep;
            foreach (DataRow oRow in voTable.Rows)
            {
                if (!voInvalidRows.Contains(c))
                {
                    sKey = ((string)oRow["aj"], (string)oRow["ak"], "");
                    if (!oCurRep.TryGetValue(sKey, out oRep))
                    {
                        oRep = new Reporting();
                        oRep.ReadFrom(null, voContext);
                        oRet.Add(oRep);
                        oCurRep.Add(sKey, oRep);
                    }
                    {
                        oRep.DefaultPackage.UseDefaultWindowsFolder = (bool)PrNoException(oRow, "b");
                        oRep.DefaultPackage.WindowsFolder = (string)PrNoException(oRow, "c");
                        oRep.DefaultPackage.UseDefaultInternetFolder = (bool)PrNoException(oRow, "d");
                        oRep.DefaultPackage.InternetFolder = (string)PrNoException(oRow, "e");
                        oRep.DefaultPackage.UseDefaultSetOfControls = (bool)PrNoException(oRow, "f");
                        oRep.DefaultPackage.SetOfControls = (string)PrNoException(oRow, "g");
                        oRep.DefaultPackage.UseDefaultLevel = (bool)PrNoException(oRow, "h");
                        oRep.DefaultPackage.LevelToReach = (short?)(long?)PrNoException(oRow, "i");
                        oRep.DefaultPackage.Blocking = (bool)PrNoException(oRow, "j");
                        oRep.DefaultPackage.UseDefaultLock = (bool)PrNoException(oRow, "k");
                        oRep.DefaultPackage.LockOnPublication = (int?)(long?)PrNoException(oRow, "l");
                        oRep.DefaultPackage.UseDefaultRuleSet = (bool)PrNoException(oRow, "m");
                        oRep.DefaultPackage.RuleSet = (string)PrNoException(oRow, "n");
                        oRep.DefaultPackage.UseDefaultOpbal = (bool)PrNoException(oRow, "o");
                        oRep.DefaultPackage.HasOpBal = (bool)PrNoException(oRow, "p");
                        oRep.DefaultPackage.OpbPhase = (string)PrNoException(oRow, "q");
                        oRep.DefaultPackage.OpbUpdatePeriod = (string)PrNoException(oRow, "r");
                        oRep.DefaultPackage.OpbScope = (string)PrNoException(oRow, "s");
                        oRep.DefaultPackage.OpbVariant = (string)PrNoException(oRow, "t");
                        oRep.DefaultPackage.OpbConsCurrency = (string)PrNoException(oRow, "u");
                        oRep.DefaultOperation.PackPublishingCutOffDate = (DateTime)PrNoException(oRow, "w");
                        oRep.DefaultOperation.AllowEarlyPublishing = (bool)PrNoException(oRow, "x");
                        oRep.DefaultOperation.AfterPublication.Standard = (bool)PrNoException(oRow, "z");
                        oRep.DefaultOperation.AfterPublication.Special = (bool)PrNoException(oRow, "aa");
                        oRep.DefaultOperation.AfterPublication.Advanced = (bool)PrNoException(oRow, "ab");
                        oRep.DefaultOperation.AfterPublication.Level = (short?)(long?)PrNoException(oRow, "ac");
                        oRep.DefaultOperation.AfterTransfer.Standard = (bool)PrNoException(oRow, "ae");
                        oRep.DefaultOperation.AfterTransfer.Special = (bool)PrNoException(oRow, "af");
                        oRep.DefaultOperation.AfterTransfer.Advanced = (bool)PrNoException(oRow, "ag");
                        oRep.DefaultOperation.AfterTransfer.Level = (short?)(long?)PrNoException(oRow, "ah");
                        oRep.Phase = (string)PrNoException(oRow, "aj");
                        oRep.UpdatePeriod = (string)PrNoException(oRow, "ak");
                        oRep.FrameworkVersion = (string)PrNoException(oRow, "al");
                        oRep.ReportingStartDate = (DateTime)PrNoException(oRow, "am");
                        oRep.ReportingEndDate = (DateTime)PrNoException(oRow, "an");

                        string s;
                        Descs oText;
                        List<Descs> oDesc = new List<Descs>();
                        foreach (var oLang in voContext.Language.SupportedLanguages)
                        {
                            oText = null;
                            foreach (var o in Descs.FieldList)
                            {
                                s = (string)PrNoException(oRow, "au" + o.Item2 + oLang.Item2.Replace('-', '_'));
                                if (!(s is null))
                                {
                                    if (oText is null)
                                    {
                                        oText = new Descs(oLang.Item2, voContext.Language);
                                        oDesc.Add(oText);
                                    }
                                    oText.Texts.Add(o.Item2, s);
                                }
                            }
                        }
                        oRep.Descriptions = oDesc.ToArray();
                    }

                    sKey = ((string)oRow["aj"], (string)oRow["ak"], (string)oRow["ca"]);
                    if (!oCurEntity.TryGetValue(sKey, out oEntityRep))
                    {
                        oEntityRep = new EntityReporting();
                        oEntityRep.ReadFrom(null, voContext);
                        oRep.EntityReportings.Add(oEntityRep);
                        oCurEntity.Add(sKey, oEntityRep);
                    }
                    {
                        oEntityRep.Entity = (string)PrNoException(oRow, "ca");
                        oEntityRep.InputCurrency = (string)PrNoException(oRow, "cb");
                        oEntityRep.InputSite = (string)PrNoException(oRow, "cc");
                        oEntityRep.PublicationSite = (string)PrNoException(oRow, "cd");
                        oEntityRep.PackPackage.UseDefaultWindowsFolder = (bool)PrNoException(oRow, "ce");
                        oEntityRep.PackPackage.WindowsFolder = (string)PrNoException(oRow, "cf");
                        oEntityRep.PackPackage.UseDefaultInternetFolder = (bool)PrNoException(oRow, "cg");
                        oEntityRep.PackPackage.InternetFolder = (string)PrNoException(oRow, "ch");
                        oEntityRep.PackPackage.UseDefaultSetOfControls = (bool)PrNoException(oRow, "ci");
                        oEntityRep.PackPackage.SetOfControls = (string)PrNoException(oRow, "cj");
                        oEntityRep.PackPackage.UseDefaultLevel = (bool)PrNoException(oRow, "ck");
                        oEntityRep.PackPackage.LevelToReach = (short?)(long?)PrNoException(oRow, "cl");
                        oEntityRep.PackPackage.Blocking = (bool)PrNoException(oRow, "cm");
                        oEntityRep.PackPackage.UseDefaultLock = (bool)PrNoException(oRow, "cn");
                        oEntityRep.PackPackage.LockOnPublication = (int?)(long?)PrNoException(oRow, "co");
                        oEntityRep.PackPackage.UseDefaultRuleSet = (bool)PrNoException(oRow, "cp");
                        oEntityRep.PackPackage.RuleSet = (string)PrNoException(oRow, "cq");
                        oEntityRep.PackPackage.UseDefaultOpbal = (bool)PrNoException(oRow, "cr");
                        oEntityRep.PackPackage.HasOpBal = (bool)PrNoException(oRow, "cs");
                        oEntityRep.PackPackage.OpbPhase = (string)PrNoException(oRow, "ct");
                        oEntityRep.PackPackage.OpbUpdatePeriod = (string)PrNoException(oRow, "cu");
                        oEntityRep.PackPackage.OpbScope = (string)PrNoException(oRow, "cv");
                        oEntityRep.PackPackage.OpbVariant = (string)PrNoException(oRow, "cw");
                        oEntityRep.PackPackage.OpbConsCurrency = (string)PrNoException(oRow, "cx");
                        oEntityRep.PackOperation.UseDefaultPublish = (bool)PrNoException(oRow, "cy");
                        oEntityRep.PackOperation.PackPublishingCutOffDate = (DateTime)PrNoException(oRow, "cz");
                        oEntityRep.PackOperation.AllowEarlyPublishing = (bool)PrNoException(oRow, "da");
                        oEntityRep.PackOperation.UseDefaultAfterPub = (bool)PrNoException(oRow, "db");
                        oEntityRep.PackOperation.AfterPublication.Standard = (bool)PrNoException(oRow, "dc");
                        oEntityRep.PackOperation.AfterPublication.Special = (bool)PrNoException(oRow, "dd");
                        oEntityRep.PackOperation.AfterPublication.Advanced = (bool)PrNoException(oRow, "de");
                        oEntityRep.PackOperation.AfterPublication.Level = (short?)(long?)PrNoException(oRow, "df");
                        oEntityRep.PackOperation.UseDefaultAfterTran = (bool)PrNoException(oRow, "dg");
                        oEntityRep.PackOperation.AfterTransfer.Standard = (bool)PrNoException(oRow, "dh");
                        oEntityRep.PackOperation.AfterTransfer.Special = (bool)PrNoException(oRow, "di");
                        oEntityRep.PackOperation.AfterTransfer.Advanced = (bool)PrNoException(oRow, "dj");
                        oEntityRep.PackOperation.AfterTransfer.Level = (short?)(long?)PrNoException(oRow, "dk");
                    }
                }
                c++;
            }
            return oRet;
        }
    }
}