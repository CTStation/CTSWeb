﻿//
// Supports a cache of FC sessions
//  Create a new session with a constructor
//  When closing it, the connection to FC is not closed, but instead the session is added to a cache of opened connections
//  Opened connectionsshoul  closed after some time based on last usage date
//  The freshest connection is returned

using System;
using System.Web;

namespace CTSWeb.Models
{
    public class FCSession : IDisposable
    {
        private class S_ConnectionInfo
        {
            private string _brokerName;
            private string _datasourceName;
            private string _datasourcePassword;
            private string _userName;
            private string _password;

            public string BrokerName            { get => _brokerName; }
            public string DatasourceName        { get => _datasourceName; }
            public string DatasourcePassword    { get => _datasourcePassword; }
            public string UserName              { get => _userName; }
            public string Password              { get => _password; }

            public S_ConnectionInfo(string rsBrokerName, string rsDatasourceName, string rsDatasourcePassword, string rsUserName, string rsPassword)
            {
                _brokerName = rsBrokerName;
                _datasourceName = rsDatasourceName;
                _datasourcePassword = rsDatasourcePassword;
                _userName = rsUserName;
                _password = rsPassword;
            }

            public S_ConnectionInfo(HttpContextBase roContext)
            {
                System.Collections.Specialized.NameValueCollection oHead = roContext.Request.Headers;

                _brokerName             = oHead.Get("P001.ctstation.fr");
                _datasourceName         = oHead.Get("P002.ctstation.fr");
                _datasourcePassword     = oHead.Get("P003.ctstation.fr");
                _userName               = oHead.Get("P004.ctstation.fr");
                _password               = oHead.Get("P005.ctstation.fr");
            }

            // No need to test for null when type is fixed
            public bool Equals(S_ConnectionInfo roObj)
            {
                return      this._brokerName        == roObj._brokerName
                        && this._datasourceName     == roObj._datasourceName
                        && this._datasourcePassword == roObj._datasourcePassword
                        && this._userName           == roObj._userName
                        && this._password           == roObj._password
                        ;
            }

            public override bool Equals(Object roObj)
            {
                if (roObj is S_ConnectionInfo) return this.Equals((S_ConnectionInfo)roObj); else return false;
            }

            public override int GetHashCode()
            {
                Func<string, int> f = (string s) => (s is null) ? 0 : s.GetHashCode();

                return      ((long)(f(this._brokerName))     // Cast to long to avoid overflow
                        + f(this._datasourceName)
                        + f(this._datasourcePassword)
                        + f(this._userName)
                        + f(this._password)
                        ).GetHashCode();
            }

            public static bool operator ==(S_ConnectionInfo ro1, S_ConnectionInfo ro2) { return ro1.Equals(ro2); }

            public static bool operator !=(S_ConnectionInfo ro1, S_ConnectionInfo ro2) { return !ro1.Equals(ro2); }
        }

        private static TimedCache<S_ConnectionInfo, ConfigClass> S_oCache = 
                new TimedCache<S_ConnectionInfo, ConfigClass>(ConfigClass.Close);   // Closes unused connections after 5 minutes

        private S_ConnectionInfo _oInfo;
        private ConfigClass _oConfig;

        public ConfigClass Config { get => _oConfig; }

        public FCSession(HttpContextBase roContext)
        {
            _oInfo = new S_ConnectionInfo(roContext);
            ConfigClass oConfig = new ConfigClass();
            bool bFoundInCache = false;

            while (S_oCache.TryPop(_oInfo, out oConfig))
            {
                if (oConfig.IsActive(_oInfo.BrokerName, _oInfo.DatasourceName, _oInfo.UserName, _oInfo.Password))
                {
                    _oConfig = oConfig;
                    bFoundInCache = true;
                    break;
                }
            }
            if (!bFoundInCache)
            {
                // Throws exceptions if needed
                _oConfig = new ConfigClass(_oInfo.BrokerName, _oInfo.DatasourceName, _oInfo.DatasourcePassword, _oInfo.UserName, _oInfo.Password, "");
                if (!(_oConfig is null)) _oConfig.Connect();
            }
        }

        public void Dispose()
        {
            if (!(_oConfig is null) && !(_oInfo is null))
            {
                // Returns the connection to the pool of available connections
                S_oCache.Push(_oInfo, _oConfig);
            }
        }
    }
}