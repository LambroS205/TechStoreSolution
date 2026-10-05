using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

/// <summary>
/// ViewModel phục vụ trang nhật ký hoạt động hệ thống và tra cứu vết kiểm toán
/// </summary>
public class AuditLogIndexViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public string CurrentModule { get; set; } = "All";
    public string? SearchQuery { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public List<string> AvailableModules { get; set; } = new();
}
