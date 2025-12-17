using System;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Exceptions;

namespace LogsDtoCloneTest.Domain.Entities;

public sealed class UserAddress : Entity
{
    public string UserId { get; private set; } = null!;

    public ApplicationUser User { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string RecipientName { get; private set; } = null!;

    public string RecipientPhone { get; private set; } = null!;

    public string Province { get; private set; } = null!;

    public string City { get; private set; } = null!;

    public string PostalCode { get; private set; } = null!;

    public string AddressLine { get; private set; } = null!;

    public string? Plaque { get; private set; }

    public string? Unit { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOn { get; private set; }

    [SetsRequiredMembers]
    private UserAddress()
    {
    }

    [SetsRequiredMembers]
    public UserAddress(
        string userId,
        string title,
        string recipientName,
        string recipientPhone,
        string province,
        string city,
        string postalCode,
        string addressLine,
        string? plaque = null,
        string? unit = null,
        bool isDefault = false)
    {
        SetUserId(userId);
        SetTitle(title);
        SetRecipientName(recipientName);
        SetRecipientPhone(recipientPhone);
        SetProvince(province);
        SetCity(city);
        SetPostalCode(postalCode);
        SetAddressLine(addressLine);
        SetPlaque(plaque);
        SetUnit(unit);
        IsDefault = isDefault;
        IsDeleted = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("شناسه کاربر معتبر نیست.");
        }

        UserId = userId.Trim();
        Touch();
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("عنوان آدرس الزامی است.");
        }

        Title = title.Trim();
        Touch();
    }

    public void SetRecipientName(string recipientName)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new DomainException("نام گیرنده الزامی است.");
        }

        RecipientName = recipientName.Trim();
        Touch();
    }

    public void SetRecipientPhone(string recipientPhone)
    {
        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            throw new DomainException("شماره تماس گیرنده الزامی است.");
        }

        RecipientPhone = recipientPhone.Trim();
        Touch();
    }

    public void SetProvince(string province)
    {
        if (string.IsNullOrWhiteSpace(province))
        {
            throw new DomainException("استان الزامی است.");
        }

        Province = province.Trim();
        Touch();
    }

    public void SetCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("شهر الزامی است.");
        }

        City = city.Trim();
        Touch();
    }

    public void SetPostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("کد پستی الزامی است.");
        }

        PostalCode = postalCode.Trim();
        Touch();
    }

    public void SetAddressLine(string addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
        {
            throw new DomainException("آدرس کامل الزامی است.");
        }

        AddressLine = addressLine.Trim();
        Touch();
    }

    public void SetPlaque(string? plaque)
    {
        Plaque = string.IsNullOrWhiteSpace(plaque) ? null : plaque.Trim();
        Touch();
    }

    public void SetUnit(string? unit)
    {
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        Touch();
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        Touch();
    }

    public void RemoveDefault()
    {
        IsDefault = false;
        Touch();
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedOn = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedOn = null;
        Touch();
    }

    public void Update(
        string title,
        string recipientName,
        string recipientPhone,
        string province,
        string city,
        string postalCode,
        string addressLine,
        string? plaque = null,
        string? unit = null)
    {
        SetTitle(title);
        SetRecipientName(recipientName);
        SetRecipientPhone(recipientPhone);
        SetProvince(province);
        SetCity(city);
        SetPostalCode(postalCode);
        SetAddressLine(addressLine);
        SetPlaque(plaque);
        SetUnit(unit);
    }

    private void Touch()
    {
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
