using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace inSyca.bpa.dynamics.wcf.behavior.oauth
{
    public class OAuthBehavior : IClientMessageInspector, IEndpointBehavior
    {
        private static Logger logger;

        // Configuration Properties
        private string tenantId_;
        private string resourceUrl_;
        private string clientId_;
        private string clientSecret_;
        private int sessionTimeout_;

        // Private Properties
        private string accessToken_;
        private DateTime tokenExpiryTime_;

        public OAuthBehavior(
            string tenantId,
            string resourceUrl,
            string clientId,
            string clientSecret,
            int sessionTimeout)
        {
            tenantId_ = tenantId;
            resourceUrl_ = resourceUrl;
            clientId_ = clientId;
            clientSecret_ = clientSecret;
            sessionTimeout_ = sessionTimeout;

            var eventLogTarget = new NLog.Targets.EventLogTarget() { 
                Log = "Application"
                , MachineName = "."
                , Source = "inSyca WCF Behavior"
                , Name= "*"
                , Layout = "${message}" };

            SimpleConfigurator.ConfigureForTargetLogging(eventLogTarget, LogLevel.Trace);
            logger = LogManager.GetCurrentClassLogger();
        }

        #region IClientMessageInspector

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            logger.Info("IClientMessageInspector\n\rAfterReceiveReply {0}", reply);
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            logger.Info("IClientMessageInspector\n\rBeforeSendRequest {0}", request);

            // We are going to send a request to Dynamics
            // Overview:
            // This behavior will do the following:
            // (1) Fetch Token from Dynamics, if required
            // (2) Add the token to the message
            // Reference: https://docs.microsoft.com/en-us/biztalk/core/step-3d-enabling-biztalk-server-to-send-and-receive-messages-from-salesforce

            FetchOAuthToken();

            HttpRequestMessageProperty httpRequestMessage;

            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;

                if (string.IsNullOrEmpty(httpRequestMessage.Headers[HttpRequestHeader.Authorization]))
                    httpRequestMessage.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken_;

                logger.Info("IClientMessageInspector\n\rBeforeSendRequest\n\rHttpRequestHeader.Authorization found and assigned Bearer Token");
            }
            else
            {
                httpRequestMessage = new HttpRequestMessageProperty();
                httpRequestMessage.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + accessToken_);

                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);

                logger.Info("IClientMessageInspector\n\rBeforeSendRequest\n\rHttpRequestHeader.Authorization added and assigned Bearer Token");
            }

            return null;
        }

        #endregion IClientMessageInspector

        #region IEndpointBehavior

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // do nothing
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // do nothing
        }

        #endregion IEndpointBehavior

        private void FetchOAuthToken()
        {
            if ((tokenExpiryTime_ == null) || (tokenExpiryTime_.CompareTo(DateTime.Now) <= 0))
            {
                StringBuilder body = new StringBuilder();
                body.Append("grant_type=client_credentials&")
                    .Append("client_id=" + clientId_ + "&")
                    .Append("client_secret=" + clientSecret_ + "&")
                    .Append("resource=" + resourceUrl_);

                try
                {
                    logger.Info("FetchOAuthToken\n\rSend Token Request\n\r{0}", body.ToString());

                    string responseString = HttpPost(string.Format("https://login.microsoftonline.com/{0}/oauth2/token", tenantId_), body.ToString());

                    logger.Info("FetchOAuthToken\n\rReceived Token Response\n\r{0}", responseString);

                    JToken responseToken = JToken.Parse(responseString);
                    accessToken_ = responseToken.SelectToken("..access_token").ToString();
                    tokenExpiryTime_ = DateTime.Now.AddSeconds(sessionTimeout_);
                    
                    logger.Info("FetchOAuthToken\n\rAccess Token\n\r{0}", accessToken_);
                }
                catch (WebException ex)
                {
                    logger.Error("FetchOAuthToken\n\rUnable to obtain access token.\n\rError\n\r", ex);
                    throw new Exception("Unable to obtain access token.", ex);
                }
            }
        }

        private string HttpPost(string URI, string Parameters)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(URI);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";

            // Add parameters to post
            byte[] data = Encoding.UTF8.GetBytes(Parameters);
            req.ContentLength = data.Length;

            using (Stream os = req.GetRequestStream())
                os.Write(data, 0, data.Length);

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                return sr.ReadToEnd().Trim();
        }
    }
}
