namespace Bourse.Models
{
    public class FuseHoraire
    {
        public TimeZoneInfo TimeZoneInfo { get; set; }
        public TimeSpan Ouverture {  get; set; }
        public TimeSpan Fermeture { get; set; }
        public HashSet<DateTime> JoursFeries { get; set; }
    }
}
