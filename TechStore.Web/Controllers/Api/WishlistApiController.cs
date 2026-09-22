using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers.Api;

[ApiController]
[Route("api/wishlist")]
public class WishlistApiController : ControllerBase
{
    private readonly TechStoreDbContext _context;

    public WishlistApiController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Bật/Tắt lưu sản phẩm vào danh sách yêu thích
    /// </summary>
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromQuery] int productId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để lưu sản phẩm yêu thích!" });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
        {
            return NotFound(new { success = false, message = "Không tìm thấy sản phẩm!" });
        }

        var existing = await _context.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        bool isWishlisted;

        if (existing != null)
        {
            _context.Wishlists.Remove(existing);
            isWishlisted = false;
        }
        else
        {
            await _context.Wishlists.AddAsync(new Wishlist
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow
            });
            isWishlisted = true;
        }

        await _context.SaveChangesAsync();

        int totalCount = await _context.Wishlists.CountAsync(w => w.UserId == userId);

        return Ok(new
        {
            success = true,
            isWishlisted = isWishlisted,
            count = totalCount,
            message = isWishlisted ? "Đã thêm vào danh sách yêu thích!" : "Đã xóa khỏi danh sách yêu thích!"
        });
    }

    /// <summary>
    /// Lấy tổng số lượng sản phẩm yêu thích của người dùng hiện tại
    /// </summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new { count = 0 });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            int count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            return Ok(new { count });
        }

        return Ok(new { count = 0 });
    }

    /// <summary>
    /// Lấy danh sách ID các sản phẩm đã yêu thích của người dùng hiện tại
    /// </summary>
    [HttpGet("ids")]
    public async Task<IActionResult> GetWishlistProductIds()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(Array.Empty<int>());
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            var ids = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            return Ok(ids);
        }

        return Ok(Array.Empty<int>());
    }
}
