using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers.interfaces;

namespace ARISESLCOM.Models.Mappers
{
    public class AirportViewMapper : IAirportViewMapper
    {
        public AirportViewModel MapAirportViewModel(AirportModel airportModel)
        {
            AirportViewModel airportViewModel = null;

            if (airportModel != null)
                airportViewModel = new()
                {
                    PKId = airportModel.PKId,
                    Logradouro = airportModel.Logradouro,
                    Bairro = airportModel.Bairro,
                    Numero = airportModel.Numero,
                    Cidade = airportModel.Cidade,
                    Complemento = airportModel.Complemento,
                    Estado = airportModel.Estado,
                    Regiao = airportModel.Regiao
                };
            return airportViewModel;
        }
    }
}
