using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace inSyca.bpa.dynamics.wcf.behavior.oauth
{
    [TestClass]
    public class testBPA_001
    {
        [TestMethod]
        public void invoke_oauth()
        {
            if (!EventLog.SourceExists("inSyca WCF Behavior"))
                EventLog.CreateEventSource("inSyca WCF Behavior", "Application");

            OAuthBehavior oAuthBehavior = 
                new OAuthBehavior("YOUR-TENANT-ID"
                , "https://YOUR-DYNAMICS-URL.crm4.dynamics.com/"
                , "YOUR-CLIENT-ID"
                , "YOUR-CLIENT-SECRET"
                , 300 );

            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("<Test/>"));
            Message request =  Message.CreateMessage(MessageVersion.Default, "GetDataResponse", XmlDictionaryReader.Create(stream));

            oAuthBehavior.BeforeSendRequest(ref request, null);
        }
    }
}
