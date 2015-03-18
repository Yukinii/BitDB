using System;
using System.IO;
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
        private static NetTcpBinding _binding;
        private static ChannelFactory<IBitDB> _factory;
        private static IBitDB _remoteDB;

        public RemoteDB(string username, string password)
        {
            KeepAlive.Elapsed += KeepAlive_Elapsed;
            KeepAlive.Start();
            _username = username;
            _password = password;
            if (!Connect())
                throw new UnauthorizedAccessException("Wrong User/Pass or Server offline");
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
            catch (Exception ex)
            {
                Connect();
                throw new InvalidDataException("The server might have rejected your request (Check InnerException)", ex);
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
            catch (Exception ex)
            {
                Connect();
                throw new InvalidDataException("The server might have rejected your request (Check InnerException)", ex);
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
            catch (Exception ex)
            {
                Connect();
                throw new InvalidDataException("The server might have rejected your request (Check InnerException)", ex);
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
            catch (Exception ex)
            {
                Connect();
                throw new InvalidDataException("The server might have rejected your request (Check InnerException)", ex);
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
            catch (Exception ex)
            {
                Connect();
                throw new InvalidDataException("The server might have rejected your request (Check InnerException)", ex);
            }
        }

        public async Task<string> ShellExecute(string command)
        {
            try
            {
                if (!_authenticated)
                    throw new UnauthorizedAccessException("Call Connect(username, pass) first!");

                var response = "";
                var split = command.SplitToArray();

                switch (split[0])
                {
                    case "ls":
                    case "dir":
                    {
                        response = await _remoteDB.ShellExecute(split[0] + " " + "\"" + _workingDirectory + "\"");
                        break;
                    }
                    case "cp":
                    case "mv":
                    case "download":
                    case "upload":
                    case "unzip":
                    {
                        response = await _remoteDB.ShellExecute(split[0] + " " + "\"" + Path.Combine(_workingDirectory, split[1]) + "\"" + " " + "\"" + Path.Combine(_workingDirectory, split[2]) + "\"");
                        break;
                    }
                    case "wget":
                    {
                        response = await _remoteDB.ShellExecute(split[0] + " " +split[1] + " " + "\"" + Path.Combine(_workingDirectory, split[2]) + "\"");
                        break;
                    }
                    case "mkdir":
                    case "rmdir":
                    case "rm":
                    case "spy":
                    {
                        if (split.Length > 1)
                            command = command.Remove(0, split[0].Length + 1);
                        response = await _remoteDB.ShellExecute(split[0] + " " + "\"" + Path.Combine(_workingDirectory, command.Replace("\"","")) + "\"");
                        break;
                    }
                    case "cd":
                    {
                        if (split.Length > 1)
                            command = command.Remove(0, split[0].Length + 1);
                        if (command != "..")
                            response = await _remoteDB.ShellExecute(split[0] + " " + "\"" + Path.Combine(_workingDirectory, command.Replace("\"", "")) + "\"");
                        else
                            response = await _remoteDB.ShellExecute(split[0] + " " + Directory.GetParent(_workingDirectory).FullName);
                        
                        if (response != "not found" && response != "access denied!")
                        {
                            if (_workingDirectory == response.Replace("\"",""))
                                return "access denied!";
                            _workingDirectory = response.Replace("\"", "");
                        }

                        response = "ok.";
                        break;
                    }
                }
                return response;
            }
            catch (Exception e)
            {
                if (e.Message == "The server did not provide a meaningful reply; this might be caused by a contract mismatch, a premature session shutdown or an internal server error.")
                    return "Overflow.";
                Connect();
                throw new InvalidDataException("The server might have rejected your request (Check InnerException)", e);
            }
        }

        /// <summary>
        /// Uploads a file to the repo.
        /// </summary>
        /// <param name="stream">Local file stream</param>
        /// <returns>Name of temp file. Use wget NAME DESTINATION to move it to the desired directory</returns>
        [Obsolete("Use UploadFileAsync(Stream, Name) instead!")]
        public async Task<string> UploadFile(Stream stream)
        {
            if (!_authenticated)
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            using (stream)
            {
                return await _remoteDB.UploadFile(stream);
            }
        }

        /// <summary>
        /// Uploads a file to the repo.
        /// </summary>
        /// <param name="stream">Local file stream</param>
        /// <param name="name">File name on the server</param>
        public async Task<string> UploadFile(Stream stream, string name)
        {
            if (!_authenticated)
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            using (stream)
            {
                var tempfile = await _remoteDB.UploadFile(stream);
                return await _remoteDB.ShellExecute("wget " + tempfile + " \"" + Path.Combine(_workingDirectory, name) + "\"");
            }
        }

        /// <summary>
        /// Uploads a file to the repo.
        /// </summary>
        /// <param name="file">Local file</param>
        /// <param name="name">File name on the server</param>
        public async Task<string> UploadFile(string file, string name)
        {
            if (File.Exists(file))
                throw new FileNotFoundException("File not found.");
            using (var s = File.OpenRead(file))
            {
                var tempfile = await _remoteDB.UploadFile(s);
                return await _remoteDB.ShellExecute("wget " + tempfile + " " + name + " " + _workingDirectory);
            }
        }

        public Stream DownloadFile(string name)
        {
            if (!_authenticated)
                throw new UnauthorizedAccessException("Call Connect(username, pass) first!");
            return _remoteDB.DownloadFile(Path.Combine(_workingDirectory, name));
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
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = 1024 * 1024 * 8,
                MaxConnections = 16,
                Security = security,
                ReaderQuotas = { MaxArrayLength = int.MaxValue, MaxBytesPerRead = int.MaxValue, MaxDepth = int.MaxValue, MaxNameTableCharCount = int.MaxValue, MaxStringContentLength = int.MaxValue },
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
            catch (Exception)
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
                _factory.Faulted -= Factory_Faulted;
                _factory = new ChannelFactory<IBitDB>(_binding, Endpoint);
                _factory.Faulted += Factory_Faulted;
                _factory.Closed += Factory_Closed;
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
                _factory.Closed -= Factory_Closed;
                _factory = new ChannelFactory<IBitDB>(_binding, Endpoint);
                _factory.Faulted += Factory_Faulted;
                _factory.Closed += Factory_Closed;
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