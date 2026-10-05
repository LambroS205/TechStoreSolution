using System.ComponentModel.DataAnnotations;

namespace TechStore.Web.Areas.Admin.Models;

/// <summary>
/// ViewModel phục vụ chức năng nhập kho sản phẩm và biến thể trong quản trị
/// </summary>
public class ImportInventoryViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn sản phẩm / biến thể cần nhập kho!")]
    public int VariantId { get; set; }

    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng nhập kho!")]
    [Range(1, 100000, ErrorMessage = "Số lượng nhập kho phải lớn hơn 0!")]
    public int Quantity { get; set; } = 10;

    [Required(ErrorMessage = "Vui lòng nhập giá nhập đơn vị!")]
    [Range(0, 1000000000, ErrorMessage = "Giá nhập đơn vị không hợp lệ!")]
    public decimal UnitPrice { get; set; }

    public string? ReferenceCode { get; set; }

    public string? Note { get; set; }
}
