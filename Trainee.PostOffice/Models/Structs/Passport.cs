namespace Trainee.PostOffice.Models.Structs;

public struct Passport
{
    public string? Series { get; set; }
    public string? Number { get; set; }
    public string? Authority { get; set; }
    public DateTime DateIssued { get; set; }

    public Passport()
    {
        Authority = "MOSCOW";
    }
}
