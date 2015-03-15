using System;
using System.IO;
using BitDB;

namespace Tester
{
    internal class Program
    {
        private static void Main()
        {
            Console.Title = "BitDB | ~@db1.bitflash.xyz";
            Console.Write("Please enter your username: ");
            var user = Console.ReadLine();
            Console.Write("Please enter your password: ");
            var pass = Console.ReadLine();
            Console.WriteLine("Logging in...");
            using (var db = new RemoteDB(user, pass))
            {
                Console.Title = "BitDB | "+ user + "@db1.bitflash.xyz";
                Console.WriteLine("Authenticated as "+user+" on db1.bitflash.xyz");
                Console.WriteLine("Type 'help' or '?' for a list of commands.");
                while (true)
                {
                    var cmd = Console.ReadLine();

                    if (string.IsNullOrEmpty(cmd))
                        continue;

                    var parts = cmd.Split(' ');
                    switch (parts[0])
                    {
                        case "cls":
                        case "clear":
                        case "cl":
                            Console.Clear();
                            break;
                        case "help":
                        case "?":
                            Console.WriteLine("{0} {1} {2} {3}", "ls|dir".PadRight(10), "   ".PadRight(10), "   ".PadRight(10), "(displays current folder contents)");
                            Console.WriteLine("{0} {1} {2} {3}", "mv".PadRight(10), "[src]".PadRight(10), "[dest]".PadRight(10), "(moves file)");
                            Console.WriteLine("{0} {1} {2} {3}", "cp".PadRight(10), "[src]".PadRight(10), "[dest]".PadRight(10), "(copies file)");
                            Console.WriteLine("{0} {1} {2} {3}", "mkdir".PadRight(10), "[name]".PadRight(10), " ".PadRight(10), "(creates directory)");
                            Console.WriteLine("{0} {1} {2} {3}", "rm".PadRight(10), "[file]".PadRight(10), "    ".PadRight(10), "(deletes file)");
                            Console.WriteLine("{0} {1} {2} {3}", "rmdir".PadRight(10), "[name]".PadRight(10), " ".PadRight(10), "(deletes directory)");
                            Console.WriteLine("{0} {1} {2} {3}", "wget".PadRight(10), "[URL]".PadRight(10), "[name]".PadRight(10), "(downloads file to the server)");
                            Console.WriteLine("{0} {1} {2} {3}", "spy".PadRight(10), "[name]".PadRight(10), "  ".PadRight(10), "Displays file contents)");
                            Console.WriteLine("{0} {1} {2} {3}", "download".PadRight(10), "[name]".PadRight(10), "[saveas name]".PadRight(10), "(downloads file to your pc)");
                            Console.WriteLine("{0} {1} {2} {3}", "upload".PadRight(10), "[FileStream]".PadRight(10), "[saveas name]".PadRight(10), "(uploads file from your pc)");
                            break;
                        case "load":
                            Console.WriteLine(db.Load(parts[0], parts[1], parts[2], parts[3]));
                            break;
                        case "save":
                            db.Save(parts[0], parts[1], parts[2], parts[3]); 
                            break;
                        case "ping":
                            var timereceived = db.Ping(DateTime.UtcNow);
                            Console.WriteLine("Ping: " + timereceived);
                            break;
                        case "download":
                            using (var writer = new FileStream(parts[2], FileMode.CreateNew))
                            {
                                Console.WriteLine("Downloading...");
                                db.DownloadFile(parts[1]).CopyTo(writer);
                            }
                            Console.WriteLine("Download finished!");
                            break;
                        case "upload":
                            if (File.Exists(parts[1]) && parts.Length == 3)
                            {
                                Console.WriteLine("Uploading...");
                                Console.WriteLine(db.UploadFile(File.OpenRead(parts[1]), parts[2]).Result);
                            }
                            break;
                        default:
                            Console.WriteLine(db.ShellExecute(cmd).Result);
                            break;
                    }
                }
            }
        }
    }
}