using Bourse.Models;
using Bourse.Models.DTO;

namespace Bourse.Interfaces
{
    public interface IIndiceService
    {
        public Task<IQueryable<Indice>> ObtenirTout();
        public Task<IQueryable<Indice>> ObtenirSelonName(string name);
        public Task<Indice> ObtenirSelonSymbol(string symbol);
        public Task<Indice> ObtenirSelonId(int id);
        public Task<IQueryable<IndiceDTO>> ObtenirToutDTO();
        public Task<IQueryable<IndiceDTO>> ObtenirSelonNameDTO(string name);
        public Task<IndiceDTO> ObtenirSelonSymbolDTO(string symbol);
        public Task<IndiceDTO> ObtenirSelonIdDTO(int id);
        public Task<List<IndiceDTO>> ObtenirAgenda(DateTime start, DateTime end);
    }
}
