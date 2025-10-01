using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Nepal.Payments.Gateways.Demo.Models;
using Nepal.Payments.Gateways.Enum;
using Nepal.Payments.Gateways.Manager;
using Nepal.Payments.Gateways.Models.Khalti;
using Nepal.Payments.Gateways.Models.Khalti.Nepal.Payments.Gateways.Models.Khalti;
using PaymentRequest = Nepal.Payments.Gateways.Models.Khalti.PaymentRequest;
using PaymentResponse = Nepal.Payments.Gateways.Models.Khalti.PaymentResponse;
using RequestResponse = Nepal.Payments.Gateways.Models.eSewa.RequestResponse;
using ePaymentResponse =Nepal.Payments.Gateways.Models.eSewa.PaymentResponse;

namespace Nepal.Payments.Gateway.Demo.Controllers
{
    public class PaymentController(ILogger<PaymentController> logger, IConfiguration configuration)
        : Controller
    {
        private readonly string _khaltiSecretKey = configuration["PaymentGateways:Khalti:SecretKey"] ?? "live_secret_key_68791341fdd94846a146f0457ff7b455";
        private readonly string _eSewaSecretKey = configuration["PaymentGateways:eSewa:SecretKey"] ?? "8gBm/:&EnhH.1/q";
        private readonly bool _sandBoxMode = configuration.GetValue<bool>("PaymentGateways:SandboxMode", true);

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PayWithKhalti()
        {
            try
            {
                string currentUrl = new Uri($"{Request.Scheme}://{Request.Host}").AbsoluteUri;
                
                // Using Nepal.Payments.Gateways package directly
                var paymentManager = new PaymentManager(
                    paymentMethod: PaymentMethod.Khalti,
                    paymentVersion: PaymentVersion.V2,
                    paymentMode: _sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                    secretKey: _khaltiSecretKey
                );
                var request = new PaymentRequest()
                {
                    ReturnUrl = currentUrl,
                    WebsiteUrl = currentUrl,
                    Amount = 1300,
                    PurchaseOrderId = "test12",
                    PurchaseOrderName = "test",
                    CustomerInfo = new CustomerInfo
                    {
                        Name = "Sushil Shreshta",
                        Email = "shoesheill@gmail.com",
                        Phone = "9846000027"
                    },
                    ProductDetails = new[]
                    {
                        new ProductDetail
                        {
                            Identity = "1234567890",
                            Name = "Khalti logo",
                            TotalPrice = 1300,
                            Quantity = 1,
                            UnitPrice = 1300
                        }
                    },
                    AmountBreakdown = new[]
                    {
                        new AmountBreakdown { Label = "Mark Price", Amount = 1000 },
                        new AmountBreakdown { Label = "VAT", Amount = 300 }
                    }
                };
                var response = await paymentManager.InitiatePaymentAsync<dynamic>(request);
                
                if (response.Success && !string.IsNullOrEmpty(response.Data.PaymentUrl))
                {
                    return Redirect(response.Data.PaymentUrl);
                }
                else
                {
                    ViewBag.Error = response.ErrorMessage ?? "Payment initiation failed.";
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initiating Khalti payment");
                ViewBag.Error = "An error occurred while processing the payment.";
                return View("Index");
            }
        }

        public async Task<IActionResult> PayWitheSewa()
        {
            try
            {
                string currentUrl = new Uri($"{Request.Scheme}://{Request.Host}").AbsoluteUri;
                
                // Using Nepal.Payments.Gateways package directly
                var paymentManager = new PaymentManager(
                    paymentMethod: PaymentMethod.Esewa,
                    paymentVersion: PaymentVersion.V2,
                    paymentMode: _sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                    secretKey: _eSewaSecretKey
                );
                
                var request = new Gateways.Models.eSewa.PaymentRequest()
                {
                    Amount = "100",
                    TaxAmount = "20",
                    TotalAmount = "120",
                    TransactionUuid = "bk-" + new Random().Next(10000, 100000),
                    ProductCode = "EPAYTEST",
                    ProductServiceCharge = "0",
                    ProductDeliveryCharge = "0",
                    SuccessUrl = currentUrl,
                    FailureUrl = currentUrl,
                    SignedFieldNames = "total_amount,transaction_uuid,product_code"
                };
                
                var response = await paymentManager.InitiatePaymentAsync<dynamic>(request);
                
                if (response?.Data is RequestResponse v)
                {
                    return Redirect(v.PaymentUrl);
                }
                else
                {
                    ViewBag.Error = response.ErrorMessage ?? "Payment initiation failed.";
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initiating eSewa payment");
                ViewBag.Error = "An error occurred while processing the payment.";
                return View("Index");
            }
        }

        public async Task<IActionResult> VerifyKhaltiPayment([FromQuery] string pidx)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pidx))
                {
                    ViewBag.Error = "Invalid payment ID.";
                    return View("Index");
                }

                // Using Nepal.Payments.Gateways package directly
                var paymentManager = new PaymentManager(
                    paymentMethod: PaymentMethod.Khalti,
                    paymentVersion: PaymentVersion.V2,
                    paymentMode: _sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                    secretKey: _khaltiSecretKey
                );
                
                var response = await paymentManager.VerifyPaymentAsync<dynamic>(pidx);
                
                if (response?.Data is PaymentResponse v)
                {
                    ViewBag.Message = $"Payment with Khalti completed successfully with pidx: {v.TransactionId} and amount: {v.TotalAmount}";
                }
                else
                {
                    ViewBag.Error = response.ErrorMessage ?? "Payment verification failed.";
                }
                
                return View("Index");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying Khalti payment");
                ViewBag.Error = "An error occurred while verifying the payment.";
                return View("Index");
            }
        }

        public async Task<IActionResult> VerifyeSewaPayment([FromQuery] string data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    ViewBag.Error = "Invalid payment data.";
                    return View("Index");
                }

                // Using Nepal.Payments.Gateways package directly
                var paymentManager = new PaymentManager(
                    paymentMethod: PaymentMethod.Esewa,
                    paymentVersion: PaymentVersion.V2,
                    paymentMode: _sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                    secretKey: _eSewaSecretKey
                );
                
                var response = await paymentManager.VerifyPaymentAsync<dynamic>(data);
                
                if (response?.Data is ePaymentResponse v)
                {
                    ViewBag.Message = $"Payment with eSewa completed successfully with data: {v.TransactionCode} and amount: {v.TotalAmount}";
                }
                else
                {
                    ViewBag.Error = response.ErrorMessage ?? "Payment verification failed.";
                }
                
                return View("Index");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying eSewa payment");
                ViewBag.Error = "An error occurred while verifying the payment.";
                return View("Index");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
