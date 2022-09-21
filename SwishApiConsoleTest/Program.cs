﻿using System;

namespace SwishApiConsoleTest
{
    class Program
    {
        /*
         * Example code for the callback call from the swish server
         * 
        [HttpPost("/p/Swish/Callback")]
        public string SwishCallback([FromBody] JsonElement jsonElement)
        {
            string json = jsonElement.ToString();

            SwishPaymentCallback callback = Newtonsoft.Json.JsonConvert.DeserializeObject<SwishPaymentCallback>(json);

            // Check if the call is done correct
            if (string.IsNullOrEmpty(callback.errorCode))
            {
                switch (callback.status)
                {
                    case "CREATED":
                        // Maybe never happening but the payment created
                        break;
                    case "PAID":
                        // Payment is done
                        break;
                    case "DECLINED":
                        // The user cancelled the payment
                        break;
                    case "ERROR":
                        // Something got wrong, if it takes 3 minutes its timeouts to ERROR
                        break;
                }
            }
            else
            {
                // ERROR
            }

            if (!string.IsNullOrEmpty(callback.payeePaymentReference))
            {
                string myReference = callback.payeePaymentReference;

            }

            return "OK";
        }*/

        static void Main(string[] args)
        {
            MainTestPayment();
            MainTestQCommerce();
            MainTestPayout();
        }

        // MainTestPaymentAndRefund
        static void MainTestPayment()
        {
            var eCommerceClient = new SwishApi.ECommerceClient("https://tabetaltmedswish.se/Test/Callback/", "12345", "1234679304", true, SwishApi.Environment.Emulator);

            string instructionUUID = Guid.NewGuid().ToString("N").ToUpper();

            // Make the Payement Request
            var response = eCommerceClient.MakePaymentRequest("1234679304", 1, "Test", instructionUUID);

            // Check if the payment request got success and not got any error
            if (string.IsNullOrEmpty(response.Error))
            {
                // All OK
                string urlForCheckingPaymentStatus = response.Location;

                // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the payment status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                // Wait so that the payment request has been processed
                System.Threading.Thread.Sleep(5000);

                // Make the payment status check
                var statusResponse = eCommerceClient.CheckPaymentStatus(urlForCheckingPaymentStatus);

                // Check if the call is done correct
                if (string.IsNullOrEmpty(statusResponse.errorCode))
                {
                    // Call was maked without any problem
                    Console.WriteLine("Status: " + statusResponse.status);

                    if (statusResponse.status == "PAID")
                    {
                        var refundClient = new SwishApi.RefundClient("https://tabetaltmedswish.se/Test/RefundCallback/", "1234", true, SwishApi.Environment.Emulator);
                        
                        string instructionUUID2 = Guid.NewGuid().ToString("N").ToUpper();

                        var refundResponse = refundClient.MakeRefundRequest(statusResponse.paymentReference, "0731596605", (int)statusResponse.amount, "Återköp", instructionUUID2);

                        if (string.IsNullOrEmpty(refundResponse.Error))
                        {
                            // Request OK
                            string urlForCheckingRefundStatus = refundResponse.Location;

                            // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the refund status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                            // Wait so that the refund has been processed
                            System.Threading.Thread.Sleep(5000);

                            // Check refund status
                            var refundCheckResposne = refundClient.CheckRefundStatus(urlForCheckingRefundStatus);

                            if (string.IsNullOrEmpty(refundCheckResposne.errorCode))
                            {
                                // Call was maked without any problem
                                Console.WriteLine("RefundChecKResponse - Status: " + statusResponse.status);
                            }
                            else
                            {
                                // ERROR
                                Console.WriteLine("RefundCheckResponse: " + refundCheckResposne.errorCode + " - " + refundCheckResposne.errorMessage);
                            }
                        }
                        else
                        {
                            // ERROR
                            Console.WriteLine("Refund Error: " + refundResponse.Error);
                        }
                    }
                }
                else
                {
                    // ERROR
                    Console.WriteLine("CheckPaymentResponse: " + statusResponse.errorCode + " - " + statusResponse.errorMessage);
                }
            }
            else
            {
                // ERROR
                Console.WriteLine("MakePaymentRequest - ERROR: " + response.Error);
            }


            Console.WriteLine(">>> Press enter to exit <<<");
            Console.ReadLine();
        }

