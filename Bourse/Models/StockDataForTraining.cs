namespace Bourse.Models
{
    public class StockDataForTraining
    {
        public int Id { get; set; }
        public int IndiceId { get; set; }
        public float CurrentPrice { get; set; }
        public float PrevPrice { get; set; }
        public float Open { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public DateTime Date { get; set; }
        public bool IsIncreasing { get; set; }
        public float Probability { get; set; }
        public float RSI_14 { get; set; }
        public float SMA_14 { get; set; }
        public float EMA_14 { get; set; }
        public float BollingerUpper { get; set; }
        public float BollingerLower { get; set; }
        public float MACD { get; set; }
        public float AverageVolume { get; set; }
        public float FuturePrice { get; set; }

    }

}
