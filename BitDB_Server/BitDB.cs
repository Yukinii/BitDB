using System;
using System.IO;
using System.Text;
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

        public void Save(string file, string section, string key, string value)
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

        public string ShellExecute(string command)
        {
            var args = command.Split(' ');
            if (args.Length == 0)
                return "empty command";

            switch (args[0])
            {
                case "ls":// Linux command
                case "dir":
                {
                    try
                    {
                        var builder = new StringBuilder();
                        var dirs = Directory.GetDirectories(args[1]);
                        var files = Directory.GetFiles(args[1]);
                        long size = 0;
                        foreach (var directory in dirs)
                        {
                            var info = new DirectoryInfo(directory);
                            builder.AppendLine(string.Format("{0} {1} {2} {3}", info.CreationTime.ToShortDateString().PadRight(10), info.CreationTime.ToShortTimeString().PadRight(5), "<DIR>".PadRight(5), directory.Replace(args[1], "")));
                        }
                        foreach (var file in files)
                        {
                            var info = new FileInfo(file);
                            size += info.Length;
                            builder.AppendLine(string.Format("{0} {1} {2} {3}", info.CreationTime.ToShortDateString().PadRight(10), info.CreationTime.ToShortTimeString().PadRight(8), ((info.Length/1024) + "kb").PadRight(8), file.Replace(args[1], "")));
                        }
                        builder.AppendLine(files.Length + " File(s) \t " + size/1024 + "kbs");
                        builder.AppendLine(dirs.Length + " Dir(s) \t ");
                        return builder.ToString();
                    }
                    catch
                    {
                        return "access denied!";
                    }
                }
                case "cd":
                {
                    return Directory.Exists(args[1]) ? args[1] : "not found";
                }
                case "mkdir":
                {
                    if (Directory.Exists(args[1]))
                        return "directory exists.";
                    Directory.CreateDirectory(args[1]);
                    return "created!";
                }
                case "rmdir":
                {
                    if (!Directory.Exists(args[1]))
                        return "directory doesnt exist.";
                    Directory.Delete(args[1], true);
                    return "deleted!";
                }
                default:
                {
                    return "command not recognized!";
                }
            }
        }
    }
}