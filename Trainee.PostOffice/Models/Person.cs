using Trainee.PostOffice.Models.Structs;

namespace Trainee.PostOffice.Models;

public record Person
{
    public FullName FullName { get; set; }
    public Passport PassportData { get; set; }
    public string MobilePhone { get; set; } = "+7946776677";
}
