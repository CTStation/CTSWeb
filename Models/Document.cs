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
using CTDOCMODULELib;
using CTCLIENTSERVERLib;
using CTCORELib;
using CTSWeb.Util;

namespace CTSWeb.Models
{
    public class Folder : ManagedObjectWithDesc
    {
        static Folder()
        {
            Manager.Register<Folder>((int)DOCMANAGERID.CT_FOLDER_MANAGER);
        }

        // Argument-less constructor
        public Folder() { }

        private ICtBookFolder _oFC;

        public ICtBookFolder FCValue() => _oFC;


        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            if (!(roObject is null))
            {
                ICtBookFolder oFC = ((ICtBookFolder)roObject);
                _oFC = oFC;
            }
        }


        public static List<string> GetIDDimensions(Context roContext)
        {
            List<string> oRet = new List<string>();
            string s = "Folder";
            if (roContext.Language.Culture.Name == "fr-FR") s = "Dossier";
            oRet.Add(s);
            return oRet;
        }


        public static MultiPartID<Folder> GetList(Context roContext) =>
            new MultiPartID<Folder>(roContext, new Folder().GetIdentifierParts, GetIDDimensions);
    }
}


