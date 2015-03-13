using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using System.Timers;
using BitDB.Interface;

namespace BitDB
{
    public class RemoteDB : IBitDB, IDisposable
    {
        private static bool _authenticated;
        private static string _username;
        private static string _password;
        private static string _workingDirectory;
        private static readonly Timer KeepAlive = new Timer(10000);
        private static readonly EndpointAddress Endpoint = new EndpointAddress("net.tcp://79.133.51.71/BitDB");
        private static NetTcpBinding _binding = new NetTcpBinding(SecurityMode.None);
        private static ChannelFactory<IBitDB> _factory = new ChannelFactory<IBitDB>(_binding, Endpoint);
        private static IBitDB _remoteDB;

        public RemoteDB(string username, string password)
        {
            KeepAlive.Elapsed += KeepAlive_Elapsed;
            KeepAlive.Start();
            _username = username;
            _password = password;
            if (!Connect())
                throw new UnauthorizedAccessException("Wrong user/pass");
        }

        private void KeepAlive_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Console.Title = "BitDB | " + _username == "" ? "~" : _username + "@db1.bitflash.xyz | Ping: " + _remoteDB.Ping(DateTime.UtcNow);
            }
            catch (Exception)
            {
                Connect();
            }
        }

        [Obsolete("Use CreateDirectory(path) instead.")]
        public bool CreateDirectory(string user, string path)
        {
            return CreateDirectory(path);
        }
        [Obsolete("Use CreateFile(path) instead.")]
        public bool CreateFile(string user, string path)
        {
            return CreateFile(path);
        }

        public int Ping(DateTime sent)
        {
            return _remoteDB.Ping(DateTime.UtcNow);
        }

        public string Load(string file, string section, string key, string Default)
        {
            try
            {
                if (_authenticated && file.Contains(_workingDirectory))
                    return _remoteDB.Load(file, section, key, Default);
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception)
            {
                Connect();
                return Load(file, section, key, Default);
            }
        }

        public void Save(string file, string section, string key, string value)
        {
            try
            {
                if (_authenticated && file.Contains(_workingDirectory))
                    _remoteDB.Save(file, section, key, value);
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception)
            {
                Connect();
                Save(file, section, key, value);
            }
        }

        public string[] GetFiles(string path, string pattern, bool recursive)
        {
            try
            {
                if (_authenticated && path.Contains(_workingDirectory))
                    return _remoteDB.GetFiles(path, pattern, recursive);
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception)
            {
                Connect();
                return GetFiles(path, pattern, recursive);
            }
        }
        public bool CreateDirectory(string path)
        {
            try
            {
                if (_authenticated && path.Contains(_workingDirectory))
                    return _remoteDB.CreateDirectory(_username,path);
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception)
            {
                Connect();
                return CreateDirectory(path);
            }
        }

        public bool CreateFile(string path)
        {
            try
            {
                if (_authenticated && path.Contains(_workingDirectory))
                    return _remoteDB.CreateFile(_username,path);
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception)
            {
                Connect();
                return CreateFile(path);
            }
        }
        public string GetPrivateFolderPath(string user, string pass)
        {
            try
            {
                if (_authenticated)
                    return _remoteDB.GetPrivateFolderPath(user, pass);
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception)
            {
                Connect();
                return GetPrivateFolderPath(user, pass);
            }
        }

        public async Task<string> ShellExecute(string command)
        {
            try
            {
                if (_authenticated)
                {
                    var response = await _remoteDB.ShellExecute(command + " " + _workingDirectory);
                    if (command.StartsWith("cd "))
                    {
                        if (response != "not found" && response != "access denied!")
                        {
                            if (_workingDirectory == response)
                                return "access denied!";
                            _workingDirectory = response;
                        }
                    }
                    return response;
                }
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            }
            catch (Exception e)
            {
                if (e.Message == "The server did not provide a meaningful reply; this might be caused by a contract mismatch, a premature session shutdown or an internal server error.")
                    return "Overflow.";
                Connect();
                return await ShellExecute(command);
            }
        }
        private bool Connect()
        {
            var security = new NetTcpSecurity { Mode = SecurityMode.TransportWithMessageCredential, Message = new MessageSecurityOverTcp {ClientCredentialType = MessageCredentialType.UserName}};
            _binding = new NetTcpBinding
            {
                CloseTimeout = TimeSpan.FromSeconds(300),
                ReceiveTimeout = TimeSpan.FromSeconds(300),
                SendTimeout = TimeSpan.FromSeconds(300),
                OpenTimeout = TimeSpan.FromSeconds(300),
                Security = security,
                TransferMode = TransferMode.Streamed
            };
            _factory = new ChannelFactory<IBitDB>(_binding, Endpoint);
            _factory.Closed += Factory_Closed;
            _factory.Faulted += Factory_Faulted;
            if (_factory.Credentials != null)
            {
                _factory.Credentials.UserName.UserName = _username;
                _factory.Credentials.UserName.Password = _password;
                _factory.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
                _factory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            }
            try
            {
                _remoteDB = _factory.CreateChannel();
                _workingDirectory = _remoteDB.GetPrivateFolderPath(_username, _password);
                _authenticated = true;
                return true;
            }
            catch
            {
                _authenticated = false;
                return false;
            }
        }

        private void Factory_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("Connection broke...");
            try
            {
                _factory = new ChannelFactory<IBitDB>(_binding, Endpoint);
                _remoteDB = _factory.CreateChannel();
                Console.WriteLine("Connection restored!");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void Factory_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("Connection closed...");
            try
            {
                _factory = new ChannelFactory<IBitDB>(_binding, Endpoint);
                _remoteDB = _factory.CreateChannel();
                Console.WriteLine("Connection restored!");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public void Dispose()
        {
            _factory.Close();
        }
    }
}