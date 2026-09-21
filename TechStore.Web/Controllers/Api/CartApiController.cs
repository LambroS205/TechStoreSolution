using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers.Api;

[ApiController]
[Route("api/cart")]
public class CartApiController : ControllerBase
{
    private readonly TechStoreDbContext _context;

    public CartApiController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API tự động gộp (Auto-merge) các mặt hàng từ LocalStorage vào CSDL User Cart khi đăng nhập
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncLocalStorageCart([FromBody] List<CartItemRequest> items)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để đồng bộ giỏ hàng." });
        }

        // 1. Lấy hoặc tạo Giỏ hàng Database của User
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
        }

        // 2. Gộp các mặt hàng từ client LocalStorage vào giỏ hàng Database
        if (items != null && items.Any())
        {
            var variantIds = items.Select(i => i.VariantId).ToList();
            var validVariants = await _context.ProductVariants
                .Where(v => variantIds.Contains(v.VariantId) && v.IsActive)
                .ToDictionaryAsync(v => v.VariantId);

            foreach (var req in items)
            {
                if (!validVariants.ContainsKey(req.VariantId)) continue;

                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.VariantId == req.VariantId);
                if (existingItem != null)
                {
                    existingItem.Quantity += req.Quantity; // Cộng dồn số lượng
                }
                else
                {
                    cart.CartItems.Add(new CartItem
                    {
                        CartId = cart.CartId,
                        VariantId = req.VariantId,
                        Quantity = req.Quantity,
                        AddedAt = DateTime.UtcNow
                    });
                }
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // 3. Trả về toàn bộ danh sách giỏ hàng sau khi đã gộp hoàn chỉnh
        var responseItems = cart.CartItems.Select(ci => new
        {
            ci.VariantId,
            ci.Quantity
        }).ToList();

        return Ok(new
        {
            success = true,
            mergedItems = responseItems,
            message = "Đã gộp giỏ hàng LocalStorage vào tài khoản thành công!"
        });
    }

    /// <summary>
    /// API nhận mảng { variantId, quantity } từ LocalStorage phía trình duyệt,
    /// truy xuất Database và trả về chi tiết sản phẩm thời gian thực (giá, tên, ảnh, tồn kho)
    /// </summary>
    [HttpPost("details")]
    public async Task<IActionResult> GetCartDetails([FromBody] List<CartItemRequest> items)
    {
        if (items == null || !items.Any())
        {
            return Ok(new CartSummaryResponse
            {
                Items = new List<CartItemDto>(),
                SubTotal = 0,
                TotalCount = 0
            });
        }

        var variantIds = items.Select(i => i.VariantId).Distinct().ToList();

        var variants = await _context.ProductVariants
            .Include(v => v.Product)
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId) && v.IsActive)
            .ToListAsync();

        var cartItems = new List<CartItemDto>();
        decimal subTotal = 0;
        int totalCount = 0;

        foreach (var req in items)
        {
            var variant = variants.FirstOrDefault(v => v.VariantId == req.VariantId);
            if (variant != null)
            {
                // Giới hạn số lượng không vượt quá tồn kho thực tế
                int validQuantity = Math.Min(req.Quantity, Math.Max(variant.StockQuantity, 1));
                decimal lineTotal = variant.SalePrice * validQuantity;
                subTotal += lineTotal;
                totalCount += validQuantity;

                cartItems.Add(new CartItemDto
                {
                    VariantId = variant.VariantId,
                    ProductId = variant.ProductId,
                    ProductName = variant.Product.Name,
                    VariantName = variant.VariantName,
                    ProductSlug = variant.Product.Slug,
                    SKU = variant.SKU,
                    ThumbnailImage = string.IsNullOrEmpty(variant.ThumbnailImage) ? variant.Product.FeaturedImage : variant.ThumbnailImage,
                    SalePrice = variant.SalePrice,
                    OriginalPrice = variant.OriginalPrice,
                    StockQuantity = variant.StockQuantity,
                    Quantity = validQuantity,
                    LineTotal = lineTotal
                });
            }
        }

        return Ok(new CartSummaryResponse
        {
            Items = cartItems,
            SubTotal = subTotal,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// API kiểm tra tính hợp lệ của mã giảm giá (Coupon/Voucher)
    /// </summary>
    [HttpPost("apply-coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CouponCode))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập mã giảm giá!" });
        }

        var now = DateTime.UtcNow;
        var coupon = await _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == request.CouponCode.Trim() && c.IsActive && c.StartDate <= now && c.EndDate >= now);

        if (coupon == null)
        {
            return BadRequest(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn sử dụng!" });
        }

        if (coupon.UsageCount >= coupon.UsageLimit)
        {
            return BadRequest(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng!" });
        }

        if (request.SubTotal < coupon.MinOrderAmount)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Mã này chỉ áp dụng cho đơn hàng từ {coupon.MinOrderAmount:N0} đ trở lên!"
            });
        }

        decimal discount = 0;
        if (coupon.DiscountType == "Percentage")
        {
            discount = (request.SubTotal * coupon.DiscountValue) / 100m;
            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            {
                discount = coupon.MaxDiscountAmount.Value;
            }
        }
        else
        {
            discount = coupon.DiscountValue;
        }

        return Ok(new
        {
            success = true,
            couponId = coupon.CouponId,
            couponCode = coupon.Code,
            description = coupon.Description,
            discountAmount = discount,
            message = $"Áp dụng mã '{coupon.Code}' thành công! Giảm {discount:N0} đ"
        });
    }
}

public class CartItemRequest
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

public class CartItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string ThumbnailImage { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int StockQuantity { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class CartSummaryResponse
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public int TotalCount { get; set; }
}

public class ApplyCouponRequest
{
    public string CouponCode { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
}