using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Bourse.Models
{
    public class StockData
    {
        public int Id { get; set; }
        public int IndiceId { get; set; }
        public float CurrentPrice { get; set; }
        public float PrevPrice { get; set; }
        public float Open { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public DateTime Date { get; set; }

        // Propriété calculée pour déterminer si le prix augmente
        public bool IsIncreasing => CurrentPrice > PrevPrice;

        [DisplayName("Change")]
        [DisplayFormat(DataFormatString = "{0:F5}", ApplyFormatInEditMode = true)]
        public double Change => CurrentPrice - PrevPrice;

        [DisplayName("(%)")]
        [DisplayFormat(DataFormatString = "{0:F3}")]
        public double ChangePercent => PrevPrice == 0 ? 0 : ((CurrentPrice - PrevPrice) / PrevPrice) * 100;

        public float SMA_14 { get; set; } // Moyenne mobile sur 14 jours
                                          // Propriété calculée pour la vue
        public string SMA_14Display => SMA_14 != 0
           ? Math.Round(SMA_14, 3).ToString()
           : "0";

        public float RSI_14 { get; set; } // RSI sur 14 jours

        public string RSI_14Display => RSI_14 != 0
            ? Math.Round(RSI_14) + " %"
            : "0 %";
        public float FuturePrice { get; set; }
        public float EMA_14 { get; set; }
        public float BollingerUpper { get; set; }
        public float BollingerLower { get; set; }
        public float MACD { get; set; }
        public float AverageVolume { get; set; }
    }
}
