using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [HasPermission("Permissions.Manage")]
    public class PermissionController : Controller
    {
        private readonly TechStoreDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLogService;

        public PermissionController(
            TechStoreDbContext context,
            IMemoryCache cache,
            IAuditLogService auditLogService)
        {
            _context = context;
            _cache = cache;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Hiển thị giao diện Ma trận Phân quyền theo Vai trò hoặc theo Admin nhỏ cụ thể
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Matrix(string tab = "role", int? roleId = null, int? userId = null)
        {
            // 1. Lấy danh sách các vai trò quản trị (loại trừ Customer)
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.NormalizedName != "CUSTOMER")
                .OrderBy(r => r.RoleId)
                .ToListAsync();

            var currentRoleId = roleId ?? roles.FirstOrDefault(r => r.NormalizedName != "SUPERADMIN")?.RoleId
                                      ?? roles.FirstOrDefault()?.RoleId ?? 0;
            var currentRole = roles.FirstOrDefault(r => r.RoleId == currentRoleId);

            // 2. Lấy danh sách các tài khoản Admin con (Nhân viên quản trị)
            var subAdmins = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.NormalizedName != "CUSTOMER"))
                .OrderBy(u => u.UserId)
                .ToListAsync();

            var currentUserId = userId ?? subAdmins.FirstOrDefault(u => u.NormalizedUsername != "ADMIN")?.UserId
                                       ?? subAdmins.FirstOrDefault()?.UserId ?? 0;
            var currentUser = subAdmins.FirstOrDefault(u => u.UserId == currentUserId);

            // 3. Lấy danh mục tất cả quyền hạn phân hệ
            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .ToListAsync();

            var groupedPermissions = allPermissions
                .GroupBy(p => p.Module)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 4. Lấy danh sách quyền đã gán cho Vai trò hiện tại
            var assignedRolePermissionIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == currentRoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            // 5. Xác định quyền của User (kế thừa từ Role + Whitelist cấp thêm + Blacklist chặn)
            var userRoleIds = currentUser?.UserRoles.Select(ur => ur.RoleId).ToList() ?? new List<int>();
            var userInheritedRolePermIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => userRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            var userCustomPerms = await _context.UserCustomPermissions
                .AsNoTracking()
                .Where(ucp => ucp.UserId == currentUserId)
                .ToListAsync();

            var userGrantedIds = userCustomPerms.Where(cp => cp.IsGranted).Select(cp => cp.PermissionId).ToHashSet();
            var userDeniedIds = userCustomPerms.Where(cp => !cp.IsGranted).Select(cp => cp.PermissionId).ToHashSet();

            var viewModel = new PermissionMatrixViewModel
            {
                ActiveTab = tab,
                Roles = roles,
                CurrentRoleId = currentRoleId,
                CurrentRole = currentRole,
                SubAdmins = subAdmins,
                CurrentUserId = currentUserId,
                CurrentUser = currentUser,
                GroupedPermissions = groupedPermissions,
                AssignedPermissionIds = new HashSet<int>(assignedRolePermissionIds),
                UserInheritedRolePermissionIds = new HashSet<int>(userInheritedRolePermIds),
                UserGrantedCustomPermissionIds = userGrantedIds,
                UserDeniedCustomPermissionIds = userDeniedIds
            };

            return View(viewModel);
        }

        /// <summary>
        /// API tiếp nhận lưu ma trận quyền cho Vai trò (Role)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMatrix([FromBody] SaveRolePermissionsDto request)
        {
            if (request == null || request.RoleId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return Json(new { success = false, message = "Không tìm thấy vai trò tương ứng." });
            }

            if (role.NormalizedName == "SUPERADMIN")
            {
                return Json(new { success = false, message = "Vai trò SuperAdmin luôn có toàn quyền và không thể sửa đổi." });
            }

            // Thu thập trạng thái cũ để ghi log kiểm toán
            var currentPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == request.RoleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(currentPermissions);

            if (request.PermissionIds != null && request.PermissionIds.Any())
            {
                var newPermissions = request.PermissionIds.Distinct().Select(pId => new RolePermission
                {
                    RoleId = request.RoleId,
                    PermissionId = pId,
                    AssignedAt = DateTime.UtcNow
                });

                await _context.RolePermissions.AddRangeAsync(newPermissions);
            }

            await _context.SaveChangesAsync();

            // Xóa cache quyền của tất cả người dùng thuộc Role này để quyền mới có hiệu lực tức thì
            var userIdsInRole = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == request.RoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var uid in userIdsInRole)
            {
                _cache.Remove($"UserPermissions_{uid}");
            }

            // Ghi nhận nhật ký kiểm toán hành động thay đổi ma trận
            await _auditLogService.LogAsync(
                action: "UpdateRolePermissionMatrix",
                module: "Permissions",
                recordId: $"Role_{role.RoleName}",
                oldValues: currentPermissions.Select(p => p.PermissionId).ToList(),
                newValues: request.PermissionIds
            );

            return Json(new { success = true, message = $"Đã cập nhật ma trận quyền cho vai trò '{role.RoleName}' thành công!" });
        }

        /// <summary>
        /// API tiếp nhận lưu quyền đặc cách cá nhân (Cấp thêm hoặc Chặn quyền) cho một Admin nhỏ cụ thể
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserPermissions([FromBody] SaveUserCustomPermissionsDto request)
        {
            if (request == null || request.UserId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            if (user.NormalizedUsername == "ADMIN")
            {
                return Json(new { success = false, message = "Tài khoản admin tối cao không thể bị hạn chế quyền hạn." });
            }

            // Xóa danh sách quyền đặc cách cũ của User
            var existingCustomPerms = await _context.UserCustomPermissions
                .Where(ucp => ucp.UserId == request.UserId)
                .ToListAsync();

            _context.UserCustomPermissions.RemoveRange(existingCustomPerms);

            // Thêm các quyền được đặc cách cấp thêm (Whitelist)
            if (request.GrantedPermissionIds != null && request.GrantedPermissionIds.Any())
            {
                foreach (var pId in request.GrantedPermissionIds.Distinct())
                {
                    _context.UserCustomPermissions.Add(new UserCustomPermission
                    {
                        UserId = request.UserId,
                        PermissionId = pId,
                        IsGranted = true,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            // Thêm các quyền bị cấm/chặn cụ thể (Blacklist)
            if (request.DeniedPermissionIds != null && request.DeniedPermissionIds.Any())
            {
                foreach (var pId in request.DeniedPermissionIds.Distinct())
                {
                    _context.UserCustomPermissions.Add(new UserCustomPermission
                    {
                        UserId = request.UserId,
                        PermissionId = pId,
                        IsGranted = false,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Xóa cache quyền của User để thay đổi áp dụng tức thì trong phiên hiện tại
            _cache.Remove($"UserPermissions_{request.UserId}");

            // Ghi nhận nhật ký kiểm toán
            await _auditLogService.LogAsync(
                action: "UpdateUserCustomPermissions",
                module: "Permissions",
                recordId: $"User_{user.Username}",
                oldValues: existingCustomPerms.Select(p => new { p.PermissionId, p.IsGranted }),
                newValues: new { Granted = request.GrantedPermissionIds, Denied = request.DeniedPermissionIds }
            );

            return Json(new { success = true, message = $"Đã cập nhật cấu hình quyền đặc cách cho tài khoản '{user.Username}' thành công!" });
        }

        /// <summary>
        /// Tạo mới tài khoản Admin nhỏ / Quản trị viên cấp dưới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubAdmin([FromForm] CreateSubAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FullName))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường bắt buộc!";
                return RedirectToAction(nameof(Matrix), new { tab = "user" });
            }

            string normalizedUsername = dto.Username.Trim().ToUpperInvariant();
            if (await _context.Users.AnyAsync(u => u.NormalizedUsername == normalizedUsername))
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{dto.Username}' đã tồn tại trong hệ thống!";
                return RedirectToAction(nameof(Matrix), new { tab = "user" });
            }

            var hasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Username = dto.Username.Trim(),
                NormalizedUsername = normalizedUsername,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? $"{dto.Username.Trim()}@techstore.vn" : dto.Email.Trim(),
                NormalizedEmail = (string.IsNullOrWhiteSpace(dto.Email) ? $"{dto.Username.Trim()}@techstore.vn" : dto.Email.Trim()).ToUpperInvariant(),
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            newUser.PasswordHash = hasher.HashPassword(newUser, dto.Password);

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            // Gán vai trò cho Admin nhỏ mới
            if (dto.RoleId > 0)
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = newUser.UserId,
                    RoleId = dto.RoleId
                });
                await _context.SaveChangesAsync();
            }

            await _auditLogService.LogAsync(
                action: "CreateSubAdmin",
                module: "Security",
                recordId: newUser.UserId.ToString(),
                oldValues: null,
                newValues: new { Username = newUser.Username, RoleId = dto.RoleId }
            );

            TempData["SuccessMessage"] = $"Đã tạo tài khoản quản trị viên '{newUser.Username}' thành công!";
            return RedirectToAction(nameof(Matrix), new { tab = "user", userId = newUser.UserId });
        }
    }

    public class PermissionMatrixViewModel
    {
        public string ActiveTab { get; set; } = "role"; // "role" hoặc "user"
        public List<Role> Roles { get; set; } = new();
        public int CurrentRoleId { get; set; }
        public Role? CurrentRole { get; set; }

        public List<User> SubAdmins { get; set; } = new();
        public int CurrentUserId { get; set; }
        public User? CurrentUser { get; set; }

        public Dictionary<string, List<Permission>> GroupedPermissions { get; set; } = new();
        public HashSet<int> AssignedPermissionIds { get; set; } = new();

        public HashSet<int> UserInheritedRolePermissionIds { get; set; } = new();
        public HashSet<int> UserGrantedCustomPermissionIds { get; set; } = new();
        public HashSet<int> UserDeniedCustomPermissionIds { get; set; } = new();
    }

    public class SaveRolePermissionsDto
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class SaveUserCustomPermissionsDto
    {
        public int UserId { get; set; }
        public List<int> GrantedPermissionIds { get; set; } = new();
        public List<int> DeniedPermissionIds { get; set; } = new();
    }

    public class CreateSubAdminDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}