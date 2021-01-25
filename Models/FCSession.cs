//
// Supports a cache of FC sessions
//  Create a new session with a constructor
//  When closing it, the connection to FC is not closed, but instead the session is added to a cache of opened connections
//  Opened connectionsshoul  closed after some time based on last usage date
//  The freshest connection is returned

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CTSWeb.Models
{
    public class FCSession
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

            S_ConnectionInfo(string rsBrokerName, string rsDatasourceName, string rsDatasourcePassword, string rsUserName, string rsPassword)
            {
                _brokerName         = rsBrokerName;
                _datasourceName     = rsDatasourceName;
                _datasourcePassword = rsDatasourcePassword;
                _userName           = rsUserName;
                _password           = rsPassword;
            }

            // No need to test for null when type is fixed
            public override bool Equals(S_ConnectionInfo roObj)
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
                if (roObj is S_ConnectionInfo) return this.Equals((S_ConnectionInfo)robj); else return false;
            }

            public static bool operator ==(S_ConnectionInfo ro1, S_ConnectionInfo ro2)
            {
                return ro1.Equals(ro2);
            }

            public static bool operator !=(S_ConnectionInfo ro1, S_ConnectionInfo ro2)
            {
                return !ro1.Equals(ro2);
            }
        }

        private static TimedCache<S_ConnectionInfo, > S_oCache = new Dictionary<string, FCSession>;;

        private string _userName;
        private string _password;
        private string _server;
        private FC
        public FCSession(HttpContext roContext)
        {
            string sConnectionID = S_ExtractConnectionID(roContext);
            if (S_oCache.ContainsKey(sConnectionID)) { return S_oCache[sConnectionID]} else { }
        }

        public void Close()
        {

        }
        
    }
}