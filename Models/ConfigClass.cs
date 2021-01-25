using System;
using ApcomFrameworkConnectionLib;
using CTCLIENTSERVERLib;
using log4net;
using System.Xml;

namespace CTSWeb.Models
{
    public class ConfigClass
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string _brokerName;
        private string _datasourceName;
        private string _datasourcePassword;
        private string _userName;
        private string _password;
        private string _filePath;

        private ICtSession _session2;

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        public string BrokerName
        {
            get { return _brokerName; }
            set { _brokerName = value; }
        }

        public string DataSourceName
        {
            get { return _datasourceName; }
            set { _datasourceName = value; }
        }

        public string DatasourcePassword
        {
            get { return _datasourcePassword; }
            set { _datasourcePassword = value; }
        }

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public ICtSession Session
        {
            get { return _session2; }
        }


        
        public ConfigClass(string BrokerName, string DatasourceName, string DatasourcePassword, string UserName, string Password, string FilePath)
        {
            _brokerName = BrokerName;
            _datasourceName = DatasourceName;
            _datasourcePassword = DatasourcePassword;
            _userName = UserName;
            _password = Password;
            _filePath = FilePath;
        }

        public ConfigClass()
        {
            _brokerName = "";
            _datasourceName = "";
            _datasourcePassword = "";
            _userName = "";
            _password = "";
            _filePath = ".\\result.csv";
        }


        public string GetConnectionString()
        {
            // ManagedClientBroker brok = new ManagedClientBroker();
            // brok.Connect(this._brokerName);
            // brok.GetConnectionString(this._datasourceName, "");
            //"Provider=\"SQLNCLI11\";Data Source=\"SAPFC101DEV\";Initial Catalog=\"SAPFC\";User ID=\"sapfc\";Password=\"P@ssw0rd\";"

            //  string connectionString = brok.GetConnectionString(this._datasourceName, "");

            string connectionString;
            ApcomFrameworkConnectionLib.ClientBroker cb = new ClientBroker();
            System.IO.StringReader stringReader = null;
            XmlReader xmlReader = null;
            try
            {
                cb.ConnectToServerBroker(this._brokerName);
                stringReader = new System.IO.StringReader(cb.GetDataSourcesInfo());
                xmlReader = XmlReader.Create(stringReader);
                while (xmlReader.Read())
                {
                    if (xmlReader.Name.Equals("DataSourceInfo", StringComparison.InvariantCultureIgnoreCase) && xmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (xmlReader.GetAttribute("Name").Equals(this._datasourceName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            connectionString = cb.GetConnectionString(xmlReader.GetAttribute("Name"), "", this._datasourcePassword);
                            //return connectionString.Replace("MSDASQL", "HDBODBC32");
                            return connectionString.Substring(connectionString.IndexOf(";"), connectionString.Length - connectionString.IndexOf(";"));

                        }
                    }
                }
                throw new System.Collections.Generic.KeyNotFoundException("Unable to find datasource " + this._datasourceName);


            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (stringReader != null)
                    stringReader.Close();
                if (xmlReader != null)
                    xmlReader.Close();
                cb.DisconnectFromServerBroker();
            }



        }
        
        public bool IsActive(string rsBrokerName = "", string rsDatasourceName = "", string rsUserName = "", string rsPassword = "")
        {
            bool bRet = false;
            if (!(_session2 is null)) { bRet = _session2.IsLogged; }
            if (bRet) bRet = rsBrokerName is null || rsBrokerName == _brokerName;
            if (bRet) bRet = rsDatasourceName is null || rsDatasourceName == _datasourceName;
            if (bRet) bRet = rsUserName is null || rsUserName == _userName;
            if (bRet) bRet = rsPassword is null || rsPassword == _password;
            return bRet;
        }


        public void Connect()
        {
            CTCLIENTSERVERLib.ICtSessionCtx sessionContext = null;
            ICtSessionClient sessionClient = null;
            CtApplicationClientClass cacc = null;

            try
            {
                cacc = new CtApplicationClientClass();
                cacc.ConnectToServer(this._brokerName, this._datasourceName, new string[] { });
                _session2 = cacc.Logon(_userName, _password);
                cacc.Initialize(_session2);
                sessionClient = (ICtSessionClient)_session2;

            }
            catch (Exception)
            {
                try
                {
                    //un bug de l'API. On retrouve le même type de traitement que dans une classe d'un custom de SAP pour une version précédente.
                    cacc = new CtApplicationClientClass();
                    cacc.ConnectToServer(this._brokerName, this._datasourceName, new string[] { });
                    _session2 = cacc.Logon(_userName, _password);
                    cacc.Initialize(_session2);
                    sessionClient = (ICtSessionClient)_session2;


                }
                catch (Exception ex1)
                {
                    //  Console.WriteLine(ex1.Message);
                    throw new Exception("Cannot initialize BFC connection" + ex1.Message);
                }
            }

            try
            {
                CTCLIENTSERVERLib.ICtApplicationInfo inf = (CTCLIENTSERVERLib.ICtApplicationInfo)sessionClient.Application;
                System.Runtime.InteropServices.Marshal.ReleaseComObject(inf);
                sessionContext = (CTCLIENTSERVERLib.ICtSessionCtx)sessionClient;

                sessionContext.SetSessionCtx();

                sessionContext = null;
                //cacc.Uninitialize(true);

            }
            catch (Exception ex)
            {
                throw new Exception("Cannot set session context:" + ex.Message);
            }
            if (!_session2.IsLogged) throw new Exception("Cannot sign in into the data source : " + this._datasourceName);
        }

        public void Disconnect()
        {
            Close(this);
        }

        public static void Close(ConfigClass roCon) 
        { 
            try
            {
                if (roCon._session2 != null)
                {
                    if (roCon._session2.IsLogged)
                    {
                        /* realease context */
                        roCon._session2.Logout();
                    }
                    CTCLIENTSERVERLib.ICtSessionCtx sessionContext = (CTCLIENTSERVERLib.ICtSessionCtx)roCon._session2;
                    sessionContext.ReleaseSessionCtx(roCon._session2);
                    sessionContext = null;
                    /* dé-initialisation de l'appli */
                    // this.cacc3.Uninitialize(true);
                    //this.cacc3 = null;
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(roCon._session2);
                    roCon._session2 = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
            }
        }
    }
}
