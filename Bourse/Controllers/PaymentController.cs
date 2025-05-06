using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace Bourse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _config;

        public PaymentController(IHttpClientFactory clientFactory, IConfiguration config)
        {
            _clientFactory = clientFactory;
            _config = config;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] PaymentRequest request)
        {
            string clientId = _config["PayPal:ClientId"];
            string clientSecret = _config["PayPal:Secret"];

            var client = _clientFactory.CreateClient();

            // Authentification
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var tokenResponse = await client.PostAsync(
                "https://api-m.paypal.com/v1/oauth2/token",
                new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded"));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var error = await tokenResponse.Content.ReadAsStringAsync();
                return StatusCode((int)tokenResponse.StatusCode, $"Erreur d'obtention du token PayPal : {error}");
            }

            var tokenData = await tokenResponse.Content.ReadAsStringAsync();
            var accessToken = JsonDocument.Parse(tokenData).RootElement.GetProperty("access_token").GetString();

            // Crée la commande
            var createOrderContent = new StringContent(
                JsonSerializer.Serialize(new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                new
                {
                    description = request.Description,
                    amount = new
                    {
                        currency_code = "USD",
                        value = request.Amount
                    }
                }
                    }
                }), Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var orderResponse = await client.PostAsync(
                "https://api-m.paypal.com/v2/checkout/orders",
                createOrderContent);

            var orderData = await orderResponse.Content.ReadAsStringAsync();

            if (!orderResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)orderResponse.StatusCode, $"Erreur lors de la création de la commande PayPal : {orderData}");
            }

            var json = JsonDocument.Parse(orderData).RootElement;

            if (!json.TryGetProperty("id", out var idElement))
            {
                return BadRequest("La réponse PayPal ne contient pas d'ID de commande.");
            }

            var orderId = idElement.GetString();

            return Ok(new { orderId });
        }
    }

    public class PaymentRequest
    {
        public string Description { get; set; }
        public string Amount { get; set; }
    }
}
