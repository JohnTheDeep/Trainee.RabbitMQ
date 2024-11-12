using Trainee.PostOffice.Models.Enums;

namespace Trainee.PostOffice.Models;

public record Package
{
    public Guid PackageGuid { get; set; } = Guid.NewGuid();
    public string AddressFrom { get; set; } = string.Empty!;
    public string AddressTo { get; set; } = string.Empty!;
    public Person Sender { get; set; } = null!;
    public Person Receive { get; set; } = null!;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? SendedAt { get; set; }
    public double Weight { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.WaitingForRegistration;
}
