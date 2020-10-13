using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PAYNLSDK;
using PAYNLSDK.Net;
using PAYNLSDK.Objects;
using Vendr.Contrib.PaymentProviders.PayNl.Api;
using Vendr.Core;
using Vendr.Core.Logging;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;

namespace Vendr.Contrib.PaymentProviders.PayNl
{


    [PaymentProvider("paynl", "Pay.Nl", "PayNl payment provider", Icon = "icon-invoice")]
    public class PayNlPaymentProvider : PaymentProviderBase<PayNlPaymentProviderSettings>
    {


        public PayNlPaymentProvider(VendrContext vendr)
            : base(vendr)
        { }

        public override bool FinalizeAtContinueUrl => true;

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, PayNlPaymentProviderSettings settings)
        {
            Vendr.Log.Info<PayNlPaymentProvider>("About to create a new PayNlTransaction for order {orderId}", order.Id);

            var currency = Vendr.Services.CurrencyService.GetCurrency(order.CurrencyId);
            var payNlConfiguration = new PAYNLSDK.API.PayNlConfiguration(settings.ServiceId, settings.ApiToken);
            var payNlClient = new ApiTokenClient(payNlConfiguration);

            // build request data
            var transactionRequest = PAYNLSDK.Transaction.CreateTransactionRequest(
                order.TotalPrice.Value.WithTax,
                NetworkHelpers.GetIpAddress(),
                continueUrl,
                testMode: settings.TestMode);
            transactionRequest.TransactionData = new TransactionData
            {
                Currency = currency.Code,
                OrderExchangeUrl = callbackUrl,
                OrderNumber = $"{order.OrderNumber}"
            };

            var languageCode = System.Globalization.CultureInfo
                .GetCultures(CultureTypes.NeutralCultures)
                .FirstOrDefault(ci => 
                    string.Equals(ci.ThreeLetterISOLanguageName, order.LanguageIsoCode,  StringComparison.OrdinalIgnoreCase))
                ?.TwoLetterISOLanguageName;
            transactionRequest.Enduser =
                new EndUser
                {
                    Language = languageCode,
                    CustomerReference = order.CustomerInfo.CustomerReference,
                    EmailAddress = order.CustomerInfo.Email
                };
            //transactionRequest.StatsData = new StatsDetails
            //{
            //    Extra1 = "shipping-" + order.ShippingInfo.ShippingMethodId
            //};

            // would be nicer if we inject this.  
            // To do so,
            // RegisterType<PAYNLSDK.Net.Client>().As<PAYNLSDK.Net.IClient>();
            // RegisterType<PAYNLSDK.Transaction>().As<PAYNLSDK.ITransaction>();
            // RegisterType<OurCustomPayNlConfiguration>().As<IPayNlConfiguration>();
            var transaction = new Transaction(payNlClient);
            var response = transaction.Start(transactionRequest);

            Vendr.Log.Info<PayNlPaymentProvider>("Created a new transaction for PayNl {@transactionData}", response.Transaction);

            return new PaymentFormResult()
            {
                Form = new PaymentForm(response.Transaction.PaymentUrl, FormMethod.Get),
                MetaData = new Dictionary<string, string>()
                {
                    { "paymentReference", response.Transaction.PaymentReference},
                    { "transactionId", response.Transaction.TransactionId},
                    { "popupAllowed", response.Transaction.PopupAllowed},

                }
            };
        }

        public override string GetCancelUrl(OrderReadOnly order, PayNlPaymentProviderSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.CancelUrl;
        }

        public override string GetErrorUrl(OrderReadOnly order, PayNlPaymentProviderSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.ErrorUrl;
        }

        public override string GetContinueUrl(OrderReadOnly order, PayNlPaymentProviderSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override bool CanCapturePayments => true;
        public override bool CanCancelPayments => true;
        public override bool CanFetchPaymentStatus => true;
        public override bool CanRefundPayments => false; // FOR NOW

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, PayNlPaymentProviderSettings settings)
        {
            Vendr.Log.Info<PayNlPaymentProvider>("PayNl Callback for order");

            var transactionId = "";
            var sync = true;
            if (sync)
            {
                transactionId = request.QueryString["orderId"];
            }
            else
            {
                // async callback on webhook url
                var callbackInfo = CallbackRequestModel.FromRequest(request);
                transactionId = callbackInfo.OrderId;
            }

            var payNlConfiguration = new PAYNLSDK.API.PayNlConfiguration(settings.ServiceId, settings.ApiToken);
            var payNlClient = new ApiTokenClient(payNlConfiguration);
            var transaction = new Transaction(payNlClient);

            Vendr.Log.Info<PayNlPaymentProvider>("Retrieve transaction details from PayNl to validate order");
            var info = transaction.Info(transactionId );
            Vendr.Log.Info<PayNlPaymentProvider>("Retrieved transaction details {@payNlTransactionDetails}", info);

            var vendrTransactionInfo = new TransactionInfo
            {
                AmountAuthorized = info.PaymentDetails.Amount,
                TransactionFee = 0m,
                TransactionId = transactionId ,
            };
            if ((int)info.PaymentDetails.State == 100)
            {
                Vendr.Log.Info<PayNlPaymentProvider>("Payment State is autorized");
                vendrTransactionInfo.PaymentStatus = PaymentStatus.Authorized;
            }
            else if ((int)info.PaymentDetails.State > 0)
            {
                Vendr.Log.Info<PayNlPaymentProvider>("Payment State is still pending");
                vendrTransactionInfo.PaymentStatus = PaymentStatus.PendingExternalSystem;
            }
            else if ((int)info.PaymentDetails.State < 0)
            {
                Vendr.Log.Info<PayNlPaymentProvider>("Payment State was closed in error");
                vendrTransactionInfo.PaymentStatus = PaymentStatus.Error;
            }

            return new CallbackResult
            {
                TransactionInfo = vendrTransactionInfo,
                MetaData = new Dictionary<string, string>()
                {
                    {"CardType", info.PaymentDetails.CardType },
                    {"CardBrand", info.PaymentDetails.CardBrand },
                    {"IpAddress", info.Connection.IP },
                    {"TransactionId", transactionId  },
                    {"PaymentSessionId", info.StatsDetails.PaymentSessionId.ToString() }
                }
            };
        }
    }
}
