using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;
using System.Collections.Generic;

namespace ARISESLCOM.Models.Domains.DB
{
    public class DestaqueDB(IRedisCacheService redis) : DBDomain(redis)
    {
        public async Task<List<DestaqueModel>> GetDestaqueListByTipoDBAsync(int tipo)
        {
            var model = new List<DestaqueModel>();
            String query = @"
                SELECT PKId, tipo, arquivo, link, ISNULL(frequencia3, 0.0) as frequencia3
                FROM tbDestaque 
                WHERE tipo = @tipo 
                ORDER BY ISNULL(frequencia3, 0.0) ASC, PKId DESC
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("tipo", tipo);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(GetDestaqueModel(rd));
            }
            return model;
        }

        public async Task<DestaqueModel> GetDestaqueDBAsync(int id)
        {
            var model = new DestaqueModel();
            String query = @"
                SELECT PKId, tipo, arquivo, link, ISNULL(frequencia3, 0.0) as frequencia3
                FROM tbDestaque 
                WHERE PKId = @id
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("id", id);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model = GetDestaqueModel(rd);
            }
            return model;
        }

        public async Task<ActionResultModel> CreateDestaqueDBAsync(DestaqueModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");
            String query = @"
                INSERT INTO tbDestaque (tipo, arquivo, link, frequencia3, conteudo_id) 
                VALUES (@tipo, @arquivo, @link, @frequencia3, 1)
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("tipo", model.Tipo);
            command.Parameters.AddWithValue("arquivo", model.Arquivo);
            command.Parameters.AddWithValue("link", model.Link ?? "");
            command.Parameters.AddWithValue("frequencia3", model.Frequencia3 ?? (object)DBNull.Value);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Destaque criado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao criar destaque. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> UpdateDestaqueDBAsync(DestaqueModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            string cmd = @"
                UPDATE tbDestaque 
                SET arquivo = @arquivo, 
                    link = @link
                WHERE PKId = @pkid
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("arquivo", model.Arquivo);
            command.Parameters.AddWithValue("link", model.Link ?? "");
            command.Parameters.AddWithValue("pkid", model.PKId);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Destaque atualizado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar destaque. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<List<DestaqueModel>> GetMosaicItemsDBAsync()
        {
            var model = new List<DestaqueModel>();
            String query = @"
                SELECT PKId, tipo, arquivo, link, ISNULL(frequencia3, 0.0) as frequencia3
                FROM tbDestaque 
                WHERE tipo = 100 
                ORDER BY ISNULL(frequencia3, 0.0) ASC
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(GetDestaqueModel(rd));
            }
            return model;
        }

        public async Task<DestaqueModel?> GetModalEntradaDBAsync()
        {
            String query = @"
                SELECT TOP 1 PKId, tipo, arquivo, link
                FROM tbDestaque 
                WHERE tipo = 200
                ORDER BY PKId DESC
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            if (rd.Read())
            {
                return new DestaqueModel()
                {
                    PKId = rd.GetInt("PKId"),
                    Tipo = rd.GetByte("tipo"),
                    Arquivo = rd.GetStr("arquivo"),
                    Link = rd.GetStr("link"),
                    Frequencia3 = null
                };
            }
            return null;
        }

        public async Task<ActionResultModel> DeleteDestaqueDBAsync(int id)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            string cmd = @"DELETE FROM tbDestaque WHERE PKId = @pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", id);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Destaque deletado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao deletar destaque. Motivo={ex.Message}";
            }

            return resultModel;
        }

        private static DestaqueModel GetDestaqueModel(SLDataReader rd)
        {
            return new DestaqueModel()
            {
                PKId = rd.GetInt("PKId"),
                Tipo = rd.GetByte("tipo"),
                Arquivo = rd.GetStr("arquivo"),
                Link = rd.GetStr("link"),
                Frequencia3 = rd.GetDecimal("frequencia3")
            };
        }
    }
}
