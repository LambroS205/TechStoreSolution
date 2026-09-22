using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Suppliers.Manage")]
public class SupplierController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public SupplierController(TechStoreDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Danh sách đối tác nhà cung cấp thiết bị và linh kiện
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search) ||
                                     (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(search)) ||
                                     s.Phone.Contains(search) ||
                                     (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        var suppliers = await query.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name).ToListAsync();
        ViewBag.Search = q;
        return View(suppliers);
    }

    /// <summary>
    /// Thêm mới hoặc cập nhật thông tin nhà cung cấp
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Supplier model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["ErrorMessage"] = "Tên nhà cung cấp không được để trống!";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(model.Phone))
        {
            TempData["ErrorMessage"] = "Số điện thoại nhà cung cấp không được để trống!";
            return RedirectToAction(nameof(Index));
        }

        if (model.SupplierId == 0)
        {
            await _context.Suppliers.AddAsync(model);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CREATE_SUPPLIER",
                module: "Suppliers",
                recordId: model.SupplierId.ToString(),
                oldValues: null,
                newValues: System.Text.Json.JsonSerializer.Serialize(new { model.SupplierId, model.Name, model.Phone }));

            TempData["SuccessMessage"] = $"Đã thêm nhà cung cấp \"{model.Name}\" thành công!";
        }
        else
        {
            var supplier = await _context.Suppliers.FindAsync(model.SupplierId);
            if (supplier == null)
            {
                return NotFound();
            }

            string oldValues = System.Text.Json.JsonSerializer.Serialize(new { supplier.Name, supplier.ContactPerson, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive });

            supplier.Name = model.Name.Trim();
            supplier.ContactPerson = model.ContactPerson?.Trim();
            supplier.Phone = model.Phone.Trim();
            supplier.Email = model.Email?.Trim();
            supplier.Address = model.Address?.Trim();
            supplier.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UPDATE_SUPPLIER",
                module: "Suppliers",
                recordId: supplier.SupplierId.ToString(),
                oldValues: oldValues,
                newValues: System.Text.Json.JsonSerializer.Serialize(new { supplier.Name, supplier.ContactPerson, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive }));

            TempData["SuccessMessage"] = $"Đã cập nhật nhà cung cấp \"{supplier.Name}\"!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa nhà cung cấp
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            string name = supplier.Name;
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DELETE_SUPPLIER",
                module: "Suppliers",
                recordId: id.ToString(),
                oldValues: System.Text.Json.JsonSerializer.Serialize(new { supplier.SupplierId, supplier.Name }),
                newValues: null);

            TempData["SuccessMessage"] = $"Đã xóa nhà cung cấp \"{name}\"!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Thay đổi trạng thái hoạt động của nhà cung cấp
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            supplier.IsActive = !supplier.IsActive;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "TOGGLE_SUPPLIER_STATUS",
                module: "Suppliers",
                recordId: id.ToString(),
                oldValues: null,
                newValues: System.Text.Json.JsonSerializer.Serialize(new { supplier.SupplierId, supplier.IsActive }));

            TempData["SuccessMessage"] = $"Đã thay đổi trạng thái đối tác \"{supplier.Name}\" thành {(supplier.IsActive ? "Đang hợp tác" : "Tạm ngưng")}!";
        }

        return RedirectToAction(nameof(Index));
    }
}
