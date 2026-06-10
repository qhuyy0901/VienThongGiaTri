using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AspNetMvcApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AspNetMvcApp.Services;

public interface IChatbotService
{
    Task<string> GetResponseAsync(string userMessage);
}

public class ChatbotService : IChatbotService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public ChatbotService(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["Gemini:ApiKey"] ?? "";
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Xin lỗi, hệ thống Chatbot chưa được cấu hình API Key. Vui lòng liên hệ quản trị viên.";
        }

        // Lấy danh sách sản phẩm từ DB để làm context
        var products = await _context.Products.Include(p => p.Category).ToListAsync();
        
        var productListStr = new StringBuilder();
        foreach (var p in products)
        {
            productListStr.AppendLine($"- Tên: {p.Name}, Giá: {(p.Price * 25000):N0} đ, Danh mục: {p.Category?.Name}, Tình trạng: {(p.StockStatus == "InStock" ? "Còn hàng" : "Sắp hết hàng")}, Link: /Products/Details/{p.Id}");
        }

        var systemPrompt = $@"Bạn là nhân viên tư vấn bán hàng của cửa hàng Viễn Thông Gia Trí.
Bạn chỉ được phép trả lời các câu hỏi liên quan đến sản phẩm, danh mục, giá, khuyến mãi, tồn kho, cách mua hàng, giỏ hàng, đơn hàng và chính sách bán hàng trong website.
Không trả lời các câu hỏi ngoài phạm vi web bán hàng.
Nếu người dùng hỏi ngoài chủ đề, hãy trả lời đúng nguyên văn: 'Mình chỉ hỗ trợ tư vấn sản phẩm và mua hàng trên website này.'
Hãy gợi ý sản phẩm phù hợp nếu khách hỏi. Lấy dữ liệu từ danh sách sản phẩm sau:
{productListStr}

Nếu gợi ý sản phẩm, hãy cung cấp link chi tiết (ví dụ: Xem chi tiết sản phẩm này tại: /Products/Details/1).
Trả lời ngắn gọn, thân thiện, súc tích (dưới 100 chữ).";

        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { parts = new[] { new { text = userMessage } } }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

        // Retry up to 3 times
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);

                    var responseText = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return responseText ?? "Mình không hiểu ý bạn lắm.";
                }

                // Rate limited - wait and retry
                if ((int)response.StatusCode == 429 && attempt < 2)
                {
                    await Task.Delay(2000 * (attempt + 1));
                    continue;
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                // Hiển thị lỗi chi tiết để debug
                return $"Lỗi AI ({(int)response.StatusCode}): {errorBody.Substring(0, Math.Min(errorBody.Length, 200))}";
            }
            catch
            {
                if (attempt < 2)
                {
                    await Task.Delay(1000);
                    continue;
                }
                return "Xin lỗi, đã xảy ra lỗi kết nối. Vui lòng thử lại.";
            }
        }

        return "Hệ thống AI đang bận, vui lòng thử lại sau.";
    }
}
