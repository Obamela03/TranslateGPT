using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using TranslateGPT.DTOs;
using TranslateGPT.Models;

namespace TranslateGPT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly List<string> mostUsedLanguages = new List<string>()
        {
            "English",
            "Mandarin Chinese",
            "Spanish",
            "Hindi",
            "Arabic",
            "Bengali",
            "Portuguese",
            "Russian",
            "Japanese",
            "French",
            "German",
            "Urdu",
            "Italian",
            "Indonesian",
            "Vietnamese",
            "Turkish",
            "Korean",
            "Tamil",
            "Albanian"
        };

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            ViewBag.Languages = new SelectList(mostUsedLanguages);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OpenAIGPT(string query, string selectedLanguage)
        {
            var openAPIKey = _configuration["OpenAI:ApiKey"];
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAPIKey}");

            var payload = new
            {
                model = "gpt-4o",
                messages = new object[]
                {
            new { role = "system", content = $"Translate to {selectedLanguage}" },
            new { role = "user", content = query }
                },
                temperature = 0,
                max_tokens = 256
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            HttpContent httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var responseMessage = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
            var responseMessageJson = await responseMessage.Content.ReadAsStringAsync();

            // Log the raw response for debugging
            _logger.LogInformation($"OpenAI API Response: {responseMessageJson}");
            _logger.LogInformation($"Status Code: {responseMessage.StatusCode}");

            if (!responseMessage.IsSuccessStatusCode)
            {
                ViewBag.Result = $"API Error: {responseMessage.StatusCode} - {responseMessageJson}";
                ViewBag.Languages = new SelectList(mostUsedLanguages);
                return View("Index");
            }

            var response = JsonConvert.DeserializeObject<OpenAIResponse>(responseMessageJson);

            if (response?.Choices?.Count > 0 && response.Choices[0].Message != null)
            {
                ViewBag.Result = response.Choices[0].Message.Content;
            }
            else
            {
                ViewBag.Result = "No response from OpenAI. Raw API response: " + responseMessageJson;
            }

            ViewBag.Languages = new SelectList(mostUsedLanguages);
            return View("Index");
        }
    }
}