using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Arsis.Domain.Entities;
using OrganizationStatus = Arsis.Domain.Entities.OrganizationStatus;

namespace EndPoint.WebSite.Areas.Admin.Models.Organizations
{
    public class OrganizationIndexRequest
    {
        public string? Search { get; set; }
        public OrganizationStatus? Status { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class OrganizationIndexViewModel : OrganizationIndexRequest
    {
        public IEnumerable<OrganizationListItem> Organizations { get; set; } = new List<OrganizationListItem>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    }

    public class OrganizationListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int ActiveTests { get; set; }
        public OrganizationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

