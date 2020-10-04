using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Vendr.Core.Models;

namespace Vendr.Contrib.PaymentProviders.PayNl.Api
{
    public class CallbackRequestModel
    {
        private readonly HttpRequestBase _httpRequest;

        public CallbackRequestModel(HttpRequestBase httpRequest)
        {
            _httpRequest = httpRequest;
        }

        public string Action { get; set; }
        
        [System.Xml.Serialization.XmlElement("payment_session_id")]
        public string payment_session_id { get; set; }

        
        [System.Xml.Serialization.XmlElement("ip_address")]
        public string IpAddress { get; set; }

        [System.Xml.Serialization.XmlElement("amount")]
        public decimal Amount { get; set; }

        [System.Xml.Serialization.XmlElement("extra1")]
        public string Extra1 { get; set; }

        [System.Xml.Serialization.XmlElement("extra2")]
        public string Extra2 { get; set; }

        [System.Xml.Serialization.XmlElement("extra3")]
        public string Extra3 { get; set; }
        
        [System.Xml.Serialization.XmlElement("order_id")]
        public string OrderId { get; set; }

        public static CallbackRequestModel FromRequest(HttpRequestBase request)
        {
            decimal.TryParse(request.QueryString["amount"], NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);
            return new CallbackRequestModel(request)
            {
                Action = request.QueryString["action"],
                OrderId = request.QueryString["order_id"],
                payment_session_id = request.QueryString["payment_session_id"],
                IpAddress = request.QueryString["ip_address"],
                Amount = amount,
                Extra1 = request.QueryString["extra1"],
                Extra2 = request.QueryString["extra2"],
                Extra3 = request.QueryString["extra3"],
                info = request.QueryString["info"]
            };
        }

    }
}
