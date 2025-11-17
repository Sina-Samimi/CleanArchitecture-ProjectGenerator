# دستورالعمل Build پروژه

## ⚠️ خطاهای رایج و راه حل

### خطا: Metadata file not found

اگر این خطا را دیدید:
```
Metadata file '...\ProjectGenerator.Core\obj\Debug\net9.0\ref\ProjectGenerator.Core.dll' could not be found
```

**راه حل:**

### 1️⃣ پاک کردن کامل obj و bin

```powershell
# در PowerShell (Windows)
Remove-Item -Recurse -Force ProjectGenerator\obj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator\bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator.UI\obj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator.UI\bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator.Core\obj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator.Core\bin -ErrorAction SilentlyContinue
```

```bash
# در Bash (Linux/Mac)
rm -rf ProjectGenerator/obj ProjectGenerator/bin
rm -rf ProjectGenerator.UI/obj ProjectGenerator.UI/bin
rm -rf ProjectGenerator.Core/obj ProjectGenerator.Core/bin
```

### 2️⃣ Build به ترتیب صحیح

```bash
# 1. اول Core را build کنید
dotnet build ProjectGenerator.Core/ProjectGenerator.Core.csproj

# 2. سپس Console
dotnet build ProjectGenerator/ProjectGenerator.csproj

# 3. در نهایت UI
dotnet build ProjectGenerator.UI/ProjectGenerator.UI.csproj
```

یا همه را با هم:
```bash
dotnet restore
dotnet build
```

---

## 🚀 Build از صفر

اگر می‌خواهید همه چیز را از صفر build کنید:

```powershell
# Windows PowerShell
Remove-Item -Recurse -Force *\obj,*\bin -ErrorAction SilentlyContinue
dotnet clean
dotnet restore
dotnet build
```

```bash
# Linux/Mac Bash
rm -rf */obj */bin
dotnet clean
dotnet restore
dotnet build
```

---

## 🔄 ترتیب Build

**مهم:** پروژه‌ها باید به این ترتیب build شوند:

```
1. ProjectGenerator.Core    (هیچ وابستگی ندارد)
   ↓
2. ProjectGenerator         (به Core وابسته است)
   
2. ProjectGenerator.UI      (به Core وابسته است)
```

⚠️ **توجه:** `ProjectGenerator` و `ProjectGenerator.UI` به یکدیگر وابسته نیستند!

---

## 🧪 تست Build

برای اطمینان از صحت build:

```bash
# 1. Clean
dotnet clean

# 2. Restore
dotnet restore

# 3. Build Core
cd ProjectGenerator.Core
dotnet build
cd ..

# 4. Build Console
cd ProjectGenerator
dotnet build
cd ..

# 5. Build UI
cd ProjectGenerator.UI
dotnet build
cd ..
```

---

## 🐛 اگر باز هم خطا داد

### چک کردن وابستگی‌ها

```bash
# چک کردن Console
dotnet list ProjectGenerator/ProjectGenerator.csproj reference

# خروجی باید این باشد:
# ProjectReference
#   ..\ProjectGenerator.Core\ProjectGenerator.Core.csproj

# چک کردن UI
dotnet list ProjectGenerator.UI/ProjectGenerator.UI.csproj reference

# خروجی باید این باشد:
# ProjectReference
#   ..\ProjectGenerator.Core\ProjectGenerator.Core.csproj
```

### چک کردن Solution

```bash
dotnet sln list

# خروجی باید این باشد:
# ProjectGenerator.Core\ProjectGenerator.Core.csproj
# ProjectGenerator\ProjectGenerator.csproj
# ProjectGenerator.UI\ProjectGenerator.UI.csproj
```

---

## ✅ Build موفق

بعد از build موفق، باید این فایل‌ها ایجاد شوند:

```
ProjectGenerator.Core/bin/Debug/net9.0/ProjectGenerator.Core.dll
ProjectGenerator/bin/Debug/net9.0/ProjectGenerator.exe
ProjectGenerator.UI/bin/Debug/net9.0-windows/ProjectGenerator.UI.exe
```

---

## 🏃 اجرای پروژه‌ها

### Console:
```bash
dotnet run --project ProjectGenerator
```

### UI:
```bash
dotnet run --project ProjectGenerator.UI
```

یا:
```bash
./RUN_WINFORMS.bat    # Windows
./RUN_WINFORMS.sh     # Linux/Mac
```
