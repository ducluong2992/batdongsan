using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bds.Models
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dự án")]
        [StringLength(255, ErrorMessage = "Tên dự án không được vượt quá 255 ký tự")]
        [Display(Name = "Tên dự án")]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết dự án")]
        [Display(Name = "Mô tả chi tiết dự án")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ (số nhà, tên đường)")]

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        [Display(Name = "Địa chỉ (số nhà, tên đường)")]
        public string Location { get; set; }


        [Range(1, 1000000, ErrorMessage = "Diện tích phải lớn hơn 0 m²")]
        [Display(Name = "Diện tích (m²)")]
        public double? Area { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày kết thúc")]
        [DateGreaterThan("StartDate", ErrorMessage = "Ngày kết thúc phải sau ngày bắt đầu")]
        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string? Status { get; set; } // "Chờ duyệt", "Đã duyệt", "Từ chối"

        [Display(Name = "Ngày tạo")]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Range(0, int.MaxValue)]
        [Display(Name = "Lượt xem")]
        public int? ClickCount { get; set; } = 0;

        // --- Khóa ngoại ---
        [ForeignKey("User")]
        [Display(Name = "Người đăng dự án")]
        public int? UserID { get; set; }

        [ForeignKey("CommuneWard")]
        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        [Display(Name = "Phường/Xã")]
        public int? CommuneID { get; set; }
        // --- Navigation Properties ---
        public virtual User? User { get; set; }

        [ForeignKey("CommuneID")]
        public virtual CommuneWard? CommuneWard { get; set; }

        // Một dự án có nhiều ảnh
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
        public string? RejectReason { get; set; } = "";

    }

    // 🔹 Custom Validation Attribute: EndDate phải lớn hơn StartDate
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var currentValue = value as DateTime?;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                return new ValidationResult($"Không tìm thấy thuộc tính {_comparisonProperty}");

            var comparisonValue = property.GetValue(validationContext.ObjectInstance) as DateTime?;

            if (currentValue.HasValue && comparisonValue.HasValue && currentValue <= comparisonValue)
            {
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} phải sau {_comparisonProperty}");
            }

            return ValidationResult.Success;
        }
    }
}
