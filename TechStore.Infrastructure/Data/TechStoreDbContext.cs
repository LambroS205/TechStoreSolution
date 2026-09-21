using System;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;

namespace TechStore.Infrastructure.Data;

/// <summary>
/// DbContext chính quản lý CSDL Entity Framework Core cho hệ thống TechStore
/// </summary>
public class TechStoreDbContext : DbContext
{
    public TechStoreDbContext(DbContextOptions<TechStoreDbContext> options) : base(options)
    {
    }

    // DbSets: Phân hệ người dùng & bảo mật phân quyền RBAC
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserCustomPermission> UserCustomPermissions => Set<UserCustomPermission>();

    // DbSets: Phân hệ CMS & Banner quảng cáo động, Coupons
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    // DbSets: Phân hệ Giỏ hàng Database & Kiểm toán (Audit Logs)
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // DbSets: Phân hệ Sản phẩm, Biến thể & Đơn hàng
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình bảng Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NormalizedUsername).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.NormalizedUsername).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
        });

        // 2. Cấu hình bảng Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);

            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(250);

            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.HasIndex(e => e.NormalizedName).IsUnique();
        });

        // 3. Cấu hình bảng UserRoles (Composite Primary Key)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. Cấu hình bảng Permissions
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.PermissionId);

            entity.Property(e => e.Module).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PermissionCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(250).IsRequired();

            entity.HasIndex(e => e.PermissionCode).IsUnique();
        });

        // 5. Cấu hình bảng RolePermissions (Composite Primary Key)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. Cấu hình bảng UserCustomPermissions
        modelBuilder.Entity<UserCustomPermission>(entity =>
        {
            entity.ToTable("UserCustomPermissions");
            entity.HasKey(ucp => new { ucp.UserId, ucp.PermissionId });

            entity.HasOne(ucp => ucp.User)
                  .WithMany(u => u.CustomPermissions)
                  .HasForeignKey(ucp => ucp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ucp => ucp.Permission)
                  .WithMany(p => p.UserCustomPermissions)
                  .HasForeignKey(ucp => ucp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. Cấu hình bảng Banners
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.ToTable("Banners");
            entity.HasKey(e => e.BannerId);

            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Subtitle).HasMaxLength(300);
            entity.Property(e => e.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MobileImageUrl).HasMaxLength(500);
            entity.Property(e => e.TargetUrl).HasMaxLength(500);
            entity.Property(e => e.Position).HasMaxLength(50).IsRequired();

            entity.HasIndex(e => new { e.Position, e.IsActive });
        });

        // 8. Cấu hình bảng Coupons
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("Coupons");
            entity.HasKey(e => e.CouponId);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(250).IsRequired();
            entity.Property(e => e.DiscountType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);

            entity.HasIndex(e => e.Code).IsUnique();
        });

        // 9. Cấu hình bảng Categories
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(150).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.ParentCategory)
      .WithMany(c => c.SubCategories)
      .HasForeignKey(e => e.ParentId)
      .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. Cấu hình bảng Brands
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");
            entity.HasKey(e => e.BrandId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(150).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // 11. Cấu hình bảng Products
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(250).IsRequired();
            entity.Property(e => e.FeaturedImage).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId);

            entity.HasOne(e => e.Brand)
                  .WithMany(b => b.Products)
                  .HasForeignKey(e => e.BrandId);
        });

        // 12. Cấu hình bảng ProductVariants
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");
            entity.HasKey(e => e.VariantId);
            entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();
            entity.Property(e => e.VariantName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.SalePrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.SKU).IsUnique();

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.Variants)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 13. Cấu hình bảng Orders & OrderDetails
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.CustomerName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CustomerPhone).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ShippingAddress).HasMaxLength(350).IsRequired();
            entity.Property(e => e.PaymentMethod).HasMaxLength(30).IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.OrderStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.ShippingFee).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.OrderCode).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails");
            entity.HasKey(e => e.OrderDetailId);
            entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.VariantName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.OrderDetails)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 14. Cấu hình bảng PaymentTransactions
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.PaymentMethod).HasMaxLength(30).IsRequired();
            entity.Property(e => e.TransactionReference).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.PaymentTransactions)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ConfirmedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ConfirmedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 15. Cấu hình bảng Carts & CartItems
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(e => e.CartId);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<Cart>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(e => e.CartItemId);
            entity.HasIndex(e => new { e.CartId, e.VariantId }).IsUnique();

            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.CartItems)
                  .HasForeignKey(e => e.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Variant)
                  .WithMany()
                  .HasForeignKey(e => e.VariantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 16. Cấu hình bảng AuditLogs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RecordId).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.Module, e.CreatedAt });
        });
    }
}