using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.DB
{
    public class CreditDB (IRedisCacheService redis) : DBDomain(redis)
    {

        public async Task<CreditModel> GetCreditByOrderAsync(int orderId)
        {
            CreditModel model = new();
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = @"
                                    SELECT cr.PKId,
                                            cr.IdCompra,
                                            cr.amount,
                                            cr.date,
                                            cr.iduser
                                    FROM tbCreDeb cr
                                    WHERE cr.idCompra = @orderId
                                  ";

            command.Parameters.AddWithValue("orderId", orderId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.PKId = rd.GetInt("PKId");
                model.IdCompra = rd.GetInt("IdCompra");
                model.Amount = rd.GetDecFromDouble("amount");
                model.Date = rd.GetDate("date");
                model.IdUsuario = rd.GetInt("iduser");
            }

            return model;
        }

        public async Task<ActionResultModel> DeleteCreditByOrderAsync(int orderId)
        {
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = @"
                                    DELETE FROM tbCreDeb WHERE idCompra = @orderId
                                  ";

            command.Parameters.AddWithValue("orderId", orderId);
            await command.ExecuteNonQueryAsync();
            return new ActionResultModel(ActionResultModel.SUCCESS, "");

        }

    }
}
