using System;
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
                    switch (cmd)
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
                            Console.WriteLine("{0} {1} {2} {3}", "wget".PadRight(10), "[URL]".PadRight(10), "[name]".PadRight(10), "(downloads file)");
                            Console.WriteLine("{0} {1} {2} {3}", "spy".PadRight(10), "[name]".PadRight(10), "  ".PadRight(10), "Displays file contents)");
                            break;
                        case "load":
                            var parts = cmd.Split(' ');
                            Console.WriteLine(db.Load(parts[0], parts[1], parts[2], parts[3]));
                            break;
                        case "save":
                            parts = cmd.Split(' ');
                            db.Save(parts[0], parts[1], parts[2], parts[3]); 
                            break;
                        case "ping":
                            var timesent = DateTime.UtcNow;
                            var timereceived= db.Ping(timesent);
                            Console.WriteLine("Ping: " +(timesent - timereceived).TotalMilliseconds);
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