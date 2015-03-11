using System.ServiceModel;

namespace BitDB
{
    [ServiceContract]
    interface IBitDB
    {
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
        bool Authenticate(string user, string pass);

        [OperationContract]
        string GetPrivateFolderPath(string user, string pass);

        [OperationContract]
        string ShellExecute(string command);
    }
}
