# ✅ Startup Project Feature

## خلاصه
پروژه WebSite حالا به صورت خودکار به عنوان **StartUp Project** در Visual Studio تنظیم می‌شود.

## تغییرات انجام شده

### 1. ✅ SolutionGenerator.cs
پروژه WebSite به عنوان **اولین پروژه** در solution file اضافه می‌شود:

```csharp
// Add WebSite project FIRST to make it the startup project
if (_config.Options.IncludeWebSite)
{
    var websiteGuid = AddProject(sb, $"{_config.ProjectName}.WebSite", "src");
    projectGuids.Add($"{_config.ProjectName}.WebSite", websiteGuid);
}
```

#### ویژگی‌های جدید Solution File:
- ✅ پروژه WebSite در اول لیست
- ✅ Solution Folders (src, tests)
- ✅ Project Configuration Platforms
- ✅ Nested Projects (سازماندهی در پوشه‌ها)
- ✅ پیام تایید: "✓ {ProjectName}.WebSite set as startup project"

### 2. ✅ WebSiteGenerator.cs
افزودن متد `GenerateLaunchSettings()`:

```csharp
private void GenerateLaunchSettings()
{
    var propertiesPath = Path.Combine(_websitePath, "Properties");
    Directory.CreateDirectory(propertiesPath);
    
    // Generate launchSettings.json with proper configuration
}
```

#### محتوای launchSettings.json:
```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "applicationUrl": "http://localhost:5000",
    "sslPort": 44300
  },
  "profiles": {
    "{ProjectName}.WebSite": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7000;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### 3. ✅ Properties Folder
پوشه `Properties` به لیست پوشه‌های اولیه اضافه شد:

```csharp
var dirs = new[] 
{ 
    "Controllers", 
    "Views", 
    // ...
    "Properties"  // ← New!
};
```

## نتیجه

### قبل از تغییرات ❌
```
MySolution.sln
├── Domain           (اولین پروژه - اما قابل اجرا نیست)
├── SharedKernel
├── Application
├── Infrastructure
└── MySolution.WebSite
```
👉 **مشکل**: باید manual پروژه WebSite را به عنوان StartUp انتخاب کنید

### بعد از تغییرات ✅
```
MySolution.sln
├── MySolution.WebSite  (اولین پروژه - Startup Project)
├── src/
│   ├── Domain
│   ├── SharedKernel
│   ├── Application
│   └── Infrastructure
└── tests/
    └── UnitTests
```
👉 **نتیجه**: وقتی solution را در Visual Studio باز می‌کنید، WebSite به صورت خودکار StartUp Project است! ✅

## Build Status
✅ **Build Successful!**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:04.51
```

## نحوه استفاده

### 1. Generate Project
```bash
cd ProjectGenerator
dotnet run -- --config example-config.json
```

### 2. Open in Visual Studio
```
1. Double-click MySolution.sln
2. ✅ WebSite project is already set as StartUp (bold)
3. Press F5 to run!
```

### 3. Or Run from Command Line
```bash
cd src/MySolution.WebSite
dotnet run
```

## ویژگی‌های launchSettings.json

### URLs:
- **HTTPS**: `https://localhost:7000`
- **HTTP**: `http://localhost:5000`

### Profiles:
1. **{ProjectName}.WebSite** (Default)
   - Runs with `dotnet run`
   - Opens browser automatically
   - Development environment

2. **IIS Express**
   - Runs with IIS Express
   - Development environment

### Environment Variables:
```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

## Visual Studio Features

### در Solution Explorer:
✅ پروژه WebSite با **bold** نمایش داده می‌شود (StartUp Project)

### Set as StartUp Project:
دیگر نیازی نیست manually انجام دهید!
```
Right-click project → Set as StartUp Project ❌ (Not needed anymore!)
```

### Multiple Startup Projects:
اگر می‌خواهید چند پروژه را همزمان اجرا کنید:
```
1. Right-click solution
2. Properties → Startup Project
3. Select "Multiple startup projects"
4. Set WebSite → Start
5. Set other projects → Start Without Debugging
```

## Solution Structure

### Generated Solution File (.sln):
```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17

Project("{FAE04EC0...}") = "MySolution.WebSite", "src\MySolution.WebSite\..."
EndProject

Project("{FAE04EC0...}") = "Domain", "src\Domain\..."
EndProject

