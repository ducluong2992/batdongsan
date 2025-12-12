using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace bds.Controllers
{
    public class AiController : Controller
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public AiController(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];
            _httpClient = new HttpClient();
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] UserMessage req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.message))
                return Json(new { reply = "Bạn chưa nhập câu hỏi nào cả." });

            var systemPrompt = @"
        Bạn là trợ lý ảo chuyên nghiệp của sàn bất động sản 'HHL RealEstate'.

        --- 1. THÔNG TIN HỖ TRỢ CHUNG ---
        - Đăng kí cần số điện thoại và email (bắt buộc).
        - Khi khách chưa đăng nhập thì không thể đăng bài/ dự án/ thêm yêu thích... chỉ xem được những chức năng cơ bản (phải đăng nhập).
        - Phí đăng tin/dự án: 10xu/tin  (hiển thị 20 ngày).
        - Phí sửa tin/dự án: 10xu/tin (không reset lại ngày đăng).
        - Nạp tiền: đổi tiền qua Coins, dùng Coins để đăng/sửa bài đăng/ dự án.
        - Địa chỉ công ty: Đường Láng, Hà Nội. Hotline: 0912 345 999.
        - Đăng tin: Cần đăng nhập -> Chọn 'Mua bán' -> 'Đăng bài viết của bạn' -> Điền đủ thông tin và nhấn Đăng bài (cần chờ để Admin duyệt bài).
        - Đăng dự án: Cần đăng nhập -> Chọn 'Dự án' -> 'Đăng dự án của bạn' -> Điền đủ thông tin và nhấn Đăng dự án (cần chờ để Admin duyệt bài).
        - Mục Phân tích và đánh giá: Gồm biểu đồ Mua bán theo xu hướng khu vực (top 10 tỉnh thành có nhiều lượt đăng rao bán nhất)
        và biểu đồ Xu hướng giá (xem xu hướng giá cả thay đổi theo tháng của từng khu vực, có thể xem theo từng tỉnh thành).

        --- 2. QUY TẮC TRẢ LỜI ---
        - Nếu khách hỏi tư vấn (đầu tư, pháp lý): Trả lời ngắn gọn, khách quan dựa trên kiến thức bất động sản chung.
        - Giọng điệu: Thân thiện, xưng 'Em', gọi khách là 'Anh/Chị'.
        - Câu trả lời ngắn gọn dưới 100 từ.
        ";

            var fullPrompt = $"{systemPrompt}\n\nKhách hỏi: {req.message}\nTrả lời:";

            var payload = new
            {
                contents = new[]
                {
                            new {
                                parts = new[] {
                                    new { text = fullPrompt }
                                }
                            }
                        }
            };

            var jsonContent = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            // XỬ LÝ LỖI TỐT HƠN
            if (!response.IsSuccessStatusCode)
            {
                // Kiểm tra nếu là lỗi 429 (Too Many Requests)
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    return Json(new { reply = "Hệ thống đang quá tải, vui lòng thử lại sau 1 phút nữa ạ." });
                }

                // Các lỗi khác (400, 500...)
                // Log lỗi ra console để dev xem, không show cho user
                Console.WriteLine($"Gemini Error: {responseString}");
                return Json(new { reply = "Em đang gặp chút sự cố kết nối, Anh/Chị đợi lát rồi hỏi lại nhé!" });
            }

            dynamic data = JsonConvert.DeserializeObject(responseString);

            try
            {
                string aiText = data.candidates[0].content.parts[0].text;
                return Json(new { reply = aiText });
            }
            catch
            {
                return Json(new { reply = "AI không trả lời được. Bạn thử hỏi lại nội dung khác nhé!" });
            }
        }
    }

    public class UserMessage
    {
        public string message { get; set; }
    }





}
