#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion

// Facades for FC objects
//          Restricted to the needs of ReportingFactory

using System;
using System.Collections.Generic;
using log4net;
using CTREPORTINGMODULELib;
using CTKREFLib;
using CTCLIENTSERVERLib;
using CTCORELib;
using CTSWeb.Util;

namespace CTSWeb.Models
{



    public class ExchangeRate
    {
        public int ID;
        public string Name;
    }


    public class ExchangeRateVersion
    {
        public int ID;
        public string Name;

    }

    public class ExchangeRateType
    {
        public int ID;
        public string Name;

    }


    public class BaseOperation
    {
        public DateTime PackPublishingCutOffDate;
        public bool AllowEarlyPublishing;
        public int IntegrateAfterPublication;
        public int IntegrateAfterTransfer;

    }


    public class ControlLevelReachedAfterPublication
    {
        public int ID;
        public string Name;
        public int Rank;
    }


    public class ReportingModifyComment
    {

    }

    public class ReportingHierarchyDate
    {

    }
}


