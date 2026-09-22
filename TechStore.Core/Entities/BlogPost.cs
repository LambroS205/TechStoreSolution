using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể bài viết tin tức, đánh giá công nghệ và mẹo sử dụng thiết bị
/// </summary>
public class BlogPost
{
    public int PostId { get; set; }
    public int BlogCategoryId { get; set; }
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int ViewCount { get; set; } = 0;
    public bool IsPublished { get; set; } = true;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual BlogCategory Category { get; set; } = null!;
    public virtual User Author { get; set; } = null!;
}
