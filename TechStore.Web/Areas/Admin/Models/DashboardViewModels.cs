using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

/// <summary>
/// ViewModel tổng hợp cho bảng điều khiển Dashboard Admin
/// </summary>
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
