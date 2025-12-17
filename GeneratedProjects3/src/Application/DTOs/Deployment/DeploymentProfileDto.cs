using System;

namespace LogTableRenameTest.Application.DTOs.Deployment;

public sealed record DeploymentProfileDto(
    Guid Id,
    string Name,
    string Branch,
    string ServerHost,
    int ServerPort,
    string ServerUser,
    string DestinationPath,
    string ArtifactName,
    bool IsActive,
    string? PreDeployCommand,
    string? PostDeployCommand,
    string? ServiceReloadCommand,
    string? SecretKeyName,
    string? Notes,
    DateTimeOffset UpdatedAt);
