using Nepal.Payments.Gateway.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Nepal.Payments.Gateway.Demo.Controllers
{
	public class HomeController(ILogger<HomeController> logger) : Controller
	{
		private readonly ILogger<HomeController> _logger = logger;

		public IActionResult Index([FromQuery] string pidx, [FromQuery] string data)
		{
			// Redirect to PaymentController for better organization
			if (!string.IsNullOrWhiteSpace(pidx))
			{
				return RedirectToAction("VerifyKhaltiPayment", "Payment", new { pidx = pidx });
			}
			else if (!string.IsNullOrWhiteSpace(data))
			{
				return RedirectToAction("VerifyeSewaPayment", "Payment", new { data = data });
			}
			
			// Redirect to PaymentController for payment methods
			return RedirectToAction("Index", "Payment");
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}