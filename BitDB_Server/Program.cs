﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using BitDB_Server.Interface;
using BitDB_Server.IO;

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
                    CloseTimeout = TimeSpan.MaxValue,
                    OpenTimeout = TimeSpan.MaxValue,
                    TransferMode = TransferMode.Streamed,
                    ReaderQuotas = {MaxArrayLength = int.MaxValue,MaxBytesPerRead = int.MaxValue,MaxDepth = int.MaxValue,MaxNameTableCharCount = int.MaxValue,MaxStringContentLength = int.MaxValue},
                    ListenBacklog = 1000,
                    MaxBufferPoolSize = 1024*1024,
                    MaxConnections =  1000000,
                    MaxBufferSize = 1024 * 1024 * 16,
                    MaxReceivedMessageSize = int.MaxValue
                    };
                var validator = new UserAuthentication();
                Host = new ServiceHost(typeof (BitDB), new Uri("net.tcp://eubfwcf.cloudapp.net"));
                Host.AddServiceEndpoint(typeof (IBitDB), Binding, "BitDB");
                Host.Faulted += (sender, args) => { Console.WriteLine("Much crash wow"); };
                Host.Closed += (sender, args) => { Console.WriteLine("Much disconnect wow"); };
                Host.Description.Behaviors.Add(new ServiceCredentials { UserNameAuthentication = { CustomUserNamePasswordValidator = validator } });
                var creds = Host.Description.Behaviors.Find<ServiceCredentials>();
                creds.UserNameAuthentication.CustomUserNamePasswordValidator = validator;
                creds.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                creds.ClientCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySubjectName, "WCfServer");
                creds.ServiceCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySubjectName, "WCfServer");
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
        public static class Core
        {
            public static string GetIP()
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
                {
                    return ip.ToString();
                }
                return "0.0.0.0";
            }
        }
    }
}
