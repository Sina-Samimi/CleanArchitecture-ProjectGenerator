using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;

namespace Attar.Domain.Entities.Settings;

public sealed class DeploymentProfile : Entity
{
    public string Name { get; private set; } = string.Empty;

    public string Branch { get; private set; } = string.Empty;

    public string ServerHost { get; private set; } = string.Empty;

    public int ServerPort { get; private set; } = 22;

    public string ServerUser { get; private set; } = string.Empty;

    public string DestinationPath { get; private set; } = string.Empty;

    public string ArtifactName { get; private set; } = string.Empty;

    public string? PreDeployCommand { get; private set; }

    public string? PostDeployCommand { get; private set; }

    public string? ServiceReloadCommand { get; private set; }

    public string? SecretKeyName { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    [SetsRequiredMembers]
    private DeploymentProfile()
    {
    }

    [SetsRequiredMembers]
    public DeploymentProfile(
        string name,
        string branch,
        string serverHost,
        int serverPort,
        string serverUser,
        string destinationPath,
        string artifactName,
        bool isActive,
        string? preDeployCommand,
        string? postDeployCommand,
        string? serviceReloadCommand,
        string? secretKeyName,
        string? notes)
    {
        ApplyValues(
            name,
            branch,
            serverHost,
            serverPort,
            serverUser,
            destinationPath,
            artifactName,
            isActive,
            preDeployCommand,
            postDeployCommand,
            serviceReloadCommand,
            secretKeyName,
            notes,
            true);
    }

    public void Update(
        string name,
        string branch,
        string serverHost,
        int serverPort,
        string serverUser,
        string destinationPath,
        string artifactName,
        bool isActive,
        string? preDeployCommand,
        string? postDeployCommand,
        string? serviceReloadCommand,
        string? secretKeyName,
        string? notes)
        => ApplyValues(
            name,
            branch,
            serverHost,
            serverPort,
            serverUser,
            destinationPath,
            artifactName,
            isActive,
            preDeployCommand,
            postDeployCommand,
            serviceReloadCommand,
            secretKeyName,
            notes,
            false);

    private void ApplyValues(
        string name,
        string branch,
        string serverHost,
        int serverPort,
        string serverUser,
        string destinationPath,
        string artifactName,
        bool isActive,
        string? preDeployCommand,
        string? postDeployCommand,
        string? serviceReloadCommand,
        string? secretKeyName,
        string? notes,
        bool initializing)
    {
        Name = NormalizeRequired(name, nameof(name));
        Branch = NormalizeRequired(branch, nameof(branch));
        ServerHost = NormalizeRequired(serverHost, nameof(serverHost));
        ServerUser = NormalizeRequired(serverUser, nameof(serverUser));
        DestinationPath = NormalizeRequired(destinationPath, nameof(destinationPath));
        ArtifactName = NormalizeRequired(artifactName, nameof(artifactName));
        ServerPort = NormalizePort(serverPort);
        PreDeployCommand = NormalizeOptional(preDeployCommand);
        PostDeployCommand = NormalizeOptional(postDeployCommand);
        ServiceReloadCommand = NormalizeOptional(serviceReloadCommand);
        SecretKeyName = NormalizeOptional(secretKeyName);
        Notes = NormalizeOptional(notes);
        IsActive = isActive;

        if (!initializing)
        {
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    private static string NormalizeRequired(string value, string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, argumentName);
        return value.Trim();
    }

    private static string NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int NormalizePort(int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "شماره پورت باید بین 1 تا 65535 باشد.");
        }

        return port;
    }
}
