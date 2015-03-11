using System;
using System.IO;
using BitDB_Server.IO;

namespace BitDB_Server
{
    [Serializable]
    public class BitDB : IBitDB
    {
        public string Load(string file, string section, string key, string Default)
        {
            return Cache.CacheLookup(file).ReadString(section, key, Default);
        }

        public void Save(string file, string section, string key,string value)
        {
            Cache.CacheLookup(file).Write(section, key, value);
        }

        public string[] GetFiles(string path, string pattern, bool recursive)
        {
            return Directory.GetFiles(path, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        public bool CreateDirectory(string user, string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CreateFile(string user, string path)
        {
            try
            {
                File.Create(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Authenticate(string user, string pass)
        {
            var reader = new INI(@"Y:\XioEmu\Database\Accounts\" + user + @"\AccountInfo.ini");
            var password = reader.ReadString("Account", "Password", "");
            if (password == pass)
                return true;
            if (!string.IsNullOrEmpty(password))
                return false;
            reader.Write("Account", "Password", pass);
            return true;
        }

        public string GetPrivateFolderPath(string user, string pass)
        {
            return @"Y:\XioEmu\Database\Accounts\" + user + @"\Storage\";
        }
    }
}