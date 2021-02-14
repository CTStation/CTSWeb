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

        public virtual void ReadFrom(ICtObject roObject, Language roLang) {
            _oLanguage = roLang;

            ID = roObject.ID;
            Name = roObject.Name;
            LDesc = roObject.get_Desc(ct_desctype.ctdesc_long, roLang.WorkingLanguage);
            _oLog.Debug($"Loaded managed object {Name}");
        }

        public virtual void WriteInto(ICtObject roObject, MessageList roMess) {
            bool bTest = !(roMess is null);

            // Nether change object ID 
            // roObject.ID = ID;
            if (!(Name is null))
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
                foreach (var o in LanguageText.TypeInfo)
                {
                    if ((iFields & (int)o.Item1) != 0) Descriptions[c].Texts[o.Item3] = Language.Description(roObject, o.Item2, oLanguage.Item1);
                }
                c++;
            }
            _oLog.Debug($"Loaded descriptions in {c} language(s) into managed object with desc {Name}");
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess)
        {
            base.WriteInto(roObject, roMess);
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
                            if (bTest && !(sOld is null) && (sOld != sNew)) roMess.Add("RF0310", o.Item3 + iLang.ToString(), sNew, sOld);
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

            ICtStatObject oObj = (ICtStatObject)roObject;
            OwnerSite = oObj.OwnerSite;
            OwnerWorkgroup = oObj.OwnerWorkgroup;
            CreationDate = oObj.CreationDate;
            Author = oObj.Author;
            UpdateDate = oObj.UpdateDate;
            UpdateAuthor = oObj.UpdateAuthor;
            _oLog.Debug($"Loaded managed object with security {Name}");
        }

        public override void WriteInto(ICtObject roObject, MessageList roMess)
        {
            base.WriteInto(roObject, roMess);

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

        // Give name to ref tables, provides lists
        private static readonly Dictionary<string, int> _oCode2MgrID = new Dictionary<string, int>()
        {
            { "Phase",              (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_PHASE },
            { "Entity",             (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_ENTITY },
            { "Currency",           (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_CURRENCY },
            { "FrameworkVersion",   (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_FRAMEWORKVERSION },
            { "Account",            (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_ACCOUNT },
            { "Flow",               (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_FLOW },
            { "Nature",             (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_NATURE },
            { "ExRateType",         (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_EXRATETYPE },
            { "ExRateVersion",      (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_EXRATEVERSION },
        };


        // Deals with Period, provides individual elements
        private static readonly Dictionary<string, int> _oCode2DimensionID = new Dictionary<string, int>()
        {
            { "Phase",              (int)CTCOMMONMODULELib.ct_dimension.DIM_PHASE},
            { "Entity",             (int)CTCOMMONMODULELib.ct_dimension.DIM_ENTITY},
            { "Currency",           (int)CTCOMMONMODULELib.ct_dimension.DIM_CURNCY},
            { "Account",            (int)CTCOMMONMODULELib.ct_dimension.DIM_ACCOUNT},
            { "Flow",               (int)CTCOMMONMODULELib.ct_dimension.DIM_FLOW},
            { "Nature",             (int)CTCOMMONMODULELib.ct_dimension.DIM_NATURE },
            { "UpdPer",             (int)CTCOMMONMODULELib.ct_dimension.DIM_UPDPER},
            { "Period",             (int)CTCOMMONMODULELib.ct_dimension.DIM_PERIOD},
        };


        private static ICtObjectManager PrGetMgr<tObject>(ConfigClass roConfig) where tObject : ManagedObject, new()
        {
            tObject oDummy = new tObject();     // Need that to call the class initialization

            ICtProviderContainer oContainer = (ICtProviderContainer)roConfig.Session;
            if (!_oType2MgrID.TryGetValue(typeof(tObject), out int iMgrID))
            {
                throw new ArgumentException($"Unregistered FC type '{typeof(tObject).Name}'");
            }
            ICtObjectManager oManager = (ICtObjectManager)oContainer.get_Provider(1, iMgrID);
            return oManager;
        }

        private static ICtObject PrGet<tObject>(ConfigClass roConfig, int viID, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true, int viMgrID = 0)
            where tObject : ManagedObject, new()
        {
            ICtObjectManager oManager = (viMgrID == 0) ? PrGetMgr<tObject>(roConfig) : (ICtObjectManager)(((ICtProviderContainer)roConfig.Session).get_Provider(1, viMgrID));
            ICtObject oRet = (ICtObject)oManager.GetObject(viID, viFlags, 0);
            if (vbRaiseErrorIfNotFound && oRet is null) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with ID '{viID}'");
            return oRet;
        }

        private static ICtObject PrGet<tObject>(ConfigClass roConfig, string vsName, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true, int viMgrID = 0)
            where tObject : ManagedObject, new()
        {
            ICtObjectManager oManager = (viMgrID == 0) ? PrGetMgr<tObject>(roConfig) : (ICtObjectManager)(((ICtProviderContainer)roConfig.Session).get_Provider(1, viMgrID));
            ICtOqlFactoryFacade oqlFactory = new CtOqlFactoryFacade();
            ICtOqlBooleanExpr oOql = oqlFactory.Equal(oqlFactory.Prop((int)ct_object_property.CT_NAME_PROP), oqlFactory.Value(vsName));
            ICtGenCollection oCollection = null;
            try
            {
                oCollection = oManager.GetObjects(oOql, viFlags, 0, null);
            }
            catch (System.Runtime.InteropServices.COMException e) 
            {
                _oLog.Debug(e);
            }
            if (vbRaiseErrorIfNotFound && (oCollection is null)) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with name '{vsName}'");
            ICtObject oRet = (oCollection is null) ? null : (ICtObject)oCollection.GetAt(1);
            if (vbRaiseErrorIfNotFound && oRet is null) throw new ArgumentNullException($"Object of type { typeof(tObject).Name } found with name '{vsName}' is null");
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


        public static List<tObject> GetAll<tObject>(Context roContext) where tObject : ManagedObject, new()
        {
            List<tObject> oRet = new List<tObject>();
            tObject oDumy;

            ICtObjectManager oManager = PrGetMgr<tObject>(roContext.Config);
            CTCLIENTSERVERLib.ICtGenCollection oCollection = oManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);
            // New tObject(oObj) generates a compiler error (new can be called only with 0 argument
            foreach (ICtObject oObj in oCollection)
            {
                oDumy = new tObject();
                oDumy.ReadFrom(oObj, roContext.Language);
                oRet.Add(oDumy);
            }
            return oRet;
        }


        public static tObject Get<tObject>(Context roContext, int viID, MessageList roMess) where tObject : ManagedObject, new()
        {
            tObject oDumy = new tObject();
            ICtObject oFCObj = PrGet<tObject>(roContext.Config, viID);
            if (oFCObj == null)
            {
                oDumy = null;
                roMess.Add("RF0110", typeof(tObject).Name, viID);
            }
            else
            {
                oDumy.ReadFrom(oFCObj, roContext.Language);
            }
            return oDumy;
        }


        public static tObject Get<tObject>(Context roContext, string vsName, MessageList roMess) where tObject : ManagedObject, new()
        {
            tObject oDumy = new tObject();
            ICtObject oFCObj = PrGet<tObject>(roContext.Config, vsName);
            if (oFCObj == null)
            {
                oDumy = null;
                roMess.Add("RF0111", typeof(tObject).Name, vsName);
            }
            else
            {
                oDumy.ReadFrom(oFCObj, roContext.Language);
            }
            return oDumy;
        }


        public static bool Exists<tObject>(ConfigClass roConfig, int viID) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roConfig, viID,ACCESSFLAGS.OM_READ, false) is null);


        public static bool Exists<tObject>(ConfigClass roConfig, string vsName) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roConfig, vsName, ACCESSFLAGS.OM_READ, false) is null);


        public static HashSet<string> GetRefValueCodes(ConfigClass roConfig, string vsTableCode)
        {
            ICtProviderContainer oContainer = (ICtProviderContainer)roConfig.Session;
            if (!_oCode2MgrID.TryGetValue(vsTableCode, out int iMgrID))
            {
                throw new ArgumentException($"Unrecognized FC table '{vsTableCode}'");
            }
            ICtObjectManager oManager = (ICtObjectManager)oContainer.get_Provider(1, iMgrID);
            HashSet<string> oRet = new HashSet<string>();
            foreach (ICtObject o in oManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null))
            {
                oRet.Add(o.Name);
            }
            return oRet;
        }


        
        public static Models.RefValue GetRefValue(ConfigClass roConfig, string vsTableCode, string vsName, Language voLang)
        {
            if (!_oCode2DimensionID.TryGetValue(vsTableCode, out int iDimID))
            {
                throw new ArgumentException($"Unrecognized FC table '{vsTableCode}'");
            }
            CTCORELib.ICtDimensionManager oDimManager = (CTCORELib.ICtDimensionManager)(((ICtProviderContainer)roConfig.Session).get_Provider(1, (int)CTCORELib.ct_core_manager.CT_DIMENSION_MANAGER));
            CTCORELib.ICtDimension oDim = oDimManager.get_Dimension(iDimID);
            CTCORELib.ICtRefValue oRefVal = oDim.get_RefValueFromName(vsName, 0);

            Models.RefValue oRet = null;
            if (!(oRefVal is null))
            {
                oRet = new Models.RefValue();
                oRet.ReadFrom(oRefVal, voLang);
            }
            return oRet;
        }


        public static void Save<tObject>(ConfigClass roConfig, tObject roObject, MessageList roMess) where tObject : ManagedObject, new()
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
                        if (Exists<tObject>(roConfig, roObject.Name))
                        {
                            throw new ArgumentOutOfRangeException($"An object of name {roObject.Name} already exists in type {typeof(tObject)}");
                        }
                        else
                        {
                            if (null == (oFCObj = (ICtObject)PrGetMgr<tObject>(roConfig)?.NewObject(1))) throw new Exception($"Can't get new {typeof(tObject)}");
                        }
                    }
                }
                else                     // Saving existing object
                {
                    if (null == (oFCObj = PrGet<tObject>(roConfig, roObject.ID, ACCESSFLAGS.OM_WRITE))) throw new Exception($"Can't open {typeof(tObject)} for writing");
                }
                _oLog.Debug($"Writing object {roObject.Name}");
                roObject.WriteInto(oFCObj, roMess);
                // Bugs on Reporting
                // oFCObj.IsObjectValid();
                ((ICtObjectManager)oFCObj.Manager).SaveObject(oFCObj);
                oFCObj.WriteUnlock();
                _oLog.Debug("Writen");
            }
        }
    }
}
