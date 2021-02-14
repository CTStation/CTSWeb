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
        }

        // Argument-less constructor
        public Framework() { }

        public string Phase;
        public string Version;
        public kref_framework_status Status;
        public List<ManagedObjectWithDesc> ControlLevels = new List<ManagedObjectWithDesc>();

        private IRefObjRef _oFCFramework;

        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);

            IRefObjRef oRef = (IRefObjRef)roObject;
            Phase = oRef.Phase?.Name;
            Version = oRef.Version?.Name;
            Status = oRef.RefStatus;
            foreach (ICtObject oLevel in oRef.ContLevelList) 
            { 
                ManagedObjectWithDesc o = new ManagedObjectWithDesc(); 
                o.ReadFrom(oLevel, roLang); 
                ControlLevels.Add(o); 
            }
            _oFCFramework = oRef;
        }


        // Manage complex identifier: users choose framework from Phase and version, not from name
        // Avoid burdening Manager with a complex identifier concept, deal with the lists here
        // List of frameworks is usualy short
        // TODO Move this to Manage with a delegate that builds the identifier as string[], then buld the delegate from attributes
        public static List<(string, string)> BuildPickupList(List<Framework> voList, kref_framework_status viStatus = 0)
        {
            List<(string, string)> oRet = new List<(string, string)>();
            foreach (Framework oFramework in voList) if (viStatus == 0 || oFramework.Status == viStatus) oRet.Add((oFramework.Phase, oFramework.Version));
            return oRet;
        }


        public static Framework GetFromPhaseVersion(List<Framework> voList, string vsPhase, string vsVersion)
        {
            Framework oRet = null;
            foreach (Framework oFramework in voList) if (oFramework.Phase == vsPhase && oFramework.Version == vsVersion) { oRet = oFramework; break; }
            return oRet;
        }


        public static Framework GetFromPhaseVersion(List<Framework> voList, string vsPhase, string vsVersion, out IRefObjRef roFCFramework)
        {
            Framework oRet = null;
            foreach (Framework oFramework in voList) if (oFramework.Phase == vsPhase && oFramework.Version == vsVersion) { oRet = oFramework; break; }
            roFCFramework = oRet?._oFCFramework;
            return oRet;
        }
    }


}

