using Microsoft.ML.Data;

namespace Bourse.Models
{
    public class StockPrediction
    {
        // La prédiction finale (vrai si le prix va augmenter, sinon faux)
        [ColumnName("PredictedLabel")]
        public bool IsIncreasing { get; set; }

        // La probabilité que le prix augmente (ou diminue)
        public float Probability { get; set; }

        // Champ supplémentaire pour la marge (score brut de la prédiction)
        public float Score { get; set; }
    }
}
