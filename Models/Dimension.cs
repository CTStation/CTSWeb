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
using CTCLIENTSERVERLib;
using CTCORELib;
using CTSWeb.Util;

namespace CTSWeb.Models
{
    
    public class Dimension : ManagedObject // Inherits ID and Name and LDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static Dimension()
        {
            Manager.Register<Dimension>((int)ct_core_manager.CT_DIMENSION_MANAGER);
        }

        // Argument-less constructor
        public Dimension() { }

        private ICtRefTable _oRefTable;

        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);

            ICtDimension oDim = (ICtDimension)roObject;
            _oRefTable = oDim.RefTable;
        }

        // Cache only recent queries
        private Dictionary<string, int> _oCode2ID = new Dictionary<string, int>();
        public bool ValueExists(string vsCode)
        {
            bool bRet = false;
            if (_oCode2ID.ContainsKey(vsCode)) {
                bRet = true;
            }
            else
            {
                ICtRefValue oRefVal = _oRefTable.RefValueFromName[vsCode, 0];
                if (bRet = (oRefVal != null)) _oCode2ID[vsCode] = oRefVal.ID;                
            }
            return bRet;
        }


        private Dictionary<int, string> _oID2Code = new Dictionary<int, string>();
        public bool ValueExists(int viID)
        {
            bool bRet = false;
            if (_oID2Code.ContainsKey(viID))
            {
                bRet = true;
            }
            else
            {
                ICtRefValue oRefVal = _oRefTable.RefValue[viID];
                if (bRet = (oRefVal != null)) _oID2Code[viID] = oRefVal.Name;
            }
            return bRet;
        }
    }


    public class DataSource : ManagedObject // Inherits ID and Name and LDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static DataSource()
        {
            Manager.Register<DataSource>((int)ct_core_manager.CT_DATASOURCE_MANAGER);
        }

        // Argument-less constructor
        public DataSource() { }

        public List<Dimension> Dimensions;

        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);

            ICtDataSource oSource = (ICtDataSource)roObject;
            Dimensions = new List<Dimension>();
            foreach (ICtObjectBase o in oSource.Dimensions) {
                Dimension oDim = new Dimension();
                oDim.ReadFrom((ICtObject)o, roLang);
                this.Dimensions.Add(oDim);
            }
        }
    }
}


