using System;
using System.Collections.Generic;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.Application.DTOs.Orders;

public sealed record ShipmentTrackingDto(
    Guid Id,
    Guid InvoiceItemId,
    string InvoiceItemName,
    Guid? ProductId,
    string? ProductName,
    ShipmentStatus Status,
    string? TrackingNumber,
    string? Notes,
    DateTimeOffset StatusDate,
    string? UpdatedById,
    string? UpdatedByName);

public sealed record ShipmentTrackingListDto(
    Guid InvoiceId,
    string InvoiceNumber,
    IReadOnlyCollection<ShipmentTrackingDto> Trackings);

public sealed record ShipmentTrackingDetailDto(
    Guid Id,
    Guid InvoiceItemId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceItemName,
    Guid? ProductId,
    string? ProductName,
    ShipmentStatus CurrentStatus,
    string? TrackingNumber,
    string? Notes,
    DateTimeOffset StatusDate,
    string? UpdatedById,
    string? UpdatedByName,
    IReadOnlyCollection<ShipmentStatusHistoryDto> History);

public sealed record ShipmentStatusHistoryDto(
    ShipmentStatus Status,
    DateTimeOffset StatusDate,
    string? TrackingNumber,
    string? Notes,
    string? UpdatedByName);

