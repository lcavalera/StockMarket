using AutoMapper;
using Bourse.Models;
using Bourse.Models.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bourse.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Indice, IndiceDTO>()
                .ForMember(dest => dest.Raccomandation, opt => opt.MapFrom(src =>
                    src.Raccomandation == "Strong Buy" && src.Probability > 0.65 ? "Strong Buy" :
                    (src.Raccomandation == "Strong Buy" || src.Raccomandation == "Buy") && src.Probability <= 0.65 && src.Probability > 0.35 ? "Buy" :
                    src.Raccomandation == "Strong Sell" && src.Probability < 0.35 ? "Strong Sell" :
                    (src.Raccomandation == "Strong Sell" || src.Raccomandation == "Sell") && src.Probability >= 0.35 && src.Probability < 0.65 ? "Sell" :
                    "Hold"
                ))
                .ForMember(dest => dest.TrainingData, opt => opt.MapFrom(src =>
                    src.TrainingData
                        .OrderByDescending(td => td.Date)
                        .Take(20)
                ));
        }

        //// Méthode pour appliquer la logique de Raccomandation
        //private string GetRecommendation(Indice indice)
        //{
        //    // Exemple de logique pour calculer la recommandation basée sur Analysis et Probability
        //    if (indice.Raccomandation == "Strong Buy" && indice.Probability > 0.65)
        //    {
        //        return "Strong Buy";
        //    }
        //    if ((indice.Raccomandation == "Strong Buy" || indice.Raccomandation == "Buy") && indice.Probability <= 0.65 && indice.Probability > 0.35)
        //    {
        //        return "Buy";
        //    }
        //    if (indice.Raccomandation == "Strong Sell" && indice.Probability < 0.35)
        //    {
        //        return "Strong Sell";
        //    }
        //    if ((indice.Raccomandation == "Strong Sell" || indice.Raccomandation == "Sell") && indice.Probability >= 0.35 && indice.Probability < 0.65)
        //    {
        //        return "Sell";
        //    }
        //    return "Hold"; // Default case
        //}
    }
}
