using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.DTOs;
using Microsoft.AspNetCore.Http;

namespace Arsis.Infrastructure.Services;

public sealed class FormFileSettingServices : IFormFileSettingServices
{
    public ResponseDto<string> UploadImage(string basePath, IFormFile file, string customFileName)
    {
        var response = new ResponseDto<string>();

        if (file is null || file.Length == 0)
        {
            response.Success = false;
            response.Code = 400;
            response.Messages = new List<Messages>
            {
                new() { message = "فایل معتبری ارسال نشده است." }
            };

            return response;
        }

        basePath = Path.Combine("images", basePath);

        var uploadRoot = Path.Combine("wwwroot", basePath);
        var folderName = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var monthlyFolder = Path.Combine(uploadRoot, folderName);

        if (!Directory.Exists(monthlyFolder))
        {
            Directory.CreateDirectory(monthlyFolder);
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var finalFileName = customFileName + extension;
        var fullPath = Path.Combine(monthlyFolder, finalFileName);

        var counter = 1;
        while (File.Exists(fullPath))
        {
            finalFileName = $"{customFileName}({counter}){extension}";
            fullPath = Path.Combine(monthlyFolder, finalFileName);
            counter++;
        }

        using (var stream = File.Create(fullPath))
        {
            file.CopyTo(stream);
        }

        var relativePath = "/" + Path.Combine(basePath, folderName, finalFileName).Replace("\\", "/");

        response.Success = true;
        response.Code = 201;
        response.Data = relativePath;
        response.Messages = new List<Messages>
        {
            new() { message = "فایل با نام دلخواه با موفقیت ذخیره شد." }
        };

        return response;
    }

    public ResponseDto<string> UploadFile(string basePath, IFormFile file, string customFileName)
    {
        var response = new ResponseDto<string>();

        if (file is null || file.Length == 0)
        {
            response.Success = false;
            response.Code = 400;
            response.Messages = new List<Messages>
            {
                new() { message = "فایل معتبری ارسال نشده است." }
            };

            return response;
        }

        basePath = basePath?.Trim('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            response.Success = false;
            response.Code = 400;
            response.Messages = new List<Messages>
            {
                new() { message = "مسیر ذخیره‌سازی معتبر نیست." }
            };

            return response;
        }

        var uploadRoot = Path.Combine("wwwroot", basePath.Replace('/', Path.DirectorySeparatorChar));
        var folderName = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var monthlyFolder = Path.Combine(uploadRoot, folderName);

        if (!Directory.Exists(monthlyFolder))
        {
            Directory.CreateDirectory(monthlyFolder);
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".dat";
        }

        var safeFileName = customFileName;
        var finalFileName = safeFileName + extension;
        var fullPath = Path.Combine(monthlyFolder, finalFileName);

        var counter = 1;
        while (File.Exists(fullPath))
        {
            finalFileName = $"{safeFileName}({counter}){extension}";
            fullPath = Path.Combine(monthlyFolder, finalFileName);
            counter++;
        }

        using (var stream = File.Create(fullPath))
        {
            file.CopyTo(stream);
        }

        var relativePath = "/" + Path.Combine(basePath, folderName, finalFileName)
            .Replace("\\", "/", StringComparison.Ordinal);

        response.Success = true;
        response.Code = 201;
        response.Data = relativePath;
        response.Messages = new List<Messages>
        {
            new() { message = "فایل با موفقیت ذخیره شد." }
        };

        return response;
    }

    public ResponseDto CopyFile(string sourceDir, string destinationDir)
    {
        var response = new ResponseDto();

        if (!File.Exists(sourceDir))
        {
            response.Success = false;
            response.Code = 404;
            response.Messages = new List<Messages> { new() { message = "فایل مورد نظر یافت نشد" } };
            return response;
        }

        var destinationDirectory = Path.GetDirectoryName(destinationDir);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        File.Copy(sourceDir, destinationDir, overwrite: true);
        response.Success = true;
        response.Code = 200;
        return response;
    }

    public ResponseDto DeleteFile(string? filePath)
    {
        var response = new ResponseDto();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            response.Success = false;
            response.Code = 400;
            response.Messages = new List<Messages> { new() { message = "مسیر فایل معتبر نیست" } };
            return response;
        }

        var trimmedPath = filePath.TrimStart('~');
        if (trimmedPath.StartsWith('/'))
        {
            trimmedPath = trimmedPath[1..];
        }

        var normalisedPath = trimmedPath.Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine("wwwroot", normalisedPath);

        if (!File.Exists(fullPath))
        {
            response.Success = false;
            response.Code = 404;
            response.Messages = new List<Messages> { new() { message = "فایل مورد نظر یافت نشد" } };
            return response;
        }

        File.Delete(fullPath);
        response.Success = true;
        response.Code = 200;
        response.Messages = new List<Messages>
        {
            new() { message = "فایل مورد نظر با موفقیت حذف شد" }
        };

        return response;
    }

    public bool IsFileSizeValid(IFormFile file, int maxSizeInKB)
    {
        ArgumentNullException.ThrowIfNull(file);

        var maxSizeInBytes = maxSizeInKB * 1024L;
        return file.Length <= maxSizeInBytes;
    }

    public bool EnsureDirectoryExists(string relativePath)
    {
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            Directory.CreateDirectory(fullPath);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطا در ساخت مسیر: {ex.Message}");
            return false;
        }
    }
}
