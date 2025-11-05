# راهنمای نصب و راه‌اندازی

## پیش‌نیازها

### نصب .NET SDK 9

#### Windows
1. از [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0) دانلود کنید
2. فایل نصب را اجرا کنید
3. برای تایید نصب:
```powershell
dotnet --version
```

#### Linux (Ubuntu/Debian)
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
```

یا از اسکریپت موجود در پروژه:
```bash
bash ../scripts/install-dotnet9.sh
```

#### macOS
```bash
brew install dotnet@9
```

## نصب ProjectGenerator

### روش 1: اجرای مستقیم (توصیه می‌شود)

```bash
cd ProjectGenerator
dotnet restore
dotnet build
dotnet run
```

### روش 2: نصب به عنوان Global Tool

```bash
cd ProjectGenerator
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release ProjectGenerator
```

سپس می‌توانید از هر جایی استفاده کنید:
```bash
project-generator -n MyProject
```

### روش 3: ایجاد Executable

```bash
cd ProjectGenerator
dotnet publish -c Release -r win-x64 --self-contained
# یا برای Linux:
dotnet publish -c Release -r linux-x64 --self-contained
# یا برای macOS:
dotnet publish -c Release -r osx-x64 --self-contained
```

فایل اجرایی در `bin/Release/net9.0/{runtime}/publish/` قرار می‌گیرد.

## تست اولیه

```bash
cd ProjectGenerator
dotnet run -- -n TestProject -o /tmp/TestProject
```

اگر موفق بود، پوشه `/tmp/TestProject` با ساختار کامل ایجاد شده است.

## عیب‌یابی

### خطا: "dotnet: command not found"
- مطمئن شوید .NET SDK نصب شده
- PATH را بررسی کنید
- ترمینال را دوباره باز کنید

### خطا: "The project file is invalid"
- `dotnet restore` را اجرا کنید
- فایل `.csproj` را بررسی کنید

### خطا: "Output directory already exists"
- دایرکتوری خروجی را پاک کنید یا مسیر دیگری انتخاب کنید
- از سوئیچ `-o` برای تعیین مسیر جدید استفاده کنید

## آماده برای استفاده!

حالا می‌توانید پروژه‌های جدید ایجاد کنید:

```bash
dotnet run
```

یا:

```bash
dotnet run -- --interactive
```

برای اطلاعات بیشتر، `README.md` را مطالعه کنید.
