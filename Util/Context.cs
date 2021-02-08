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
using System.Globalization;
using System.Web;
using System.Threading;
using log4net;

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
    //      TODO: group all objects (managers, ...) in this class as methods to ensure disposal

    public class Context : IDisposable
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private class PrConnectionInfo
        {
            // Extending Tuple creates a struct, not a class, and reference semantic is needed to use as Key in a Dictionary
            // Thus we need to overload Equal, ==, != and GetHashCode

            // App crashes if a session is reused in another thread, thus add thread ID to the key

            private readonly (string, string, string, string, string, int) _oInfo;

            public string BrokerName            { get => _oInfo.Item1; }
            public string DatasourceName        { get => _oInfo.Item2; }
            public string DatasourcePassword    { get => _oInfo.Item3; }
            public string UserName              { get => _oInfo.Item4; }
            public string Password              { get => _oInfo.Item5; }
            public int    ThreadID              { get => _oInfo.Item6; }

            public PrConnectionInfo(string rsBrokerName, string rsDatasourceName, string rsDatasourcePassword, string rsUserName, string rsPassword)
            {
                _oInfo = (rsBrokerName, rsDatasourceName, rsDatasourcePassword, rsUserName, rsPassword, Thread.CurrentThread.ManagedThreadId);
            }

            // Reading params in header is not ideal, as queries can be replayed
            // TODO: use session token
            public PrConnectionInfo(HttpContextBase roContext)
            {
                System.Collections.Specialized.NameValueCollection oHead = roContext.Request.Headers;

                _oInfo =    (   oHead.Get("P001.ctstation.fr"), 
                                oHead.Get("P002.ctstation.fr"), 
                                oHead.Get("P003.ctstation.fr"),
                                oHead.Get("P004.ctstation.fr"),
                                oHead.Get("P005.ctstation.fr"),
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

        private readonly PrConnectionInfo _oKey;

        public readonly Language Language;

        public CultureInfo Culture { get => Language.Culture; }

        public CTCLIENTSERVERLib.lang_t WorkingLanguage { get => Language.WorkingLanguage;  }

        public List<string> GetActiveLanguages() => Language.GetActiveLanguages();

        public ConfigClass Config { get; }

        public Context(HttpContextBase roContext)
        {
            _oKey = new PrConnectionInfo(roContext);

            bool bFoundInCache = false;
            while (S_oCache.TryPop(_oKey, out ConfigClass oConfig))
            {
                // This test should prevent using a stalled connection
                if (oConfig.IsActive(_oKey.BrokerName, _oKey.DatasourceName, _oKey.UserName, _oKey.Password))
                {
                    Config = oConfig;
                    bFoundInCache = true;
                    _oLog.Debug($"Reusing {_oKey.BrokerName}_{_oKey.DatasourceName} {_oKey.UserName} in thread {_oKey.ThreadID}");
                    break;
                }
            }
            if (!bFoundInCache)
            {
                // Throws exceptions if needed
                _oLog.Debug($"Opening new config {_oKey.BrokerName}_{_oKey.DatasourceName} {_oKey.UserName} in thread {_oKey.ThreadID}");
                Config = new ConfigClass(_oKey.BrokerName, _oKey.DatasourceName, _oKey.DatasourcePassword, _oKey.UserName, _oKey.Password, "");
                _oLog.Debug("Connectong...");
                Config?.Connect();
                _oLog.Debug("Connected");
            }

            string sWorkingLanguageISO = roContext.Request.Headers.Get("P007.ctstation.fr");
            string sModifyedLanguagesISO = roContext.Request.Headers.Get("P008.ctstation.fr");
            Language = new Language(Config, sWorkingLanguageISO, sModifyedLanguagesISO);
            Config.Session.UserLanguage = Language.WorkingLanguage;
        }

        # region  Manager functions
        
        public List<tObject> GetAll<tObject>()      where tObject : ManagedObject, new() => Manager.GetAll<tObject>(this);

        public tObject Get<tObject>(int viID)       where tObject : ManagedObject, new() => Manager.Get<tObject>(this, viID);
        public tObject Get<tObject>(string vsName)  where tObject : ManagedObject, new() => Manager.Get<tObject>(this, vsName);

        public bool Exists<tObject>(int viID)       where tObject : ManagedObject, new() => Manager.Exists<tObject>(Config, viID);
        public bool Exists<tObject>(string vsName)  where tObject : ManagedObject, new() => Manager.Exists<tObject>(Config, vsName);

        public void Save<tObject>(tObject voObj)    where tObject : ManagedObject, new() => Manager.Save<tObject>(Config, voObj);

        #endregion


        // This is called by the using() pattern, as the class implements iDisposible
        public void Dispose()
        {
            if (!(Config is null) && !(_oKey is null))
            {
                // Get read of all the facade objects created, and their COM objects
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Returns the connection to the pool of available connections
                S_oCache.Push(_oKey, Config);
                _oLog.Debug($"Returned {_oKey.BrokerName}_{_oKey.DatasourceName} {_oKey.UserName} in thread {_oKey.ThreadID} to the pool of available connections");
            }
        }
    }
}