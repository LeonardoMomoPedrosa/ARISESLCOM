using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Services.interfaces;
using System.Globalization;
using System;

namespace ARISESLCOM.Models.Domains
{
    public class ShipmentDomainModel(IRedisCacheService redis,
                                        ICorreiosService correiosService,
                                        ILogger<ShipmentDomainModel> logger) : ShipmentDB(redis), IShipmentDomainModel
    {
        private readonly ICorreiosService _correiosService = correiosService;
        private readonly ILogger<ShipmentDomainModel> _logger = logger;

        public async Task<ShipmentModel> GetShipmentTranspAsync(int orderId)
        {
            _logger.LogInformation("Order {#orderId}", orderId);
            ShipmentModel model = new();
            var peso = await GetOrderPesoAsync(orderId);
            _logger.LogInformation("Peso {@peso}", peso);

            model.Peso = peso.Peso;

            if (peso.Peso == 0)
            {
                model.Preco = 0;
            }
            else if (peso.Peso <= 30)
            {
                
                var capital = await GetCapitalFretePreco(peso.Estado, peso.Peso);
                model.Preco = capital;
                if (peso.LCidade.Equals("interior", StringComparison.OrdinalIgnoreCase))
                {
                    var interior = await GetInteriorFretePreco(peso.Estado, peso.Peso);
                    model.Preco += interior;
                }
            }
            else if (peso.Peso > 30)
            {
                var pesoDiff = peso.Peso - 30;
                if (peso.LCidade.Equals("interior", StringComparison.OrdinalIgnoreCase))
                {
                    var interior = await GetInteriorFretePreco(peso.Estado, 30);
                    var interiorAdd = await GetInteriorFretePreco(peso.Estado, 111);

                    model.Preco = interior + pesoDiff * interiorAdd;

                }
                else
                {
                    var capital = await GetCapitalFretePreco(peso.Estado, 30);
                    var capitalAdd = await GetCapitalFretePreco(peso.Estado, 111);

                    model.Preco = capital + pesoDiff * capitalAdd;
                }

            }

            model.Preco = GetMinimum(model.Preco);

            return model;
        }

        public async Task<ShipmentModel> GetShipmentAirportAsync(int orderId)
        {
            ShipmentModel model = new();
            var peso = await GetOrderPesoAsync(orderId);
            model.Peso = peso.Peso;

            if (peso.Peso == 0)
            {
                model.Preco = 0;
            }
            else if (peso.Peso > 0)
            {
                model.Preco = await GetGolShipmentPrice(peso.Estado, peso.Peso);
            }

            model.Preco = GetMinimum(model.Preco);

            return model;
        }

        public async Task<ShipmentModel> GetShipmentCorreiosAsync(int orderId)
        {
            _logger.LogDebug("Order {#orderId}", orderId);
            ShipmentModel model = new();
            var peso = await GetOrderPesoAsync(orderId);
            model.Peso = peso.Peso;
            _logger.LogDebug("Peso {@peso}", peso.Peso);

            if (peso.Peso == 0)
            {
                model.Preco = 0;
            }
            else if (peso.Peso > 0)
            {
                _logger.LogDebug("Chamando Correios CEP {@CEP}", peso.CEP);
                var correiosModel = await _correiosService.GetCorreiosPACAsync(peso.CEP, peso.Peso);
                _logger.LogDebug("Correios retornou R$ {@pcFinal}", correiosModel.CorreiosPrecoDTO.pcFinal);
                model.Preco = decimal.Parse(correiosModel.CorreiosPrecoDTO.pcFinal, new CultureInfo("pt-BR"));
                //model.Prazo = correiosModel.CorreiosPrazoDTO.prazoEntrega;
            }

            model.Preco = GetMinimum(model.Preco);

            return model;
        }

        public async Task<ShipmentModel> GetShipmentBuslogAsync(int orderId)
        {
            ShipmentModel model = new();
            var peso = await GetOrderPesoAsync(orderId);
            model.Peso = peso.Peso;

            if (peso.Peso == 0)
            {
                model.Preco = 0;
            }
            else if (peso.Peso > 0)
            {
                model.Preco = await GetBuslogShipmentPrice(peso.Estado, peso.Peso);
                // Note: Buslog does NOT apply minimum freight (R$ 18.80) according to documentation
            }

            return model;
        }

        private static decimal GetMinimum(decimal price)
        {
            return price < (decimal)18.8 ? (decimal)18.8 : price;
        }
    }
}
