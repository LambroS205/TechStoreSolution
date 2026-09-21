using System.IO;
using System.Threading.Tasks;

namespace TechStore.Core.Interfaces;

/// <summary>
/// Dịch vụ quản lý lưu trữ tập tin hình ảnh (Local hoặc Cloud)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Lưu tập tin vào thư mục chỉ định và trả về đường dẫn tương đối (VD: /uploads/products/abc.webp)
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string originalFileName, string folderName);

    /// <summary>
    /// Xóa tập tin vật lý khỏi máy chủ
    /// </summary>
    Task<bool> DeleteFileAsync(string relativePath);
}
