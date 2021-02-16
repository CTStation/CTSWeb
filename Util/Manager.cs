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
using System.Reflection;
using log4net;
using CTCLIENTSERVERLib;

namespace CTSWeb.Util
{
    // FC API is built around specialized managers that return object instances
    // This class provides a generic manager to avoid re implementing the same pattern over and over
    // A class representing an FC object should inherit ManagedObject, and its class constructor should register the manager number
    // Manager is static as only a singleton is needed

    // Public interface is like a collection:
    //  GetAll <==> ToList
    //  Get accesses by FC ID or by Name
    //  Exists <==> Contains, also by ID or Name
    //  Save create or updates


    public class ManagedObject
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Needs a parameterless constructor that builds an empty object
        // C# does not allow virtual constructors to state this fact, like virtual ManagedObject() { } should
        //  However, the new() constraint enforces it

        // Needs a pair of functions to read from and write to a generic FC object
        // These will be called both for new and existing objects

        // Duplicating the language in every object may seem wasteful
        // However, we would otherwise need a pointer to the context, that is equally wasteful
        private protected Language _oLanguage;

        public int ID;
        public string Name;
        public string LDesc;             // Ldesc in working language

        public virtual void ReadFrom(ICtObject roObject, Language roLang) 
        {
            _oLanguage = roLang;

            if (!(roObject is null))
            {
                ID = roObject.ID;
                Name = roObject.Name;
                LDesc = roObject.get_Desc(ct_desctype.ctdesc_long, roLang.WorkingLanguage);
                _oLog.Debug($"Loaded managed object {Name}");
            }
        }

        public virtual void WriteInto(ICtObject roObject, MessageList roMess, Context roContext) 
        {
            bool bTest = !(roMess is null);

            // Access to generic type static fields is possible only through reflexion
            Type oType = this.GetType();
            FieldInfo oField = oType.GetField("_bDontSaveName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            // Never change object ID 
            // roObject.ID = ID;
            if (!(Name is null) && (oField is null))
            {
                if (bTest && !(roObject.Name is null) && roObject.Name != Name) roMess.Add("RF0310", "Name", Name, roObject.Name);
                roObject.Name = Name;
            }
            // Will be done with other descriptions roObject.set_Desc(ct_desctype.ctdesc_long, _oLanguage.WorkingLanguage, LDesc);
        }
    }


    public class ManagedObjectWithDesc : ManagedObject
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static int _iSupportedTranslatableFields;

