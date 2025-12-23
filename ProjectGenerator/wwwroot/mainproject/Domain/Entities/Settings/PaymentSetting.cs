using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;

namespace MobiRooz.Domain.Entities.Settings;

public sealed class PaymentSetting : Entity
{
    public string ZarinPalMerchantId { get; private set; } = string.Empty;

    public bool ZarinPalIsSandbox { get; private set; }

    public bool IsActive { get; private set; }

    [SetsRequiredMembers]
    private PaymentSetting()
    {
    }

    [SetsRequiredMembers]
    public PaymentSetting(
        string zarinPalMerchantId,
        bool zarinPalIsSandbox = false,
        bool isActive = true)
    {
        Update(zarinPalMerchantId, zarinPalIsSandbox, isActive);
    }

    public void Update(
        string zarinPalMerchantId,
        bool zarinPalIsSandbox = false,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(zarinPalMerchantId))
        {
            throw new ArgumentException("ZarinPal Merchant ID cannot be empty", nameof(zarinPalMerchantId));
        }

        ZarinPalMerchantId = zarinPalMerchantId.Trim();
        ZarinPalIsSandbox = zarinPalIsSandbox;
        IsActive = isActive;
        
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }
}
