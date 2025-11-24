using System.Collections.Generic;

namespace EndPoint.WebSite.Areas.Organization.Models;

public class OrganizationDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveTests { get; set; }
    public int CompletedTests { get; set; }
    public IEnumerable<WeaknessSummary> TopWeaknesses { get; set; } = new List<WeaknessSummary>();
}

public class WeaknessSummary
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
}
