using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;

namespace ARISESLCOM.Models.Domains.DB
{
    public class ShipmentDB(IRedisCacheService redis) : DBDomain(redis)
    {
        public async Task<decimal> GetCapitalFretePreco(string estado, int peso)
        {
            decimal retVal = 1500;
            estado = $"%{estado}%";
            using SqlCommand command = GetSqlCommand();
            command.CommandText = @"
                                    SELECT TOP 1 preco FROM tbTamex 
                                    where estado like @estado 
                                    AND kg >= @peso order by kg asc
                                  ";

            command.Parameters.AddWithValue("peso", peso);
            command.Parameters.AddWithValue("estado", estado);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                retVal = rd.GetDecFromDouble("preco");
            }

            return retVal;
        }

        public async Task<decimal> GetInteriorFretePreco(string estado, int peso)
        {
            decimal retVal = 1500;
            estado = $"%{estado}%";
            using SqlCommand command = GetSqlCommand();
            command.CommandText = @"
                                    SELECT TOP 1 preco FROM tbVaspex 
                                    where estado like @estado 
                                    AND kg >= @peso order by kg asc
                                  ";

            command.Parameters.AddWithValue("peso", peso);
            command.Parameters.AddWithValue("estado", estado);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                retVal = rd.GetDecFromDouble("preco");
            }

            return retVal;
        }

        public async Task<decimal> GetGolShipmentPrice(string estado, int peso)
        {
            decimal retVal = 1500;
            estado = $"%{estado}%";
            using SqlCommand command = GetSqlCommand();
            command.CommandText = @"
                                    SELECT TOP 1 kg,estado,preco FROM tbGol 
                                    where estado like @estado AND kg >= @peso order by kg asc
                                  ";

            command.Parameters.AddWithValue("peso", peso);
            command.Parameters.AddWithValue("estado", estado);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                retVal = rd.GetDecFromDouble("preco");
            }

            return retVal;
        }

        public async Task<decimal> GetBuslogShipmentPrice(string estado, int peso)
        {
            decimal retVal = 1500;
            estado = $"%{estado}%";
            using SqlCommand command = GetSqlCommand();
            command.CommandText = @"
                                    SELECT TOP 1 preco FROM tbBusLog 
                                    where estado like @estado 
                                    AND estado NOT LIKE '%Adicional%' 
                                    AND kg >= @peso 
                                    order by kg asc
                                  ";

            command.Parameters.AddWithValue("peso", peso);
            command.Parameters.AddWithValue("estado", estado);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                retVal = rd.GetDecFromDouble("preco");
            }

            return retVal;
        }

        public async Task<bool> GetDeliveryBuslogInd(string cityName)
        {
            using SqlCommand command = GetSqlCommand();
            command.CommandText = @"
                                    SELECT TOP 1 estado FROM tbbusloglocal 
                                    WHERE nomeMunicipio COLLATE Latin1_general_CI_AI = @cityName
                                  ";

            command.Parameters.AddWithValue("cityName", cityName);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            return rd.Read();
        }

        public async Task<OrderPesoModel> GetOrderPesoAsync(int orderId)
        {
            OrderPesoModel orderPesoModel = new();
            using SqlCommand command = GetSqlCommand();
            command.CommandText = @"
                                        select sum(pc.quantidade*p.peso) as peso, d.lcidade, d.estado, d.cep
                                        from tbprodutoscompra pc, tbprodutos p, tbcompra c, tbdadoscompra d
                                        where pc.idproduto = p.pkid and pc.pkidcompra = @orderId
                                        and pc.pkidcompra = c.pkid
                                        and c.iddados = d.pkid 
                                        group by d.lcidade, d.estado, d.cep
                                  ";

            command.Parameters.AddWithValue("orderId", orderId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                orderPesoModel.Peso = rd.GetInt("peso");
                var val1 = (decimal)orderPesoModel.Peso / 1000;
                orderPesoModel.Peso = (int)Math.Ceiling(val1);
                orderPesoModel.LCidade = rd.GetStr("lcidade");
                orderPesoModel.Estado = rd.GetStr("estado");
                orderPesoModel.CEP = rd.GetStr("cep");
            }

            return orderPesoModel;
        }
    }


}
