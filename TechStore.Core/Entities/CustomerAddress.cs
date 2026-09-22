namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể sổ địa chỉ nhận hàng của khách hàng
/// </summary>
public class CustomerAddress
{
    public int AddressId { get; set; }
    public int UserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
}
