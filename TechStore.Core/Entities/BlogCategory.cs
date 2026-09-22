using System.Collections.Generic;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể danh mục bài viết tin tức công nghệ
/// </summary>
public class BlogCategory
{
    public int BlogCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    // Navigation Properties
    public virtual ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}
