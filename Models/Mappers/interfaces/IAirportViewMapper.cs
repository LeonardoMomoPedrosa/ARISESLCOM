using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Mappers.interfaces
{
    public interface IAirportViewMapper
    {
        public AirportViewModel MapAirportViewModel(AirportModel airportModel);
    }
}