using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Dashboard.View")]
public class DashboardController : Controller
{
    private readonly TechStoreDbContext _context;

    public DashboardController(TechStoreDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. Thống kê tổng quan
        var totalRevenue = await _context.Orders
            .AsNoTracking()
            .Where(o => o.PaymentStatus == "Paid" || o.OrderStatus == "Delivered")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var totalOrders = await _context.Orders.AsNoTracking().CountAsync();
        var pendingVietQr = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.PaymentMethod == "VietQR" && o.PaymentStatus == "Pending");

        var totalProducts = await _context.Products.AsNoTracking().CountAsync();
        var totalCustomers = await _context.Users
            .AsNoTracking()
            .CountAsync(u => u.UserRoles.Any(ur => ur.Role.NormalizedName == "CUSTOMER"));

        // 2. Thống kê trạng thái đơn hàng
        var pendingOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == "Pending");
        var processingOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == "Processing");
        var shippingOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == "Shipping");
        var deliveredOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == "Delivered");
        var cancelledOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == "Cancelled");

        // 3. Biến thể sản phẩm sắp hết hàng (Tồn kho <= 20)
        var lowStockVariants = await _context.ProductVariants
            .AsNoTracking()
            .Include(pv => pv.Product)
            .Where(pv => pv.StockQuantity <= 20 && pv.IsActive)
            .OrderBy(pv => pv.StockQuantity)
            .Take(6)
            .Select(pv => new LowStockItemDto
            {
                VariantId = pv.VariantId,
                ProductId = pv.ProductId,
                ProductName = pv.Product.Name,
                VariantName = pv.VariantName,
                SKU = pv.SKU,
                StockQuantity = pv.StockQuantity,
                ThumbnailImage = pv.ThumbnailImage ?? pv.Product.FeaturedImage
            })
            .ToListAsync();

        // 4. Đơn hàng gần nhất
        var recentOrders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .ToListAsync();

        // 5. Nhật ký kiểm toán gần nhất
        var recentAuditLogs = await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .ToListAsync();

        // 6. Doanh thu 6 tháng gần nhất cho biểu đồ
        var now = DateTime.UtcNow;
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var revenue = await _context.Orders
                .AsNoTracking()
                .Where(o => (o.PaymentStatus == "Paid" || o.OrderStatus == "Delivered")
                            && o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                MonthLabel = $"T{targetMonth.Month}/{targetMonth.Year}",
                Revenue = revenue
            });
        }

        var viewModel = new DashboardViewModel
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            PendingVietQrCount = pendingVietQr,
            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            PendingOrders = pendingOrders,
            ProcessingOrders = processingOrders,
            ShippingOrders = shippingOrders,
            DeliveredOrders = deliveredOrders,
            CancelledOrders = cancelledOrders,
            LowStockVariants = lowStockVariants,
            RecentOrders = recentOrders,
            RecentAuditLogs = recentAuditLogs,
            MonthlyRevenues = monthlyRevenue
        };

        return View(viewModel);
    }
}

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingVietQrCount { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }

    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }

    public List<LowStockItemDto> LowStockVariants { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenues { get; set; } = new();
}

public class LowStockItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string? ThumbnailImage { get; set; }
}

public class MonthlyRevenueDto
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}
