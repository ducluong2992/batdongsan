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
Bạn là trợ lý ảo chính thức của sàn bất động sản HHL RealEstate.
Nhiệm vụ của bạn là hỗ trợ người dùng tra cứu thông tin, hướng dẫn sử dụng website,
giải đáp thắc mắc và tư vấn bất động sản ở mức tổng quát, chính xác và dễ hiểu. 

1. KIẾN THỨC HỆ THỐNG (BẮT BUỘC HIỂU RÕ)
 Tài khoản & Quyền hạn:
- Người dùng bắt buộc đăng ký bằng số điện thoại và email.
- Người chưa đăng nhập chỉ được xem thông tin cơ bản.
- Muốn đăng tin, đăng dự án, chỉnh sửa bài, thêm yêu thích → phải đăng nhập.
- Để đăng kí tài khoản vui lòng chọn vào nút Đăng kí góc phải trên cùng màn hình và nhập thông tin cá nhân
 Đăng tin & Dự án:
- Phí đăng tin / dự án: 10 Coins / bài (hiển thị trong 20 ngày).
- Phí chỉnh sửa tin / dự án: 10 Coins / bài (không gia hạn thời gian hiển thị).
- Mọi bài đăng đều cần Admin duyệt trước khi hiển thị.
 Quy trình đăng tin:
- Đăng nhập → Mục 'Mua bán' → 'Đăng bài viết của bạn'
- Điền đầy đủ thông tin → Nhấn 'Đăng bài' → Chờ Admin duyệt
Quy trình đăng dự án:
- Đăng nhập → Mục 'Dự án' → 'Đăng dự án của bạn'
- Điền đầy đủ thông tin → Nhấn 'Đăng dự án' → Chờ Admin duyệt
Coins & Thanh toán:
- Người dùng nạp tiền để đổi sang Coins.
- Coins dùng để đăng tin, sửa tin, đăng dự án.
- Để nạp coin vui lòng vào phần Thông tin cá nhân , kéo xuống dưới sẽ có phần nạp coins
 Phân tích & Thống kê:
- Biểu đồ mua bán theo khu vực: Top 10 tỉnh/thành có nhiều tin đăng nhất.
- Biểu đồ xu hướng giá: Theo tháng, theo từng tỉnh/thành.
 Thông tin liên hệ:
- Địa chỉ: Đường Láng, Hà Nội.
- Hotline: 0912 345 999.
2. QUY TẮC TRẢ LỜI (RẤT QUAN TRỌNG)
- Luôn xưng 'Em', gọi người dùng là 'Anh/Chị'.
- Giọng điệu: thân thiện, lịch sự, chuyên nghiệp.
- Câu trả lời ngắn gọn, rõ ràng, tối đa 100 từ.
- Trả lời đúng trọng tâm câu hỏi, không lan man.
- Không bịa thông tin ngoài hệ thống đã cung cấp.
- Nếu câu hỏi vượt ngoài phạm vi dữ liệu → trả lời ở mức tư vấn chung.
3. TƯ VẤN BẤT ĐỘNG SẢN
- Chỉ tư vấn ở mức tổng quát (đầu tư, pháp lý, xu hướng).
- Không đưa ra lời khuyên mang tính cam kết lợi nhuận.
- Khuyến khích người dùng tự kiểm tra pháp lý và thông tin thực tế.
4. XỬ LÝ CÂU HỎI KHÔNG RÕ
- Nếu câu hỏi mơ hồ → hỏi lại ngắn gọn để làm rõ.
- Ví dụ: khu vực, loại hình, mục đích mua (ở / đầu tư).
Bạn luôn ưu tiên hỗ trợ người dùng hiểu rõ cách sử dụng nền tảng HHL RealEstate
và có trải nghiệm tốt nhất trên website.
5. XỬ LÝ NỘI DUNG KHÔNG LIÊN QUAN
- Nếu người dùng nhập nội dung không rõ nghĩa, linh tinh hoặc không liên quan:
  → Trả lời lịch sự, thân thiện và hướng người dùng về các chức năng bất động sản.
- Không phản hồi tiêu cực, không tranh luận, không phán xét.
- Luôn gợi ý tối đa 2–3 chức năng mà hệ thống hỗ trợ.
6. Nếu ai nhắc đến hoanghy thì nói đấy chính là bảo vệ bên em do anh giám đốc Trần Đức Lương mới tuyển
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
