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
using CTTRANSFERLib;
using CTSWeb.Util;

namespace CTSWeb.Models
{
    public class UserLight : ManagedObject // Inherits ID and Name and LDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static UserLight()
        {
            Manager.Register<UserLight>((int)CT_CLIENTSERVER_MANAGERS.CT_USER_MANAGER);
        }

        // Argument-less constructor
        public UserLight() { }
    }



    public class Recipient : ManagedObject // Inherits ID and Name and LDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static Recipient()
        {
            Manager.Register<Recipient>(-524253);
        }

        // Argument-less constructor
        public Recipient() { }

        private ICtRecipient _oFCRefValue;
        public ICtRecipient FCValue() => _oFCRefValue;

        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            _oFCRefValue = (ICtRecipient)roObject;
        }


        public static List<string> GetIDDimensions(Context roContext)
        {
            List<string> oRet = new List<string>();
            string s = "Recipient";
            if (roContext.Language.Culture.Name == "fr-FR") s = "Site";
            oRet.Add(s);
            return oRet;
        }


        public static MultiPartID<Recipient> GetList(Context roContext) =>
            new MultiPartID<Recipient>(roContext, new Recipient().GetIdentifierParts, GetIDDimensions);

    }
}


