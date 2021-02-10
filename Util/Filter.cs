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
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using log4net;

namespace CTSWeb.Util
{
    // Supports a list of tags that can be used to filter
    // Filter delimits the intersection of 2 N-dimensions cubes: (A and B and ...) AND NOT (D and E and ...)
    //
    public class Tag<tObject>
    {
        public readonly string Name;
        public tObject Value;

        public Tag(string vsName, tObject voValue = default)
        {
            if (string.IsNullOrWhiteSpace(vsName)) throw new ArgumentNullException();
            Name = vsName.Trim().ToLowerInvariant();
            Value = voValue;
        }
    }
    
    
    public class TagList<tObject>
    {
        public readonly Dictionary<String, tObject> Table;

        public TagList(List<Tag<tObject>> voTags)
        {
            Table = new Dictionary<string, tObject>();
            foreach (Tag<tObject> oTag in voTags)
            {
                Table.Add(oTag.Name, oTag.Value);        // Throws exception if duplicate
            }
        }
    }


    // Can have different or identical headers in each part
    public class Filter<tObject>
    {
        public readonly TagList<Predicate<tObject>> IncludeAll;
        public readonly TagList<Predicate<tObject>> ExcludeAll;

        public Filter(TagList<Predicate<tObject>> voIncludeAll, TagList<Predicate<tObject>> voExcludeAll)
        {
            if (voIncludeAll == null && voExcludeAll == null) throw new ArgumentNullException();
            IncludeAll = voIncludeAll;
            ExcludeAll = voExcludeAll;
        }


        public bool Match(TagList<tObject> voCriteria)
        {
            bool bRet = true;

            if (IncludeAll != null) {
                foreach (string s in IncludeAll.Table.Keys)
                {
                    bRet &= IncludeAll.Table[s](voCriteria.Table[s]);      // throws exception if column not found
                    if (!bRet) break;
                }
            }
            if (bRet && ExcludeAll != null)
            {
                foreach (string s in ExcludeAll.Table.Keys)
                {
                    bRet &= ExcludeAll.Table[s](voCriteria.Table[s]);      // throws exception if column not found
                    if (!bRet) break;
                }
            }
            return bRet;
        }
    }
}