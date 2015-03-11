using System;
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
                Console.WriteLine("Your password is: " + db.Load(@"Y:\XioEmu\Database\Accounts\Yuki\Storage\AccountInfo.ini", "Account", "Password", "ERROR"));
                Console.WriteLine("Your Working dir is: " + db.GetPrivateFolderPath("Yuki","lolmaster123"));
            }
            else
            {
                Console.WriteLine("Lol nope.");
            }
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
