using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Shared.Kernel.Results;

namespace Shared.Kernel.Pagination;

/// <summary>
/// The position after a row in a list ordered by a moment, then by id: the last row's (<see cref="At"/>,
/// <see cref="Id"/>). Clients treat it as an opaque string.
/// </summary>
public readonly record struct TimeCursor(DateTime At, Guid Id)
{
    public static readonly Error Invalid = Error.Validation("INVALID_CURSOR", "Nieprawidłowy kursor stronicowania.");

    public string Encode() =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{At.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)}:{Id:N}"));

    public static bool TryDecode(string? value, out TimeCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(value)) return false;

        string text;
        try
        {
            text = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = text.Split(':');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks > DateTime.MaxValue.Ticks
            || !Guid.TryParseExact(parts[1], "N", out var id))
            return false;

        cursor = new TimeCursor(new DateTime(ticks, DateTimeKind.Utc), id);
        return true;
    }
}
