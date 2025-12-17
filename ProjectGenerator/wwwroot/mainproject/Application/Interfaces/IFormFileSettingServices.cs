using Attar.SharedKernel.DTOs;
using Microsoft.AspNetCore.Http;

namespace Attar.Application.Interfaces;

public interface IFormFileSettingServices
{
    ResponseDto<string> UploadImage(string basePath, IFormFile file, string customFileName);

    ResponseDto<string> UploadFile(string basePath, IFormFile file, string customFileName);

    ResponseDto CopyFile(string sourceDir, string destinationDir);

    ResponseDto DeleteFile(string? filePath);

    bool IsFileSizeValid(IFormFile file, int maxSizeInKB);

    bool EnsureDirectoryExists(string relativePath);
}
