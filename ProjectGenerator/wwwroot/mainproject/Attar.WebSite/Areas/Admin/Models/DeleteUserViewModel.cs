using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Areas.Admin.Models;

public sealed class DeleteUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}
