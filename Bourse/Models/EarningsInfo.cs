namespace Bourse.Models
{
    public class EarningsInfo
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Company { get; set; }
        public string? Symbol { get; set; }
        public string? Url { get; set; }
    }
}
