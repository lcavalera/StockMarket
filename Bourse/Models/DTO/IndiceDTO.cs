using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Bourse.Models.DTO
{
    public class IndiceDTO
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
        public DateTime[] DatesExercicesFinancieres { get; set; } = new DateTime[] { new DateTime() };

        [DisplayName("Tendance")]
        public bool Label { get { return RegularMarketChangePercent > 0 ? true : false; } }

        [DisplayName("Prob.")]
        public float Probability { get; set; }

        [DisplayName("Racc.")]
        public string? Raccomandation { get; set; }

        public string? AnalysisJson { get; set; }

        [NotMapped]
        public IDictionary<string, int>? Analysis
        {
            get => string.IsNullOrEmpty(AnalysisJson)
                ? new Dictionary<string, int>()
                : JsonSerializer.Deserialize<Dictionary<string, int>>(AnalysisJson!);

            set => AnalysisJson = JsonSerializer.Serialize(value);
        }

        [DisplayName("Update")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DateUpdated { get; set; }

        public virtual List<StockData>? TrainingData { get; set; }

    }
}
