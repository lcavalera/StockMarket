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
                .ForMember(dest => dest.Raccomandation, opt => opt.MapFrom(src => GetRecommendation(src)));

            CreateMap<Indice, IndiceDTO>()
                .ForMember(dest => dest.TrainingData, opt => opt.MapFrom(src => src.TrainingData));

        }

        // Méthode pour appliquer la logique de Raccomandation
        private string GetRecommendation(Indice indice)
        {
            // Exemple de logique pour calculer la recommandation basée sur Analysis et Probability
            if (indice.Raccomandation == "Strong Buy" && indice.Probability > 0.65)
            {
                return "Strong Buy";
            }
            if ((indice.Raccomandation == "Strong Buy" || indice.Raccomandation == "Buy") && indice.Probability <= 0.65 && indice.Probability > 0.35)
            {
                return "Buy";
            }
            if (indice.Raccomandation == "Strong Sell" && indice.Probability < 0.35)
            {
                return "Strong Sell";
            }
            if ((indice.Raccomandation == "Strong Sell" || indice.Raccomandation == "Sell") && indice.Probability >= 0.35 && indice.Probability < 0.65)
            {
                return "Sell";
            }
            return "Hold"; // Default case
        }
    }
}
