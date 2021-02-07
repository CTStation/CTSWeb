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

    public enum TranslatableField
    {
        ShortDesc = 1,
        LongDesc = 2,
        XDesc = 4,
        Comment = 8,
        All = 15
    }

    public class TranslatedText
    {
        public string Culture;      // ISO culture code describes the language in a portable way
        public string ShortDesc;
        public string LongDesc;
        public string XDesc;
        public string Comment;
    }

    public class ManagedObject
    {
        // Needs a parameterless constructor that builds an empty object
        //      used to access the factory function
        // C# does not allow virtual constructors to state this fact, like virtual ManagedObject() { } should
        //  However, the new() constraint enforces it

        // Needs a factory to construct new object from a generic FC object
        // This will be called both for new and existing objects

        // Duplicating the working language in every object may seem wasteful
        // However, we would otherwise need a pointer to the session, that is equally wasteful
        private protected Language _oLanguage;

        public int ID;
        public string Name;
        public string LDesc;             // Ldesc in working language

        public virtual ManagedObject CreateFrom(ICtObjectBase roObject, Language roLang) { throw new NotImplementedException(); }

        public virtual void WriteInto(ICtObjectBase roObject) { throw new NotImplementedException(); }
    }


    public class ManagedObjectWithDesc : ManagedObject
    {
        public static int _iSupportedTranslatableFields;        // To implement in each class. Inheritance isn't supported

        public TranslatedText[] aoDescs;
    }

    public class ManagedObjectWithDescAndSecurity : ManagedObjectWithDesc
    {
        public int OwnerSite;
        public int OwnerWorkgroup;
        public int VisibilityMode;
        public DateTime CreationDate;
        public int Author;
        public DateTime UpdateDate;
        public int UpdateAuthor;
    }


    public static class Manager
    {
        #region Private
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Dictionary<Type, int> _oType2MgrID = new Dictionary<Type, int>();

        private static ICtObjectManager PrGetMgr<tObject>(ConfigClass roConfig) where tObject : ManagedObject, new()
        {
            ICtProviderContainer oContainer = (ICtProviderContainer)roConfig.Session;
            if (!_oType2MgrID.TryGetValue(typeof(tObject), out int iMgrID))
            {
                throw new ArgumentException($"Unregistered FC type '{typeof(tObject).Name}'");
            }
            ICtObjectManager oManager = (ICtObjectManager)oContainer.get_Provider(1, iMgrID);
            return oManager;
        }

        private static ICtObjectBase PrGet<tObject>(ConfigClass roConfig, int viID, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true)
            where tObject : ManagedObject, new()
        {
            ICtObjectManager oManager = PrGetMgr<tObject>(roConfig);
            ICtObjectBase oRet = oManager.GetObject(viID, viFlags, 0);
            if (vbRaiseErrorIfNotFound && oRet is null) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with ID '{viID}'");
            return oRet;
        }

        private static ICtObjectBase PrGet<tObject>(ConfigClass roConfig, string vsName, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true)
            where tObject : ManagedObject, new()
        {
            ICtObjectManager oManager = PrGetMgr<tObject>(roConfig);
            ICtOqlFactoryFacade oqlFactory = new CtOqlFactoryFacade();
            ICtOqlBooleanExpr oOql = oqlFactory.Equal(oqlFactory.Prop((int)ct_object_property.CT_NAME_PROP), oqlFactory.Value(vsName));
            ICtGenCollection oCollection = oManager.GetObjects(oOql, viFlags, 0, null);
            if (vbRaiseErrorIfNotFound && oCollection.Count == 0) throw new KeyNotFoundException($"No object of type { typeof(tObject).Name } found with name '{vsName}'");
            ICtObjectBase oRet = (oCollection.Count == 0) ? null : oCollection.GetAt(1);
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
            tObject oDumy = new tObject();

            ICtObjectManager oManager = PrGetMgr<tObject>(roContext.Config);
            CTCLIENTSERVERLib.ICtGenCollection oCollection = oManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);
            // New tObject(oObj) generates a compiler error (new can be called only with 0 argument
            foreach (ICtObjectBase oObj in oCollection)
                oRet.Add((tObject)oDumy.CreateFrom(oObj, roContext.Language));
            return oRet;
        }


        public static tObject Get<tObject>(Context roContext, int viID) where tObject : ManagedObject, new()
        {
            tObject oDumy = new tObject();

            return (tObject)oDumy.CreateFrom(PrGet<tObject>(roContext.Config, viID), roContext.Language);
        }


        public static tObject Get<tObject>(Context roContext, string vsName) where tObject : ManagedObject, new()
        {
            tObject oDumy = new tObject();

            return (tObject)oDumy.CreateFrom(PrGet<tObject>(roContext.Config, vsName), roContext.Language);
        }


        public static bool Exists<tObject>(ConfigClass roConfig, int viID) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roConfig, viID,ACCESSFLAGS.OM_READ, false) is null);


        public static bool Exists<tObject>(ConfigClass roConfig, string vsName) where tObject : ManagedObject, new()
            => !(PrGet<tObject>(roConfig, vsName, ACCESSFLAGS.OM_READ, false) is null);


        public static void Save<tObject>(ConfigClass roConfig, tObject roObject) where tObject : ManagedObject, new()
        {
            if (roObject != null)
            {
                // Get COM object to save
                ICtObjectBase oFCObj;
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
                            oFCObj = PrGetMgr<tObject>(roConfig).NewObject();
                        }
                    }
                }
                else                     // Saving existing object
                {
                    oFCObj = PrGet<tObject>(roConfig, roObject.ID, ACCESSFLAGS.OM_WRITE);
                }
                roObject.WriteInto(oFCObj);
                oFCObj.IsObjectValid();
                ((ICtObjectManager)oFCObj.Manager).SaveObject(oFCObj);
                oFCObj.WriteUnlock();
            }
        }

        public static void LoadFromFC<tObject>(ManagedObject roFacade, dynamic roFCObject, Language roLang)
        {
            roFacade.ID = roFCObject.ID;
            _oLog.Debug("Get ID");

            roFacade.Name = roFCObject.Name;
            _oLog.Debug($"Get Name {roFacade.Name}"); _oLog.Debug("Get ID");

            roFacade.LDesc = roFCObject.Desc[ct_desctype.ctdesc_long, roLang.WorkingLanguage];
            _oLog.Debug("Get main LDesc");
        }

        public static void LoadFromFC<tObject>(ManagedObjectWithDesc roFacade, dynamic roFCObject, Language roLang)
        {
            LoadFromFC<tObject>((ManagedObject)roFacade, roFCObject, roLang);
            roFacade.aoDescs = new TranslatedText[roLang.SupportedLanguages.Count];
            int iFields;
            // Access to generic type static fields is possible only through reflexion
            Type oType = roFacade.GetType();
            //FieldInfo oField = oType.GetField("_iSupportedTranslatableFields", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            FieldInfo oField = null;
            iFields = (int)((oField != null) ? oField.GetValue(null) : TranslatableField.All);

            int c = 0;
            foreach ((lang_t, string) oLanguage in roLang.SupportedLanguages)
            {
                roFacade.aoDescs[c] = new TranslatedText();
                roFacade.aoDescs[c].Culture = oLanguage.Item2;
                if ((iFields & (int)TranslatableField.Comment) != 0)
                {
                    roFacade.aoDescs[c].Comment = roFCObject.Desc[ct_desctype.ctdesc_comment, oLanguage.Item1];
                    _oLog.Debug("Get Comment");
                }
                if ((iFields & (int)TranslatableField.LongDesc) != 0)
                {
                    roFacade.aoDescs[c].LongDesc = roFCObject.Desc[ct_desctype.ctdesc_long, oLanguage.Item1];
                    _oLog.Debug("Get LDesc");
                }
                if ((iFields & (int)TranslatableField.ShortDesc) != 0)
                {
                    roFacade.aoDescs[c].ShortDesc = roFCObject.Desc[ct_desctype.ctdesc_short, oLanguage.Item1];
                    _oLog.Debug("Get SDesc");
                }
                if ((iFields & (int)TranslatableField.XDesc) != 0)
                {
                    roFacade.aoDescs[c].XDesc = roFCObject.Desc[ct_desctype.ctdesc_extralong, oLanguage.Item1];
                    _oLog.Debug("Get XDesc");
                }
                c++;
            }
        }

        public static void LoadFromFC<tObject>(ManagedObjectWithDescAndSecurity roFacade, dynamic roFCObject, Language roLang)
        {
            LoadFromFC<tObject>((ManagedObjectWithDesc)roFacade, roFCObject, roLang);
            roFacade.OwnerSite = roFCObject.OwnerSite;
            roFacade.OwnerWorkgroup = roFCObject.OwnerWorkgroup;
            roFacade.VisibilityMode = roFCObject.VisibilityMode;
            roFacade.CreationDate = roFCObject.CreationDate;
            roFacade.Author = roFCObject.Author;
            roFacade.UpdateDate = roFCObject.UpdateDate;
            roFacade.UpdateAuthor = roFCObject.UpdateAuthor;
            _oLog.Debug("Get Security");
        }

    }
}
