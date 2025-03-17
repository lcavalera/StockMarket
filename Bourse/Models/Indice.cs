using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bourse.Models
{
    public class Indice
    {
        public int Id { get; set; }

        [Required]
        public string? Symbol { get; set; }

        [Required]
        public string? Name { get; set; }

        [DisplayName("Price")]
        [DisplayFormat(DataFormatString = "{0:F4}")]
        public double? RegularMarketPrice { get; set; }

        [DisplayName("Change")]
        [DisplayFormat(DataFormatString = "{0:F5}", ApplyFormatInEditMode = true)]
        public double? RegularMarketChange { get; set; }

        [DisplayName("Open")]
        [DisplayFormat(DataFormatString = "{0:F4}")]
        public double? RegularMarketOpen { get; set; }

        [DisplayName("Prev.Close")]
        [DisplayFormat(DataFormatString = "{0:F4}")]
        public double? RegularMarketPreviousClose { get; set; }

        [DisplayName("Day High")]
        [DisplayFormat(DataFormatString = "{0:F4}")]
        public double? RegularMarketDayHigh { get; set; }

        [DisplayName("Day Low")]
        [DisplayFormat(DataFormatString = "{0:F4}")]
        public double? RegularMarketDayLow { get; set; }

        [DisplayName("(%)")]
        [DisplayFormat(DataFormatString = "{0:F3}")]
        public double? RegularMarketChangePercent { get; set; }

        [DisplayName("Volume")]
        public long? RegularMarketVolume { get; set; }

        [DisplayName("Type")]
        public string? QuoteType { get; set; }

        public string? Exchange { get; set; }

        [DisplayName("Zone")]
        public string? ExchangeTimezoneName { get; set; }

        [DisplayName("Code Zone")]
        public string? ExchangeTimezoneShortName { get; set; }

        public string? Bourse { get; set; }

        [DisplayName("État Financier")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime[] DatesExercicesFinancieres { get; set; } = new DateTime[] {new DateTime()};

        public bool Label { get { return RegularMarketChangePercent > 0 ? true : false ; } }

        [DisplayName("Predict")]
        public bool IsIncreasing { get; set; }

        [DisplayName("Prob.")]
        public float Probability { get; set; }

        [DisplayName("Analysis")]
        public string? Raccomandation { get; set; }

        [DisplayName("Image")]
        public byte[]? imageAnalysis { get; set; }

        [DisplayName("Update")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateUpdated { get; set; }

        public DateOnly DatePrevision { get; set; }

        public virtual List<StockData>? TrainingData { get; set; }
       
    }
}
