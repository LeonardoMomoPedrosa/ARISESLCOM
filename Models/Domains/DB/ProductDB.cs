using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ARISESLCOM.Models.Domains.DB
{
    public class ProductDB(IRedisCacheService redis) : DBDomain(redis)
    {
        protected async Task<ActionResultModel> CreateProductDBAsync(ProductModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");
            String query = @"
                            INSERT INTO tbProdutos (nome,nome_new,id_subsubtipo,meta_name,meta_keys,descricao,preco,precoant,peso,
						                            atrelado,id_subtipo,nome_foto,estoque,promocao,disponivel,lucro,minParcJuros,sys_creation_date,
                                                    id_fornecedor,ativo) 
                            VALUES ('',@nome,@subtipo,@metanome,@metakeys,@descricao,@preco,0,@peso,0,@tipo,'',@estoque,@promocao,
                                    @disponivel,@lucro,0,getDate(),0,0)
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("nome", model.Nome);
            command.Parameters.AddWithValue("subtipo", model.SubTipo);
            command.Parameters.AddWithValue("metanome", model.MetaNome);
            command.Parameters.AddWithValue("metakeys", model.MetaKey);
            command.Parameters.AddWithValue("descricao", model.Descricao);
            command.Parameters.AddWithValue("preco", model.PrecoAtacado);
            command.Parameters.AddWithValue("peso", model.Peso);
            command.Parameters.AddWithValue("tipo", model.Tipo);
            command.Parameters.AddWithValue("estoque", model.Estoque);
            command.Parameters.AddWithValue("promocao", model.Promocao);
            command.Parameters.AddWithValue("disponivel", model.DiasDisp);
            command.Parameters.AddWithValue("lucro", model.Lucro);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Produto criado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao criar produto. Motivo={ex.Message}";
            }

            return resultModel;
        }

        private readonly string productListFields = @"nome_new,
                                                video,
                                                p.pkid,
                                                p.id_subtipo,
                                                p.id_subsubtipo,
                                                ativo,
                                                preco,
                                                display_order,
                                                nopac,
                                                peso,
                                                recomenda,
                                                disponivel,
                                                nome_foto,
                                                estoque,
                                                eps6id,
                                                eps6StockMin,
                                                promocao,
                                                p.descricao,
                                                precoant,
                                                lucro,
                                                minparcjuros,
                                                id_fornecedor,
                                                p.meta_name,
                                                p.meta_keys";

        public async Task<List<ProductModel>> GetProductListBySubTypeDBAsync(int subTypeId)
        {
            var model = new List<ProductModel>();
            String query = @$"
                                select {productListFields} 
                                from tbProdutos p 
                                left join tbsubtipoproduto stp on p.id_subsubtipo = stp.pkid
                                where stp.pkid=@subTypeId 
                                order by display_order desc,replace(nome_new,'<b>','')
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("subTypeId", subTypeId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(GetProductModel(rd));
            }
            return model;
        }

        public async Task<ProductModel> GetProductDBAsync(int productId)
        {
            var model = new ProductModel();
            String query = @$"
                                select {productListFields} 
                                from tbProdutos p 
                                where p.pkid=@id 
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = query;
            command.Parameters.AddWithValue("id", productId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model = GetProductModel(rd);
            }
            return model;
        }

        public async Task<List<ProductModel>> GetProductListByTypeDBAsync(int typeId)
        {
            var model = new List<ProductModel>();
            String query = @$"
                                select {productListFields}  
                                from tbProdutos p 
                                where id_subtipo=@typeId 
                                order by display_order desc,replace(nome_new,'<b>','')
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("typeId", typeId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(GetProductModel(rd));
            }
            return model;
        }

        public async Task<List<ProductModel>> GetProductListByNameDBAsync(string pname)
        {
            var model = new List<ProductModel>();

            pname = $"%{pname}%";

            String query = @$"
                                select {productListFields}  
                                from tbProdutos p 
                                where nome_new like @pname
                                and id_subtipo not in (8,19,20,21)
                                order by replace(nome_new,'<b>','')
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("pname", pname);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(GetProductModel(rd));
            }
            return model;
        }

        public async Task<List<ProductModel>> GetProductListByStockDBAsync(string pType, bool inStockInd)
        {
            var model = new List<ProductModel>();

            var clause = pType.Equals("A") ? "in" : "not in";
            var stockInd = inStockInd ? 1 : 0;

            String query = @$"
                                select {productListFields}  
                                from tbProdutos p 
                                where id_subtipo {clause} (8,19,20,21)
                                and estoque = @stockInd
                                and ativo = 1
                                order by id_subtipo, replace(nome_new,'<b>','')
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("stockInd", stockInd);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(GetProductModel(rd));
            }
            return model;
        }

        private static ProductModel GetProductModel(SLDataReader rd)
        {
            return new ProductModel()
            {
                PKId = rd.GetInt("pkid"),
                SubTipo = int.Parse(rd.GetStr("id_subtipo")),
                SubSubTipo = rd.GetInt("id_subsubtipo"),
                Nome = rd.GetStr("nome_new"),
                Video = rd.GetStr("video"),
                Ativo = rd.GetByte("ativo") == 1,
                PrecoAtacado = rd.GetDecFromDouble("preco"),
                DisplayOrder = rd.GetByte("display_order"),
                NoPac = rd.GetByte("nopac") == 1,
                Peso = rd.GetInt("peso"),
                Recomenda = rd.GetByte("recomenda") == 1,
                DiasDisp = rd.GetInt("disponivel"),
                Estoque = rd.GetBool("estoque"),
                Promocao = rd.GetStr("promocao").Trim().Equals("1"),
                Descricao = rd.GetStr("descricao"),
                PrecoAnt = rd.GetDecFromDouble("precoant"),
                Lucro = rd.GetDecFromDouble("lucro"),
                MinParcJuros = rd.GetInt16("minparcjuros"),
                IdFornecedor = rd.GetInt("id_fornecedor"),
                MetaNome = rd.GetStr("meta_name"),
                MetaKey = rd.GetStr("meta_keys"),
                EPS6Id = rd.GetInt("eps6id"),
                EPS6StockMin = rd.GetInt("eps6stockmin"),
                NomeFoto = rd.GetStr("nome_foto")
            };
        }

        public async Task<ActionResultModel> UpdateProductAsync(ProductModel model)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            string cmd = @"
                            update tbprodutos set nome_new=@nome,
                                                video=@video,
                                                ativo=@ativo,
                                                preco=@preco,
                                                peso=@peso,
                                                display_order=@display_order,
                                                nopac=@nopac,
                                                recomenda=@recomenda,
                                                disponivel=@disponivel,
                                                estoque=@estoque,
                                                promocao=@promocao,
                                                descricao=@descricao,
                                                precoant=@precoant,
                                                lucro=@lucro,
                                                minparcjuros=@minparcjuros,
                                                id_fornecedor=@id_fornecedor,
                                                meta_name=@meta_name,
                                                meta_keys=@meta_keys,
                                                sys_update_date = getDate()
                            where pkid=@pkid
            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("nome", model.Nome);
            command.Parameters.AddWithValue("video", model.Video);
            command.Parameters.AddWithValue("ativo", model.Ativo);
            command.Parameters.AddWithValue("preco", model.PrecoAtacado);
            command.Parameters.AddWithValue("display_order", model.DisplayOrder);
            command.Parameters.AddWithValue("nopac", model.NoPac);
            command.Parameters.AddWithValue("peso", model.Peso);
            command.Parameters.AddWithValue("recomenda", model.Recomenda);
            command.Parameters.AddWithValue("disponivel", model.DiasDisp);
            command.Parameters.AddWithValue("estoque", model.Estoque);
            command.Parameters.AddWithValue("promocao", model.Promocao);
            command.Parameters.AddWithValue("descricao", model.Descricao);
            command.Parameters.AddWithValue("precoant", model.PrecoAnt);
            command.Parameters.AddWithValue("lucro", model.Lucro);
            command.Parameters.AddWithValue("minparcjuros", model.MinParcJuros);
            command.Parameters.AddWithValue("id_fornecedor", model.IdFornecedor);
            command.Parameters.AddWithValue("meta_name", model.MetaNome);
            command.Parameters.AddWithValue("meta_keys", model.MetaKey);
            command.Parameters.AddWithValue("pkid", model.PKId);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Produto atualizado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar produto. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> UpdateProductImageDBAsync(int pkid, string imageFileName)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            string cmd = @"update tbprodutos set nome_foto=@nome_foto, sys_update_date = getDate() where pkid=@pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", pkid);
            command.Parameters.AddWithValue("nome_foto", imageFileName);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Imagem do produto atualizada com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar imagem do produto. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> DeleteProductAsync(int id)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            string cmd = @"delete from tbprodutos where pkid=@pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", id);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Produto deletado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao deletar produto. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> PatchERPIdAsync(int id, int erpId, int stockMin)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            string cmd = @"update tbprodutos set eps6id=@erpid,eps6stockmin=@erpstockmin where pkid=@pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("pkid", id);
            command.Parameters.AddWithValue("erpid", erpId);
            command.Parameters.AddWithValue("erpstockmin", stockMin);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Produto autalizado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar produto. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<ActionResultModel> UpdateProductStockDBAsync(int pkid, bool stockInd)
        {
            ActionResultModel resultModel = new(ActionResultModel.SUCCESS, "");

            var estoque = stockInd ? 1 : 0;

            string cmd = @"update tbprodutos set estoque = @estoque,sys_update_date = getDate() where pkid=@pkid";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("estoque", estoque);
            command.Parameters.AddWithValue("pkid", pkid);

            try
            {
                await command.ExecuteNonQueryAsync();
                resultModel.Message = "Produto atualizado com sucesso";
            }
            catch (Exception ex)
            {
                resultModel.SetError();
                resultModel.Message = $"Erro ao atualizar produto. Motivo={ex.Message}";
            }

            return resultModel;
        }

        public async Task<List<ProductFullTextSearchResult>> FullTextSearchAsync(string terms)
        {
            List<ProductFullTextSearchResult> model = [];

            string transformedQuery = string.Join(" AND ", terms.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            string cmd = @"
                            SELECT top 15 cast(pkid as varchar)+'-'+left(nome_new,100)+' - R$ '+cast(round(preco*(1+lucro/100),2) as varchar) as txt 
                            FROM tbprodutos 
                            WHERE CONTAINS(nome_new, @terms) 
                            and ativo=1;
                        ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = cmd;
            command.Parameters.AddWithValue("terms", transformedQuery);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new() { Result = rd.GetStr("txt") });
            }
            return model;
        }

    }
}
