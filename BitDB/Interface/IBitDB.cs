using System;
using System.IO;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BitDB.Interface
{
    [ServiceContract]
    internal interface IBitDB
    {
        [OperationContract]
        int Ping(DateTime sentTime);

        [OperationContract]
        string Load(string file, string section, string key, string Default);

        [OperationContract]
        void Save(string file, string section, string key, string value);

        [OperationContract]
        string[] GetFiles(string path, string pattern, bool recursive);

        [OperationContract]
        bool CreateDirectory(string user, string path);

        [OperationContract]
        bool CreateFile(string user, string path);

        [OperationContract]
        string GetPrivateFolderPath(string user, string pass);

        [OperationContract]
        Task<string> ShellExecute(string command, string username, string password);

        [OperationContract]
        Task<string> UploadFile(Stream stream);

        [OperationContract]
        Stream DownloadFile(string name);
    }
}