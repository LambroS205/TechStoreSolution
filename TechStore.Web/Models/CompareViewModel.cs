using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Models;

/// <summary>
/// ViewModel phục vụ trang so sánh chi tiết sản phẩm
/// </summary>
public class CompareViewModel
{
    public List<Product> Products { get; set; } = new List<Product>();
    public List<Product> SuggestedProducts { get; set; } = new List<Product>();
}
