using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.DB
{
    public class GerenciaDB(IRedisCacheService redis) : DBDomain(redis)
    {
        public async Task<String> GetGerPasswordDBAsync(string name)
        {
            String pwd = "";

            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
                            select senha from tbusuariosger where nome = @nome and ativo > 0
                        ");

            command.Parameters.AddWithValue("nome", name);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                pwd = rd.GetStr("senha");
            }

            return pwd;
        }

        public async Task<string> GetBannerPromoValueDBAsync()
        {
            string value = "P";

            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = "SELECT value FROM tbGeneric WHERE code = @code";
            command.Parameters.AddWithValue("code", "BANNER_PROMO");

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            if (rd.Read())
            {
                value = rd.GetStr("value");
            }

            return value;
        }

        public async Task UpdateBannerPromoDBAsync(string value)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = "UPDATE tbGeneric SET value = @value WHERE code = @code";
            command.Parameters.AddWithValue("code", "BANNER_PROMO");
            command.Parameters.AddWithValue("value", value);

            await command.ExecuteNonQueryAsync();
        }
    }
}
