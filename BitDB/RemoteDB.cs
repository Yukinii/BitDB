using System;
using System.ServiceModel;

namespace BitDB
{
    public class RemoteDB : IBitDB
    {
        private bool _authenticated;
        private static readonly NetTcpBinding Binding = new NetTcpBinding(SecurityMode.None);
        private static readonly EndpointAddress Endpoint = new EndpointAddress("net.tcp://79.133.51.71/BitDB");
        private static readonly ChannelFactory<IBitDB> Factory = new ChannelFactory<IBitDB>(Binding, Endpoint);
        private IBitDB _remoteDB;
        private string _username;
        private string _workingDirectory;


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

        public string Load(string file, string section, string key, string Default)
        {
            if (_authenticated && file.Contains(_workingDirectory))
                return _remoteDB.Load(file, section, key, Default);
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }

        public void Save(string file, string section, string key, string value)
        {
            if (_authenticated && file.Contains(_workingDirectory))
                _remoteDB.Save(file, section, key, value);
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }

        public string[] GetFiles(string path, string pattern, bool recursive)
        {
            if (_authenticated && path.Contains(_workingDirectory))
                return _remoteDB.GetFiles(path, pattern, recursive);
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }
        public bool CreateDirectory(string path)
        {
            if (_authenticated && path.Contains(_workingDirectory))
                return _remoteDB.CreateDirectory(_username,path);
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }

        public bool CreateFile(string path)
        {
            if (_authenticated && path.Contains(_workingDirectory))
                return _remoteDB.CreateFile(_username,path);
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }

        public bool Authenticate(string user, string pass)
        {
            _remoteDB = Factory.CreateChannel();
            _authenticated = _remoteDB.Authenticate(user, pass);
            _username = user;
            _workingDirectory = _remoteDB.GetPrivateFolderPath(user, pass);
            return _authenticated;
        }

        public string GetPrivateFolderPath(string user, string pass)
        {
            if (_authenticated)
                return _remoteDB.GetPrivateFolderPath(user, pass);
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }

        public string ShellExecute(string command)
        {
            if (_authenticated)
            {
                var response = _remoteDB.ShellExecute(command + " " + _workingDirectory);
                if (command.StartsWith("cd "))
                {
                    if (response != "not found")
                        _workingDirectory = response;
                }
                return response;
            }
            throw new UnauthorizedAccessException("Call Authenticate(username, pass) first!");
        }
    }
}