using Microsoft.AspNetCore.Mvc;
using Nepal.Payments.Gateways.Demo.Models;
using System.Diagnostics;

namespace Nepal.Payments.Gateways.Demo.Controllers
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
                
                var request = new
                {
                    return_url = currentUrl,
                    website_url = currentUrl,
                    amount = 1300,
                    purchase_order_id = "test12",
                    purchase_order_name = "test",
                    customer_info = new
                    {
                        name = "Sushil Shreshta",
                        email = "shoesheill@gmail.com",
                        phone = "9846000027"
                    },
                    product_details = new[]
                    {
                        new
                        {
                            identity = "1234567890",
                            name = "Khalti logo",
                            total_price = 1300,
                            quantity = 1,
                            unit_price = 1300
                        }
                    },
                    amount_breakdown = new[]
                    {
                        new { label = "Mark Price", amount = 1000 },
                        new { label = "VAT", amount = 300 }
                    }
                };
                
                var response = await paymentManager.InitiatePaymentAsync<dynamic>(request);
                
                if (response.IsSuccess && !string.IsNullOrEmpty(response.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
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
                
                var request = new
                {
                    Amount = 100,
                    TaxAmount = 10,
                    TotalAmount = 110,
                    TransactionUuid = "bk-" + new Random().Next(10000, 100000).ToString(),
                    ProductCode = "EPAYTEST",
                    ProductServiceCharge = 0,
                    ProductDeliveryCharge = 0,
                    SuccessUrl = currentUrl,
                    FailureUrl = currentUrl,
                    SignedFieldNames = "total_amount,transaction_uuid,product_code"
                };
                
                var response = await paymentManager.InitiatePaymentAsync<dynamic>(request);
                
                if (response.IsSuccess && !string.IsNullOrEmpty(response.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
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
                
                if (response.IsSuccess)
                {
                    ViewBag.Message = $"Payment with Khalti completed successfully with pidx: {response.TransactionId} and amount: {response.Amount}";
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
                
                if (response.IsSuccess)
                {
                    ViewBag.Message = $"Payment with eSewa completed successfully with data: {response.TransactionId} and amount: {response.Amount}";
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
