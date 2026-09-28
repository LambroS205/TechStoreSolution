namespace TechStore.Web.Areas.Admin.Models;

/// <summary>
/// ViewModel phục vụ trang quản trị Cài đặt Giao diện & Hệ thống CMS
/// </summary>
public class SiteSettingsViewModel
{
    // 1. Thông tin chung (General)
    public string SiteName { get; set; } = "TechStore";
    public string Hotline { get; set; } = "1800 6868";
    public string Email { get; set; } = "support@techstore.vn";
    public string Address { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;

    // 2. Nhận diện thương hiệu & Màu sắc (Branding & Theme)
    public string LogoUrl { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#2563eb";
    public string SecondaryColor { get; set; } = "#4f46e5";
    public string AccentColor { get; set; } = "#f59e0b";

    // 3. Cấu hình thanh toán VietQR NAPAS 247
    public string VietQrBankId { get; set; } = "970422";
    public string VietQrBankName { get; set; } = "MBBank (Ngân hàng Quân Đội)";
    public string VietQrAccountNo { get; set; } = "0869162534";
    public string VietQrAccountName { get; set; } = "CONG TY TNHH TECHSTORE VIET NAM";
    public string VietQrTemplate { get; set; } = "compact2";

    // 4. Chân trang & Mạng xã hội (Footer & Social)
    public string FooterAboutText { get; set; } = string.Empty;
    public string FooterCopyright { get; set; } = string.Empty;
    public string FacebookUrl { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
    public string TiktokUrl { get; set; } = string.Empty;

    // 5. Tùy biến mã CSS & Scripts (Custom Code)
    public string CustomCss { get; set; } = string.Empty;
    public string CustomHeaderHtml { get; set; } = string.Empty;
}
