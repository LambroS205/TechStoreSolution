using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;
using TechStore.Web.Areas.Admin.Models;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Analytics.View")]
public class AnalyticsController : Controller
{
    private readonly TechStoreDbContext _context;

    public AnalyticsController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Bảng điều khiển phân tích kinh doanh, doanh thu, thanh toán và sản phẩm bán chạy
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .ToListAsync();

        var validPaidOrders = orders
            .Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid")
            .ToList();

        // 1. KPIs
        var totalRevenue = validPaidOrders.Sum(o => o.TotalAmount);
        var monthlyRevenue = validPaidOrders
            .Where(o => o.CreatedAt >= startOfMonth)
            .Sum(o => o.TotalAmount);

        var totalOrdersCount = orders.Count;
        var averageOrderValue = validPaidOrders.Any()
            ? validPaidOrders.Average(o => o.TotalAmount)
            : 0;

        var newCustomersThisMonth = await _context.Users
            .Where(u => u.CreatedAt >= startOfMonth)
            .CountAsync();

        // 2. Phân loại theo phương thức thanh toán
        var codOrders = orders.Where(o => o.PaymentMethod == "COD").ToList();
        var vietQrOrders = orders.Where(o => o.PaymentMethod == "VietQR").ToList();

        var codRevenue = codOrders.Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount);
        var vietQrRevenue = vietQrOrders.Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount);

        // 3. Biểu đồ doanh thu 6 tháng gần nhất
        var monthlyTrends = new List<MonthlyRevenueItem>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var mYear = targetMonth.Year;
            var mMonth = targetMonth.Month;
            var label = $"T{mMonth:D2}/{mYear}";

            var monthOrders = validPaidOrders
                .Where(o => o.CreatedAt.Year == mYear && o.CreatedAt.Month == mMonth)
                .ToList();

            monthlyTrends.Add(new MonthlyRevenueItem
            {
                MonthLabel = label,
                Revenue = monthOrders.Sum(o => o.TotalAmount),
                OrderCount = monthOrders.Count
            });
        }

        // 4. Phân bổ trạng thái đơn hàng
        var statusDistribution = orders
            .GroupBy(o => o.OrderStatus)
            .ToDictionary(g => g.Key, g => g.Count());

        // Đảm bảo đủ các trạng thái chính
        string[] standardStatuses = { "Pending", "Confirmed", "Processing", "Shipping", "Delivered", "Cancelled" };
        foreach (var st in standardStatuses)
        {
            if (!statusDistribution.ContainsKey(st))
                statusDistribution[st] = 0;
        }

        // 5. Top 5 sản phẩm bán chạy nhất
        var orderDetails = await _context.OrderDetails
            .Include(od => od.Variant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.Category)
            .Where(od => od.Order.OrderStatus != "Cancelled")
            .AsNoTracking()
            .ToListAsync();

        var topProducts = orderDetails
            .Where(od => od.Variant?.Product != null)
            .GroupBy(od => od.Variant.Product.ProductId)
            .Select(g =>
            {
                var first = g.First();
                return new TopProductItem
                {
                    ProductId = g.Key,
                    Name = first.Variant.Product.Name,
                    FeaturedImage = first.Variant.Product.FeaturedImage,
                    CategoryName = first.Variant.Product.Category?.Name ?? "Công nghệ",
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                };
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(5)
            .ToList();

        // 6. Đơn hàng giá trị cao gần đây
        var recentOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .ToList();

        var model = new AnalyticsDashboardViewModel
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            TotalOrdersCount = totalOrdersCount,
            AverageOrderValue = averageOrderValue,
            NewCustomersThisMonth = newCustomersThisMonth,
            CodOrdersCount = codOrders.Count,
            CodRevenue = codRevenue,
            VietQrOrdersCount = vietQrOrders.Count,
            VietQrRevenue = vietQrRevenue,
            MonthlyTrends = monthlyTrends,
            StatusDistribution = statusDistribution,
            TopProducts = topProducts,
            RecentOrders = recentOrders
        };

        return View(model);
    }

    /// <summary>
    /// Xuất danh sách đơn hàng sang file CSV UTF-8 kèm BOM
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportOrdersCsv()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã Đơn Hàng,Ngày Đặt,Khách Hàng,Số Điện Thoại,Địa Chỉ Nhận Hàng,Hình Thức Thanh Toán,Trạng Thái TT,Trạng Thái Đơn,Giảm Giá (VND),Phí Ship (VND),Tổng Tiền (VND)");

        foreach (var o in orders)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(o.OrderCode),
                EscapeCsv(o.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(o.CustomerName),
                EscapeCsv(o.CustomerPhone),
                EscapeCsv(o.ShippingAddress),
                EscapeCsv(o.PaymentMethod),
                EscapeCsv(o.PaymentStatus),
                EscapeCsv(o.OrderStatus),
                o.DiscountAmount.ToString("F0", CultureInfo.InvariantCulture),
                o.ShippingFee.ToString("F0", CultureInfo.InvariantCulture),
                o.TotalAmount.ToString("F0", CultureInfo.InvariantCulture)
            ));
        }

        var fileName = $"DonHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// Xuất danh sách khách hàng và lịch sử chi tiêu sang file CSV UTF-8 kèm BOM
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportCustomersCsv()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var userIds = users.Select(u => u.UserId).ToList();
        var allOrders = await _context.Orders
            .Where(o => o.UserId.HasValue && userIds.Contains(o.UserId.Value))
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã KH,Tên Đăng Nhập,Họ Và Tên,Email,Số Điện Thoại,Ngày Đăng Ký,Vai Trò,Trạng Thái,Số Đơn Đã Đặt,Tổng Chi Tiêu (VND)");

        foreach (var u in users)
        {
            var userOrders = allOrders.Where(o => o.UserId == u.UserId).ToList();
            var spent = userOrders.Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount);
            var roles = string.Join(" | ", u.UserRoles.Select(ur => ur.Role.RoleName));

            sb.AppendLine(string.Join(",",
                u.UserId,
                EscapeCsv(u.Username),
                EscapeCsv(u.FullName),
                EscapeCsv(u.Email),
                EscapeCsv(u.PhoneNumber ?? ""),
                EscapeCsv(u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(roles),
                EscapeCsv(u.IsActive ? "Hoạt động" : "Bị khóa"),
                userOrders.Count,
                spent.ToString("F0", CultureInfo.InvariantCulture)
            ));
        }

        var fileName = $"KhachHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// Xuất danh sách báo cáo hàng tồn kho và giá vốn/giá bán sang file CSV UTF-8 kèm BOM
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportInventoryCsv()
    {
        var variants = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .OrderBy(v => v.Product.Name)
            .ThenBy(v => v.VariantName)
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã SP,Tên Sản Phẩm,Danh Mục,Mã SKU,Tên Biến Thể,Giá Niêm Yết (VND),Giá Bán (VND),Số Lượng Tồn Kho,Cảnh Báo Kho");

        foreach (var v in variants)
        {
            var status = v.StockQuantity switch
            {
                <= 0 => "Hết hàng",
                <= 5 => "Sắp hết hàng (Nguy cấp)",
                <= 15 => "Tồn kho thấp",
                _ => "Ổn định"
            };

            sb.AppendLine(string.Join(",",
                v.ProductId,
                EscapeCsv(v.Product?.Name ?? ""),
                EscapeCsv(v.Product?.Category?.Name ?? ""),
                EscapeCsv(v.SKU),
                EscapeCsv(v.VariantName),
                v.OriginalPrice.ToString("F0", CultureInfo.InvariantCulture),
                v.SalePrice.ToString("F0", CultureInfo.InvariantCulture),
                v.StockQuantity,
                EscapeCsv(status)
            ));
        }

        var fileName = $"TonKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var sanitized = value.Replace("\"", "\"\"");
        return $"\"{sanitized}\"";
    }
}
