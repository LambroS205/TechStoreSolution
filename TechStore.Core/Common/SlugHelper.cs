using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TechStore.Core.Common;

/// <summary>
/// Tiện ích chuyển đổi chuỗi tiếng Việt có dấu thành URL Slug thân thiện SEO
/// </summary>
public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Chuyển chữ thường
        text = text.Trim().ToLowerInvariant();

        // 2. Thay thế ký tự tiếng Việt đặc thù
        string[] patterns = {
            "[áàảãạâấầẩẫậăắằẳẵặ]", "a",
            "[éèẻẽẹêếềểễệ]", "e",
            "[íìỉĩị]", "i",
            "[óòỏõọôốồổỗộơớờởỡợ]", "o",
            "[úùủũụưứừửữự]", "u",
            "[ýỳỷỹỵ]", "y",
            "[đ]", "d"
        };

        for (int i = 0; i < patterns.Length; i += 2)
        {
            text = Regex.Replace(text, patterns[i], patterns[i + 1]);
        }

        // 3. Chuẩn hóa FormD để loại bỏ các dấu kết hợp còn lại (nếu có)
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        string cleanText = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // 4. Thay thế ký tự không phải chữ số thành dấu gạch ngang
        cleanText = Regex.Replace(cleanText, @"[^a-z0-9\s-]", "");
        cleanText = Regex.Replace(cleanText, @"[\s-]+", "-").Trim('-');

        return cleanText;
    }
}
