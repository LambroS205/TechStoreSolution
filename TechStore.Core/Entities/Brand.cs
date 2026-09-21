using System.Collections.Generic;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể thương hiệu sản phẩm (Apple, Samsung, Dell, Asus, Sony...)
/// </summary>
public class Brand
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}