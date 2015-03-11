using System;
using System.Threading.Tasks;
using BitDB;

namespace Tester
{
    class Program
    {
        static void Main()
        {
            var db = new RemoteDB();
            if (db.Authenticate("Yuki", "lolmaster123"))
            {
                while (true)
                {
                    var cmd = Console.ReadLine();
                    Console.WriteLine(db.ShellExecute(cmd));
                }
            }
            Console.WriteLine("Lol nope.");
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
