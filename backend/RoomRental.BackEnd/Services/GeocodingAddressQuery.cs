using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RoomRental.BackEnd.DTO.Location;

namespace RoomRental.BackEnd.Services;

internal sealed record GeocodingAddressQuery(string HouseNumber, string Street, string Ward, string District, string Province)
{
    internal static GeocodingAddressQuery? Parse(string query)
    {
        var parts = query.Split(',').Select(s => Regex.Replace(s.Trim(), @"\s+\d{5,6}$", "")).Where(s => s.Length > 0)
            .Where(s => Fold(s) is not ("viet nam" or "vietnam" or "vn")).ToList();
        if (parts.Count < 2) return null;
        var first = Regex.Match(parts[0], @"^(?<house>\d[\w/-]*)\s+(?<street>.+)$");
        if (!first.Success) return null;
        var street = Regex.Replace(first.Groups["street"].Value, @"^P\.\s*", "Phố ", RegexOptions.IgnoreCase);
        var province = parts[^1];
        var district = parts.Count >= 4 ? parts[^2] : "";
        var ward = parts.Count >= 3 ? parts[^ (parts.Count >= 4 ? 3 : 2)] : "";
        return new(first.Groups["house"].Value, street, ward, district, province);
    }

    internal string PhotonPath() => "structured?countrycode=VN&limit=5&housenumber=" + Uri.EscapeDataString(HouseNumber)
        + "&street=" + Uri.EscapeDataString(WithoutPrefix(Street)) + "&city=" + Uri.EscapeDataString(WithoutPrefix(Province))
        + (Ward.Length > 0 ? "&district=" + Uri.EscapeDataString(WithoutPrefix(Ward)) : "")
        + (District.Length > 0 ? "&county=" + Uri.EscapeDataString(WithoutPrefix(District)) : "");

    internal List<LocationSearchResultDto> Relevant(IEnumerable<LocationSearchResultDto> results)
    {
        var matching = results.Where(r => Fold(WithoutPrefix(r.Street)) == Fold(WithoutPrefix(Street))
            && (r.Province.Length == 0 || Fold(WithoutPrefix(r.Province)) == Fold(WithoutPrefix(Province)))
            && (r.HouseNumber.Length == 0 || Fold(r.HouseNumber) == Fold(HouseNumber))).ToList();
        var exact = matching.Where(r => r.HouseNumber.Length > 0).ToList();
        return exact.Count > 0 ? exact : matching;
    }

    private static string WithoutPrefix(string value) => Regex.Replace(value.Trim(), @"^(phố|đường|thành phố|tỉnh|phường|quận|huyện|thị xã|tp\.|p\.|q\.|h\.)\s+", "", RegexOptions.IgnoreCase);
    private static string Fold(string value)
    {
        var chars = value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return Regex.Replace(new string(chars.ToArray()).Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant(), @"\s+", " ").Trim();
    }
}
