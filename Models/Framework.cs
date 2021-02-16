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
using CTKREFLib;
using CTSWeb.Util;

namespace CTSWeb.Models
{

    public class Framework : ManagedObject // Inherits ID and Name and LDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static Framework()
        {
            Manager.Register<Framework>(-524238);
            Manager.RegisterDelegate<Framework>((Context roContext, ICtObjectManager voMgr, string vsID1, string vsID2) =>
                                                        (ICtObject)((ICtFrameworkManager)voMgr).FrameworkFromPhaseVersion[
                                                            roContext.GetRefValue("Phase", vsID1).FCRefValue(),
                                                            roContext.GetRefValue("FrameworkVersion", vsID2).FCRefValue()]
                                                    );
        }

        // Argument-less constructor
        public Framework() { }

        public string Phase;
        public string Version;
        public kref_framework_status Status;
        public List<ManagedObjectWithDesc> ControlLevels = new List<ManagedObjectWithDesc>();

        private IRefObjRef _oFCFramework;

        public IRefObjRef FCValue() => _oFCFramework;


        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);

            IRefObjRef oRef = (IRefObjRef)roObject;
            _oFCFramework = oRef;
            if (!(roObject is null))
            {
                Phase = oRef.Phase?.Name;
                Version = oRef.Version?.Name;
                Status = oRef.RefStatus;
                foreach (ICtObject oLevel in oRef.ContLevelList)
                {
                    ManagedObjectWithDesc o = new ManagedObjectWithDesc();
                    o.ReadFrom(oLevel, roLang);
                    ControlLevels.Add(o);
                }
            }
        }
    }
}

