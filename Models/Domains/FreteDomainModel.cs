using Microsoft.Data.SqlClient;
using ARISESLCOM.Data;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class FreteDomainModel(IRedisCacheService redis) : FreteDB(redis), IFreteDomainModel
    {

        public async Task<OrderModel> GetFreteInfo(OrderModel orderModel)
        {
            if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_AEROPORTO))
            {
                orderModel.AirportModel = await GetAirPortAsync(orderModel.OrderSummary.IdAeroporto);
            }
            else if (orderModel.OrderSummary.Fretetp.Equals(SLCOMLIB.Helpers.LibConsts.FRETE_BUSLOG))
            {
                orderModel.BuslogModel = await GetBuslogAsync(orderModel.OrderSummary.IdAeroporto);
            }

            return orderModel;
        }

        public async Task<AirportModel> GetAirPortAsync(int airportId)
        {
            AirportModel model = new();
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = @"
                                            SELECT a.PKId,
                                                    a.Regiao,
                                                    a.Cidade,
                                                    a.Estado,
                                                    a.Logradouro,
                                                    a.Numero,
                                                    a.Complemento,
                                                    a.Bairro
                                            FROM tbAeroporto a
                                            WHERE a.PKId = @airportId
                                        ";

            command.Parameters.AddWithValue("airportId", airportId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Cidade = rd.GetStr("Cidade");
                model.PKId = rd.GetInt("PKId");
                model.Numero = rd.GetStr("Numero");
                model.Estado = rd.GetStr("Estado");
                model.Complemento = rd.GetStr("Complemento");
                model.Regiao = rd.GetStr("Regiao");
                model.Logradouro = rd.GetStr("Logradouro");
                model.Bairro = rd.GetStr("Bairro");
            }

            return model;
        }

        public Task<List<AirportModel>> GetAirPortListAsync(string UF, string cidade)
        {
            throw new NotImplementedException();
        }

        public async Task<BuslogModel> GetBuslogAsync(int pkid)
        {
            BuslogModel model = new();
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = @"
                                            SELECT a.pkid,
                                                    a.estado,
                                                    a.unidade,
                                                    a.ruav,
                                                    a.complemento,
                                                    a.bairro,
                                                    a.cep,
                                                    a.horario,
                                                    a.nomeMunicipio
                                            FROM tbBuslogLocal a
                                            WHERE a.pkid = @pkid
                                        ";

            command.Parameters.AddWithValue("pkid", pkid);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.PKId = rd.GetInt("PKId");
                model.Estado = rd.GetStr("estado");
                model.Unidade = rd.GetStr("unidade");
                model.Logradouro = rd.GetStr("ruav");
                model.Complemento = rd.GetStr("complemento");
                model.Bairro = rd.GetStr("bairro");
                model.CEP = rd.GetStr("cep");
                model.nomeMunicipio = rd.GetStr("nomeMunicipio");
            }

            return model;
        }

    }
}
