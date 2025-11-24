using Arsis.Application.DTOs.OrganizationAnalysis;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace EndPoint.WebSite.Areas.Organization.Models;

public class OrganizationAnalysisViewModel
{
    public IEnumerable<OrganizationWeaknessDto> OrganizationWeaknesses { get; set; } = new List<OrganizationWeaknessDto>();
    public IEnumerable<UserTestResultDto> UserTestResults { get; set; } = new List<UserTestResultDto>();
    public IEnumerable<SelectListItem> AvailableWeaknessTypes { get; set; } = new List<SelectListItem>();
}
