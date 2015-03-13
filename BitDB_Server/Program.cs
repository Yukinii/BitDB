using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
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
                ServiceSecurity.Mode = SecurityMode.TransportWithMessageCredential;
                ServiceSecurity.Message = new MessageSecurityOverTcp { ClientCredentialType = MessageCredentialType.UserName };
                Binding = new NetTcpBinding
                {
                    Security = ServiceSecurity,
                    ReceiveTimeout = TimeSpan.MaxValue,
                    SendTimeout = TimeSpan.MaxValue,
                    TransferMode = TransferMode.Buffered,
                    CloseTimeout = TimeSpan.MaxValue,
                    OpenTimeout = TimeSpan.MaxValue,
                    ListenBacklog = 1000,
                    MaxBufferPoolSize = 1024*10,
                    MaxConnections =  1000000,
                    MaxBufferSize = 1024*10,
                    MaxReceivedMessageSize = 1024*10,
                    
                };
                var validator = new UserAuthentication();
                Host = new ServiceHost(typeof (BitDB), new Uri("net.tcp://"+Core.GetIP()));
                Host.AddServiceEndpoint(typeof (IBitDB), Binding, "BitDB");
                Host.Faulted += (sender, args) => { Console.WriteLine("Much crash wow"); };
                Host.Closed += (sender, args) => { Console.WriteLine("Much disconnect wow"); };
                Host.Description.Behaviors.Add(new ServiceCredentials { UserNameAuthentication = { CustomUserNamePasswordValidator = validator } });
                var creds = Host.Description.Behaviors.Find<ServiceCredentials>();
                creds.UserNameAuthentication.CustomUserNamePasswordValidator = validator;
                creds.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                creds.ClientCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySubjectName, Core.GetIP());
                creds.ServiceCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySubjectName, Core.GetIP());
                creds.ClientCertificate.Authentication.TrustedStoreLocation = StoreLocation.LocalMachine;
                creds.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
                creds.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
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
