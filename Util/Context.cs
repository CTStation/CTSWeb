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
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Threading;
using log4net;
using CTCLIENTSERVERLib;

namespace CTSWeb.Util
{
    // Supports a cache of FC contexts
    //      A context groups a session and language choice
    //      Create a new session with a constructor that gives connection parameters
    //      When closing it, the connection to FC is not closed, but instead the session is added to a cache of opened connections
    //      The cache is purged regularly to avoid burdening FC under too many opened sessions
    //      The last recently used connection is returned to avoid letting it root
    //      A livelihood test is done to avoid retrieving stalled connections
    //
    //      Should be used as using(Context(...)){} to be sure to call Dispose
    //      Created in each controller verb, so no language change is allowed
    //      All objects referencing a Context or a Contxet.Config should be disposed of before the context is Disposed


    public class Context : IDisposable
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private class PrConnectionInfo
        {
            // Extending Tuple creates a struct, not a class, and reference semantic is needed to use as Key in a Dictionary
            // Thus we need to overload Equal, ==, != and GetHashCode

            // App crashes if a session is reused in another thread, thus add thread ID to the key

            private readonly (string, string, string, string, string, int) _oInfo;

            public string BrokerName { get => _oInfo.Item1; }
            public string DatasourceName { get => _oInfo.Item2; }
            public string DatasourcePassword { get => _oInfo.Item3; }
            public string UserName { get => _oInfo.Item4; }
            public string Password { get => _oInfo.Item5; }
            public int ThreadID { get => _oInfo.Item6; }

            public PrConnectionInfo(string rsBrokerName, string rsDatasourceName, string rsDatasourcePassword, string rsUserName, string rsPassword)
            {
                _oInfo = (rsBrokerName, rsDatasourceName, rsDatasourcePassword, rsUserName, rsPassword, Thread.CurrentThread.ManagedThreadId);
            }

            // Reading params in header is not ideal, as queries can be replayed
            // TODO: use session token
            public PrConnectionInfo(NameValueCollection roColl)
            {
                string PrVoidIfNull(string vsKey)
                {
                    string s = roColl.Get(vsKey);
                    return (s is null) ? "" : s;
                }

                _oInfo = (PrVoidIfNull("P001.ctstation.fr"),
                                PrVoidIfNull("P002.ctstation.fr"),
                                PrVoidIfNull("P003.ctstation.fr"),
                                PrVoidIfNull("P004.ctstation.fr"),
                                PrVoidIfNull("P005.ctstation.fr"),
                                Thread.CurrentThread.ManagedThreadId
                            );
            }


            // No need to test for null when type is fixed
            public bool Equals(PrConnectionInfo roObj) => _oInfo == roObj._oInfo;

            public override bool Equals(object roObj) => roObj is PrConnectionInfo info && Equals(info);

            public override int GetHashCode() => _oInfo.GetHashCode();

            public static bool operator ==(PrConnectionInfo ro1, PrConnectionInfo ro2) { return ro1.Equals(ro2); }

            public static bool operator !=(PrConnectionInfo ro1, PrConnectionInfo ro2) { return !ro1.Equals(ro2); }
        }

        #region static class fields
        private static readonly TimedCache<PrConnectionInfo, ConfigClass> S_oCache =
                new TimedCache<PrConnectionInfo, ConfigClass>(ConfigClass.Close);   // Closes unused connections after 5 minutes
        #endregion

        private PrConnectionInfo _oKey;
        private bool _bHadFailedRequests = false;

        // Readonly is no longer an option when many constructors share a common function
        // Has to use set-only properties
        private ConfigClass _oConfig;
        private Language _oLanguage;

        public Language Language { get => _oLanguage; }

        public CultureInfo Culture { get => _oLanguage.Culture; }

        public CTCLIENTSERVERLib.lang_t WorkingLanguage { get => _oLanguage.WorkingLanguage; }

        //public List<string> GetActiveLanguages() => _oLanguage.GetActiveLanguages();

        public ConfigClass Config { get => _oConfig; }

        public bool HasFailedRquests { get => _bHadFailedRequests; set { _oLog.Debug("Context has a failed request"); _bHadFailedRequests = value; } }


        public Context(HttpContextBase roContext)
        {
            PrContext(roContext.Request.Headers);
        }

        // For tests
        public Context(NameValueCollection voColl)
        {
            PrContext(voColl);
        }

