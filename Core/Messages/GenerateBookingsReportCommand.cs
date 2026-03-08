namespace Core.Messages;

public class GenerateBookingsReportCommand
{
    public Guid StudioId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string RequestedByEmail { get; set; } = null!;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}