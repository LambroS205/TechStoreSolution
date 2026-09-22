using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Reviews.Manage")]
public class ReviewController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ReviewController(TechStoreDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Danh sách đánh giá sản phẩm với bộ lọc trạng thái và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? status = "All", string? q = null, int page = 1)
    {
        int pageSize = 15;
        var query = _context.ProductReviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .AsNoTracking()
            .AsQueryable();

        // 1. Lọc theo trạng thái kiểm duyệt
        if (status == "Approved")
        {
            query = query.Where(r => r.IsApproved);
        }
        else if (status == "Pending")
        {
            query = query.Where(r => !r.IsApproved);
        }

        // 2. Tìm kiếm theo tên sản phẩm hoặc tên khách hàng
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(r => r.Product.Name.ToLower().Contains(keyword) ||
                                     r.User.FullName.ToLower().Contains(keyword) ||
                                     (r.Comment != null && r.Comment.ToLower().Contains(keyword)));
        }

        int totalItems = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.SearchQuery = q;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = totalItems;

        // Thống kê nhanh
        ViewBag.TotalAll = await _context.ProductReviews.CountAsync();
        ViewBag.TotalApproved = await _context.ProductReviews.CountAsync(r => r.IsApproved);
        ViewBag.TotalPending = await _context.ProductReviews.CountAsync(r => !r.IsApproved);

        return View(reviews);
    }

    /// <summary>
    /// Bật/Tắt trạng thái kiểm duyệt đánh giá (Duyệt hoặc Ẩn)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var review = await _context.ProductReviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần thao tác!";
            return RedirectToAction(nameof(Index));
        }

        review.IsApproved = !review.IsApproved;
        await _context.SaveChangesAsync();

        int? currentUserId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid)) currentUserId = uid;

        await _auditLogService.LogAsync(
            action: review.IsApproved ? "APPROVE_REVIEW" : "UNAPPROVE_REVIEW",
            module: "Reviews",
            recordId: review.ReviewId.ToString(),
            oldValues: new { IsApproved = !review.IsApproved },
            newValues: new { IsApproved = review.IsApproved }
        );

        TempData["SuccessMessage"] = review.IsApproved
            ? $"Đã duyệt hiển thị đánh giá của sản phẩm '{review.Product.Name}'!"
            : $"Đã ẩn đánh giá của sản phẩm '{review.Product.Name}' khỏi giao diện!";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa đánh giá vi phạm tiêu chuẩn cộng đồng hoặc spam
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.ProductReviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần xóa!";
            return RedirectToAction(nameof(Index));
        }

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "DELETE_REVIEW",
            module: "Reviews",
            recordId: id.ToString(),
            oldValues: new { Comment = review.Comment, Rating = review.Rating },
            newValues: null
        );

        TempData["SuccessMessage"] = "Đã xóa đánh giá thành công!";
        return RedirectToAction(nameof(Index));
    }
}
