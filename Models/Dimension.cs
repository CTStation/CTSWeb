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
using CTCOMMONMODULELib;
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

        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            if (!(roObject is null))
            {
                ICtDataSource oSource = (ICtDataSource)roObject;
                Dimensions = new List<Dimension>();
                foreach (ICtObjectBase o in oSource.Dimensions)
                {
                    Dimension oDim = new Dimension();
                    oDim.ReadFrom((ICtObject)o, roContext);
                    this.Dimensions.Add(oDim);
                }
            }
        }
    }


    public class RefTable : ManagedObject // Inherits ID and Name and LDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static RefTable()
        {
            Manager.Register<RefTable>((int)ct_core_manager.CT_REFTABLE_MANAGER);
        }

        // Argument-less constructor
        public RefTable() { }

    }


    public class RefValueLight : ManagedObject
    {
        public RefValueLight() { }

        public RefValueLight(ICtObject roFCObject, Context roContext)
        {
            ReadFrom(roFCObject, roContext);
        }

    }

    public class RefValue : ManagedObjectWithDesc
    {
        static RefValue()
        {
            // Manager.Register<RefTable>((int)ct_core_manager.CT_REFTABLE_MANAGER);
        }

        private Dims _iDimension;

        // Argument-less constructor
        public RefValue() { }

        // Constructor with one argument to store Dimension
        public RefValue(Dims viDimension) { _iDimension = viDimension; }

        private ICtRefValue _oFCRefValue;
        public ICtRefValue FCValue() => _oFCRefValue;

        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            _oFCRefValue = (ICtRefValue)roObject;
        }


        // TODO: integrate dimensions in the Get<> Exists<> pattern

        private static List<string> PrGetDimensionDesc(Context roContext, Dims viDim)
        {
            List<string> oRet = new List<string>();
            int iDim;
            switch (viDim)
            {
                case Dims.Currency:
                    iDim = (int)CTCOMMONMODULELib.ct_dimension.DIM_CURNCY;
                    break;
                case Dims.Entity:
                    iDim = (int)CTCOMMONMODULELib.ct_dimension.DIM_ENTITY;
                    break;
                default:
                    iDim = (int)CTCOMMONMODULELib.ct_dimension.DIM_PHASE;
                    break;
            }
            oRet.Add(roContext.Get<Dimension>(iDim).LDesc.Trim());
            return oRet;
        }

        public List<string> GetIDDimensions(Context roContext) => PrGetDimensionDesc(roContext, _iDimension);


        public static MultiPartID<RefValue> GetDim(Context roContext, Dims viDim)
        {
            List<List<ManagedObject>> oIds = new List<List<ManagedObject>>();
            Manager.Execute<RefValue>(roContext, (ICtObjectManager oMgr) =>
            {
                foreach (ICtObject o in oMgr.GetObjects(null, ACCESSFLAGS.OM_READ, 0, null))
                {
                    ManagedObject oItem = new ManagedObject();
                    oItem.ReadFrom(o, roContext);
                    oIds.Add(new List<ManagedObject>() { oItem });
                }
            }, viDim);
            return new MultiPartID<RefValue>(PrGetDimensionDesc(roContext, viDim), oIds);
        }

    }

}


