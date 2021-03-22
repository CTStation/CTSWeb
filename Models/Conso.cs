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
using CTCONSOLib;
using CTCLIENTSERVERLib;
using CTSWeb.Util;

namespace CTSWeb.Models
{
    public class Conso : ManagedObjectWithDesc
    {
        static Conso()
        {
            Manager.Register<Conso>((int)ct_conso_managers.CT_CONSODEF_MANAGER);
        }

        private protected override bool _bSaveName() => false;
        private protected override Descs.Field _iSupportedTranslatableFields() => Descs.Field.LDesc;


        // Argument-less constructor
        public Conso() { }

        private ICtConsoDef _oFC;

        public ICtConsoDef FCValue() => _oFC;

        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            if (!(roObject is null))
            {
                ICtConsoDef oFC = ((ICtConsoDef)roObject);
                _oFC = oFC;
            }
        }


        public static List<string> GetIDDimensions(Context roContext)
        {
            List<string> oRet = new List<string>();
            oRet.Add(roContext.Get<Dimension>((int)CTCOMMONMODULELib.ct_dimension.DIM_PHASE).LDesc.Trim());
            oRet.Add(roContext.Get<Dimension>((int)CTCOMMONMODULELib.ct_dimension.DIM_UPDPER).LDesc.Trim());
            oRet.Add(roContext.Get<Dimension>((int)CTCOMMONMODULELib.ct_dimension.DIM_SCOPE_CODE_FOR_AMOUNTS).LDesc.Trim());
            oRet.Add(roContext.Get<Dimension>((int)CTCOMMONMODULELib.ct_dimension.DIM_VARIANT).LDesc.Trim());
            oRet.Add(roContext.Get<Dimension>((int)CTCOMMONMODULELib.ct_dimension.DIM_CONSOCURNCY).LDesc.Trim());
            return oRet;
        }

        public override List<ManagedObject> GetIdentifierParts(ICtObject roFCObject, Context roContext)
        {
            Conso oCon = new Conso();
            oCon.ReadFrom(roFCObject, roContext);
            ICtConsoDef o = (ICtConsoDef)roFCObject;
            return new List<ManagedObject>() {
                (ManagedObject)new RefValueLight(o.Phase, roContext),
                (ManagedObject)new RefValueLight(o.UpdPer, roContext),
                (ManagedObject)new RefValueLight(o.ScopeCode, roContext),
                (ManagedObject)new RefValueLight(o.Variant, roContext),
                (ManagedObject)new RefValueLight(o.Curncy, roContext)
            };
        }


        public static MultiPartID<Conso> GetList(Context roContext)
        {
            MultiPartID<Conso> oRet = new MultiPartID<Conso>(roContext, new Conso().GetIdentifierParts, Conso.GetIDDimensions);
            // TODO: sort and return the newest N
            return oRet;
        }
    }
}


