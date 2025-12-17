using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Exceptions;

namespace LogTableRenameTest.Domain.Entities.Catalog;

/// <summary>
/// ثبت درخواست کاربر برای دریافت اطلاع‌رسانی در زمان موجود شدن دوباره یک محصول
/// (برای محصول اصلی یا پیشنهاد فروشنده).
/// </summary>
public sealed class ProductBackInStockSubscription : Entity
{
    [SetsRequiredMembers]
    private ProductBackInStockSubscription()
    {
        PhoneNumber = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductBackInStockSubscription(
        Guid? productId,
        Guid? productOfferId,
        string phoneNumber,
        string? userId)
    {
        if (productId is null && productOfferId is null)
        {
            throw new DomainException("حداقل یکی از شناسه‌های محصول یا پیشنهاد فروشنده باید مشخص شود.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("شماره تماس برای ثبت درخواست الزامی است.");
        }

        ProductId = productId;
        ProductOfferId = productOfferId;
        PhoneNumber = phoneNumber.Trim();
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        IsNotified = false;
    }

    /// <summary>شناسه محصول اصلی (در صورت وجود)</summary>
    public Guid? ProductId { get; private set; }

    /// <summary>شناسه پیشنهاد فروشنده (در صورت وجود)</summary>
    public Guid? ProductOfferId { get; private set; }

    /// <summary>شماره موبایل کاربر (به صورت نرمال‌شده)</summary>
    public string PhoneNumber { get; private set; }

    /// <summary>شناسه کاربر (در صورت لاگین بودن)</summary>
    public string? UserId { get; private set; }

    /// <summary>آیا پیامک/اعلان ارسال شده است؟</summary>
    public bool IsNotified { get; private set; }

    /// <summary>تاریخ و ساعت ارسال پیامک (در صورت ارسال)</summary>
    public DateTimeOffset? NotifiedAt { get; private set; }

    public void MarkAsNotified()
    {
        if (IsNotified)
        {
            return;
        }

        IsNotified = true;
        NotifiedAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}


