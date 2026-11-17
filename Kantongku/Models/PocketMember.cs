namespace Kantongku.Models;

public class PocketMember
{
    public int Id { get; set; }

    public int PocketId { get; set; }
    public Pocket Pocket { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public PocketRole Role { get; set; } = PocketRole.Viewer;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
