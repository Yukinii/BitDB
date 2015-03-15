using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using BitDB_Server.Interface;

namespace BitDB_Server.IO
{
    [DataContract]
    public class BitDB : IBitDB
    {
        public int Ping(DateTime sentTime)
        {
            return (sentTime -DateTime.UtcNow).Milliseconds;
        }

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
            var reader = new INI(@"X:\BitDB\Users\" + user + @"\AccountInfo.ini");
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
            return @"Y:\BitDB\Users\" + user + @"\Storage\";
        }

        public async Task<string> ShellExecute(string command)
        {
            var args = command.Split(' ');
            if (args.Length == 0)
                return "empty command";

            await Task.Delay(0);
            switch (args[0])
            {
                case "ls": // Linux command
                case "dir":
                {
                    try
                    {
                            var path = command.Replace(args[0], "");
                        var builder = new StringBuilder();
                        var dirs = Directory.GetDirectories(path);
                        var files = Directory.GetFiles(path);
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
                        if (!command.Contains(".."))
                        {
                            command = command.Replace("cd ", "");
                            if (command != "..")
                            {
                                return Directory.GetParent(command).FullName.Contains(@"\Storage") ? command : "access denied!";
                            }
                        }
                        return Directory.GetParent(args[2]).FullName.Contains(@"\Storage") ? Directory.GetParent(args[2]).FullName : "access denied!";
                    }
                case "rm":
                {
                    if (!File.Exists(Path.Combine(args[2], args[1])))
                            return "not found!";
                    File.Delete(Path.Combine(args[2], args[1]));
                    return "deleted!";
                }
                case "mkdir":
                {
                    if (Directory.Exists(Path.Combine(args[2], args[1])))
                        return "directory exists.";
                    if (Directory.EnumerateDirectories(args[2]).Count() > 512)
                        return "too many directories (512)";
                    Directory.CreateDirectory(Path.Combine(args[2], args[1]));
                    return "created!";
                }
                case "rmdir":
                {
                    if (!Directory.Exists(Path.Combine(args[2], args[1])))
                        return "directory doesnt exist.";
                    Directory.Delete(Path.Combine(args[2], args[1]), true);
                    return "deleted!";
                }
                case "wget":
                    {
                        try
                        {
                            if (!File.Exists(Path.Combine(args[3], args[2])))
                            {
                                using (var client = new WebClient())
                                {
                                    File.WriteAllBytes(Path.Combine(args[3], args[2]), await client.DownloadDataTaskAsync(args[1]));
                                    try
                                    {
                                        File.Delete(args[1]);
                                    }
                                    catch
                                    {
                                        return "finished.";
                                    }
                                }
                            }
                            else
                                return "file exists.";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            return ex.Message;
                        }
                        return "finished.";
                    }
                case "cp":
                    {
                        if (File.Exists(Path.Combine(args[3], args[1])))
                        {
                            File.Copy(Path.Combine(args[3], args[1]), Path.Combine(args[3], args[2]));
                            return "copied.";
                        }
                        return "fail.";
                    }
                case "mv":
                    {
                        if (File.Exists(Path.Combine(args[3], args[1])))
                        {
                            File.Move(Path.Combine(args[3], args[1]), Path.Combine(args[3], args[2]));
                            return "copied.";
                        }
                        return "fail.";
                    }
                case "spy":
                    {
                        try
                        {
                            var builder = new StringBuilder();
                            builder.Append(File.ReadAllText(Path.Combine(args[2], args[1])));
                            return builder.ToString();
                        }
                        catch
                        {
                            return "file not found";
                        }
                    }
                case "unzip":
                {
                    if (File.Exists(Path.Combine(args[2],args[1])))
                    {
                        try
                        {
                            using (var archive = new ZipArchive(File.OpenRead(Path.Combine(args[2], args[1])), ZipArchiveMode.Read, false))
                            {
                                archive.ExtractToDirectory(Path.Combine(args[2], Path.GetFileNameWithoutExtension(args[1])));
                            }
                        }
                        catch (Exception ex)
                        {
                            return "something went wrong. ("+ex.Message+ ")";
                        }
                    }
                    else
                        return "file not found.";

                    return "done.";
                }
                default:
                {
                    return "command not recognized!";
                }
            }
        }

        public async Task<string> UploadFile(Stream stream)
        {
            var name = Path.GetRandomFileName();
            using (var writer = new FileStream(name, FileMode.CreateNew))
            {
                await stream.CopyToAsync(writer);
            }
            return name;
        }

        public Stream DownloadFile(string name)
        {
            if (File.Exists(name))
            {
                return File.Open(name, FileMode.Open);
            }
            throw new FileNotFoundException();
        }
    }
}