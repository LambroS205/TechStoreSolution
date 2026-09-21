using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Common;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Products.View")]
public class ProductController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;
    private readonly HtmlSanitizer _sanitizer;

    public ProductController(
        TechStoreDbContext context,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditLogService = auditLogService;
        _sanitizer = new HtmlSanitizer();
    }

    /// <summary>
    /// Danh sách sản phẩm kết hợp tìm kiếm, bộ lọc danh mục/thương hiệu, phân trang
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        int? brandId,
        bool? isActive,
        int page = 1)
    {
        const int pageSize = 10;
        if (page < 1) page = 1;

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s)
                || p.Slug.ToLower().Contains(s)
                || p.Variants.Any(v => v.SKU.ToLower().Contains(s) || v.VariantName.ToLower().Contains(s)));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue && brandId.Value > 0)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var products = await query
            .OrderByDescending(p => p.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.Brands = await _context.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.BrandId = brandId;
        ViewBag.IsActive = isActive;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(products);
    }

    /// <summary>
    /// Giao diện tạo mới sản phẩm
    /// </summary>
    [HttpGet]
    [HasPermission("Products.Create")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new ProductCreateViewModel());
    }

    /// <summary>
    /// Xử lý tạo mới sản phẩm và biến thể mặc định đầu tiên
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Products.Create")]
    public async Task<IActionResult> Create(ProductCreateViewModel model, IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Tên sản phẩm không được để trống.");
        }

        if (model.CategoryId <= 0)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục.");
        }

        if (model.BrandId <= 0)
        {
            ModelState.AddModelError("BrandId", "Vui lòng chọn thương hiệu.");
        }

        if (string.IsNullOrWhiteSpace(model.DefaultSku))
        {
            ModelState.AddModelError("DefaultSku", "SKU ban đầu không được để trống.");
        }

        if (model.DefaultSalePrice <= 0)
        {
            ModelState.AddModelError("DefaultSalePrice", "Giá bán phải lớn hơn 0.");
        }

        // Tự sinh slug nếu người dùng không nhập
        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? SlugHelper.GenerateSlug(model.Name)
            : SlugHelper.GenerateSlug(model.Slug);

        // Kiểm tra trùng Slug
        if (await _context.Products.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        // Kiểm tra trùng SKU
        if (await _context.ProductVariants.AnyAsync(v => v.SKU == model.DefaultSku.Trim()))
        {
            ModelState.AddModelError("DefaultSku", $"Mã SKU '{model.DefaultSku}' đã tồn tại trên hệ thống!");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        // Xử lý upload ảnh đại diện
        string featuredImageUrl = model.FeaturedImage ?? "/assets/images/placeholder.png";
        if (imageFile != null && imageFile.Length > 0)
        {
            using var stream = imageFile.OpenReadStream();
            featuredImageUrl = await _fileStorageService.SaveFileAsync(stream, imageFile.FileName, "products");
        }

        var product = new Product
        {
            CategoryId = model.CategoryId,
            BrandId = model.BrandId,
            Name = model.Name.Trim(),
            Slug = slug,
            FeaturedImage = featuredImageUrl,
            WarrantyMonths = model.WarrantyMonths,
            ShortDescription = model.ShortDescription?.Trim(),
            FullDescription = string.IsNullOrWhiteSpace(model.FullDescription) ? null : _sanitizer.Sanitize(model.FullDescription),
            IsFeatured = model.IsFeatured,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Biến thể mặc định đầu tiên
        var defaultVariant = new ProductVariant
        {
            SKU = model.DefaultSku.Trim(),
            Barcode = model.DefaultBarcode?.Trim(),
            VariantName = string.IsNullOrWhiteSpace(model.DefaultVariantName) ? model.Name.Trim() : model.DefaultVariantName.Trim(),
            OriginalPrice = model.DefaultOriginalPrice > 0 ? model.DefaultOriginalPrice : model.DefaultSalePrice,
            SalePrice = model.DefaultSalePrice,
            StockQuantity = model.DefaultStockQuantity,
            ThumbnailImage = featuredImageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        product.Variants.Add(defaultVariant);

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "CreateProduct",
            module: "Products",
            recordId: product.ProductId.ToString(),
            oldValues: null,
            newValues: new { product.Name, product.Slug, defaultVariant.SKU, defaultVariant.SalePrice }
        );

        TempData["SuccessMessage"] = $"Đã thêm mới sản phẩm '{product.Name}' thành công!";
        return RedirectToAction(nameof(Edit), new { id = product.ProductId });
    }

    /// <summary>
    /// Giao diện chỉnh sửa sản phẩm & quản lý biến thể
    /// </summary>
    [HttpGet]
    [HasPermission("Products.Edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return NotFound();

        await LoadDropdownsAsync();

        var viewModel = new ProductEditViewModel
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            Name = product.Name,
            Slug = product.Slug,
            FeaturedImage = product.FeaturedImage,
            WarrantyMonths = product.WarrantyMonths,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive,
            Variants = product.Variants.OrderBy(v => v.VariantId).ToList()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Xử lý cập nhật thông tin chung sản phẩm
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Products.Edit")]
    public async Task<IActionResult> Edit(int id, ProductEditViewModel model, IFormFile? imageFile)
    {
        if (id != model.ProductId) return BadRequest();

        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return NotFound();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Tên sản phẩm không được để trống.");
        }

        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? SlugHelper.GenerateSlug(model.Name)
            : SlugHelper.GenerateSlug(model.Slug);

        // Kiểm tra trùng Slug với sản phẩm khác
        if (await _context.Products.AnyAsync(p => p.Slug == slug && p.ProductId != id))
        {
            ModelState.AddModelError("Slug", "Đường dẫn Slug đã được sử dụng bởi sản phẩm khác.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            model.Variants = product.Variants.OrderBy(v => v.VariantId).ToList();
            return View(model);
        }

        // Xử lý upload ảnh mới nếu có
        if (imageFile != null && imageFile.Length > 0)
        {
            using var stream = imageFile.OpenReadStream();
            product.FeaturedImage = await _fileStorageService.SaveFileAsync(stream, imageFile.FileName, "products");
        }
        else if (!string.IsNullOrWhiteSpace(model.FeaturedImage))
        {
            product.FeaturedImage = model.FeaturedImage.Trim();
        }

        var oldValues = new { product.Name, product.Slug, product.CategoryId, product.BrandId, product.IsActive, product.IsFeatured };

        product.Name = model.Name.Trim();
        product.Slug = slug;
        product.CategoryId = model.CategoryId;
        product.BrandId = model.BrandId;
        product.WarrantyMonths = model.WarrantyMonths;
        product.ShortDescription = model.ShortDescription?.Trim();
        product.FullDescription = string.IsNullOrWhiteSpace(model.FullDescription) ? null : _sanitizer.Sanitize(model.FullDescription);
        product.IsFeatured = model.IsFeatured;
        product.IsActive = model.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "UpdateProduct",
            module: "Products",
            recordId: product.ProductId.ToString(),
            oldValues: oldValues,
            newValues: new { product.Name, product.Slug, product.CategoryId, product.BrandId, product.IsActive, product.IsFeatured }
        );

        TempData["SuccessMessage"] = $"Cập nhật sản phẩm '{product.Name}' thành công!";
        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>
    /// Thêm mới hoặc cập nhật một Biến thể (Variant)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Products.Edit")]
    public async Task<IActionResult> SaveVariant([FromForm] SaveVariantDto dto, IFormFile? variantImageFile)
    {
        if (dto.ProductId <= 0 || string.IsNullOrWhiteSpace(dto.SKU) || string.IsNullOrWhiteSpace(dto.VariantName))
        {
            TempData["ErrorMessage"] = "Thông tin biến thể không đầy đủ!";
            return RedirectToAction(nameof(Edit), new { id = dto.ProductId });
        }

        var sku = dto.SKU.Trim();
        // Kiểm tra trùng SKU ngoại trừ chính nó
        bool isDuplicateSku = await _context.ProductVariants
            .AnyAsync(v => v.SKU == sku && v.VariantId != dto.VariantId);

        if (isDuplicateSku)
        {
            TempData["ErrorMessage"] = $"Mã SKU '{sku}' đã tồn tại ở sản phẩm khác!";
            return RedirectToAction(nameof(Edit), new { id = dto.ProductId });
        }

        string? thumbnail = dto.ThumbnailImage;
        if (variantImageFile != null && variantImageFile.Length > 0)
        {
            using var stream = variantImageFile.OpenReadStream();
            thumbnail = await _fileStorageService.SaveFileAsync(stream, variantImageFile.FileName, "variants");
        }

        if (dto.VariantId == 0)
        {
            // Thêm mới biến thể
            var newVariant = new ProductVariant
            {
                ProductId = dto.ProductId,
                SKU = sku,
                Barcode = dto.Barcode?.Trim(),
                VariantName = dto.VariantName.Trim(),
                OriginalPrice = dto.OriginalPrice > 0 ? dto.OriginalPrice : dto.SalePrice,
                SalePrice = dto.SalePrice,
                StockQuantity = dto.StockQuantity,
                WeightGrams = dto.WeightGrams > 0 ? dto.WeightGrams : 200,
                ThumbnailImage = thumbnail,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ProductVariants.AddAsync(newVariant);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "AddVariant",
                module: "Products",
                recordId: newVariant.VariantId.ToString(),
                oldValues: null,
                newValues: new { newVariant.ProductId, newVariant.SKU, newVariant.SalePrice, newVariant.StockQuantity }
            );

            TempData["SuccessMessage"] = $"Đã thêm biến thể '{newVariant.VariantName}' thành công!";
        }
        else
        {
            // Sửa biến thể
            var variant = await _context.ProductVariants.FindAsync(dto.VariantId);
            if (variant == null || variant.ProductId != dto.ProductId)
            {
                TempData["ErrorMessage"] = "Không tìm thấy biến thể tương ứng!";
                return RedirectToAction(nameof(Edit), new { id = dto.ProductId });
            }

            variant.SKU = sku;
            variant.Barcode = dto.Barcode?.Trim();
            variant.VariantName = dto.VariantName.Trim();
            variant.OriginalPrice = dto.OriginalPrice > 0 ? dto.OriginalPrice : dto.SalePrice;
            variant.SalePrice = dto.SalePrice;
            variant.StockQuantity = dto.StockQuantity;
            variant.WeightGrams = dto.WeightGrams;
            variant.IsActive = dto.IsActive;
            if (!string.IsNullOrWhiteSpace(thumbnail))
            {
                variant.ThumbnailImage = thumbnail;
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateVariant",
                module: "Products",
                recordId: variant.VariantId.ToString(),
                oldValues: null,
                newValues: new { variant.SKU, variant.SalePrice, variant.StockQuantity }
            );

            TempData["SuccessMessage"] = $"Đã cập nhật biến thể '{variant.VariantName}' thành công!";
        }

        return RedirectToAction(nameof(Edit), new { id = dto.ProductId });
    }

    /// <summary>
    /// Xóa biến thể
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Products.Edit")]
    public async Task<IActionResult> DeleteVariant(int variantId, int productId)
    {
        var variant = await _context.ProductVariants.FindAsync(variantId);
        if (variant == null || variant.ProductId != productId)
        {
            TempData["ErrorMessage"] = "Biến thể không tồn tại.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        // Kiểm tra biến thể đã có trong OrderDetail chưa
        bool hasOrders = await _context.OrderDetails.AnyAsync(od => od.VariantId == variantId);
        if (hasOrders)
        {
            // Nếu đã từng có đơn hàng, chỉ tắt kích hoạt
            variant.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["ErrorMessage"] = $"Biến thể '{variant.SKU}' đã có lịch sử đơn hàng, hệ thống đã tự động chuyển sang trạng thái Ngừng kinh doanh.";
        }
        else
        {
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa biến thể '{variant.SKU}' thành công!";
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của sản phẩm qua AJAX
    /// </summary>
    [HttpPost]
    [HasPermission("Products.Edit")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = product.IsActive });
    }

    /// <summary>
    /// Bật/Tắt sản phẩm nổi bật qua AJAX
    /// </summary>
    [HttpPost]
    [HasPermission("Products.Edit")]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

        product.IsFeatured = !product.IsFeatured;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isFeatured = product.IsFeatured });
    }

    /// <summary>
    /// Xóa sản phẩm
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Products.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra xem các biến thể của sản phẩm đã phát sinh đơn hàng chưa
        var variantIds = product.Variants.Select(v => v.VariantId).ToList();
        bool hasOrders = await _context.OrderDetails.AnyAsync(od => variantIds.Contains(od.VariantId));

        if (hasOrders)
        {
            // Soft delete
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            foreach (var v in product.Variants) v.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["ErrorMessage"] = $"Sản phẩm '{product.Name}' đã có đơn hàng trong hệ thống nên được chuyển sang trạng thái Ngừng kinh doanh!";
        }
        else
        {
            _context.ProductVariants.RemoveRange(product.Variants);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DeleteProduct",
                module: "Products",
                recordId: id.ToString(),
                oldValues: new { product.Name, product.Slug },
                newValues: null
            );

            TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn sản phẩm '{product.Name}' thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDropdownsAsync()
    {
        var categories = await _context.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        var brands = await _context.Brands.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();

        ViewBag.CategoryList = new SelectList(categories, "CategoryId", "Name");
        ViewBag.BrandList = new SelectList(brands, "BrandId", "Name");
    }
}

public class ProductCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public string? FeaturedImage { get; set; }
    public int WarrantyMonths { get; set; } = 12;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Initial Variant
    public string DefaultSku { get; set; } = string.Empty;
    public string? DefaultBarcode { get; set; }
    public string? DefaultVariantName { get; set; }
    public decimal DefaultOriginalPrice { get; set; }
    public decimal DefaultSalePrice { get; set; }
    public int DefaultStockQuantity { get; set; } = 10;
}

public class ProductEditViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public string? FeaturedImage { get; set; }
    public int WarrantyMonths { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }

    public List<ProductVariant> Variants { get; set; } = new();
}

public class SaveVariantDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int WeightGrams { get; set; } = 200;
    public string? ThumbnailImage { get; set; }
    public bool IsActive { get; set; } = true;
}
