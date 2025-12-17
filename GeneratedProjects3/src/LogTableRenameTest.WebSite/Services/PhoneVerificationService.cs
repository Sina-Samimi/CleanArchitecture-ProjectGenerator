using System;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace LogTableRenameTest.WebSite.Services;

public sealed class PhoneVerificationService : IPhoneVerificationService
{
    private const string CacheKeyPrefix = "phone-verification:";

    private readonly IMemoryCache _cache;
    private readonly TimeSpan _lifetime;

    public PhoneVerificationService(IMemoryCache cache)
    {
        _cache = cache;
        _lifetime = TimeSpan.FromMinutes(2);
    }

    public TimeSpan CodeLifetime => _lifetime;

    public PhoneVerificationCode GenerateCode(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(_lifetime);
        var code = RandomNumberGenerator.GetInt32(100000, 1000000)
            .ToString("D6", CultureInfo.InvariantCulture);

        var state = new VerificationState(code, now, expiresAt);
        _cache.Set(BuildKey(phoneNumber), state, expiresAt);

        return new PhoneVerificationCode(phoneNumber, code, expiresAt);
    }

    public PhoneVerificationValidationResult ValidateCode(string phoneNumber, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!_cache.TryGetValue(BuildKey(phoneNumber), out VerificationState? state) || state is null)
        {
            return new PhoneVerificationValidationResult(false, PhoneVerificationError.NotFound, null);
        }

        if (state.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _cache.Remove(BuildKey(phoneNumber));
            return new PhoneVerificationValidationResult(false, PhoneVerificationError.Expired, state.ExpiresAt);
        }

        if (!TimeConstantEquals(state.Code, code))
        {
            return new PhoneVerificationValidationResult(false, PhoneVerificationError.Incorrect, state.ExpiresAt);
        }

        _cache.Remove(BuildKey(phoneNumber));
        return new PhoneVerificationValidationResult(true, PhoneVerificationError.None, state.ExpiresAt);
    }

    public PhoneVerificationState? GetState(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        if (!_cache.TryGetValue(BuildKey(phoneNumber), out VerificationState? state) || state is null)
        {
            return null;
        }

        return new PhoneVerificationState(state.GeneratedAt, state.ExpiresAt);
    }

    public void ClearCode(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        _cache.Remove(BuildKey(phoneNumber));
    }

    private static string BuildKey(string phoneNumber) => $"{CacheKeyPrefix}{phoneNumber}";

    private static bool TimeConstantEquals(string expected, string provided)
    {
        if (expected.Length != provided.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            result |= expected[i] ^ provided[i];
        }

        return result == 0;
    }

    private sealed record VerificationState(string Code, DateTimeOffset GeneratedAt, DateTimeOffset ExpiresAt);
}
