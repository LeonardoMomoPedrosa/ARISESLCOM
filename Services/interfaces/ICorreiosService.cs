using ARISESLCOM.DTO;

namespace ARISESLCOM.Services.interfaces
{
    public interface ICorreiosService
    {
        public Task<CorreiosDTO> GetCorreiosPACAsync(string cep, int peso);
        public Task<CorreiosRastreamentoDTO> GetRastreamentoAsync(string codigoRastreamento);
    }
}
