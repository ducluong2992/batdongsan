using System.ComponentModel.DataAnnotations;
namespace bds.Models
{
    public class Register

    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Tên đăng nhập từ 4–20 ký tự.")]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ, số và dấu gạch dưới.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6–20 ký tự.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại bắt đầu bằng 0 và gồm 10–11 chữ số.")]
        public string PhoneNumber { get; set; }
    }
}
