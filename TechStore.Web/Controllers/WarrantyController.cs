using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

public class WarrantyController : Controller
{
    private readonly TechStoreDbContext _context;

    public WarrantyController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Trung tâm tra cứu Bảo hành Điện tử & Thiết bị chính hãng TechStore
    /// Hỗ trợ tra cứu theo Số điện thoại đặt hàng hoặc Mã đơn hàng (ORD...)
    /// </summary>
    [HttpGet]
    [HttpPost]
    [Route("warranty-check")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index(string? query)
    {
        var model = new WarrantyLookupViewModel
        {
            Query = query?.Trim()
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            string term = query.Trim();
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Brand)
                .AsNoTracking()
                .Where(o => (o.CustomerPhone == term || o.OrderCode == term) && o.OrderStatus != "Cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var order in orders)
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Variant?.Product == null) continue;

                    var prod = detail.Variant.Product;
                    int warrantyMonths = prod.WarrantyMonths > 0 ? prod.WarrantyMonths : 12;
                    var activatedDate = order.CreatedAt;
                    var expireDate = activatedDate.AddMonths(warrantyMonths);
                    var daysRemaining = (expireDate - now).Days;
                    double totalDays = (expireDate - activatedDate).TotalDays;
                    int percent = totalDays > 0 ? Math.Clamp((int)((daysRemaining / totalDays) * 100), 0, 100) : 0;

                    model.Results.Add(new WarrantyDeviceItemDto
                    {
                        OrderCode = order.OrderCode,
                        CustomerName = order.CustomerName,
                        CustomerPhone = order.CustomerPhone,
                        ProductName = prod.Name,
                        ProductSlug = prod.Slug,
                        VariantName = detail.VariantName,
                        SKU = detail.SKU,
                        ProductImage = string.IsNullOrEmpty(detail.Variant.ThumbnailImage) ? prod.FeaturedImage : detail.Variant.ThumbnailImage,
                        BrandName = prod.Brand?.Name ?? "TechStore",
                        SerialNumber = $"TS{prod.BrandId:D2}-{detail.VariantId:D4}-{order.OrderId:D5}",
                        PurchaseDate = order.CreatedAt,
                        WarrantyMonths = warrantyMonths,
                        ExpireDate = expireDate,
                        DaysRemaining = Math.Max(0, daysRemaining),
                        Status = daysRemaining > 30 ? "Active" : daysRemaining > 0 ? "ExpiringSoon" : "Expired",
                        ProgressPercent = percent
                    });
                }
            }

            model.HasSearched = true;
        }

        return View(model);
    }
}

public class WarrantyLookupViewModel
{
    public string? Query { get; set; }
    public bool HasSearched { get; set; }
    public List<WarrantyDeviceItemDto> Results { get; set; } = new();
}

public class WarrantyDeviceItemDto
{
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public int WarrantyMonths { get; set; }
    public DateTime ExpireDate { get; set; }
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = "Active"; // Active, ExpiringSoon, Expired
    public int ProgressPercent { get; set; }
}

