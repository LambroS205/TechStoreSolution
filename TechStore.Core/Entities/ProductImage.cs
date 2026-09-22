namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể lưu trữ các góc chụp bổ sung của sản phẩm trong thư viện ảnh
/// </summary>
public class ProductImage
{
    public int ImageId { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;

    // Navigation Properties
    public virtual Product Product { get; set; } = null!;
}
