using System;
using System.ServiceModel;
using BitDB;

namespace BitDB_Server
{
    class Program
    {
        public static ServiceHost Host;
        public static NetTcpBinding Binding;
        public static readonly NetTcpSecurity ServiceSecurity = new NetTcpSecurity();
        static void Main()
        {
            Console.Write("Starting...");
            while (!Start())
            {
                Console.Write(".");
            }
            Console.WriteLine();
            Console.WriteLine("Started!");
            while (true)
            {
                Console.ReadLine();
            }
        }

        public static bool Start()
        {
            try
            {
                ServiceSecurity.Mode = SecurityMode.None;
                
                Binding = new NetTcpBinding()
                {
                    Security = ServiceSecurity,
                    ReceiveTimeout = TimeSpan.MaxValue,
                    SendTimeout = TimeSpan.MaxValue,
                    TransferMode = TransferMode.Streamed,
                    CloseTimeout = TimeSpan.MaxValue,
                    ListenBacklog = 1000,
                    MaxBufferPoolSize = 1024*10,
                    MaxConnections =  1000000,
                    MaxBufferSize = 1024*10,
                    MaxReceivedMessageSize = 1024*10,
                    //PortSharingEnabled = true
                };
                Host = new ServiceHost(typeof (BitDB), new Uri("net.tcp://79.133.51.71"));
                Host.AddServiceEndpoint(typeof (IBitDB), Binding, "BitDB");
                Host.Faulted += (sender, args) => { Console.WriteLine("Much crash wow"); };
                Host.Closed += (sender, args) => { Console.WriteLine("Much disconnect wow"); };
                Host.Open();

               return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
