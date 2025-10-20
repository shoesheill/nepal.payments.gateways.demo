using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nepal.Payments.Gateways.Demo.Data;
using Nepal.Payments.Gateways.Demo.Hubs;
using Nepal.Payments.Gateways.Demo.Models;
using Nepal.Payments.Gateways.Enum;
using Nepal.Payments.Gateways.Manager;
using Nepal.Payments.Gateways.Models.Fonepay;
using Nepal.Payments.Gateways.Models.Khalti;
using Nepal.Payments.Gateways.Models.Khalti.Nepal.Payments.Gateways.Models.Khalti;
using Nepal.Payments.Gateways.WebSocket;
using System.Diagnostics;
using System.Text.Json;
using static Nepal.Payments.Gateways.Constants.ApiEndpoints;
using ePaymentResponse = Nepal.Payments.Gateways.Models.eSewa.PaymentResponse;
using PaymentRequest = Nepal.Payments.Gateways.Models.Khalti.PaymentRequest;
using PaymentResponse = Nepal.Payments.Gateways.Models.Khalti.PaymentResponse;
using RequestResponse = Nepal.Payments.Gateways.Models.eSewa.RequestResponse;

namespace Nepal.Payments.Gateway.Demo.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<PaymentHub> _hubContext;
        private readonly IPaymentWebSocketManager _webSocketManager;

        private readonly string _khaltiSecretKey;
        private readonly string _eSewaSecretKey;
        private readonly string _fonepaySecretKey;
        private readonly string _fonepayMerchantCode;
        private readonly string _fonepayUsername;
        private readonly string _fonepayPassword;
        private bool _sandBoxMode;
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public PaymentController(
            ILogger<PaymentController> logger,
            IConfiguration configuration,
            IHubContext<PaymentHub> hubContext,
            IPaymentWebSocketManager webSocketManager, AppDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _hubContext = hubContext;
            _webSocketManager = webSocketManager;

            _khaltiSecretKey = configuration["PaymentGateways:Khalti:SecretKey"] ?? "live_secret_key_68791341fdd94846a146f0457ff7b455";
            _eSewaSecretKey = configuration["PaymentGateways:eSewa:SecretKey"] ?? "8gBm/:&EnhH.1/q";
            _fonepaySecretKey = configuration["PaymentGateways:Fonepay:SecretKey"] ?? "a7e3512f5032480a83137793cb2021dc";
            _fonepayMerchantCode = configuration["PaymentGateways:Fonepay:MerchantCode"] ?? "NBQM";
            _fonepayUsername = configuration["PaymentGateways:Fonepay:Username"] ?? "9861101076";
            _fonepayPassword = configuration["PaymentGateways:Fonepay:Password"] ?? "admin123456";
            _sandBoxMode = configuration.GetValue<bool>("PaymentGateways:SandboxMode", false);

            // Register WebSocket event handlers once during controller construction
            _webSocketManager.StatusChanged += OnStatusChanged;
            _webSocketManager.PaymentVerified += OnPaymentVerified;
            _webSocketManager.PaymentTimeout += OnPaymentTimeout;
            _webSocketManager.PaymentError += OnPaymentError;
            _webSocketManager.PaymentCancelled += OnPaymentCancelled;
            _context = context;
            _scopeFactory=serviceScopeFactory;
        }

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
                _logger.LogError(ex, "Error initiating Khalti payment");
                ViewBag.Error = $"Payment error: {ex.Message}";
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
                    ViewBag.Error = response?.ErrorMessage ?? "Payment initiation failed.";
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating eSewa payment");
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
                    ViewBag.Error = response?.ErrorMessage ?? "Payment verification failed.";
                }
                
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Khalti payment");
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
                    ViewBag.Error = response?.ErrorMessage ?? "Payment verification failed.";
                }
                
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying eSewa payment");
                ViewBag.Error = "An error occurred while verifying the payment.";
                return View("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> FonepayQr([FromBody] JsonElement body)
        {

            try
            {
                _sandBoxMode = false; // since Fonepay QR doesn't support sandbox mode
                string amount = body.TryGetProperty("amount", out var a) ? a.GetString() ?? string.Empty : string.Empty;
                string remarks1 = body.TryGetProperty("remarks1", out var r1) ? r1.GetString() ?? string.Empty : string.Empty;
                string remarks2 = body.TryGetProperty("remarks2", out var r2) ? r2.GetString() ?? string.Empty : string.Empty;

                if (string.IsNullOrWhiteSpace(amount))
                {
                    return BadRequest(new { success = false, message = "amount is required" });
                }

                var paymentManager = new PaymentManager(
                    paymentMethod: PaymentMethod.FonePay,
                    paymentVersion: PaymentVersion.V2,
                    paymentMode: _sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                    secretKey: _fonepaySecretKey
                );

                var request = new QrRequest
                {
                    Amount = amount,
                    Remarks1 = remarks1,
                    Remarks2 = remarks2,
                    Prn = Guid.NewGuid().ToString(),
                    MerchantCode = _fonepayMerchantCode,
                    Username = _fonepayUsername,
                    Password = _fonepayPassword
                };

                var response = await paymentManager.InitiatePaymentAsync<dynamic>(request);

                if (response.Success && response.Data != null)
                {
                    //_context.FonepayTransactions.Add(new FonepayTransaction { Data= JsonSerializer.Serialize(response) });
                    //_context.SaveChanges();
                    var qrResponse = response.Data as QrResponse;
                    if (qrResponse != null)
                    {
                        try
                        {
                            var credentials = new PaymentCredentials
                            {
                                SecretKey = _fonepaySecretKey,
                                MerchantCode = _fonepayMerchantCode,
                                Username = _fonepayUsername,
                                Password = _fonepayPassword,
                                SandboxMode = _sandBoxMode
                            };

                            // Start WebSocket monitoring
                            await _webSocketManager.StartMonitoringAsync(request.Prn, qrResponse.ThirdpartyQrWebSocketUrl, credentials);

                            return Json(new
                            {
                                success = true,
                                prn = request.Prn,
                                qrMessage = qrResponse.QrMessage,
                                message = "QR code generated successfully"
                            });
                        }
                        catch (Exception wsEx)
                        {
                            _logger.LogError(wsEx, "Failed to start WebSocket monitoring");

                            return Json(new
                            {
                                success = true,
                                prn = request.Prn,
                                qrMessage = qrResponse.QrMessage,
                                message = "QR generated but WebSocket failed",
                                error = wsEx.Message
                            });
                        }
                    }
                }

                return BadRequest(new { success = false, message = response.Message ?? "Failed to generate QR code" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Fonepay QR");
                return StatusCode(500, new { success = false, message = "Server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FonepayStatus([FromBody] JsonElement body)
        {
            try
            {
                _sandBoxMode = false; // since Fonepay QR doesn't support sandbox mode
                string prn = body.TryGetProperty("prn", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(prn))
                {
                    return BadRequest(new { success = false, message = "prn is required" });
                }

                var paymentManager = new PaymentManager(
                    paymentMethod: PaymentMethod.FonePay,
                    paymentVersion: PaymentVersion.V2,
                    paymentMode: _sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                    secretKey: _fonepaySecretKey
                );

                var verificationData = new Dictionary<string, string>
                {
                    ["prn"] = prn,
                    ["merchantCode"] = _fonepayMerchantCode,
                    ["username"] = _fonepayUsername,
                    ["password"] = _fonepayPassword
                };

                var response = await paymentManager.VerifyPaymentAsync<dynamic>(
                    System.Text.Json.JsonSerializer.Serialize(verificationData)
                );

                if (response.Success && response.Data != null)
                {
                    var statusResponse = response.Data as QrStatusResponse;
                    if (statusResponse != null)
                    {
                        return Json(new
                        {
                            success = true,
                            fonepayTraceId = statusResponse.FonepayTraceId,
                            paymentStatus = statusResponse.PaymentStatus,
                            prn = statusResponse.Prn
                        });
                    }
                }

                return BadRequest(new { success = false, message = response.Message ?? "Status check failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Fonepay status");
                return StatusCode(500, new { success = false, message = "Server error" });
            }
        }

        // Event handlers for WebSocket events (registered in constructor)
        private async void OnStatusChanged(object? sender, PaymentStatusEventArgs args)
        {
            //using var scope = _scopeFactory.CreateScope();
            //var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //context.FonepayTransactions.Add(new FonepayTransaction { Data = JsonSerializer.Serialize(args) });
            //context.SaveChanges();
            _logger.LogInformation("WebSocket Event - StatusChanged: PRN={Prn}, Status={Status}, QrVerified={QrVerified}, PaymentSuccess={PaymentSuccess}",
                args.Prn, args.PaymentStatus, args.QrVerified, args.PaymentSuccess);

            await HandlePaymentStatusChange(args);
        }

        private async void OnPaymentVerified(object? sender, PaymentVerifiedEventArgs args)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger.LogInformation("WebSocket Event - PaymentVerified: PRN={Prn}, Success={Success}",
                args.Prn, args.Success);
            context.FonepayTransactions.Add(new FonepayTransaction { Data = JsonSerializer.Serialize(args) });
            context.SaveChanges();
            await ProcessPaymentVerified(args);
        }

        private async void OnPaymentTimeout(object? sender, PaymentTimeoutEventArgs args)
        {
            //using var scope = _scopeFactory.CreateScope();
            //var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //context.FonepayTransactions.Add(new FonepayTransaction { Data = JsonSerializer.Serialize(args) });
            //context.SaveChanges();
            _logger.LogInformation("WebSocket Event - PaymentTimeout: PRN={Prn}", args.Prn);

            await _hubContext.Clients.Group($"payment-{args.Prn}")
                .SendAsync("PaymentStatusUpdate", new
                {
                    eventType = "PAYMENT_TIMEOUT",
                    data = new
                    {
                        prn = args.Prn,
                        status = "timeout",
                        message = "Payment monitoring timed out",
                        timestamp = args.Timestamp
                    }
                });
        }

        private async void OnPaymentError(object? sender, PaymentErrorEventArgs args)
        {
            _logger.LogError("WebSocket Event - PaymentError: PRN={Prn}, Error={Error}",
                args.Prn, args.ErrorMessage);

            await _hubContext.Clients.Group($"payment-{args.Prn}")
                .SendAsync("PaymentStatusUpdate", new
                {
                    eventType = "WEBSOCKET_ERROR",
                    data = new
                    {
                        prn = args.Prn,
                        status = "error",
                        message = args.ErrorMessage,
                        timestamp = args.Timestamp
                    }
                });
        }

        private async void OnPaymentCancelled(object? sender, PaymentCancelledEventArgs args)
        {
            _logger.LogInformation("WebSocket Event - PaymentCancelled: PRN={Prn}, Reason={Reason}",
                args.Prn, args.Reason);

            await _hubContext.Clients.Group($"payment-{args.Prn}")
                .SendAsync("PaymentStatusUpdate", new
                {
                    eventType = "PAYMENT_CANCELLED",
                    data = new
                    {
                        prn = args.Prn,
                        status = "cancelled",
                        message = args.Reason,
                        cancelledBy = args.CancelledBy,
                        timestamp = args.Timestamp
                    }
                });
        }

        [HttpPost]
        public async Task<IActionResult> StopWebSocketMonitoring([FromBody] JsonElement body)
        {
            try
            {
                string prn = body.TryGetProperty("prn", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(prn))
                {
                    return BadRequest(new { success = false, message = "prn is required" });
                }

                await _webSocketManager.StopMonitoringAsync(prn);

                return Json(new { success = true, message = "WebSocket monitoring stopped" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping WebSocket monitoring");
                return StatusCode(500, new { success = false, message = "Server error" });
            }
        }

        private async Task HandlePaymentStatusChange(PaymentStatusEventArgs args)
        {
            try
            {
                // Map payment status to event type
                var eventType = args.PaymentStatus switch
                {
                    "websocket_connected" => "WEBSOCKET_CONNECTED",
                    "qr_verified" => "QR_VERIFIED",
                    "payment_success" => "PAYMENT_SUCCESS",
                    "payment_failed" => "PAYMENT_FAILED",
                    _ => "STATUS_UPDATE"
                };

                // Send SignalR notification to client
                await _hubContext.Clients.Group($"payment-{args.Prn}")
                    .SendAsync("PaymentStatusUpdate", new
                    {
                        eventType = eventType,
                        data = new
                        {
                            prn = args.Prn,
                            status = args.PaymentStatus,
                            qrVerified = args.QrVerified,
                            paymentSuccess = args.PaymentSuccess,
                            timestamp = args.Timestamp,
                            rawMessage = args.RawMessage
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment status change for PRN: {Prn}", args.Prn);
            }
        }

        private async Task ProcessPaymentVerified(PaymentVerifiedEventArgs args)
        {
            try
            {
                if (args.Success)
                {
                    await _hubContext.Clients.Group($"payment-{args.Prn}")
                        .SendAsync("PaymentStatusUpdate", new
                        {
                            eventType = "PAYMENT_VERIFIED",
                            data = new
                            {
                                prn = args.Prn,
                                status = "verified_success",
                                message = "Payment completed and verified successfully",
                                verificationData = args.VerificationData,
                                timestamp = args.Timestamp
                            }
                        });
                }
                else
                {
                    await _hubContext.Clients.Group($"payment-{args.Prn}")
                        .SendAsync("PaymentStatusUpdate", new
                        {
                            eventType = "VERIFICATION_FAILED",
                            data = new
                            {
                                prn = args.Prn,
                                status = "verification_failed",
                                message = args.ErrorMessage ?? "Payment verification failed",
                                timestamp = args.Timestamp
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment verification for PRN: {Prn}", args.Prn);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
