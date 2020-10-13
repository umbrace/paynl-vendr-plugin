using Vendr.Core.Web.PaymentProviders;

namespace Vendr.Contrib.PaymentProviders.PayNl
{
    public class PayNlPaymentProviderSettings
    {
        [PaymentProviderSetting(Name = "Continue URL", Description = "The URL to continue to after this provider has done processing. eg: /continue/")]
        public string ContinueUrl { get; set; }

       
        [PaymentProviderSetting(Name = "Cancel URL",
            Description = "The URL to return to if the payment attempt is canceled. eg: /cancel/",
            SortOrder = 200)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error URL",
            Description = "The URL to return to if the payment attempt errors. eg: /error/",
            SortOrder = 300)]
        public string ErrorUrl { get; set; }

        
        //[PaymentProviderSetting(Name = "Api Code", Description = "The api code, starts with AT-", SortOrder = 1300)]
        //public string ApiCode { get; set; }    

        [PaymentProviderSetting(Name = "Api Token", Description = "The api token", SortOrder = 1300)]
        public string ApiToken { get; set; }     
        
        //[PaymentProviderSetting(Name = "Merchant Id", Description = "The ID of the merchant. Starts with M-", SortOrder = 1300)]
        //public string MerchantId { get; set; }

        [PaymentProviderSetting(Name = "Service Id", Description = "Sales location, starts with SL-", SortOrder = 1400)]
        public string ServiceId { get; set; }

        [PaymentProviderSetting(Name = "Test Mode", Description = "Force transactions to be performed in test mode", SortOrder = 1400)]
        public bool TestMode { get; set; }
    }
}
