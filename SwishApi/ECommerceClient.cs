﻿using Newtonsoft.Json;
using SwishApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SwishApi
{
    public class ECommerceClient
    {
        readonly string _environment;
        readonly string _merchantAlias;
        readonly string _callbackUrl;
        readonly string _payeePaymentReference;
        readonly ClientCertificate _certificate;
        readonly bool _enableHTTPLog;

        /// <summary>
        /// Construct a E-Commerce client for Swish Payment, with certificate file
        /// </summary>
        /// <param name="certificatePath">Path where to find the .p12 certificate on disc example: c:\cert\swish.p12</param>
        /// <param name="certificatePassword">The password to use the certificate</param>
        /// <param name="callbackUrl">URL where you like to get the Swish Payment Callback</param>
        /// <param name="payeePaymentReference">Payment reference supplied by theMerchant. This is not used by Swish but is included in responses back to the client. This reference could for example be an order id or similar. If set the value must not exceed 35 characters and only the following characters are allowed: [a-ö, A-Ö, 0-9, -]</param>
        /// <param name="merchantAlias">The Swish number of the payee. It needs to match with Merchant Swish number.</param>
        /// <param name="enableHTTPLog">Set to true to log HTTP Requests to the Swish Payment API</param>
        /// <param name="environment">Set what environment of Swish Payment API should be used, PROD, SANDBOX or EMULATOR</param>
        public ECommerceClient(string certificatePath, string certificatePassword, string callbackUrl, string payeePaymentReference, string merchantAlias, bool enableHTTPLog = false, string environment = "PROD")
        {
            _certificate = new ClientCertificate()
            {
                CertificateFilePath = certificatePath,
                Password = certificatePassword
            };
            _environment = environment;
            _callbackUrl = callbackUrl;
            _merchantAlias = merchantAlias;
            _payeePaymentReference = payeePaymentReference;
            _enableHTTPLog = enableHTTPLog;
        }

        /// <summary>
        /// Construct a E-Commerce client for Swish Payment, with certificate file
        /// </summary>
        /// <param name="clientCertificate">Client Certificate object</param>
        /// <param name="callbackUrl">URL where you like to get the Swish Payment Callback</param>
        /// <param name="payeePaymentReference">Payment reference supplied by theMerchant. This is not used by Swish but is included in responses back to the client. This reference could for example be an order id or similar. If set the value must not exceed 35 characters and only the following characters are allowed: [a-ö, A-Ö, 0-9, -]</param>
        /// <param name="payeeAlias">The Swish number of the payee. It needs to match with Merchant Swish number.</param>
        /// <param name="enableHTTPLog">Set to true to log HTTP Requests to the Swish Payment API</param>
        /// <param name="environment">Set what environment of Swish Payment API should be used, PROD, SANDBOX or EMULATOR</param>
        public ECommerceClient(ClientCertificate clientCertificate, string callbackUrl, string payeePaymentReference, string merchantAlias, bool enableHTTPLog = false, string environment = "PROD")
        {
            _certificate = clientCertificate;
            _environment = environment;
            _callbackUrl = callbackUrl;
            _merchantAlias = merchantAlias;
            _payeePaymentReference = payeePaymentReference;
            _enableHTTPLog = enableHTTPLog;
        }

        /// <summary>
        /// Initiate a Swish Payment Request
        /// </summary>
        /// <param name="swishNumberToPay">The registered Cell phone number of the person that makes the payment. It can only contain numbers and has to be at least 8 and at most 15 digits. It also needs to match the following format in order to be found in Swish: country code + cell phone number (without leading zero). E.g.: 46712345678 If set, request is handled as E-Commerce payment. If not set, request is handled as M- Commerce payment.</param>
        /// <param name="amount">The amount of money to pay. The amount cannot be less than 0.01 SEK and not more than 999999999999.99 SEK. Valid value has to be all digits or with 2 digit decimal separated with a period.</param>
        /// <param name="message">Merchant supplied message about the payment/order. Max 50 characters. Common allowed characters are the letters a-ö, A-Ö, the numbers 0-9, and special characters !?=#$%&()*+,-./:;<'"@. In addition, the following special characters are also allowed: ^¡¢£€¥¿Š§šŽžŒœŸÀÁÂÃÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕØØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ.</param>
        /// <param name="instructionUUID">An identifier created by the merchant to uniquely identify a payout instruction sent to the Swish system. Swish uses this identifier to guarantee the uniqueness of a payout instruction and prevent occurrence of unintended double payments. 32 hexadecimal (16- based) digits. Use Guid.NewGuid().ToString("N").ToUpper()</param>
        /// <returns></returns>
        public PaymentRequestECommerceResponse MakePaymentRequest(string swishNumberToPay, decimal amount, string message, string instructionUUID)
        {
            try
            {
                var requestData = new PaymentRequestECommerceData()
                {
                    payeePaymentReference = _payeePaymentReference,
                    callbackUrl = _callbackUrl,
                    payerAlias = swishNumberToPay,
                    payeeAlias = _merchantAlias,
                    amount = Math.Round(amount, 2).ToString().Replace(",", "."), // Amount to be paid. Only period/dot (”.”) are accepted as decimal character with maximum 2 digits after. Digits after separator are optional.
                    currency = "SEK",
                    message = message
                };

                HttpClientHandler handler;
                HttpClient client;
                PrepareHttpClientAndHandler.LoadCert(_certificate, out handler, out client, _enableHTTPLog);

                string requestURL = URL.ProductionPaymentRequest + instructionUUID;

                var httpMethod = HttpMethod.Put;

                switch (_environment)
                {
                    case "EMULATOR":
                        requestURL = URL.EmulatorPaymentRequest + instructionUUID;
                        break;
                    case "SANDBOX":
                        requestURL = URL.SandboxPaymentRequest + instructionUUID;
                        break;
                }

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = httpMethod,
                    RequestUri = new Uri(requestURL),
                    Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json")
                };

                httpRequestMessage.Headers.Add("host", httpRequestMessage.RequestUri.Host);

                var response = client.SendAsync(httpRequestMessage).Result;

                string errorMessage = string.Empty;
                string location = string.Empty;

                if (response.IsSuccessStatusCode)
                {
                    var headers = response.Headers.ToList();

                    if (headers.Any(x => x.Key == "Location"))
                    {
                        location = response.Headers.GetValues("Location").FirstOrDefault();
                    }
                }
                else
                {
                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    errorMessage = readAsStringAsync.Result;
                }

                client.Dispose();
                handler.Dispose();

                return new PaymentRequestECommerceResponse()
                {
                    Error = errorMessage,
                    Location = location
                };
            }
            catch (Exception ex)
            {
                return new PaymentRequestECommerceResponse()
                {
                    Error = ex.ToString(),
                    Location = ""
                };
            }
        }

        /// <summary>
        /// Check what the status of the payment is
        /// </summary>
        /// <param name="url">The URL we got from the payment request Location header</param>
        /// <returns></returns>
        public CheckPaymentRequestStatusResponse CheckPaymentStatus(string url)
        {
            try
            {
                HttpClientHandler handler;
                HttpClient client;
                PrepareHttpClientAndHandler.LoadCert(_certificate, out handler, out client, _enableHTTPLog);
                client.BaseAddress = new Uri(url);


                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get
                };

                httpRequestMessage.Headers.Add("host", client.BaseAddress.Host);

                var response = client.SendAsync(httpRequestMessage).Result;

                string errorMessage = string.Empty;
                string PaymentRequestToken = string.Empty;
                CheckPaymentRequestStatusResponse r = null;

                if (response.IsSuccessStatusCode)
                {
                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    string jsonResponse = readAsStringAsync.Result;

                    r = JsonConvert.DeserializeObject<CheckPaymentRequestStatusResponse>(jsonResponse);
                }
                else
                {
                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    errorMessage = readAsStringAsync.Result;
                }

                client.Dispose();
                handler.Dispose();

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return new CheckPaymentRequestStatusResponse()
                    {
                        errorCode = "Error",
                        errorMessage = errorMessage
                    };
                }
                else
                {
                    return r;
                }
            }
            catch (Exception ex)
            {
                return new CheckPaymentRequestStatusResponse()
                {
                    errorCode = "Exception",
                    errorMessage = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Cancel a Swish Payment Request
        /// </summary>
        /// <param name="paymentLocationURL">Payment response location url</param>
        /// <returns></returns>
        public CancelPaymentResponse CancelPaymentRequest(string paymentLocationURL)
        {
            try
            {
                var o = new CancelPaymentRequest()
                {
                    op = "replace",
                    path = "/status",
                    value = "cancelled"
                };

                var requestData = new List<CancelPaymentRequest>() { o };

                HttpClientHandler handler;
                HttpClient client;
                PrepareHttpClientAndHandler.LoadCert(_certificate, out handler, out client, _enableHTTPLog);

                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Patch,
                    RequestUri = new Uri(paymentLocationURL),
                    Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json-patch+json")
                };

                httpRequestMessage.Headers.Add("host", httpRequestMessage.RequestUri.Host);

                var response = client.SendAsync(httpRequestMessage).Result;

                string errorMessage = string.Empty;
                string location = string.Empty;

                CancelPaymentResponse r;

                if (response.IsSuccessStatusCode)
                {
                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    string jsonResponse = readAsStringAsync.Result;

                    r = JsonConvert.DeserializeObject<CancelPaymentResponse>(jsonResponse);
                }
                else
                {
                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    errorMessage = readAsStringAsync.Result;

                    r = new CancelPaymentResponse()
                    {
                        ErrorMessage = errorMessage
                    };
                }

                client.Dispose();
                handler.Dispose();

                return r;
            }
            catch (Exception ex)
            {
                return new CancelPaymentResponse()
                {
                    ErrorMessage = ex.ToString()
                };
            }
        }
    }
}
