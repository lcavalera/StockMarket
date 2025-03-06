using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bourse.Models
{
    public class RealPrice
    {
        public int Id { get; set; }
        public int IndiceId { get; set; }
        [DisplayName("Price")]
        public double? RegularMarketPrice { get; set; }
        [DisplayName("Change")]
        public double? RegularMarketChange { get; set; }
        [DisplayName("Open")]
        public double? RegularMarketOpen { get; set; }
        [DisplayName("Change (%)")]
        [DisplayFormat(DataFormatString = "{0:F3}")]
        public double? RegularMarketChangePercent { get; set; }
        [DisplayName("Previous Close")]
        public double? RegularMarketPreviousClose { get; set; }
        [DisplayName("Day High")]
        public double? RegularMarketDayHigh { get; set; }
        [DisplayName("Day Low")]
        public double? RegularMarketDayLow { get; set; }
        [DisplayName("Volume")]
        public long? RegularMarketVolume { get; set; }
        [DisplayName("Quote Type")]
        public string? QuoteType { get; set; }
        public string? Bourse { get; set; }
        public string? Exchange { get; set; }
        [DisplayName("Zone")]
        public string? ExchangeTimezoneName { get; set; }
        [DisplayName("Code Zone")]
        public string? ExchangeTimezoneShortName { get; set; }
        public bool Label { get { return RegularMarketChangePercent > 0 ? true : false; } }
        public DateTime Date { get; set; } = DateTime.Now;

    }
}
