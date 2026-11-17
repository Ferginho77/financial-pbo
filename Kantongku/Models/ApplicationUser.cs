using Microsoft.AspNetCore.Identity;

namespace Kantongku.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Pocket> OwnedPockets { get; set; } = new List<Pocket>();
    public ICollection<PocketMember> PocketMemberships { get; set; } = new List<PocketMember>();
    public ICollection<ExpenseGroup> ExpenseGroups { get; set; } = new List<ExpenseGroup>();
}