        // MainTestQCommerce
        static void MainTestQCommerce()
        {
            var mCommerceClient = new SwishApi.MCommerceClient("https://tabetaltmedswish.se/Test/Callback/", "12345", "1234679304", true, SwishApi.Environment.Emulator);

            string instructionUUID = Guid.NewGuid().ToString("N").ToUpper();

            var responseMCommerce = mCommerceClient.MakePaymentRequest(1, "Test", instructionUUID);

            var getQRCodeResponse = mCommerceClient.GetQRCode(responseMCommerce.Token, "svg");

            if (string.IsNullOrEmpty(getQRCodeResponse.Error))
            {
                System.IO.File.WriteAllText("test.svg", getQRCodeResponse.SVGData);

                // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the payment status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                // Wait so that the payment request has been processed
                System.Threading.Thread.Sleep(5000);

                // Make the payment status check
                var statusResponse = mCommerceClient.CheckPaymentStatus(responseMCommerce.Location);

                // Check if the call is done correct
                if (string.IsNullOrEmpty(statusResponse.errorCode))
                {
                    // Call was maked without any problem
                    Console.WriteLine("Status: " + statusResponse.status);

                    if (statusResponse.status == "PAID")
                    {
                        var refundClient = new SwishApi.RefundClient("https://tabetaltmedswish.se/Test/RefundCallback/", "1234", true, SwishApi.Environment.Emulator);

                        string instructionUUID2 = Guid.NewGuid().ToString("N").ToUpper();

                        var refundResponse = refundClient.MakeRefundRequest(statusResponse.paymentReference, "0731596605", (int)statusResponse.amount, "Återköp", instructionUUID2);

                        if (string.IsNullOrEmpty(refundResponse.Error))
                        {
                            // Request OK
                            string urlForCheckingRefundStatus = refundResponse.Location;

                            // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the refund status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                            // Wait so that the refund has been processed
                            System.Threading.Thread.Sleep(5000);

                            // Check refund status
                            var refundCheckResposne = refundClient.CheckRefundStatus(urlForCheckingRefundStatus);

                            if (string.IsNullOrEmpty(refundCheckResposne.errorCode))
                            {
                                // Call was maked without any problem
                                Console.WriteLine("RefundChecKResponse - Status: " + statusResponse.status);
                            }
                            else
                            {
                                // ERROR
                                Console.WriteLine("RefundCheckResponse: " + refundCheckResposne.errorCode + " - " + refundCheckResposne.errorMessage);
                            }
                        }
                        else
                        {
                            // ERROR
                            Console.WriteLine("Refund Error: " + refundResponse.Error);
                        }
                    }
                }
                else
                {
                    // ERROR
                    Console.WriteLine("CheckPaymentResponse: " + statusResponse.errorCode + " - " + statusResponse.errorMessage);
                }
            }
            else
            {
                Console.WriteLine("ERROR Get QR Code: " + getQRCodeResponse.Error);
            }

            Console.WriteLine(">>> Press enter to exit <<<");
            Console.ReadLine();
        }

        static void MainTestPayout()
        {
            string certificatePath = Environment.CurrentDirectory + "\\TestCert\\Swish_Merchant_TestSigningCertificate_1234679304.p12";

            var payoutClient = new SwishApi.PayoutClient("https://tabetaltmedswish.se/Test/PayoutCallback/", "1234", "1234679304", true, SwishApi.Environment.Emulator);

            string instructionUUID = Guid.NewGuid().ToString("N").ToUpper();

            // Test payeeAlias and payeeSSN from MSS_UserGuide_v1.9.pdf
            var response = payoutClient.MakePayoutRequest("46722334455", "197501088327", 1, "Test", instructionUUID, "7d70445ec8ef4d1e3a713427e973d097", new SwishApi.Models.ClientCertificate() { Path = certificatePath, Password = "swish" });

            if (string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine("Location: " + response.Location);

                // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the payment status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                // Wait so that the payment request has been processed
                System.Threading.Thread.Sleep(5000);

                // Make the payment status check
                var statusResponse = payoutClient.CheckPayoutStatus(response.Location);

                // Check if the call is done correct
                if (string.IsNullOrEmpty(statusResponse.errorCode))
                {
                    // Call was maked without any problem
                    Console.WriteLine("Status: " + statusResponse.status);

                }
                else
                {
                    // ERROR
                    Console.WriteLine("CheckPayoutResponse: " + statusResponse.errorCode + " - " + statusResponse.errorMessage);
                }
            }
            else
            {
                // ERROR
                Console.WriteLine("MakePayoutRequest - ERROR: " + response.Error);
            }

            Console.WriteLine(">>> Press enter to exit <<<");
            Console.ReadLine();
        }
    }
}
