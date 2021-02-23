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
        static Framework()
        {
            Manager.Register<Framework>(-524238);
            // Get framework from phase and version 
            Manager.RegisterDelegate<Framework>((Context roContext, ICtObjectManager voMgr, string vsID1, string vsID2) =>
                                                        (ICtObject)((ICtFrameworkManager)voMgr).FrameworkFromPhaseVersion[
                                                            roContext.GetRefValue(Dims.Phase, vsID1).FCValue(),
                                                            roContext.GetRefValue(Dims.FrameworkVersion, vsID2).FCValue()]
                                                    );
        }

        // Argument-less constructor
        public Framework() { }

        public string Phase;
        public string Version;
        public kref_framework_status Status;
        public SortedList<string, ControlSet> ControlSets = new SortedList<string, ControlSet>();
        public SortedList<short, ControlLevel> ControlLevels = new SortedList<short, ControlLevel>();

        private IRefObjRef _oFC;

        public IRefObjRef FCValue() => _oFC;


        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            if (!(roObject is null))
            {
                IRefObjRef oRef = (IRefObjRef)roObject;
                _oFC = oRef;
                Phase = oRef.Phase?.Name;
                Version = oRef.Version?.Name;
                Status = oRef.RefStatus;
                foreach (ICtObject oLevel in oRef.ContLevelList)
                {
                    ControlLevel o = new ControlLevel();
                    o.ReadFrom(oLevel, roContext);
                    ControlLevels.Add(o.Rank, o);
                }
                foreach (ICtObject oSet in oRef.CtrlSetList)
                {
                    ControlSet o = new ControlSet();
                    o.ReadFrom(oSet, roContext);
                    ControlSets.Add(o.Name, o);
                }
            }
        }

        public ControlLevel GetControlLevel(short? viRank)
        {
            return viRank is null ? null : ControlLevels[(short)viRank];
        }

        public ControlSet GetSetOfControl(string vsName)
        {
            return vsName is null ? null : ControlSets[vsName];
        }
    }

    //public class Framework
    //{
    //   // public List<ControlLevel> ControlLevels { get; }

    //    public Framework(IRefObjRef roFramework)
    //    {
    //        // ControlLevels = new List<ControlLevel>();

    //        //foreach (IRefCtrlFamily obj in framework.CtrlFamilyList)
    //        //{
    //        //        Console.WriteLine(obj.Name);
    //        //}

    //        //foreach (IRefControl obj in framework.ControlList)
    //        //{
    //        //    Controls.Add(new Control(obj));
    //        //}

    //        //foreach (IRefControlSet obj in framework.CtrlSetList)
    //        //{
    //        //    ControlSets.Add(new ControlsSets(obj));
    //        //}
    //    }

    //    public bool TryGet(string rsPhaseName, string rsVersionName, Context roFCSession, out IRefObjRef roRef)
    //    {
    //        bool bFound = false;

    //        ICtProviderContainer oProviderContainer = (ICtProviderContainer)roFCSession.Config; ;
    //        ICtObjectManager oManager = (ICtObjectManager)oProviderContainer.get_Provider(1, (int)ct_core_manager.CT_REFTABLE_MANAGER);
    //        ICtGenCollection oColl = oManager. GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);

    //        roRef = default;
    //        foreach (IRefObjRef oFrame in oColl)
    //        {
    //            if (oFrame.Phase.Name == rsPhaseName && oFrame.Version.Name == rsVersionName)
    //            {
    //                bFound = true;
    //                roRef = oFrame;
    //                break;
    //            }
    //        }
    //        return bFound;
    //    }
    //}


    public class ControlSet : ManagedObject
    {
        static ControlSet()
        {
        // No registery
        }

        // Argument-less constructor
        public ControlSet() { }

        private IRefControlSet _oFC;

        public IRefControlSet FCValue() => _oFC;

        // public List<FCControl> Controls;
        //List<ControlsSets> ControlSets;
        //public List<ControlSubSets> ControlSubSets;


        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            _oFC = (IRefControlSet)roObject;
            //foreach (IRefCtrlFamily refObjRef in obj.Content)
            //{

            //    switch (refObjRef.Type)
            //    {
            //        case -524234: // controlset
            //            //ControlSets.Add(new ControlsSets((IRefControlSet)refObjRef));

            //            break;

            //        case -524221: // subset
            //            ControlSubSets.Add(new ControlSubSets((IRefCtrlFamily)refObjRef));
            //            break;
            //        default:
            //            Controls.Add(new FCControl((IRefControl)refObjRef));
            //            break;

            //    }

            //}
        }
    }

    public class ControlSubSets
    {
        public int ID;
        public string Name;
        public List<FCControl> ManualControls = new List<FCControl>();
        public List<FCControl> AutomaticControls = new List<FCControl>();

        public ControlSubSets(IRefCtrlFamily controlFamily)
        {
            ID = controlFamily.ID;
            Name = controlFamily.Name;
            ManualControls = new List<FCControl>();
            AutomaticControls = new List<FCControl>();
            foreach (IRefControlBase control in controlFamily.Content)
            {
                switch (control.Type)
                {
                    case -524233:
                        ManualControls.Add(new FCControl(control));
                        break;

                    case -524232:
                        AutomaticControls.Add(new FCControl(control));
                        break;
                }
                // Controls.Add(new Control(control));
            }
        }
    }

    public class FCControl
    {
        public int ID;
        public string Name;
        public string Expression;
        public ControlLevel Level;

        public FCControl(IRefControlBase control)
        {
            ID = control.ID;
            Name = control.Name;
            Expression = control.Expr;
            // Level = control.Level;
        }
    }


    public class ControlLevel : ManagedObjectWithDesc
    {
        static ControlLevel()
        {
            // No manager, searched in Framework
            // Manager.Register<ControlLevel>();
            // Get framework from phase and version 
        }

        // Argument-less constructor
        public ControlLevel() { }

        private IRefLevel _oFC;
        private short _iRank;

        public short Rank { get => _iRank; }

        public IRefLevel FCValue() => _oFC;


        public override void ReadFrom(ICtObject roObject, Context roContext)
        {
            base.ReadFrom(roObject, roContext);

            if (!(roObject is null))
            {
                IRefLevel oRef = (IRefLevel)roObject;
                _oFC = oRef;
                _iRank = oRef.Rank;
            }
        }
    }
}