        private void PrContext(NameValueCollection voColl)
        {
            _oKey = new PrConnectionInfo(voColl);

            bool bFoundInCache = false;
            while (S_oCache.TryPop(_oKey, out ConfigClass oConfig))
            {
                // This test should prevent using a stalled connection TODO remove connection by thread or add it here
                if (oConfig.IsActive(_oKey.BrokerName, _oKey.DatasourceName, _oKey.UserName, _oKey.Password))
                {
                    _oConfig = oConfig;
                    bFoundInCache = true;
                    _oLog.Debug($"Reusing {_oKey.BrokerName}_{_oKey.DatasourceName} {_oKey.UserName} in thread {_oKey.ThreadID}");
                    break;
                }
            }
            if (!bFoundInCache)
            {
                // Throws exceptions if needed
                _oLog.Debug($"Opening new config {_oKey.BrokerName}_{_oKey.DatasourceName} {_oKey.UserName} in thread {_oKey.ThreadID}");
                _oConfig = new ConfigClass(_oKey.BrokerName, _oKey.DatasourceName, _oKey.DatasourcePassword, _oKey.UserName, _oKey.Password, "");
                _oLog.Debug("Connecting...");
                _oConfig?.Connect();
                _oLog.Debug("Connected");
            }

            string sWorkingLanguageISO = voColl.Get("P007.ctstation.fr");
            string sModifyedLanguagesISO = voColl.Get("P008.ctstation.fr");
            _oLanguage = new Language(Config, sWorkingLanguageISO, sModifyedLanguagesISO);
            _oConfig.Session.UserLanguage = Language.WorkingLanguage;
        }


        #region MessageList functions

        public MessageList NewMessageList()
        {
            return new MessageList(Culture.Name);
        }

        #endregion

        #region  Manager functions

        public List<tObject> GetAll<tObject>()                                      where tObject : ManagedObject, new() => Manager.GetAll<tObject>(this); 

        public tObject Get<tObject>(int viID)                                       where tObject : ManagedObject, new() => Manager.Get<tObject>(this, viID);
        public tObject Get<tObject>(string vsName)                                  where tObject : ManagedObject, new() => Manager.Get<tObject>(this, vsName);
        public tObject Get<tObject>(string vsID1, string vsID2)                     where tObject : ManagedObject, new() => Manager.Get<tObject>(this, vsID1, vsID2);

        public bool Exists<tObject>(int viID)                                       where tObject : ManagedObject, new() => Manager.Exists<tObject>(this, viID);
        public bool Exists<tObject>(string vsName)                                  where tObject : ManagedObject, new() => Manager.Exists<tObject>(this, vsName);
        public bool Exists<tObject>(string vsID1, string vsID2)                     where tObject : ManagedObject, new() => Manager.Exists<tObject>(this, vsID1, vsID2);

        public bool TryGet<tObject>(int viID, out tObject ro)                       where tObject : ManagedObject, new() => Manager.TryGet<tObject>(this, viID, out ro);
        public bool TryGet<tObject>(string vsName, out tObject ro)                  where tObject : ManagedObject, new() => Manager.TryGet<tObject>(this, vsName, out ro);
        public bool TryGet<tObject>(string vsID1, string vsID2, out tObject ro)     where tObject : ManagedObject, new() => Manager.TryGet<tObject>(this, vsID1, vsID2, out ro);

        public void Save<tObject>(tObject voObj, MessageList roMess)                where tObject : ManagedObject, new() => Manager.Save<tObject>(this, voObj, roMess);

        public void Execute<tObject>(Action<ICtObjectManager> voAction)             where tObject : ManagedObject, new() => Manager.Execute<tObject>(this, voAction);


        // Cache for RetTable values
        private readonly Dictionary<Dims, HashSet<string>> _oRefValues = new Dictionary<Dims, HashSet<string>>();

        public HashSet<string> GetRefValues(Dims viTableName)
        {
            HashSet<string> oRet;
            if (_oRefValues.ContainsKey(viTableName))
            {
                oRet = _oRefValues[viTableName];
            }
            else
            {
                oRet = Manager.GetRefValueCodes(this, viTableName);
                _oRefValues.Add(viTableName, oRet);
            }
            return oRet;
        }

        public Models.RefValue GetRefValue(Dims viTable, string vsName) => Manager.GetRefValue(this, viTable, vsName);

        public Models.RefValue GetRefValue(Dims viTable, int viID) => Manager.GetRefValue(this, viTable, viID);

        #endregion


        public static Predicate<string> GetPeriodValidator = (string s) =>
        {
            int i;
            return s.Length == 7 && Int32.TryParse(s.Substring(0, 4), out i) && (1900 <= i) && (i <= 2999) &&
                            s.Substring(4, 1) == "." &&
                            Int32.TryParse(s.Substring(5), out i) && (1 <= i) && (i <= 12);
        };


        // This is called by the using() pattern, as the class implements iDisposible
        public void Dispose()
        {
            // Get read of all the facade objects created, and their COM objects
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!(Config is null) && !(_oKey is null))      
            {
                if (!_bHadFailedRequests)           // Test for errors
                {
                    // Returns the connection to the pool of available connections
                    S_oCache.Push(_oKey, Config);
                    _oLog.Debug($"Returned {_oKey.BrokerName}_{_oKey.DatasourceName} {_oKey.UserName} in thread {_oKey.ThreadID} to the pool of available connections");
                }
                else
                {
                    Config.Disconnect();
                }
            }
        }
    }
}