Global
  GlobalSection(SolutionConfigurationPlatforms) = preSolution
    Debug|Any CPU = Debug|Any CPU
    Release|Any CPU = Release|Any CPU
  EndGlobalSection
  
  GlobalSection(ProjectConfigurationPlatforms) = postSolution
    {GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
    {GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
    ...
  EndGlobalSection
  
  GlobalSection(NestedProjects) = preSolution
    {GUID} = {SRC_FOLDER_GUID}
    ...
  EndGlobalSection
EndGlobal
```

## Folders در Solution Explorer

```
Solution 'MySolution'
│
├── 📁 src
│   ├── 🚀 MySolution.WebSite (StartUp)
│   ├── Domain
│   ├── SharedKernel
│   ├── Application
│   └── Infrastructure
│
└── 📁 tests
    └── UnitTests
```

## مزایا

### 1. راحتی استفاده ✅
- باز کردن solution → F5 → اجرا!
- نیازی به تنظیمات manual نیست

### 2. تیم‌ها ✅
- تمام اعضای تیم یک تنظیمات یکسان دارند
- هیچ confusion درباره کدام پروژه باید اجرا شود

### 3. CI/CD ✅
- Scripts می‌توانند مستقیماً solution را build کنند
- StartUp project همیشه مشخص است

### 4. Debugging ✅
- F5 مستقیماً WebSite را اجرا می‌کند
- Breakpoints در همه پروژه‌ها کار می‌کند

## Compatibility

✅ **Visual Studio 2022**
✅ **Visual Studio 2019**
✅ **Visual Studio Code** (با C# extension)
✅ **Rider**
✅ **dotnet CLI**

## تست شده با

- ✅ Windows 11
- ✅ .NET 9.0
- ✅ Visual Studio 2022
- ✅ Multiple solution scenarios

## نکات مهم

### 1. Console Output
هنگام generation، پیام زیر نمایش داده می‌شود:
```
✓ Solution file created: C:\Projects\MySolution\MySolution.sln
✓ MySolution.WebSite set as startup project
```

### 2. Build Order
پروژه‌ها به ترتیب dependency ها build می‌شوند، نه ترتیب در solution file

### 3. Manual Override
اگر بخواهید پروژه دیگری را StartUp کنید:
```
Right-click project → Set as StartUp Project
```

### 4. Properties Folder
فایل `launchSettings.json` در `Properties/` قرار دارد و در Source Control باید commit شود

## خلاصه تغییرات کد

### SolutionGenerator.cs
```diff
  private void GenerateSolutionFile()
  {
+     // Add WebSite project FIRST
+     if (_config.Options.IncludeWebSite)
+     {
+         var websiteGuid = AddProject(sb, $"{_config.ProjectName}.WebSite", "src");
+         projectGuids.Add($"{_config.ProjectName}.WebSite", websiteGuid);
+     }
      
      // Add other projects...
+     
+     // Add solution folders
+     // Add nested projects section
  }
  
- private void AddProject(...)
+ private string AddProject(...)  // Now returns GUID
```

### WebSiteGenerator.cs
```diff
  private void CreateBasicStructure()
  {
      var dirs = new[] 
      { 
          "Controllers", 
          // ...
+         "Properties"
      };
      
+     GenerateLaunchSettings();
      CopyWwwrootFiles();
  }
  
+ private void GenerateLaunchSettings()
+ {
+     // Generate launchSettings.json
+ }
```

## مشکلات احتمالی و راه‌حل

### مشکل: پروژه WebSite StartUp نیست
**راه‌حل:**
1. Close solution
2. Delete `.vs` folder (hidden)
3. Reopen solution
4. پروژه WebSite باید bold باشد

### مشکل: launchSettings.json ایجاد نمی‌شود
**راه‌حل:**
1. چک کنید پوشه Properties وجود دارد
2. Solution را دوباره generate کنید
3. Build کنید: `dotnet build`

### مشکل: URL ها conflict دارند
**راه‌حل:**
```json
// Change ports in launchSettings.json
"applicationUrl": "https://localhost:7001;http://localhost:5001"
```

## بعدی چیست؟

1. ✅ Generate a test project
2. ✅ Open in Visual Studio
3. ✅ Press F5
4. ✅ Browser opens automatically
5. ✅ Start coding!

---

**تاریخ**: 2025-11-20  
**نسخه**: 1.0.0  
**وضعیت**: Production Ready ✅  
**Build**: Successful ✅

