using Bourse.Models;
using Bourse.Models.DTO;

namespace Bourse.Interfaces
{
    public interface IIndiceService
    {
        public IQueryable<Indice> ObtenirTout();
        public IQueryable<Indice> ObtenirSelonName(string name);
        public Task<Indice?> ObtenirSelonSymbol(string symbol);
        public Task<Indice?> ObtenirSelonId(int id);
        public Task<List<IndiceDTO>> ObtenirToutDTO();
        public Task<List<IndiceDTO>> ObtenirSelonNameDTO(string name);
        public Task<IndiceDTO?> ObtenirSelonSymbolDTO(string symbol);
        public Task<IndiceDTO?> ObtenirSelonIdDTO(int id);
        public Task<List<IndiceDTO>> ObtenirAgenda(DateTime start, DateTime end);
    }
}
