using Bourse.Models;

namespace Bourse.Interfaces
{
    public interface IIndiceService
    {
        public Task<List<Indice>> ObtenirTout();
        public Task<List<Indice>> ObtenirSelonName(string name);
        public Task<Indice> ObtenirSelonSymbol(string symbol);
        public Task<Indice> ObtenirSelonId(int id);
        public Task GetImageAnalysisIndice(int id);
    }
}
