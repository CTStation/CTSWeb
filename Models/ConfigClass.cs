using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApcomFrameworkConnectionLib;
using CTCLIENTSERVERLib;
using CTCLIENTSERVERPRIVATELib;
using Newtonsoft.Json;
using log4net;
using System.Xml;
using System.Security.Cryptography;
using System.Configuration;
using System.Reflection;
using System.IO;

namespace CTSREPOLIB
{
    public class ConfigClass
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string _brokerName;
        private string _datasourceName;
        private string _datasourcePassword;
        private string _userName;
        private string _password;
        private string _filePath;
        public string _currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
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
            get { return session2; }
        }



        private ICtSession session2;
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
                throw new KeyNotFoundException("Unable to find datasource " + this._datasourceName);


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
        public bool TestConnection()
        {

            CTCLIENTSERVERLib.ICtSessionCtx sessionContext = null;
            ICtSessionClient sessionClient = null;
            if (this.session2 != null)
            {
                if (this.session2.IsLogged)
                {
                    sessionClient = (ICtSessionClient)this.session2;
                    sessionContext = (CTCLIENTSERVERLib.ICtSessionCtx)sessionClient;

                    sessionContext.SetSessionCtx();

                    sessionContext = null;
                    return false;
                }

            }

            CtApplicationClientClass cacc = null;
            try
            {
                cacc = new CtApplicationClientClass();
                cacc.ConnectToServer(this._brokerName, this._datasourceName, new string[] { });
                this.session2 = cacc.Logon(_userName, _password);
                cacc.Initialize(this.session2);
                sessionClient = (ICtSessionClient)this.session2;

            }
            catch (Exception)
            {
                try
                {
                    //un bug de l'API. On retrouve le même type de traitement que dans une classe d'un custom de SAP pour une version précédente.
                    cacc = new CtApplicationClientClass();
                    cacc.ConnectToServer(this._brokerName, this._datasourceName, new string[] { });
                    this.session2 = cacc.Logon(_userName, _password);
                    cacc.Initialize(this.session2);
                    sessionClient = (ICtSessionClient)this.session2;


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
                cacc.Uninitialize(true);

            }
            catch (Exception ex)
            {
                throw new Exception("Cannot set session context:" + ex.Message);
            }
            if (!session2.IsLogged) throw new Exception("Cannot sign in into the data source : " + this._datasourceName);
            return true;
        }


        public bool Connect()
        {

            CTCLIENTSERVERLib.ICtSessionCtx sessionContext = null;
            ICtSessionClient sessionClient = null;
            if (this.session2 != null)
            {
                if (this.session2.IsLogged)
                {
                    sessionClient = (ICtSessionClient)this.session2;
                    sessionContext = (CTCLIENTSERVERLib.ICtSessionCtx)sessionClient;

                    sessionContext.SetSessionCtx();

                    sessionContext = null;
                    return false;
                }

            }

            CtApplicationClientClass cacc = null;
            try
            {
                cacc = new CtApplicationClientClass();
                cacc.ConnectToServer(this._brokerName, this._datasourceName, new string[] { });
                this.session2 = cacc.Logon(_userName, _password);
                cacc.Initialize(this.session2);
                sessionClient = (ICtSessionClient)this.session2;

            }
            catch (Exception)
            {
                try
                {
                    //un bug de l'API. On retrouve le même type de traitement que dans une classe d'un custom de SAP pour une version précédente.
                    cacc = new CtApplicationClientClass();
                    cacc.ConnectToServer(this._brokerName, this._datasourceName, new string[] { });
                    this.session2 = cacc.Logon(_userName, _password);
                    cacc.Initialize(this.session2);
                    sessionClient = (ICtSessionClient)this.session2;


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
            if (!session2.IsLogged) throw new Exception("Cannot sign in into the data source : " + this._datasourceName);
            return true;
        }
        public void Disconnect()
        {
            try
            {
                if (this.session2 != null)
                {
                    if (session2.IsLogged)
                    {
                        /* realease context */
                        session2.Logout();
                    }
                    CTCLIENTSERVERLib.ICtSessionCtx sessionContext = (CTCLIENTSERVERLib.ICtSessionCtx)session2;
                    sessionContext.ReleaseSessionCtx(this.session2);
                    sessionContext = null;
                    /* dé-initialisation de l'appli */
                    // this.cacc3.Uninitialize(true);
                    //this.cacc3 = null;
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(this.session2);
                    session2 = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);

            }

        }
        public bool SaveConfig()
        {
            log.Debug("Saving configuration file");
            try
            {

                System.IO.File.WriteAllText(AssemblyDirectory + "\\config", Encrypt(JsonConvert.SerializeObject(this), "4815162342"));


                //ConfigurationManager.AppSettings.Add("ConnectionString", Encrypt(JsonConvert.SerializeObject(this), "4815162342"));
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return false;
            }
            log.Debug("Configuration file saved properly");
            return true;
        }
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static ConfigClass LoadConfig()
        {

            log.Debug("Reading configuration file");
            ConfigClass tempClass;
            try
            {
                string file = Decrypt(System.IO.File.ReadAllText(AssemblyDirectory + "\\config"), "4815162342");
                tempClass = JsonConvert.DeserializeObject<ConfigClass>(file);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return null;
            }

            log.Debug("Configuration loaded");
            return tempClass;

        }

        public string Encrypt(string source, string key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = Encoding.UTF8.GetBytes(source);

            string encoded =
                Convert.ToBase64String(desCryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return encoded;
        }

        public static string Decrypt(string encodedText, string key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = Convert.FromBase64String(encodedText);

            string plaintext = Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return plaintext;
        }
    }
}
