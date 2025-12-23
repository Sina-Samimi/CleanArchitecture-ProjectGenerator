using MobiRooz.SharedKernel.DTOs;
using Microsoft.AspNetCore.Http;

namespace MobiRooz.Application.Interfaces;

public interface IFormFileSettingServices
{
    Task<ResponseDto<string>> UploadImageAsync(string basePath, IFormFile file, string customFileName, CancellationToken cancellationToken = default);

    Task<ResponseDto<string>> UploadFileAsync(string basePath, IFormFile file, string customFileName, CancellationToken cancellationToken = default);

    Task<ResponseDto> CopyFileAsync(string sourceDir, string destinationDir, CancellationToken cancellationToken = default);

    Task<ResponseDto> DeleteFileAsync(string? filePath, CancellationToken cancellationToken = default);

    bool IsFileSizeValid(IFormFile file, int maxSizeInKB);

    bool EnsureDirectoryExists(string relativePath);
}
