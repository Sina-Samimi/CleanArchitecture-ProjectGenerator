using System.Collections.Generic;
using Arsis.Domain.Entities;
using OrganizationStatus = Arsis.Domain.Entities.OrganizationStatus;

namespace EndPoint.WebSite.Areas.Admin.Models.Organizations
{
    public class OrganizationDetailsViewModel
    {
        public OrganizationDetailItem Organization { get; set; } = new();
        public IEnumerable<TestSummary> RecentTests { get; set; } = new List<TestSummary>();
    }

    public class OrganizationDetailItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int UserCount { get; set; }
        public int ActiveTests { get; set; }
        public int CompletedTests { get; set; }
        public OrganizationStatus Status { get; set; }
        public int MaxUsers { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class TestSummary
    {
        public string UserName { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public decimal Score { get; set; }
    }
}