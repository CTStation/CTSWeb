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


    public class NamedObject
    {
        public int ID { get; }
        public string Name { get; }

        public NamedObject(int viID, string viName)
        {
            ID = viID;
            Name = viName;
        }
    }

    public class Descriptions
    {
        private static readonly ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICtObjectBase _FCObject;
        private readonly Context _Session;

        public Descriptions(ICtObjectBase roFCObject, Context roSession)
        {
            _FCObject = roFCObject;
            _Session = roSession;
        }

        public string LDesc { get => ""; }

        public string Get(CTCLIENTSERVERLib.ct_desctype viType, CTCLIENTSERVERLib.lang_t viLanguage)
        {
            try
            {
                return (string) _FCObject.PropVal[0];
            }
            catch (Exception e)
            {
                _log.Warn($"Error getting description {viType} in language {viLanguage} for object {_FCObject.ToString()}", e);
                return "";
            }
        }

    }

    public class Framework
    {
       // public List<ControlLevel> ControlLevels { get; }

        public Framework(IRefObjRef roFramework)
        {
            // ControlLevels = new List<ControlLevel>();

            //foreach (IRefCtrlFamily obj in framework.CtrlFamilyList)
            //{
            //        Console.WriteLine(obj.Name);
            //}

            //foreach (IRefControl obj in framework.ControlList)
            //{
            //    Controls.Add(new Control(obj));
            //}

            //foreach (IRefControlSet obj in framework.CtrlSetList)
            //{
            //    ControlSets.Add(new ControlsSets(obj));
            //}
        }

        public bool TryGet(string rsPhaseName, string rsVersionName, Context roFCSession, out IRefObjRef roRef)
        {
            bool bFound = false;

            ICtProviderContainer oProviderContainer = (ICtProviderContainer)roFCSession.Config; ;
            ICtObjectManager oManager = (ICtObjectManager)oProviderContainer.get_Provider(1, (int)ct_core_manager.CT_REFTABLE_MANAGER);
            ICtGenCollection oColl = oManager. GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);

            roRef = default;
            foreach (IRefObjRef oFrame in oColl)
            {
                if (oFrame.Phase.Name == rsPhaseName && oFrame.Version.Name == rsVersionName)
                {
                    bFound = true;
                    roRef = oFrame;
                    break;
                }
            }
            return bFound;
        }
    }

    public class ControlsSets
    {
        public int ID;
        public string Name;
        public List<Control> Controls;
        //List<ControlsSets> ControlSets;
        public List<ControlSubSets> ControlSubSets;


        public ControlsSets(IRefControlSet obj)
        {
            ID = obj.ID;
            Name = obj.Name;
            Controls = new List<Control>();
            ControlSubSets = new List<ControlSubSets>();
            foreach (IRefCtrlFamily refObjRef in obj.Content)
            {

                switch (refObjRef.Type)
                {
                    case -524234: // controlset
                        //ControlSets.Add(new ControlsSets((IRefControlSet)refObjRef));

                        break;

                    case -524221: // subset
                        ControlSubSets.Add(new ControlSubSets((IRefCtrlFamily)refObjRef));
                        break;
                    default:
                        Controls.Add(new Control((IRefControl)refObjRef));
                        break;

                }

            }


        }
    }

    public class ControlSubSets
    {
        public int ID;
        public string Name;
        public List<Control> ManualControls = new List<Control>();
        public List<Control> AutomaticControls = new List<Control>();

        public ControlSubSets(IRefCtrlFamily controlFamily)
        {
            ID = controlFamily.ID;
            Name = controlFamily.Name;
            ManualControls = new List<Control>();
            AutomaticControls = new List<Control>();
            foreach (IRefControlBase control in controlFamily.Content)
            {
                switch (control.Type)
                {
                    case -524233:
                        ManualControls.Add(new Control(control));
                        break;

                    case -524232:
                        AutomaticControls.Add(new Control(control));
                        break;
                }
                // Controls.Add(new Control(control));
            }
        }
    }

    public class Control
    {
        public int ID;
        public string Name;
        public string Expression;
        public Control(IRefControlBase control)
        {
            ID = control.ID;
            Name = control.Name;
            Expression = control.Expr;
        }
    }


    public class ExchangeRate
    {
        public int ID;
        public string Name;
        public ExchangeRate(ICtExchangeRate exchangeRate)
        {
            if (exchangeRate != null)
            {
                ID = exchangeRate.ID;
                Name = exchangeRate.Name;

            }    
        }
    }


    public class ExchangeRateUpdatePeriod
    {
        public int ID;
        public string Name;

        public ExchangeRateUpdatePeriod(ICtRefValue exchangeRateUpdatePeriod)
        {
            if (exchangeRateUpdatePeriod!=null)
            {
                ID = exchangeRateUpdatePeriod.ID;
                Name = exchangeRateUpdatePeriod.Name;
            }

        }
    }

    public class ExchangeRateVersion
    {
        public int ID;
        public string Name;
        public ExchangeRateVersion(ICtRefValue exchangeRateVersion)
        {
            if (exchangeRateVersion != null)
            {
                ID = exchangeRateVersion.ID;
                Name = exchangeRateVersion.Name;
            }

        }

    }

    public class ExchangeRateType
    {
        public int ID;
        public string Name;

    }

    public class RelatedEntityReportingCollection
    {
        public int ID;
        public string Name;
        public InputCurrency InputCurrency;
        public Entity Entity;
        public bool IsInputSiteLocal;
        public BaseOperation BaseOperation;


        public RelatedEntityReportingCollection(ICtEntityReporting entityReporting)
        {
            ID = entityReporting.ID;
            Name = entityReporting.Name;
            InputCurrency = new InputCurrency()
            {
                ID = entityReporting.ID,
                Name = entityReporting.Name
            };

            Entity = new Entity()
            {
                ID = entityReporting.Entity.ID,
                Name = entityReporting.Entity.Name
            };
            IsInputSiteLocal = entityReporting.IsInputSiteLocal;

            BaseOperation = new BaseOperation()
            {
                PackPublishingCutOffDate = entityReporting.DefaultPackOperation.PackPublishingCutOffDate,
                AllowEarlyPublishing = entityReporting.DefaultPackOperation.AllowEarlyPublishing,
                IntegrateAfterPublication = entityReporting.DefaultPackOperation.IntegrateAfterPublication,
                IntegrateAfterTransfer = entityReporting.DefaultPackOperation.IntegrateAfterTranfer


            };

        }

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
    public class Entity
    {
        public int ID;
        public string Name;
    }


    public class InputCurrency
    {
        public int ID;
        public string Name;
    }

    public class Period
    {
        public int ID;
        public string Name;
    }
    public class ReportingModifyComment
    {

    }

    public class ReportingHierarchyDate
    {

    }
}


