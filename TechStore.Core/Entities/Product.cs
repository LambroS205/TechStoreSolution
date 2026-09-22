using System;
using System.Collections.Generic;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể thông tin sản phẩm chung (Base Product)
/// </summary>
public class Product
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public int? SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string FeaturedImage { get; set; } = string.Empty;
    public int WarrantyMonths { get; set; } = 12;

    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Category Category { get; set; } = null!;
    public virtual Brand Brand { get; set; } = null!;
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}