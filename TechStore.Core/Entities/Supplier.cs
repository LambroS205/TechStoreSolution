namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể nhà cung cấp thiết bị công nghệ chính hãng
/// </summary>
public class Supplier
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
