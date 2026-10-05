using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Models;

/// <summary>
/// ViewModel phục vụ trang danh sách sản phẩm, phân trang và bộ lọc nâng cao
/// </summary>
public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public Category? CurrentCategory { get; set; }
    public List<Brand> Brands { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public string? SelectedBrand { get; set; }
    public string? SelectedStorage { get; set; }
    public string? SelectedRam { get; set; }
    public decimal? SelectedMinPrice { get; set; }
    public decimal? SelectedMaxPrice { get; set; }
    public string SelectedSort { get; set; } = "default";
}

/// <summary>
/// ViewModel định kiểu tường minh cho trang chi tiết sản phẩm, loại bỏ ViewBag
/// </summary>
public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    public List<Product> RelatedProducts { get; set; } = new();
    public List<ProductReview> Reviews { get; set; } = new();
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; } = 5.0;
    public int Count5 { get; set; }
    public int Count4 { get; set; }
    public int Count3 { get; set; }
    public int Count2 { get; set; }
    public int Count1 { get; set; }
    public bool HasPurchased { get; set; }
    public bool HasReviewed { get; set; }
}
