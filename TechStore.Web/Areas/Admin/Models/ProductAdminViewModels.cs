using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

/// <summary>
/// ViewModel phục vụ tạo mới sản phẩm và biến thể mặc định ban đầu
/// </summary>
public class ProductCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [Display(Name = "Tên sản phẩm")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không quá 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
    public int BrandId { get; set; }

    public string? FeaturedImage { get; set; }

    [Range(0, 120, ErrorMessage = "Thời gian bảo hành từ 0 đến 120 tháng")]
    public int WarrantyMonths { get; set; } = 12;

    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Biến thể mặc định đầu tiên
    [Required(ErrorMessage = "Vui lòng nhập mã SKU mặc định")]
    public string DefaultSku { get; set; } = string.Empty;

    public string? DefaultBarcode { get; set; }
    public string? DefaultVariantName { get; set; }
    public decimal DefaultOriginalPrice { get; set; }
    public decimal DefaultSalePrice { get; set; }
    public int DefaultStockQuantity { get; set; } = 10;
}

/// <summary>
/// ViewModel phục vụ chỉnh sửa thông tin sản phẩm, quản lý biến thể và thư viện ảnh
/// </summary>
public class ProductEditViewModel
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public string? FeaturedImage { get; set; }
    public int WarrantyMonths { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }

    public List<ProductVariant> Variants { get; set; } = new();
    public List<ProductImage> GalleryImages { get; set; } = new();
}

/// <summary>
/// DTO phục vụ lưu / cập nhật biến thể sản phẩm qua Ajax
/// </summary>
public class SaveVariantDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int WeightGrams { get; set; } = 200;
    public string? ThumbnailImage { get; set; }
    public bool IsActive { get; set; } = true;
}
