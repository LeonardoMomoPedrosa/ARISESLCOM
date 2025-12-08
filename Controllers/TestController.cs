using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Controllers
{
    public class TestController(IShipmentDomainModel shipDomainModel,
                                    ILogger<TestController> logger,
                                    IDBContext dBContext) : Controller
    {
        private readonly IShipmentDomainModel _shipmentDomainModel = shipDomainModel;
        private readonly IDBContext _dBContext = dBContext;
        private readonly ILogger<TestController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            ShipmentModel model = null;
            _dBContext.GetSqlConnection().Open();
            try
            {
                _logger.LogInformation("TestController start");
                _shipmentDomainModel.SetContext(_dBContext);
                //model = await _shipmentDomainModel.GetShipmentAirportAsync(58001);
                model = await _shipmentDomainModel.GetShipmentCorreiosAsync(58001);
            }
            finally
            {
                await _dBContext.CloseAsync();
            }
            return View(model);
        }
    }
}
