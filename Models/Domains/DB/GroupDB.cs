using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.DB
{
    public class GroupDB(IRedisCacheService redis) : DBDomain(redis)
    {
        public async Task<List<ProductTypeModel>> GetProductTypeModelsAsync()
        {
            var cacheKey = RedisCacheService.GetProductTypeListKey();
            var model = await _redis.GetCacheValueAsync<List<ProductTypeModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            String query = @"
                                select tipo,
                                        descricao,
                                        pkid
                                from tbtipoproduto
                                order by tipo
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new ProductTypeModel()
                {
                    PKId = rd.GetInt("pkid"),
                    Descricao = rd.GetStr("descricao"),
                    Tipo = rd.GetStr("tipo")
                });
            }

            await SetCacheAsync<List<ProductTypeModel>>(cacheKey, model, int.MaxValue);
            return model;
        }

        public async Task<List<ProductTypeModel>> GetProductTypeModelsWithProductsAsync()
        {
            var model = new List<ProductTypeModel>();
            String query = @"
                                select distinct tp.tipo,
                                        tp.descricao,
                                        tp.pkid
                                from tbtipoproduto tp
                                inner join tbsubtipoproduto stp on tp.pkid = stp.id_tipo
                                inner join tbProdutos p on (p.id_subtipo = stp.pkid or p.id_subsubtipo = stp.pkid)
                                where p.ativo = 1
                                order by tp.tipo
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new ProductTypeModel()
                {
                    PKId = rd.GetInt("pkid"),
                    Descricao = rd.GetStr("descricao"),
                    Tipo = rd.GetStr("tipo")
                });
            }

            return model;
        }

        public async Task<List<ProductSubTypeModel>> GetProductSubTypeModelsAsync(int typeId)
        {
            var cacheKey = RedisCacheService.GetProductSubTypeListKey(typeId);
            var model = await _redis.GetCacheValueAsync<List<ProductSubTypeModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = new List<ProductSubTypeModel>();
            String query = @"
                                select PKId,
                                        id_tipo,
                                        visivel,
                                        subtipo,
                                        descricao,
                                        meta_name,
                                        meta_keys
                                from tbsubtipoproduto
                                where id_tipo = @idTipo
                                order by subtipo
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("idTipo", typeId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new ProductSubTypeModel()
                {
                    PKId = rd.GetInt("PKId"),
                    TypeId = rd.GetInt("id_tipo"),
                    Description = rd.GetStr("descricao"),
                    MetaName = rd.GetStr("meta_name"),
                    MetaKeys = rd.GetStr("meta_keys"),
                    SubType = rd.GetStr("subtipo"),
                    Visible = rd.GetBool("visivel")
                });
            }

            await SetCacheAsync<List<ProductSubTypeModel>>(cacheKey, model, int.MaxValue);
            return model;
        }

        public async Task<ActionResultModel> UpdateGroupDBAsync(ProductTypeModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var cacheKey = RedisCacheService.GetProductTypeListKey();

            String cmd = @"UPDATE tbTipoProduto SET tipo = @tipo, descricao=@desc WHERE PKId = @pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", model.PKId);
            command.Parameters.AddWithValue("tipo", model.Tipo);
            command.Parameters.AddWithValue("desc", model.Descricao);

            try
            {
                await command.ExecuteNonQueryAsync();
                await _redis.DeleteCacheValueAsync(cacheKey);
                resultModel.Message = "Grupo atualizado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar grupo. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> UpdateSubGroupDBAsync(ProductSubTypeModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var cacheKey = RedisCacheService.GetProductSubTypeListKey(model.TypeId);

            String cmd = @"UPDATE tbSubTipoProduto 
                            SET subtipo = @subtipo, 
                                descricao= @descricao,
                                meta_name= @meta_name,
                                meta_keys= @meta_keys
                            WHERE pkid = @pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", model.PKId);
            command.Parameters.AddWithValue("subtipo", model.SubType);
            command.Parameters.AddWithValue("descricao", model.Description);
            command.Parameters.AddWithValue("meta_name", model.MetaName);
            command.Parameters.AddWithValue("meta_keys", model.MetaKeys);

            try
            {
                await command.ExecuteNonQueryAsync();
                await _redis.DeleteCacheValueAsync(cacheKey);
                resultModel.Message = "Sub Grupo atualizado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar sub grupo. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> NewSubGroupDBAsync(ProductSubTypeModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var cacheKey = RedisCacheService.GetProductSubTypeListKey(model.TypeId);

            String cmd = @"INSERT INTO tbSubTipoProduto (subtipo,descricao,id_tipo) 
                            VALUES (@nome,@descricao,@idtipo)";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("nome", model.SubType);
            command.Parameters.AddWithValue("descricao", model.Description);
            command.Parameters.AddWithValue("idtipo", model.TypeId);

            try
            {
                await command.ExecuteNonQueryAsync();
                await _redis.DeleteCacheValueAsync(cacheKey);
                resultModel.Message = "Grupo criado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao criar grupo. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> DeleteGroupDBAsync(int pkid)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var cacheKey = RedisCacheService.GetProductTypeListKey();

            String cmd = @"DELETE FROM tbTipoProduto WHERE PKId = @pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", pkid);

            try
            {
                await command.ExecuteNonQueryAsync();
                await _redis.DeleteCacheValueAsync(cacheKey);
                resultModel.Message = "Grupo removido com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError($"Erro ao remover grupo. Motivo={ex.Message}");
            }

            return resultModel;
        }

        public async Task<ActionResultModel> DeleteSubGroupDBAsync(int typeId, int subtypeId)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var cacheKey = RedisCacheService.GetProductSubTypeListKey(typeId);

            String cmd = @"DELETE FROM tbSubTipoProduto WHERE PKId = @pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", subtypeId);

            try
            {
                await command.ExecuteNonQueryAsync();
                await _redis.DeleteCacheValueAsync(cacheKey);
                resultModel.Message = "Grupo removido com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError($"Erro ao remover grupo. Motivo={ex.Message}");
            }

            return resultModel;
        }

        public async Task<ActionResultModel> NewGroupDBAsync(string tipo, string descricao)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var cacheKey = RedisCacheService.GetProductTypeListKey();

            String cmd = @"INSERT INTO tbTipoProduto (tipo,descricao) VALUES (@tipo,@descricao)";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("tipo", tipo);
            command.Parameters.AddWithValue("descricao", descricao);

            try
            {
                await command.ExecuteNonQueryAsync();
                await _redis.DeleteCacheValueAsync(cacheKey);
                resultModel.Message = "Grupo adicionado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError($"Erro ao adicionar grupo. Motivo={ex.Message}");
            }

            return resultModel;
        }

    }
}

