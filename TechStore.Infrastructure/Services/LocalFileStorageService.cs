using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using TechStore.Core.Interfaces;

namespace TechStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ lưu trữ tập tin cục bộ tại thư mục wwwroot/uploads trên máy chủ
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private static readonly System.Collections.Generic.HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<string> SaveFileAsync(Stream fileStream, string originalFileName, string folderName)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Luồng dữ liệu tập tin trống.", nameof(fileStream));

        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("Kích thước tập tin vượt quá giới hạn cho phép (tối đa 5MB).");

        string ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Định dạng tập tin '{ext}' không hợp lệ. Hệ thống chỉ cho phép tải lên các định dạng ảnh: .jpg, .jpeg, .png, .webp, .gif");

        string cleanFolderName = Regex.Replace(folderName, @"[^a-zA-Z0-9_\-]", "").Trim();
        if (string.IsNullOrEmpty(cleanFolderName)) cleanFolderName = "misc";

        string webRootPath = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        string targetDirectory = Path.Combine(webRootPath, "uploads", cleanFolderName);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string rawName = Path.GetFileNameWithoutExtension(originalFileName);
        string safeName = Regex.Replace(rawName, @"[^a-zA-Z0-9_\-]", "-").Trim('-');
        if (string.IsNullOrEmpty(safeName)) safeName = "image";

        string uniqueFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}_{safeName}{ext}";
        string physicalPath = Path.Combine(targetDirectory, uniqueFileName);

        using (var output = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(output);
        }

        return $"/uploads/{folderName}/{uniqueFileName}";
    }

    public Task<bool> DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult(false);

        try
        {
            string webRootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            string cleanPath = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(webRootPath, cleanPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
        }
        catch
        {
            // Bỏ qua lỗi xóa file vật lý nếu không tồn tại hoặc đang bị lock
        }

        return Task.FromResult(false);
    }
}