        public LanguageText[] Descriptions;
         
        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);
            Descriptions = new LanguageText[roLang.SupportedLanguages.Count];

            // Access to generic type static fields is possible only through reflexion
            Type oType = this.GetType();
            FieldInfo oField = oType.GetField("_iSupportedTranslatableFields", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            int iFields = (int)((oField != null) ? oField.GetValue(null) : LanguageMasks.All);

            int c = 0;
            foreach ((lang_t, string) oLanguage in roLang.SupportedLanguages)
            {
                Descriptions[c] = new LanguageText(oLanguage.Item1, roLang);
                if (Descriptions[c].CultureName != oLanguage.Item2) _oLog.Debug($"Invalid culture name: expected '{Descriptions[c].CultureName}' and found '{oLanguage.Item2}'");
                if (!(roObject is null))
                {
                    foreach (var o in LanguageText.TypeInfo)
                    {
                        if ((iFields & (int)o.Item1) != 0) Descriptions[c].Texts[o.Item3] = Language.Description(roObject, o.Item2, oLanguage.Item1);
                    }
                }
                c++;
            }
            _oLog.Debug($"Loaded descriptions in {c} language(s) into managed object with desc {Name}");
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess, Context roContext)
        {
            base.WriteInto(roObject, roMess, roContext);
            bool bTest = !(roMess is null);
            string sOld;
            string sNew;

            foreach (LanguageText oText in Descriptions)
            {
                if (_oLanguage.TryGetLanguageID(oText.CultureName, out lang_t iLang))
                {
                    foreach (var o in LanguageText.TypeInfo)
                    {
                        if (oText.Texts.ContainsKey(o.Item3))
                        {
                            sOld = Language.Description(roObject, o.Item2, iLang);
                            sNew = oText.Texts[o.Item3];
                            if (bTest && (!(sNew is null)) && (sOld != sNew)) roMess.Add("RF0310", o.Item3 + ((int)iLang).ToString(), sNew, sOld);
                            Language.SetDesc(roObject, o.Item2, iLang, sNew);
                        }
                    }
                } // No else: if language isn't found or active, ignore
            }
        }
    }

    public class ManagedObjectWithDescAndSecurity : ManagedObjectWithDesc
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public int OwnerSite;
        public int OwnerWorkgroup;
        public DateTime CreationDate;
        public int Author;
        public DateTime UpdateDate;
        public int UpdateAuthor;

        public override void ReadFrom(ICtObject roObject, Language roLang)
        {
            base.ReadFrom(roObject, roLang);

            if (!(roObject is null))
            {
                ICtStatObject oObj = (ICtStatObject)roObject;
                OwnerSite = oObj.OwnerSite;
                OwnerWorkgroup = oObj.OwnerWorkgroup;
                CreationDate = oObj.CreationDate;
                Author = oObj.Author;
                UpdateDate = oObj.UpdateDate;
                UpdateAuthor = oObj.UpdateAuthor;
                _oLog.Debug($"Loaded managed object with security {Name}");
            }
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess, Context roContext)
        {
            base.WriteInto(roObject, roMess, roContext);

            // Let FC do that
            //ICtStatObject oObj = (ICtStatObject)roObject;
            //oObj.OwnerSite = OwnerSite;
            //oObj.OwnerWorkgroup = OwnerWorkgroup;
            //oObj.CreationDate = CreationDate;
            //oObj.Author = Author;
            //oObj.UpdateDate = UpdateDate;
            //oObj.UpdateAuthor = UpdateAuthor;
        }
    }




    public static class Manager
    {
        #region Private
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Dictionary<Type, int> _oType2MgrID = new Dictionary<Type, int>();
        // Allow access with 2 names, like Reporting or Framework
        private static readonly Dictionary<Type, Func<Context, ICtObjectManager, string, string, ICtObject>> _oType2Delegate = 
                            new Dictionary<Type, Func<Context, ICtObjectManager, string, string, ICtObject>>();

        private static readonly Dictionary<string, PrDimensionAccess> _oCode2DimAccess = new Dictionary<string, PrDimensionAccess>();

        private class PrDimensionAccess
        {
            public readonly bool HasRefTable;
            public readonly int RefTableManagerID;
            public readonly int DimensionId;

            public PrDimensionAccess((bool, int, int) voInfo)
            {
                HasRefTable = voInfo.Item1;
                RefTableManagerID = voInfo.Item2;
                DimensionId = voInfo.Item3;

                bool bContract = (RefTableManagerID == 0) ^ (DimensionId == 0);
                bContract &= HasRefTable == (RefTableManagerID != 0);
                if (!bContract) throw new ArgumentException();
            }
        }


        static Manager()
        {
            // Give access mode to ref tables or dimensions. Stores a PrDimensionAccess
            Dictionary<string, (bool, int, int)> oAccess = new Dictionary<string, (bool, int, int)>()
            {
                { "Phase",              (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_PHASE, 0) },
                { "Entity",             (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_ENTITY, 0) },
                { "Currency",           (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_CURRENCY, 0) },
                { "FrameworkVersion",   (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_FRAMEWORKVERSION, 0) },
                { "Account",            (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_ACCOUNT, 0) },
                { "Flow",               (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_FLOW, 0) },
                { "Nature",             (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_NATURE, 0) },
                { "ExRateType",         (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_EXRATETYPE, 0) },
                { "ExRateVersion",      (true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_EXRATEVERSION, 0) },
                { "UpdPer",             (false, 0, (int)CTCOMMONMODULELib.ct_dimension.DIM_UPDPER) },
                { "Period",             (false, 0, (int)CTCOMMONMODULELib.ct_dimension.DIM_PERIOD) },
            };

            foreach (string sDimName in oAccess.Keys)
            {
                PrDimensionAccess o = new PrDimensionAccess(oAccess[sDimName]);
                _oCode2DimAccess.Add(sDimName, o);
            }
        }

        // Wrapper to signal the context has stained when an FC call throws an exception
        public static void COMMonitor(Context roContext, Action voAction)
        {
            try
            {
                voAction();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                roContext.HasFailedRquests = true;
                throw e;
            }
        }


        private static ICtObjectManager PrGetMgr<tObject>(Context roContext) where tObject : ManagedObject, new()
        {
            ICtProviderContainer oContainer = (ICtProviderContainer)roContext.Config.Session;
            if (!_oType2MgrID.TryGetValue(typeof(tObject), out int iMgrID))
            {
                // Maybe the class wasn't initialised. So try creating an object to ensure class init
                tObject oDummy = new tObject();
                if (!_oType2MgrID.TryGetValue(typeof(tObject), out iMgrID)) throw new ArgumentException($"Unregistered FC type '{typeof(tObject).Name}'");
            }
            ICtObjectManager oManager = null;
            COMMonitor(roContext, () => oManager = (ICtObjectManager)oContainer.get_Provider(1, iMgrID));
            return oManager;
        }

        private static ICtObject PrGet<tObject>(Context roContext, int viID, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true, int viMgrID = 0)
            where tObject : ManagedObject, new()
        {
            ICtObject oRet = null;
            ICtObjectManager oManager = null;
            COMMonitor(roContext, () => {
                oManager = (viMgrID == 0) ? PrGetMgr<tObject>(roContext) : (ICtObjectManager)(((ICtProviderContainer)roContext.Config.Session).get_Provider(1, viMgrID));
                oRet = (ICtObject)oManager.GetObject(viID, viFlags, 0);
            });
            if (vbRaiseErrorIfNotFound && oRet is null) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with ID '{viID}'");
            return oRet;
        }

        private static ICtObject PrGet<tObject>(Context roContext, string vsName, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true, int viMgrID = 0)
            where tObject : ManagedObject, new()
        {
            ICtObject oRet = null;
            ICtObjectManager oManager = null;
            COMMonitor(roContext, () => {
                oManager = (viMgrID == 0) ? PrGetMgr<tObject>(roContext) : (ICtObjectManager)(((ICtProviderContainer)roContext.Config.Session).get_Provider(1, viMgrID));
                ICtOqlFactoryFacade oqlFactory = new CtOqlFactoryFacadeClass();
                ICtOqlBooleanExpr oOql = oqlFactory.Equal(oqlFactory.Prop((int)ct_object_property.CT_NAME_PROP), oqlFactory.Value(vsName));
                ICtGenCollection oCollection = null;
                try {
                    oCollection = oManager.GetObjects(oOql, viFlags, 0, null);
                } catch (System.Runtime.InteropServices.COMException e) {
                    _oLog.Debug($"Object '{vsName}' not found: {e}");
                }
                if (vbRaiseErrorIfNotFound && (oCollection is null)) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with name '{vsName}'");
                oRet = (oCollection is null) ? null : (ICtObject)oCollection.GetAt(1);
            });
            if (vbRaiseErrorIfNotFound && oRet is null) throw new ArgumentNullException($"Object of type { typeof(tObject).Name } found with name '{vsName}' is null");
            return oRet;
        }


        private static ICtObject PrGet<tObject>(Context roContext, string vsID1, string vsID2, bool vbRaiseErrorIfNotFound = true)
            where tObject : ManagedObject, new()
        {
            ICtObject oRet = null;
            ICtObjectManager oManager = PrGetMgr<tObject>(roContext);
            if (!_oType2Delegate.TryGetValue(typeof(tObject), out var oDelegate))
            {
                // Init is done by previous call to PrGetMgr
                throw new ArgumentException($"FC type '{typeof(tObject).Name}' does not support identification with 2 names");
            }
            try
            {
                oRet = oDelegate(roContext, oManager, vsID1, vsID2);
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                oRet = null;
                _oLog.Debug($"Object not found with criteria '{vsID1}' and '{vsID2}': {e}");
            }
            if (vbRaiseErrorIfNotFound && (oRet is null)) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with criteria '{vsID1}' and '{vsID2}'");
            return oRet;
        }

        #endregion

        public static void Register<tObject>(int viFCManagerID) where tObject : ManagedObject, new()
        {
            Type oType = typeof(tObject);

            _oType2MgrID.Add(oType, viFCManagerID);
        }

        public static void Register<tObject>(int viFCManagerID, int viSupportedTranslatableFields) where tObject : ManagedObjectWithDesc, new()
        {
            Type oType = typeof(tObject);

            Register<tObject>(viFCManagerID);
            // Access to generic type static fields is possible only through reflexion
            FieldInfo oField = oType.GetField("_iSupportedTranslatableFields", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            oField?.SetValue(null, viSupportedTranslatableFields);
        }


        public static void RegisterDelegate<tObject>(Func<Context, ICtObjectManager, string, string, ICtObject> voDelegate) where tObject : ManagedObject, new()
        {
            Type oType = typeof(tObject);
            _oType2Delegate.Add(oType, voDelegate);
        }


        public static List<tObject> GetAll<tObject>(Context roContext) where tObject : ManagedObject, new()
        {
            List<tObject> oRet = new List<tObject>();

            ICtObjectManager oManager = PrGetMgr<tObject>(roContext);
            COMMonitor(roContext, () => {
                CTCLIENTSERVERLib.ICtGenCollection oCollection = oManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);
                // New tObject(oObj) generates a compiler error (new can be called only with 0 argument
                tObject o;
                foreach (ICtObject oFCObj in oCollection)
                {
                    o = new tObject();
                    o.ReadFrom(oFCObj, roContext.Language);
                    oRet.Add(o);
                }
            });
            return oRet;
        }


        public static tObject Get<tObject>(Context roContext, int viID) where tObject : ManagedObject, new()
        {
            ICtObject oFCObj = PrGet<tObject>(roContext, viID);
            tObject oRet = new tObject();
            oRet.ReadFrom(oFCObj, roContext.Language);
            return oRet;
        }


        public static tObject Get<tObject>(Context roContext, string vsName) where tObject : ManagedObject, new()
        {
            ICtObject oFCObj = PrGet<tObject>(roContext, vsName);
            tObject oRet = new tObject();
            oRet.ReadFrom(oFCObj, roContext.Language);
            return oRet;
        }


        public static tObject Get<tObject>(Context roContext, string vsID1, string vsID2) where tObject : ManagedObject, new()
        {
            ICtObject oFCObj = PrGet<tObject>(roContext, vsID1, vsID2);
            tObject oRet = new tObject();
            oRet.ReadFrom(oFCObj, roContext.Language);
            return oRet;
        }


        public static bool Exists<tObject>(Context roContext, int viID) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roContext, viID,ACCESSFLAGS.OM_READ, false) is null);


        public static bool Exists<tObject>(Context roContext, string vsName) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roContext, vsName, ACCESSFLAGS.OM_READ, false) is null);


        public static bool Exists<tObject>(Context roContext, string vsID1, string vsID2) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roContext, vsID1, vsID2, false) is null);


        public static HashSet<string> GetRefValueCodes(Context roContext, string vsTableCode)
        {
            HashSet<string> oRet = new HashSet<string>();
            if (!_oCode2DimAccess.TryGetValue(vsTableCode, out PrDimensionAccess oDimAccess))
            {
                throw new ArgumentException($"Unrecognized FC table '{vsTableCode}'");
            } 
            else
            {
                if (!oDimAccess.HasRefTable)
                {
                    throw new ArgumentException($"{vsTableCode} does not have a list of elements");
                }
                else
                {
                    ICtProviderContainer oContainer = (ICtProviderContainer)roContext.Config.Session;
                    ICtObjectManager oManager = null;
                    COMMonitor(roContext, () => {
                        oManager = (ICtObjectManager)oContainer.get_Provider(1, oDimAccess.RefTableManagerID);
                        foreach (ICtObject o in oManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null))
                        {
                            oRet.Add(o.Name);
                        }
                    });
                }
            }
            return oRet;
        }


        public static Models.RefValue GetRefValue(Context roContext, string vsTableCode, string vsName)
        {
            Models.RefValue oRet = null;
            CTCORELib.ICtRefValue oRefVal = null;
            if (!_oCode2DimAccess.TryGetValue(vsTableCode, out PrDimensionAccess oDimAccess))
            {
                throw new ArgumentException($"Unrecognized FC table '{vsTableCode}'");
            }
            else
            {
                ICtProviderContainer oContainer = (ICtProviderContainer)roContext.Config.Session;
                COMMonitor(roContext, () =>
                {
                    if (oDimAccess.HasRefTable)
                    {
                        CTCORELib.ICtRefValueManager oManager = (CTCORELib.ICtRefValueManager)oContainer.get_Provider(1, oDimAccess.RefTableManagerID);
                        oRefVal = oManager.get_RefValueFromName(vsName, 0);
                    }
                    else
                    {
                        CTCORELib.ICtDimensionManager oDimManager = (CTCORELib.ICtDimensionManager)(oContainer.get_Provider(1, (int)CTCORELib.ct_core_manager.CT_DIMENSION_MANAGER));
                        CTCORELib.ICtDimension oDim = oDimManager.get_Dimension(oDimAccess.DimensionId);
                        oRefVal = oDim.get_RefValueFromName(vsName, 0);
                    }
                });
            }
            if (!(oRefVal is null))
            {
                oRet = new Models.RefValue();
                oRet.ReadFrom(oRefVal, roContext.Language);
            }
            return oRet;
        }

        // TODO
        public static void Save<tObject>(Context roContext, tObject roObject, MessageList roMess) where tObject : ManagedObject, new()
        {
            if (roObject != null)
            {
                // Get COM object to save
                ICtObject oFCObj;
                if (roObject.ID == 0) // New object
                {
                    if (roObject.Name is null)
                    {
                        throw new ArgumentNullException($"A new object of type {typeof(tObject)} must have a Name");
                    }
                    else
                    {
                        if (Exists<tObject>(roContext, roObject.Name))
                        { // TODO: Change to message
                            throw new ArgumentOutOfRangeException($"An object of name {roObject.Name} already exists in type {typeof(tObject)}");
                        }
                        else
                        {
                            oFCObj = (ICtObject)PrGetMgr<tObject>(roContext)?.NewObject(1);
                            if (oFCObj is null) throw new Exception($"Can't get new {typeof(tObject)}");
                        }
                    }
                }
                else                     // Saving existing object
                {
                    oFCObj = PrGet<tObject>(roContext, roObject.ID, ACCESSFLAGS.OM_WRITE);
                    if (oFCObj is null) throw new Exception($"Can't open {typeof(tObject)} for writing");
                }
                _oLog.Debug($"Writing object {roObject.Name}");
                roObject.WriteInto(oFCObj, roMess, roContext);
        
                try
                {
                    oFCObj.IsObjectValid();
                    ((dynamic)(oFCObj.Manager)).SaveObject(oFCObj);
                    //PrGetMgr<tObject>(roContext).SaveObject(oFCObj);
                }
                catch (Exception e)
                {
                    roMess.Add("RF0311", e.Message);
                    _oLog.Debug(e);
                }
                try
                {
                    oFCObj.WriteUnlock();
                }
                catch (Exception e) { } // TODO
                _oLog.Debug("Writen");
            }
        }
    }
}
