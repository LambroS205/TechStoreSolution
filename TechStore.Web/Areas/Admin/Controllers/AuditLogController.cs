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
[HasPermission("AuditLogs.View")]
public class AuditLogController : Controller
{
    private readonly TechStoreDbContext _context;

    public AuditLogController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hiển thị danh sách Nhật ký Kiểm toán hệ thống
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? module, string? search, int page = 1)
    {
        int pageSize = 20;
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => 
                a.Action.Contains(search) || 
                (a.RecordId != null && a.RecordId.Contains(search)) ||
                (a.User != null && a.User.Username.Contains(search)) ||
                (a.IpAddress != null && a.IpAddress.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var availableModules = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Module)
            .Distinct()
            .ToListAsync();

        var viewModel = new AuditLogIndexViewModel
        {
            Logs = logs,
            CurrentModule = module ?? "All",
            SearchQuery = search,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalCount = totalCount,
            AvailableModules = availableModules
        };

        return View(viewModel);
    }
}

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
