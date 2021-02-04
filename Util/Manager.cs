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
using CTCLIENTSERVERLib;
using CTSWeb.Models;

namespace CTSWeb.Util
{
    public class ManagedObject
    {
        public virtual ManagedObject Factory(ICtObjectBase roObject) { return default; } 
    }

    public static class Manager
    {
        public static List<tObject> GetAll<tObject>(ConfigClass roConfig) where tObject : ManagedObject, new()
        {
            List<tObject> oRet = new List<tObject>();
            tObject oDumy = new tObject();

            ICtObjectManager oManager = PrGetMgr<tObject>(roConfig);
            CTCLIENTSERVERLib.ICtGenCollection oCollection = oManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);
            // New tObject(oObj) generates a compiler error (new can be called only with 0 argument
            foreach (ICtObjectBase oObj in oCollection)
                oRet.Add((tObject)oDumy.Factory(oObj));
            return oRet;
        }

        public static tObject GetByID<tObject>(ConfigClass roConfig, int viID) where tObject : ManagedObject, new()
        {
            tObject oDumy = new tObject();

            ICtObjectManager oManager = PrGetMgr<tObject>(roConfig);
            return (tObject)oDumy.Factory(oManager.GetObject(viID, ACCESSFLAGS.OM_READ, 0));
        }

        public static tObject New<tObject>(ConfigClass roConfig) where tObject : ManagedObject, new()
        {
            tObject oDumy = new tObject();

            ICtObjectManager oManager = PrGetMgr<tObject>(roConfig);
            ICtObjectBase oObj = (ICtObjectBase)oManager.NewObject();
            tObject oRet = (tObject)oDumy.Factory(oObj);
            // TODO free com object
            return oRet;
        }


        private static ICtObjectManager PrGetMgr<tObject>(ConfigClass roConfig)
        {
            int iMgrID;

            ICtProviderContainer oContainer = (ICtProviderContainer)roConfig.Session;
            if (!_oType2MgrID.TryGetValue(typeof(tObject), out iMgrID))
            {
                throw new ArgumentException($"Unknown FC type '{typeof(tObject).Name}'");
            }
            ICtObjectManager oManager = (ICtObjectManager)oContainer.get_Provider(1, iMgrID);
            return oManager;
        }

        private static Dictionary<Type, int> _oType2MgrID = new Dictionary<Type, int>
        {
            {typeof(ReportingModel), (int)CTREPORTINGMODULELib.CtReportingManagers.CT_REPORTING_MANAGER}
        };
    }
}
