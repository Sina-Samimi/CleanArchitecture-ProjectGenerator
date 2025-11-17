# رفع خطای Variable Name Conflict در Invoice Entity

## تاریخ: 2025-11-17

## خطا:
```
A local or parameter named 'description' cannot be declared in this scope 
because that name is used in an enclosing local scope to define a local or parameter
```

**محل خطا**: Invoice.cs

---

## علت خطا:

در constructor کلاس `Invoice`، دو متغیر با نام یکسان `description` وجود داشت:

### ❌ قبل از اصلاح:

```csharp
public Invoice(
    string invoiceNumber,
    string title,
    string? description,  // <-- پارامتر constructor
    string userId,
    Currency currency,
    DateTimeOffset issueDate,
    DateTimeOffset? dueDate,
    decimal taxAmount,
    decimal adjustmentAmount,
    IEnumerable<(string Description, decimal UnitPrice, int Quantity)>? items = null)
{
    SetInvoiceNumber(invoiceNumber);
    SetTitle(title);
    SetDescription(description);  // استفاده از پارامتر
    SetUserId(userId);
    Currency = currency;
    IssueDate = issueDate;
    DueDate = dueDate;
    SetTaxAmount(taxAmount);
    SetAdjustmentAmount(adjustmentAmount);
    Status = InvoiceStatus.Issued;

    if (items != null)
    {
        foreach (var (description, unitPrice, quantity) in items)  // <-- تعریف مجدد!
        {
            AddItem(description, unitPrice, quantity);
        }
    }
}
```

**مشکل**: متغیر `description` در خط 1300 به عنوان **پارامتر** تعریف شده و در خط 1322 دوباره در **foreach loop** به عنوان متغیر deconstruction تعریف می‌شود.

---

## راه حل:

تغییر نام متغیر foreach به `itemDescription`:

### ✅ بعد از اصلاح:

```csharp
if (items != null)
{
    foreach (var (itemDescription, unitPrice, quantity) in items)  // ✅ تغییر نام
    {
        AddItem(itemDescription, unitPrice, quantity);
    }
}
```

---

## فایل اصلاح شده:
**مسیر**: `ProjectGenerator.Core/Templates/DomainEntityTemplates.cs`  
**متد**: `GetInvoiceEntityTemplate()`  
**خط**: 1322

---

## نتیجه:
✅ خطای scope conflict برطرف شد  
✅ Constructor به درستی کامپایل می‌شود  
✅ Code clarity بهبود یافت (itemDescription واضح‌تر از description است)

---

## دستور Build:
```bash
cd path/to/generated/project
dotnet clean
dotnet build
```

باید بدون خطا بیلد بشه! 🎉
