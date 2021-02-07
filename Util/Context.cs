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
        private class PrConnectionInfo
        {
            // Extending Tuple creates a struct, not a class, and reference semantic is needed to use as Key in a Dictionary
            // Thus we need to overload Equal, ==, != and GetHashCode
            private readonly (string, string, string, string, string) _oInfo;

            public string BrokerName            { get => _oInfo.Item1; }
            public string DatasourceName        { get => _oInfo.Item2; }
            public string DatasourcePassword    { get => _oInfo.Item3; }
            public string UserName              { get => _oInfo.Item4; }
            public string Password              { get => _oInfo.Item5; }

            public PrConnectionInfo(string rsBrokerName, string rsDatasourceName, string rsDatasourcePassword, string rsUserName, string rsPassword)
            {
                _oInfo = (rsBrokerName, rsDatasourceName, rsDatasourcePassword, rsUserName, rsPassword);
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
                                oHead.Get("P005.ctstation.fr")
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

        private readonly PrConnectionInfo _oInfo;

        public readonly Language Language;

        public CultureInfo Culture { get => Language.Culture; }

        public CTCLIENTSERVERLib.lang_t WorkingLanguage { get => Language.WorkingLanguage;  }

        public List<string> GetActiveLanguages() => Language.GetActiveLanguages();

        public ConfigClass Config { get; }

        public Context(HttpContextBase roContext)
        {
            _oInfo = new PrConnectionInfo(roContext);

            bool bFoundInCache = false;
                        while (S_oCache.TryPop(_oInfo, out ConfigClass oConfig))
            {
                // This test should prevent using a stalled connection
                if (oConfig.IsActive(_oInfo.BrokerName, _oInfo.DatasourceName, _oInfo.UserName, _oInfo.Password))
                {
                    Config = oConfig;
                    bFoundInCache = true;

                    break;
                }
            }
            if (!bFoundInCache)
            {
                // Throws exceptions if needed
                Config = new ConfigClass(_oInfo.BrokerName, _oInfo.DatasourceName, _oInfo.DatasourcePassword, _oInfo.UserName, _oInfo.Password, "");
                Config?.Connect();
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

        public void LoadFromFC<tObject>(tObject roObject, dynamic roFCObject) where tObject : ManagedObject, new()
        {
            Manager.LoadFromFC<tObject>(roObject, roFCObject, Language);
        }

        #endregion


        // This is called by the using() pattern, as the class implements iDisposible
        public void Dispose()
        {
            if (!(Config is null) && !(_oInfo is null))
            {
                // Get read of all the facade objects created, and their COM objects
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Returns the connection to the pool of available connections
                S_oCache.Push(_oInfo, Config);
            }
        }
    }
}