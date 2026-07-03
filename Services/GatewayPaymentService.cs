using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services
{
    public class GatewayPaymentService
    {
        private readonly HttpClient _httpClient;
        private const string AccessToken = "SK_KWT_pYO2mEerKRr4YWlJwYC04l556wBZo0pzCrf5p0WthjLPZZyon0VsVwZfz1N5TebS";
        private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

        public GatewayPaymentService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<GatewayPaymentResponse?> RequestPaymentLinkAsync(GatewayPaymentRequest request)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = null, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

            // إرسال الطلب بعد أن أصبح السيرفر يرى المتصفح كـ Chrome حقيقي
            var response = await _httpClient.PostAsJsonAsync(new Uri("https://demo.myfatoorah.com/v2/ExecutePayment"), request, options);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"MyFatoorah Error Details: {errorContent}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GatewayPaymentResponse>(_serializerOptions);
        }
        public async Task<GatewayPaymentResponse?> QueryPaymentStatusAsync(string paymentId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            var body = new { Key = paymentId, KeyType = "PaymentId" };

            var response = await _httpClient.PostAsJsonAsync(new Uri("https://demo.myfatoorah.com/v2/GetPaymentStatus"), body, new JsonSerializerOptions { PropertyNamingPolicy = null });
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<GatewayPaymentResponse>(_serializerOptions);
        }
    }
}