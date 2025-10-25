using Microsoft.AspNetCore.Mvc;
using NZWalks.UI.Models;
using NZWalks.UI.Models.DTO;
using System.Diagnostics;
using System.Text;
using System.Text.Json;


namespace NZWalks.UI.Controllers
{
    public class HomeController : Controller

    {

        private readonly ILogger<HomeController> _logger;

        private readonly IHttpClientFactory httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)

        {

            _logger = logger;

            this.httpClientFactory = httpClientFactory;

        }

        [HttpGet]

        public IActionResult Registration()

        {

            return View();

        }

        [HttpPost]

        public async Task<IActionResult> Registration(RegisterRequestDto model)

        {

            if (!ModelState.IsValid)

                return View(model);

            var client = httpClientFactory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage()

            {

                Method = HttpMethod.Post,

                RequestUri = new Uri("http://localhost:5249/api/Auth/Register"),

                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")

            };

            var httpResponse = await client.SendAsync(httpRequestMessage);

            if (httpResponse.IsSuccessStatusCode)

            {

                return RedirectToAction("Index", "Home");

            }

            var content = await httpResponse.Content.ReadAsStringAsync();

            try

            {

                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)

                {

                    foreach (var prop in errorsElement.EnumerateObject())

                    {

                        var fieldName = prop.Name;

                        if (prop.Value.ValueKind == JsonValueKind.Array)

                        {

                            foreach (var errorMsg in prop.Value.EnumerateArray())

                            {

                                ModelState.AddModelError(fieldName, errorMsg.GetString() ?? "Invalid value");

                            }

                        }

                    }

                }

                else if (doc.RootElement.TryGetProperty("message", out var messageElement))

                {

                    ModelState.AddModelError(string.Empty, messageElement.GetString() ?? content);

                }

                else

                {

                    ModelState.AddModelError(string.Empty, content);

                }

            }

            catch

            {

                ModelState.AddModelError(string.Empty, content);

            }

            return View(model);

        }

        public IActionResult Index()

        {

            return View();

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
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = httpClientFactory.CreateClient();
            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("http://localhost:5249/api/Auth/Login"),
                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
            };

            var httpResponse = await client.SendAsync(httpRequestMessage);

            var content = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                // Try to extract token from common response shapes
                string token = null;
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("jwtToken", out var t) || root.TryGetProperty("Token", out t) ||
                            root.TryGetProperty("access_token", out t) || root.TryGetProperty("accessToken", out t))
                        {
                            token = t.GetString();
                        }
                        else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                        {
                            if (data.TryGetProperty("jwtToken", out var t2)) token = t2.GetString();
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.String)
                    {
                        token = root.GetString();
                    }
                }
                catch
                {
                    // fallback to raw content (may be raw token string)
                    token = content?.Trim('"');
                }

                if (!string.IsNullOrWhiteSpace(token))
                {
                    HttpContext.Session.SetString("JWTToken", token);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Login succeeded but token was not returned by API.");
                    return View(model);
                }
            }

            // Non-success: parse validation or message errors from API
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in errorsElement.EnumerateObject())
                    {
                        var fieldName = prop.Name;
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var errorMsg in prop.Value.EnumerateArray())
                            {
                                ModelState.AddModelError(fieldName, errorMsg.GetString() ?? "Invalid value");
                            }
                        }
                    }
                }
                else if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    ModelState.AddModelError(string.Empty, messageElement.GetString() ?? content);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, content);
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, content);
            }

            return View(model);
        }

    }
}
