using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace inSyca.bpa.dynamics.wcf.behavior.oauth
{
    class OAuthBehaviorElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(OAuthBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new OAuthBehavior(TenantId, ResourceUrl, ClientId, ClientSecret, SessionTimeout);
        }

        [ConfigurationProperty("tenantId", IsRequired = true)]
        public string TenantId
        {
            get { return (string)this["tenantId"]; }
            set { this["tenantId"] = value; }
        }

        [ConfigurationProperty("resourceUrl", IsRequired = true)]
        public string ResourceUrl
        {
            get { return (string)this["resourceUrl"]; }
            set { this["resourceUrl"] = value; }
        }

        [ConfigurationProperty("clientId", IsRequired = true)]
        public string ClientId
        {
            get { return (string)this["clientId"]; }
            set { this["clientId"] = value; }
        }

        [ConfigurationProperty("clientSecret", IsRequired = true)]
        public string ClientSecret
        {
            get { return (string)this["clientSecret"]; }
            set { this["clientSecret"] = value; }
        }

        [ConfigurationProperty("sessionTimeout", IsRequired = false, DefaultValue = 300)]
        public int SessionTimeout
        {
            get { return (int)this["sessionTimeout"]; }
            set { this["sessionTimeout"] = value; }
        }
    }
}