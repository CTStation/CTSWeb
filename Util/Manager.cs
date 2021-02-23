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
using System.Runtime.InteropServices;
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
	//  Get accesses by FC ID or by Name or by 2 strings (framework, reporting). Throws exception if not found
	//  Exists <==> Contains, also by ID or Name or 2 strings
	//	TryGet does the same but doesn't throw exception
	//  Save create or updates

	public enum Dims
	{
		Phase,
		Entity,
		Currency,
		FrameworkVersion,
		Account,
		Flow,
		Nature,
		Scope,
		Variant,
		ExRateType,
		ExRateVersion,
		UpdPer,
		Period
	}

	public class ManagedObject
	{
		private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		private static readonly HashSet<char> _oAllowedCharsInNames = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-".ToCharArray());

		private static bool PrIsValidName(string vsName, MessageList roMess)
		{
			bool bRet = 0 < vsName.Length && vsName.Length <= 12;

			if (bRet)
			{
				foreach (char i in vsName.ToCharArray())
				{
					if (!_oAllowedCharsInNames.Contains(i)) roMess.Add("RF0412", i, vsName);
				}
			}
			else
			{
				roMess.Add((vsName.Length == 0) ? "RF0413" : "RF0414", vsName);
			}
			return bRet;
		}

		// Needs a parameterless constructor that builds an empty object
		// C# does not allow virtual constructors to state this fact, like virtual ManagedObject() { } should
		//  However, the new() constraint enforces it

		// Needs a pair of functions to read from and write to a generic FC object
		// These will be called both for new and existing objects

		// Duplicating the language in every object may seem wasteful
		// However, we would otherwise need a pointer to the context, that is equally wasteful
		private string _sName;


		public int ID;
		public string Name { get { return _sName; } set { _sName = value.ToUpperInvariant(); } }
		public string LDesc;             // Ldesc in working language

		public virtual void ReadFrom(ICtObject roObject, Context roContext)
		{

			if (!(roObject is null))
			{
				ID = roObject.ID;
				Name = roObject.Name;
				LDesc = roObject.get_Desc(ct_desctype.ctdesc_long, roContext.Language.WorkingLanguage);
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



		public virtual bool Exists(Context roContext)
		{
			bool bRet;
			if (ID != 0)
			{
				bRet = Manager.TryGetFCObject(roContext, ID, GetType(), out _);
			}
			else
			{
				if (String.IsNullOrEmpty(Name))
				{
					bRet = false;
				}
				else
				{
					bRet = Manager.TryGetFCObject(roContext, Name, GetType(), out _);
				}
			}
			return bRet;
		}


		public virtual bool IsValid(Context roContext, MessageList roMess)
		{
			bool bRet = false;
			Type oType = this.GetType();

			// Access to generic type static fields is possible only through reflexion
			FieldInfo oField = oType.GetField("_bDontSaveName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

			if (ID == 0)            // New object
			{
				if (Name is null)
				{
					roMess.Add("RF0410", oType.Name);
				}
				else
				{
					if (Exists(roContext))
					{
						roMess.Add("RF0411", Name, oType.Name);
					}
					else
					{
						bRet = !(oField is null) ? true : PrIsValidName(Name, roMess);
					}
				}
			}
			else                    // Saving existing object
			{
				bRet = Manager.TryGetFCObject(roContext, ID, oType, out _);           // Name can be different, allow changing the name && oFCObj.Name == Name;
				if (bRet)
				{
					if (oField is null) bRet = PrIsValidName(Name, roMess);             // Overkill if name is not changed
				}
				else
				{
					roMess.Add("RF0415", oType.Name, ID);
				}
			}
			return bRet;
		}
	}


	// Set descriptions to empty string to force an empty desc. Null means 'do not change what's already there'
	public class ManagedObjectWithDesc : ManagedObject
	{
		private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LanguageText[] Descriptions;


		public string GetDesc(string vsCulture, LanguageText.Type viType)
		{
			bool bNotFound = true;
			string sRet = null;
			string sCode = LanguageText.GetCode(viType);

			foreach (LanguageText o in Descriptions)
			{
				if (o.CultureName == vsCulture) {
					if (o.Texts.ContainsKey(sCode)) sRet = o.Texts[sCode];
					bNotFound = false;
					break;
				}
			}
			if (bNotFound) throw new KeyNotFoundException($"Unsupported language '{vsCulture}'");
			return sRet;
		}


		public void SetDesc(string vsCulture, LanguageText.Type viType, string vsValue)
		{
			bool bNotFound = true;
			string sCode = LanguageText.GetCode(viType);

			foreach (LanguageText o in Descriptions)
			{
				if (o.CultureName == vsCulture) {
					o.Texts[sCode] = vsValue; // Creates or updates
					bNotFound = false;
					break;
				}
			}
			if (bNotFound) throw new KeyNotFoundException($"Unsupported language '{vsCulture}'");
		}


		public override void ReadFrom(ICtObject roObject, Context roContext)
		{
			base.ReadFrom(roObject, roContext);

			Descriptions = new LanguageText[roContext.Language.SupportedLanguages.Count];

			// Access to generic type static fields is possible only through reflexion
			Type oType = this.GetType();
			FieldInfo oField = oType.GetField("_iSupportedTranslatableFields", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			int iFields = (int)((oField != null) ? oField.GetValue(null) : LanguageMasks.All);

			int c = 0;
			string s;
			foreach ((lang_t, string) oLanguage in roContext.Language.SupportedLanguages)
			{
				Descriptions[c] = new LanguageText(oLanguage.Item1, roContext.Language);
				if (!(roObject is null))
				{
					if (Descriptions[c].CultureName != oLanguage.Item2) _oLog.Debug($"Invalid culture name: expected '{Descriptions[c].CultureName}' and found '{oLanguage.Item2}'");
					foreach (var o in LanguageText.TypeInfo)
					{
						s = Language.Description(roObject, o.Item2, oLanguage.Item1);
						if (((iFields & (int)o.Item1) != 0) && (!(s is null))) Descriptions[c].Texts[o.Item3] = s;
					}
				}
				c++;
			}
			_oLog.Debug((roObject is null) ? $"Null BFC object loaded into managed object with desc {Name}" : $"Loaded descriptions in {c} language(s) into managed object with desc {Name}");
		}

		public override void WriteInto(ICtObject roObject, MessageList roMess, Context roContext)
		{
			base.WriteInto(roObject, roMess, roContext);
			bool bTest = !(roMess is null);
			string sOld;
			string sNew;

			foreach (LanguageText oText in Descriptions)
			{
				if (roContext.Language.TryGetLanguageID(oText.CultureName, out lang_t iLang))
				{
					foreach (var o in LanguageText.TypeInfo)
					{
						if (oText.Texts.ContainsKey(o.Item3))
						{
							sOld = Language.Description(roObject, o.Item2, iLang);
							sNew = oText.Texts[o.Item3];
							if (bTest && (!(sNew is null)) && (sOld != sNew)) roMess.Add("RF0310", o.Item3 + ((int)iLang).ToString(), sNew, sOld);
							if (!(sNew is null)) Language.SetDesc(roObject, o.Item2, iLang, sNew);
						}
					}
				} // No else: if language isn't found or active, ignore
			}
		}

		public override bool IsValid(Context roContext, MessageList roMess)
		{
			return base.IsValid(roContext, roMess);
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

		public override void ReadFrom(ICtObject roObject, Context roContext)
		{
			base.ReadFrom(roObject, roContext);

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

		public override bool IsValid(Context roContext, MessageList roMess)
		{
			return base.IsValid(roContext, roMess);
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

		private static readonly Dictionary<Dims, PrDimensionAccess> _oCode2DimAccess = new Dictionary<Dims, PrDimensionAccess>();

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
			// Edit Dis enum to add dimensions
			// Give access mode to ref tables or dimensions. Stores a PrDimensionAccess
			Dictionary<Dims, (bool, int, int)> oAccess = new Dictionary<Dims, (bool, int, int)>()
			{
				{ Dims.Phase,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_PHASE, 0) },
				{ Dims.Entity,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_ENTITY, 0) },
				{ Dims.Currency,			(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_CURRENCY, 0) },
				{ Dims.FrameworkVersion,	(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_FRAMEWORKVERSION, 0) },
				{ Dims.Account,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_ACCOUNT, 0) },
				{ Dims.Flow,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_FLOW, 0) },
				{ Dims.Nature,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_NATURE, 0) },
				{ Dims.Scope,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_SCOPE_CODE, 0) },
				{ Dims.Variant,				(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_VARIANT, 0) },
				{ Dims.ExRateType,			(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_EXRATETYPE, 0) },
				{ Dims.ExRateVersion,		(true, (int)CTCOMMONMODULELib.ct_refvalue_managers.REFVALUEMANAGER_EXRATEVERSION, 0) },
				{ Dims.UpdPer,				(false, 0, (int)CTCOMMONMODULELib.ct_dimension.DIM_UPDPER) },
				{ Dims.Period,				(false, 0, (int)CTCOMMONMODULELib.ct_dimension.DIM_PERIOD) },
			};

			foreach (Dims iDim in oAccess.Keys)
			{
				PrDimensionAccess o = new PrDimensionAccess(oAccess[iDim]);
				_oCode2DimAccess.Add(iDim, o);
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

		// Has to add the Type argument for TryGetFCObject
		private static ICtObjectManager PrGetMgr<tObject>(Context roContext, Type voT = null) where tObject : ManagedObject, new()
		{
			ICtProviderContainer oContainer = (ICtProviderContainer)roContext.Config.Session;
			Type oType = (voT is null) ? typeof(tObject) : voT;
			if (!_oType2MgrID.TryGetValue(oType, out int iMgrID))
			{
				// Maybe the class wasn't initialized. So try creating an object to ensure class init
				// This won't work in TryGetFCObject, but hopefully it should not happen at that time
				tObject oDummy = new tObject();
				if (!_oType2MgrID.TryGetValue(oType, out iMgrID)) throw new ArgumentException($"Unregistered FC type '{typeof(tObject).Name}'");
			}
			ICtObjectManager oManager = null;
			COMMonitor(roContext, () => oManager = (ICtObjectManager)oContainer.get_Provider(1, iMgrID));
			return oManager;
		}

		// Has to add the Type argument for TryGetFCObject
		private static ICtObject PrGet<tObject>(Context roContext, int viID, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true, int viMgrID = 0, Type voT = null)
			where tObject : ManagedObject, new()
		{
			ICtObject oRet = null;
			ICtObjectManager oManager = null;
			Type oType = (voT is null) ? typeof(tObject) : voT;
			COMMonitor(roContext, () => {
				oManager = (viMgrID == 0) ? PrGetMgr<tObject>(roContext, oType) : (ICtObjectManager)(((ICtProviderContainer)roContext.Config.Session).get_Provider(1, viMgrID));
				oRet = (ICtObject)oManager.GetObject(viID, viFlags, 0);
			});
			if (vbRaiseErrorIfNotFound && oRet is null) throw new KeyNotFoundException($"No object of type { oType.Name } found with ID '{viID}'");
			return oRet;
		}

		// Has to add the Type argument for TryGetFCObject
		private static ICtObject PrGet<tObject>(Context roContext, string vsName, ACCESSFLAGS viFlags = ACCESSFLAGS.OM_READ, bool vbRaiseErrorIfNotFound = true, int viMgrID = 0, Type voT = null)
			where tObject : ManagedObject, new()
		{
			ICtObject oRet = null;
			ICtObjectManager oManager = null;
			Type oType = (voT is null) ? typeof(tObject) : voT;
			COMMonitor(roContext, () => {
				oManager = (viMgrID == 0) ? PrGetMgr<tObject>(roContext, oType) : (ICtObjectManager)(((ICtProviderContainer)roContext.Config.Session).get_Provider(1, viMgrID));
				ICtOqlFactoryFacade oqlFactory = new CtOqlFactoryFacadeClass();
				ICtOqlBooleanExpr oOql = oqlFactory.Equal(oqlFactory.Prop((int)ct_object_property.CT_NAME_PROP), oqlFactory.Value(vsName));
				ICtGenCollection oCollection = null;
				try {
					oCollection = oManager.GetObjects(oOql, viFlags, 0, null);
				} catch (System.Runtime.InteropServices.COMException e) {
					_oLog.Debug($"Object '{vsName}' not found: {e}");
				}
				if (vbRaiseErrorIfNotFound && (oCollection is null)) throw new KeyNotFoundException($"No object of type { oType.Name } found with name '{vsName}'");
				oRet = (oCollection is null) ? null : (ICtObject)oCollection.GetAt(1);
			});
			if (vbRaiseErrorIfNotFound && oRet is null) throw new ArgumentNullException($"Object of type { oType.Name } found with name '{vsName}' is null");
			return oRet;
		}


		// Has to add the Type argument for TryGetFCObject
		private static ICtObject PrGet<tObject>(Context roContext, string vsID1, string vsID2, bool vbRaiseErrorIfNotFound = true, Type voT = null)
			where tObject : ManagedObject, new()
		{
			ICtObject oRet = null;
			Type oType = (voT is null) ? typeof(tObject) : voT;
			ICtObjectManager oManager = PrGetMgr<tObject>(roContext, oType);
			if (!_oType2Delegate.TryGetValue(oType, out var oDelegate))
			{
				// Init is done by previous call to PrGetMgr
				throw new ArgumentException($"FC type '{oType.Name}' does not support identification with 2 names");
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
			if (vbRaiseErrorIfNotFound && (oRet is null)) throw new KeyNotFoundException($"No object of type { oType.Name } found with criteria '{vsID1}' and '{vsID2}'");
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
					o.ReadFrom(oFCObj, roContext);
					oRet.Add(o);
				}
			});
			return oRet;
		}



		// Get throws exception if not found
		public static tObject Get<tObject>(Context roContext, int viID) where tObject : ManagedObject, new()
		{
			ICtObject oFCObj = PrGet<tObject>(roContext, viID);
			tObject oRet = new tObject();
			oRet.ReadFrom(oFCObj, roContext);
			return oRet;
		}

		public static tObject Get<tObject>(Context roContext, string vsName) where tObject : ManagedObject, new()
		{
			ICtObject oFCObj = PrGet<tObject>(roContext, vsName);
			tObject oRet = new tObject();
			oRet.ReadFrom(oFCObj, roContext);
			return oRet;
		}

		public static tObject Get<tObject>(Context roContext, string vsID1, string vsID2) where tObject : ManagedObject, new()
		{
			ICtObject oFCObj = PrGet<tObject>(roContext, vsID1, vsID2);
			tObject oRet = new tObject();
			oRet.ReadFrom(oFCObj, roContext);
			return oRet;
		}



		// TryGet does not throw exception
		public static bool TryGet<tObject>(Context roContext, int viID, out tObject roRet) where tObject : ManagedObject, new()
		{
			ICtObject oFCObj = PrGet<tObject>(roContext, viID, ACCESSFLAGS.OM_READ, false);
			bool bRet = !(oFCObj is null);
			if (bRet)
			{
				roRet = new tObject();
				roRet.ReadFrom(oFCObj, roContext);
			}
			else
			{
				roRet = null;
			}
			return bRet;
		}

		public static bool TryGet<tObject>(Context roContext, string vsName, out tObject roRet) where tObject : ManagedObject, new()
		{
			ICtObject oFCObj = PrGet<tObject>(roContext, vsName, ACCESSFLAGS.OM_READ, false);
			bool bRet = !(oFCObj is null);
			if (bRet)
			{
				roRet = new tObject();
				roRet.ReadFrom(oFCObj, roContext);
			}
			else
			{
				roRet = null;
			}
			return bRet;
		}

		public static bool TryGet<tObject>(Context roContext, string vsID1, string vsID2, out tObject roRet) where tObject : ManagedObject, new()
		{
			ICtObject oFCObj = PrGet<tObject>(roContext, vsID1, vsID2, false);
			bool bRet = !(oFCObj is null);
			if (bRet)
			{
				roRet = new tObject();
				roRet.ReadFrom(oFCObj, roContext);
			}
			else
			{
				roRet = null;
			}
			return bRet;
		}



		public static bool Exists<tObject>(Context roContext, int viID) where tObject : ManagedObject, new()
			=> !(PrGet<tObject>(roContext, viID,ACCESSFLAGS.OM_READ, false) is null);

		public static bool Exists<tObject>(Context roContext, string vsName) where tObject : ManagedObject, new()
			=> !(PrGet<tObject>(roContext, vsName, ACCESSFLAGS.OM_READ, false) is null);

		public static bool Exists<tObject>(Context roContext, string vsID1, string vsID2) where tObject : ManagedObject, new()
			=> !(PrGet<tObject>(roContext, vsID1, vsID2, false) is null);



		// Helper function for IsValid, as Exist<> can't know at compile time the type of the calling inherited class
		// Sets the object to null if not found
		public static bool TryGetFCObject(Context roContext, int viID, Type voT, out ICtObject roFCObj)
		{
			if (voT is null) throw new ArgumentNullException();
			roFCObj = PrGet<ManagedObject>(roContext, viID, ACCESSFLAGS.OM_READ, false, 0, voT);
			return !(roFCObj is null);
		}
		public static bool TryGetFCObject(Context roContext, string vsName, Type voT, out ICtObject roFCObj)
		{
			if (voT is null) throw new ArgumentNullException();
			roFCObj = PrGet<ManagedObject>(roContext, vsName, ACCESSFLAGS.OM_READ, false, 0, voT);
			return !(roFCObj is null);
		}
		public static bool TryGetFCObject(Context roContext, string vsID1, string vsID2, Type voT, out ICtObject roFCObj)
		{
			if (voT is null) throw new ArgumentNullException();
			roFCObj = PrGet<ManagedObject>(roContext, vsID1, vsID2, false, voT);
			return !(roFCObj is null);
		}



		// TODO renumber messages
		public static void Save<tObject>(Context roContext, tObject roObject, MessageList roMess) where tObject : ManagedObject, new()
		{
			if (roObject != null)
			{
				if (roObject.IsValid(roContext, roMess))
				{
					bool bStop = roContext.HasFailedRquests;
					bool bNeedsUnlock = false;
					ICtObject oFCObj = null;               // Get COM object to save

					if (!bStop)
					{
						if (roObject.ID == 0)   // New object
						{
							COMMonitor(roContext, () => oFCObj = (ICtObject)PrGetMgr<tObject>(roContext)?.NewObject(1));
							if (oFCObj is null)
							{
								roMess.Add("RF0450", roObject.GetType().Name);
								bStop = true;
							}
						}
						else                    // Saving existing object
						{
							try
							{
								COMMonitor(roContext, () => oFCObj = PrGet<tObject>(roContext, roObject.ID, ACCESSFLAGS.OM_WRITE));      // IsValid already checked for existence
								bNeedsUnlock = true;
							} catch (UnauthorizedAccessException e)
							{
								_oLog.Debug($"Exception {e} while opening for writing");
							}
							if (oFCObj is null)
							{
								roMess.Add("RF0451", roObject.Name, roObject.GetType().Name);
								bStop = true;
							}
						}
					}
					else
					{
						roMess.Add("RF0452");
					}

					bStop = bStop || roContext.HasFailedRquests;
					if (!bStop)
					{
						_oLog.Debug($"Writing object {roObject.Name}");
						COMMonitor(roContext, () => roObject.WriteInto(oFCObj, roMess, roContext));
						_oLog.Debug($"Writen object {roObject.Name}");
					}

					bStop = bStop || roContext.HasFailedRquests;
					if (!bStop)
					{
						try { oFCObj.IsObjectValid(); }
						catch (COMException e)
						{
							roMess.Add("RF0311", roObject.GetType().Name, roObject.Name);
							_oLog.Debug($"Error {e} in validating {roObject.Name}");
						}
					}

					bStop = bStop || roContext.HasFailedRquests;
					if (!bStop)
					{
						try
						{
							((dynamic)(oFCObj.Manager)).SaveObject(oFCObj);
							//PrGetMgr<tObject>(roContext).SaveObject(oFCObj);
							_oLog.Debug("Saved object {roObject.Name}");
						}
						catch (COMException e)
						{
							roMess.Add("RF0312", e.Message, roObject.Name, roObject.GetType().Name);
							_oLog.Debug($"Error {e} in saving {roObject.Name}");
							roContext.HasFailedRquests = true;
						}
					}

					if (bNeedsUnlock)
					{
						COMMonitor(roContext, () => oFCObj.WriteUnlock());
						_oLog.Debug($"Released {roObject.Name}");
					}
				} // No else: if object knows it's not valid, no need to try and save it. IsValid already built up messages
			} // No else, null objects don't need saving
		}



		public static HashSet<string> GetRefValueCodes(Context roContext, Dims viTableCode)
		{
			HashSet<string> oRet = new HashSet<string>();
			if (!_oCode2DimAccess.TryGetValue(viTableCode, out PrDimensionAccess oDimAccess))
			{
				throw new ArgumentException($"Unrecognized FC table '{viTableCode}'");
			}
			else
			{
				if (!oDimAccess.HasRefTable)
				{
					throw new ArgumentException($"{viTableCode} does not have a list of elements");
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



		public static Models.RefValue GetRefValue(Context roContext, Dims viTableCode, string vsName)
		{
			Models.RefValue oRet = null;
			CTCORELib.ICtRefValue oRefVal = null;
			if (!_oCode2DimAccess.TryGetValue(viTableCode, out PrDimensionAccess oDimAccess))
			{
				throw new ArgumentException($"Unrecognized FC table '{viTableCode}'");
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
				oRet.ReadFrom(oRefVal, roContext);
			}
			return oRet;
		}


		public static Models.RefValue GetRefValue(Context roContext, Dims viTableCode, int? viID)
		{
			Models.RefValue oRet = null;

			if (!(viID is null)) 
			{
				CTCORELib.ICtRefValue oRefVal = null;
				if (!_oCode2DimAccess.TryGetValue(viTableCode, out PrDimensionAccess oDimAccess))
				{
					throw new ArgumentException($"Unrecognized FC table '{viTableCode}'");
				}
				else
				{
					ICtProviderContainer oContainer = (ICtProviderContainer)roContext.Config.Session;
					COMMonitor(roContext, () =>
					{
						if (oDimAccess.HasRefTable)
						{
							CTCORELib.ICtRefValueManager oManager = (CTCORELib.ICtRefValueManager)oContainer.get_Provider(1, oDimAccess.RefTableManagerID);
							oRefVal = oManager.RefValue[(int)viID];
						}
						else
						{
							CTCORELib.ICtDimensionManager oDimManager = (CTCORELib.ICtDimensionManager)(oContainer.get_Provider(1, (int)CTCORELib.ct_core_manager.CT_DIMENSION_MANAGER));
							CTCORELib.ICtDimension oDim = oDimManager.get_Dimension(oDimAccess.DimensionId);
							oRefVal = oDim.RefValue[(int)viID];
						}
					});
				}
				if (!(oRefVal is null))
				{
					oRet = new Models.RefValue();
					oRet.ReadFrom(oRefVal, roContext);
				}
			}
			return oRet;
		}
	}
}
