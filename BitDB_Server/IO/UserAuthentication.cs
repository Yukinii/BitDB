using System.IdentityModel.Selectors;
using System.IO;
using System.ServiceModel;

namespace BitDB_Server.IO
{
    public class UserAuthentication : UserNamePasswordValidator
    {
        public override void Validate(string userName, string password)
        {
            Directory.CreateDirectory(@"Users\" + userName);
            Directory.CreateDirectory(@"Users\" + userName + @"\Storage\");
            var reader = new INI(@"Users\" + userName + @"\AccountInfo.ini");
            var pw = reader.ReadString("Account", "Password", "");
            if (pw == password)
                return;
            if (!string.IsNullOrEmpty(pw))
                throw new FaultException("Unknown Username or Incorrect Password");
            reader.Write("Account", "Password", password);
            reader.Flush();
        }
    }
}