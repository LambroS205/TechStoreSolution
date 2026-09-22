using System;
using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

public class AnalyticsDashboardViewModel
{
    // KPIs
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalOrdersCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int NewCustomersThisMonth { get; set; }

    // Payment Method Breakdown
    public int CodOrdersCount { get; set; }
    public decimal CodRevenue { get; set; }
    public int VietQrOrdersCount { get; set; }
    public decimal VietQrRevenue { get; set; }

    // Monthly Chart Data
    public List<MonthlyRevenueItem> MonthlyTrends { get; set; } = new();

    // Order Status Breakdown
    public Dictionary<string, int> StatusDistribution { get; set; } = new();

    // Top Selling Products
    public List<TopProductItem> TopProducts { get; set; } = new();

    // Recent High Value Orders
    public List<Order> RecentOrders { get; set; } = new();
}

public class MonthlyRevenueItem
{
    public string MonthLabel { get; set; } = string.Empty; // e.g., "T04/2026"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class TopProductItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FeaturedImage { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
