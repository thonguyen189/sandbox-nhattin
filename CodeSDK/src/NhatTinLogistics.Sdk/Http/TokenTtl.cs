using System;
using System.Globalization;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>
/// Parses NhatTin's token TTL strings (e.g. <c>"24h"</c>, <c>"7d"</c>, <c>"3600s"</c>) into a
/// <see cref="TimeSpan"/> so the SDK can refresh a token proactively before it expires.
/// A bare number is read as seconds. Unrecognized input returns <c>null</c>, which disables
/// proactive refresh for that token (the reactive 401 path still applies).
/// </summary>
internal static class TokenTtl
{
    public static TimeSpan? Parse(string? ttl)
    {
        if (string.IsNullOrWhiteSpace(ttl)) return null;
        var s = ttl.Trim();

        // A bare number is interpreted as seconds (e.g. "120").
        if (TryParseNonNegative(s, out var bareSeconds))
            return TimeSpan.FromSeconds(bareSeconds);

        var unit = s[s.Length - 1];
        var numberPart = s.Substring(0, s.Length - 1);
        if (!TryParseNonNegative(numberPart, out var value))
            return null;

        return unit switch
        {
            's' or 'S' => TimeSpan.FromSeconds(value),
            'm' or 'M' => TimeSpan.FromMinutes(value),
            'h' or 'H' => TimeSpan.FromHours(value),
            'd' or 'D' => TimeSpan.FromDays(value),
            _ => (TimeSpan?)null,
        };
    }

    private static bool TryParseNonNegative(string s, out long value)
        => long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
