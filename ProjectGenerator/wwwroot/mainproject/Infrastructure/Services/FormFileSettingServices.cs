using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.DTOs;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace MobiRooz.Infrastructure.Services;

public sealed class FormFileSettingServices : IFormFileSettingServices
{
    private const int MaxWebPFileSizeInKB = 100;

    private async Task<Stream?> ConvertToWebPAsync(Stream inputStream, int maxSizeInKB = MaxWebPFileSizeInKB, CancellationToken cancellationToken = default)
    {
        try
        {
            inputStream.Position = 0;
            using var image = await Image.LoadAsync(inputStream, cancellationToken);
            
            var maxSizeInBytes = maxSizeInKB * 1024;
            var outputStream = new MemoryStream();
            
            // Helper method to create encoder with specific quality
            WebpEncoder CreateEncoder(int qualityValue)
            {
                return new WebpEncoder
                {
                    Quality = qualityValue,
                    Method = WebpEncodingMethod.BestQuality
                };
            }

            // Try to save with initial quality (85)
            var encoder = CreateEncoder(85);
            await image.SaveAsync(outputStream, encoder, cancellationToken);
            
            // If file is too large, reduce quality iteratively
            if (outputStream.Length > maxSizeInBytes)
            {
                outputStream.SetLength(0);
                outputStream.Position = 0;
                
                encoder = CreateEncoder(75);
                await image.SaveAsync(outputStream, encoder, cancellationToken);
                
                // If still too large, try resizing
                if (outputStream.Length > maxSizeInBytes)
                {
                    var maxDimension = 1920; // Max width or height
                    var width = image.Width;
                    var height = image.Height;
                    
                    if (width > maxDimension || height > maxDimension)
                    {
                        var ratio = Math.Min((float)maxDimension / width, (float)maxDimension / height);
                        var newWidth = (int)(width * ratio);
                        var newHeight = (int)(height * ratio);
                        
                        outputStream.SetLength(0);
                        outputStream.Position = 0;
                        
                        using var resizedImage = image.CloneAs<Rgba32>();
                        resizedImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(newWidth, newHeight),
                            Mode = ResizeMode.Max
                        }));
                        
                        encoder = CreateEncoder(80);
                        await resizedImage.SaveAsync(outputStream, encoder, cancellationToken);
                        
                        // If still too large, reduce quality more
                        if (outputStream.Length > maxSizeInBytes)
                        {
                            encoder = CreateEncoder(70);
                            outputStream.SetLength(0);
                            outputStream.Position = 0;
                            await resizedImage.SaveAsync(outputStream, encoder, cancellationToken);
                            
                            // Last attempt with lower quality
                            if (outputStream.Length > maxSizeInBytes)
                            {
                                encoder = CreateEncoder(60);
                                outputStream.SetLength(0);
                                outputStream.Position = 0;
                                await resizedImage.SaveAsync(outputStream, encoder, cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        // Image is small enough, just reduce quality
                        encoder = CreateEncoder(70);
                        outputStream.SetLength(0);
                        outputStream.Position = 0;
                        await image.SaveAsync(outputStream, encoder, cancellationToken);
                        
                        if (outputStream.Length > maxSizeInBytes)
                        {
                            encoder = CreateEncoder(60);
                            outputStream.SetLength(0);
                            outputStream.Position = 0;
                            await image.SaveAsync(outputStream, encoder, cancellationToken);
                        }
                    }
                }
            }

            outputStream.Position = 0;
            return outputStream;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ResponseDto<string>> UploadImageAsync(string basePath, IFormFile file, string customFileName, CancellationToken cancellationToken = default)
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

        // Check if file is already WebP
        var contentType = file.ContentType ?? string.Empty;
        var isWebP = contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase) ||
                     Path.GetExtension(file.FileName).Equals(".webp", StringComparison.OrdinalIgnoreCase);

        basePath = Path.Combine("images", basePath);

        var uploadRoot = Path.Combine("wwwroot", basePath);
        var folderName = DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var monthlyFolder = Path.Combine(uploadRoot, folderName);

        if (!Directory.Exists(monthlyFolder))
        {
            Directory.CreateDirectory(monthlyFolder);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var extension = ".webp";
        var finalFileName = customFileName + extension;
        var fullPath = Path.Combine(monthlyFolder, finalFileName);

        var counter = 1;
        while (File.Exists(fullPath))
        {
            finalFileName = $"{customFileName}({counter}){extension}";
            fullPath = Path.Combine(monthlyFolder, finalFileName);
            counter++;
        }

        try
        {
            if (isWebP)
            {
                // If already WebP, just copy it (but still optimize if needed)
                using var inputStream = new MemoryStream();
                await file.CopyToAsync(inputStream, cancellationToken);
                var convertedStream = await ConvertToWebPAsync(inputStream, MaxWebPFileSizeInKB, cancellationToken);
                
                if (convertedStream is null)
                {
                    // If conversion fails, use original
                    inputStream.Position = 0;
                    using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    await inputStream.CopyToAsync(outputStream, cancellationToken);
                }
                else
                {
                    using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    await convertedStream.CopyToAsync(outputStream, cancellationToken);
                    await convertedStream.DisposeAsync();
                }
            }
            else
            {
                // Convert to WebP
                using var inputStream = new MemoryStream();
                await file.CopyToAsync(inputStream, cancellationToken);
                var convertedStream = await ConvertToWebPAsync(inputStream, MaxWebPFileSizeInKB, cancellationToken);
                
                if (convertedStream is null)
                {
                    response.Success = false;
                    response.Code = 500;
                    response.Messages = new List<Messages>
                    {
                        new() { message = "خطا در تبدیل تصویر به فرمت WebP." }
                    };
                    return response;
                }

                using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await convertedStream.CopyToAsync(outputStream, cancellationToken);
                await convertedStream.DisposeAsync();
            }

            var relativePath = "/" + Path.Combine(basePath, folderName, finalFileName).Replace("\\", "/");

            response.Success = true;
            response.Code = 201;
            response.Data = relativePath;
            response.Messages = new List<Messages>
            {
                new() { message = "تصویر با موفقیت به فرمت WebP تبدیل و ذخیره شد." }
            };

            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = 500;
            response.Messages = new List<Messages>
            {
                new() { message = $"خطا در ذخیره‌سازی تصویر: {ex.Message}" }
            };
            return response;
        }
    }

    public async Task<ResponseDto<string>> UploadFileAsync(string basePath, IFormFile file, string customFileName, CancellationToken cancellationToken = default)
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

        cancellationToken.ThrowIfCancellationRequested();

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

        using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await file.CopyToAsync(stream, cancellationToken);
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

    public async Task<ResponseDto> CopyFileAsync(string sourceDir, string destinationDir, CancellationToken cancellationToken = default)
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

        cancellationToken.ThrowIfCancellationRequested();

        using (var sourceStream = new FileStream(sourceDir, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        using (var destinationStream = new FileStream(destinationDir, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }
        response.Success = true;
        response.Code = 200;
        return response;
    }

    public async Task<ResponseDto> DeleteFileAsync(string? filePath, CancellationToken cancellationToken = default)
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

        cancellationToken.ThrowIfCancellationRequested();
        
        await Task.Run(() => File.Delete(fullPath), cancellationToken);
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
