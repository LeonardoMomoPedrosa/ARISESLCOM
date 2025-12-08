using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography;

namespace ARISESLCOM.Models.Domains.DB
{
    public class ReportDB(IRedisCacheService redis) : DBDomain(redis)
    {
        public async Task<List<DayReportModel>> GetDayReportModel(DateModel date)
        {
            var cacheKey = RedisCacheService.GetDayReportKey(date.Data1.ToString("yyyyMMdd"));
            var model = await _redis.GetCacheValueAsync<List<DayReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            string query = @"
                                select	convert(NVARCHAR,c.data, 103) as data,
		                                convert(NVARCHAR,c.datamdst, 103) as datamdst,
		                                c.pkid,
		                                c.lionorderid,
		                                nf.numeronf as notafiscal,
		                                u.nome,
		                                d.cidade,
		                                d.estado,
		                                sum(preco*quantidade*(1-c.desconto/100)) as valorpago,
		                                isNull(c.frete,0) as frete,
		                                c.fretetp, 
		                                isNull(nf.juros,0) as juros,
		                                c.metodopagto 
                                from tbcompra c 
                                join tbprodutoscompra pc on c.pkid=pc.pkidcompra 
                                join tbdadoscompra d on c.iddados = d.pkid 
                                join tbusuarios u on c.pkidusuario = u.id 
                                left join tbnotafiscal nf on c.pkid = nf.pkidcompra 
                                where c.status = 'V' 
                                and format(c.datamdst,'yyyyMMdd') = @date 
                                group by c.data,c.datamdst,c.pkid,c.lionorderid,nf.numeronf,u.nome,
				                                d.cidade,d.estado,c.frete,c.fretetp,nf.juros,c.metodopagto 
                                order by c.datamdst,c.pkid;
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("date", date.Data1.ToString("yyyyMMdd"));

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new DayReportModel()
                {
                    Data = rd.GetStr("data"),
                    Datamdst = rd.GetStr("datamdst"),
                    Pkid = rd.GetInt("pkid"),
                    Lionorderid = rd.GetInt("lionorderid"),
                    Notafiscal = rd.GetStr("notafiscal"),
                    Nome = rd.GetStr("nome"),
                    Cidade = rd.GetStr("cidade"),
                    Estado = rd.GetStr("estado"),
                    Valorpago = rd.GetDecFromDouble("valorpago"),
                    Frete = rd.GetDecFromDouble("frete"),
                    Fretetp = rd.GetStr("fretetp"),
                    Juros = rd.GetDecFromDouble("juros"),
                    Modopagto = rd.GetStr("metodopagto")
                });
            }

            await SetCacheAsync(cacheKey, model, RedisCacheService.DAY_REPORT_MINUTES);

            return model;
        }

        public async Task<List<DayReportModel>> GetCupomReportModelDBAsync(string cupom, DateModel date)
        {
            var cacheKey = $"CupomReport:{cupom}:{date.Month:MMyyyy}";
            var model = await _redis.GetCacheValueAsync<List<DayReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            string query = @"
                                select	convert(NVARCHAR,c.data, 103) as data,
		                                convert(NVARCHAR,c.datamdst, 103) as datamdst,
		                                c.pkid,
		                                c.lionorderid,
		                                nf.numeronf as notafiscal,
		                                u.nome,
		                                d.cidade,
		                                d.estado,
		                                sum(preco*quantidade*(1-c.desconto/100)) as valorpago,
		                                isNull(c.frete,0) as frete,
		                                c.fretetp, 
		                                isNull(nf.juros,0) as juros,
		                                c.metodopagto 
                                from tbcompra c 
                                join tbprodutoscompra pc on c.pkid=pc.pkidcompra 
                                join tbdadoscompra d on c.iddados = d.pkid 
                                join tbusuarios u on c.pkidusuario = u.id 
                                left join tbnotafiscal nf on c.pkid = nf.pkidcompra 
                                where c.status = 'V' 
                                and c.cupom = @cupom
                                and month(c.datamdst) = @month 
                                and year(c.datamdst) = @year 
                                group by c.data,c.datamdst,c.pkid,c.lionorderid,nf.numeronf,u.nome,
				                                d.cidade,d.estado,c.frete,c.fretetp,nf.juros,c.metodopagto 
                                order by c.datamdst,c.pkid;
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("cupom", cupom);
            command.Parameters.AddWithValue("month", date.Month.Month);
            command.Parameters.AddWithValue("year", date.Month.Year);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new DayReportModel()
                {
                    Data = rd.GetStr("data"),
                    Datamdst = rd.GetStr("datamdst"),
                    Pkid = rd.GetInt("pkid"),
                    Lionorderid = rd.GetInt("lionorderid"),
                    Notafiscal = rd.GetStr("notafiscal"),
                    Nome = rd.GetStr("nome"),
                    Cidade = rd.GetStr("cidade"),
                    Estado = rd.GetStr("estado"),
                    Valorpago = rd.GetDecFromDouble("valorpago"),
                    Frete = rd.GetDecFromDouble("frete"),
                    Fretetp = rd.GetStr("fretetp"),
                    Juros = rd.GetDecFromDouble("juros"),
                    Modopagto = rd.GetStr("metodopagto")
                });
            }

            await SetCacheAsync(cacheKey, model, RedisCacheService.DAY_REPORT_MINUTES);

            return model;
        }

        public async Task<List<MonthReportModel>> GetMonthReportModel(DateModel date)
        {
            var cacheKey = RedisCacheService.GetMonthReportKey(date.Month.ToString("MMyyyy"));
            var model = await _redis.GetCacheValueAsync<List<MonthReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            string query = @"
                                   select t1.day as dia,
                                            sum(t1.val) as total,
                                            sum(t1.crd) as credito,
                                            count(t1.cnt) as numero 
                                            from(
                                                select   day(co.datamdst) as day, 
                                                            sum(preco * quantidade * (1 - co.desconto / 100)) as val, 
                                                            co.credito as crd, 
                                                            co.pkid as cnt 
                                                from tbcompra co 
                                                join tbprodutoscompra pc on co.pkid = pc.pkidcompra 
                                                where co.status = 'V' 
                                                and month(co.datamdst) = @month 
                                                and year(co.datamdst) = @year 
                                                group by day(co.datamdst), co.pkid, co.credito
                                            ) t1 group by t1.day order by t1.day
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("month", date.Month.Month);
            command.Parameters.AddWithValue("year", date.Month.Year);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new MonthReportModel()
                {
                    Dia = rd.GetInt("dia"),
                    Total = rd.GetDecFromDouble("total"),
                    Credito = rd.GetDecFromDouble("credito"),
                    NumPedidos = rd.GetInt("numero")
                });
            }

            await SetCacheAsync(cacheKey, model, RedisCacheService.DAY_REPORT_MINUTES);

            return model;
        }

        public async Task<List<GroupItemReportModel>> GetGroupItemReportModel(DateModel date)
        {
            var cacheKey = RedisCacheService.GetGroupItemKey(date.Month.ToString("MMyyyy"));
            var model = await _redis.GetCacheValueAsync<List<GroupItemReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            string query = @"
                                select t1.* from (
	                                select		pc.id_subtipo as subtipo,
				                                sum(pc.preco*pc.quantidade*(1-co.desconto/100)) as val, 
				                                sum(p.preco*pc.quantidade*(1-co.desconto/100)) as valc 
	                                from		tbcompra co 
	                                join		tbprodutoscompra pc on co.pkid=pc.pkidcompra 
	                                join		tbprodutos p on p.pkid=pc.idproduto 
	                                where		co.status='V' 
	                                and			month(co.datamdst)=@month
	                                and			year(co.datamdst)=@year
	                                and			pc.id_subtipo in (8,19,20,21) 
	                                group by	pc.id_subtipo 
	                                UNION 
	                                select		1 as subtipo,
				                                sum(pc.preco*pc.quantidade*(1-co.desconto/100)) as val, 
				                                sum(p.preco*pc.quantidade*(1-co.desconto/100)) as valc 
	                                from		tbcompra co 
	                                join		tbprodutoscompra pc on co.pkid=pc.pkidcompra 
	                                join		tbprodutos p on p.pkid=pc.idproduto 
	                                where		co.status='V' 
	                                and			month(co.datamdst)=@month
	                                and			year(co.datamdst)=@year
	                                and			pc.id_subtipo not in (8,19,20,21) 
                                ) t1 order by val desc
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("month", date.Month.Month);
            command.Parameters.AddWithValue("year", date.Month.Year);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new GroupItemReportModel()
                {
                    Subtipo = rd.GetInt("subtipo"),
                    Amount = rd.GetDecFromDouble("val"),
                    Cost = rd.GetDecFromDouble("valc")
                });
            }

            await SetCacheAsync(cacheKey, model, RedisCacheService.DAY_REPORT_MINUTES);

            return model;
        }

        public async Task<List<GroupItemReportModel>> GetGroupDetailReportModel(int id, DateModel date)
        {
            var cacheKey = RedisCacheService.GetGroupDetailKey(date.Month.ToString("MMyyyy"), id);
            var model = await _redis.GetCacheValueAsync<List<GroupItemReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            var customCondition = id == 1 ? "not in (8,19,20,21)" : "=@id";

            model = [];
            string query = @$"
                                select nome,val,valc from (
	                                select		t1.nome,sum(t1.val) as val,sum(t1.valc) as valc from (
		                                select		pc.nome,
					                                sum(pc.preco*pc.quantidade*(1-co.desconto/100)) as val, 
					                                sum(p.preco*pc.quantidade*(1-co.desconto/100)) as valc 
		                                from    	tbprodutoscompra pc 
		                                left join	tbcompra co on co.pkid=pc.pkidcompra 
		                                left join	tbprodutos p on p.pkid=pc.idProduto 
		                                where		co.status='V' 
		                                and			month(co.datamdst)=@month
		                                and			year(co.datamdst)=@year
		                                and			pc.id_subtipo {customCondition}
		                                group by	pc.nome) t1
	                                group by	t1.nome) as t1
                                order by val desc
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("month", date.Month.Month);
            command.Parameters.AddWithValue("year", date.Month.Year);
            command.Parameters.AddWithValue("id", id);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new GroupItemReportModel()
                {
                    Name = rd.GetStr("nome"),
                    Amount = rd.GetDecFromDouble("val"),
                    Cost = rd.GetDecFromDouble("valc")
                });
            }

            await SetCacheAsync(cacheKey, model, RedisCacheService.DAY_REPORT_MINUTES);

            return model;
        }

        public async Task<List<YearReportModel>> GetYearReportModel()
        {
            var cacheKey = RedisCacheService.GetYearReportKey();
            var model = await _redis.GetCacheValueAsync<List<YearReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            string query = @"
                                select	t1.month as mes,
                                        t1.ano,
                                        sum(t1.val) as valorpago,
                                        sum(t1.crd) as credito,
                                        sum(t1.juros) as juros,
                                        sum(t1.frete) as frete,
                                        count(t1.pkid) as num_ped,
                                        count(distinct t1.day) as days 
                                from (
                                        select		month(co.datamdst) as month,
                                                    year(co.datamdst) as ano,
                                                    day(co.datamdst) as day,
                                                    sum(preco*quantidade*(1-co.desconto/100)) as val,
                                                    co.credito as crd,
                                                    isnull(nf.juros,0) as juros,
                                                    co.frete,
                                                    co.pkid 
                                        from tbcompra co 
                                        join tbprodutoscompra pc on co.pkid=pc.pkidcompra 
                                        left join tbnotafiscal nf on nf.pkidcompra = co.pkid 
                                        where co.status='V' 
                                        and year(co.datamdst) >= 2006 
                                        group by month(co.datamdst),co.frete,nf.juros,year(co.datamdst),day(co.datamdst),co.pkid,co.credito) t1 
                                group by t1.month,t1.ano order by t1.ano,t1.month
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new YearReportModel()
                {
                    Mes = rd.GetInt("mes"),
                    Ano = rd.GetInt("ano"),
                    ValorVenda = rd.GetDecFromDouble("valorpago"),
                    Credito = rd.GetDecFromDouble("credito"),
                    Juros = rd.GetDecFromDouble("juros"),
                    Frete = rd.GetDecFromDouble("frete"),
                    Num_ped = rd.GetInt("num_ped"),
                    Days = rd.GetInt("days")
                });
            }

            await SetCacheAsync(cacheKey, model, RedisCacheService.YEAR_REPORT_MINUTES);

            return model;
        }

        public async Task<List<AviseMeReportModel>> GetAviseMeReportModel()
        {
            var model = new List<AviseMeReportModel>();
            string query = @"
                                select a.product_id,
	                                    count(a.product_id) as count,
	                                    p.nome_new,
	                                    p.nome_foto
                                from tbAvise a
                                left join tbProdutos p on p.pkid = a.product_id
                                group by a.product_id,p.nome_new,p.nome_foto
                                order by count desc
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new AviseMeReportModel()
                {
                    ProductId = rd.GetInt("product_id"),
                    Count = rd.GetInt("count"),
                    ProductName = rd.GetStr("nome_new") ?? "",
                    ProductImage = rd.GetStr("nome_foto") ?? ""
                });
            }

            return model;
        }

    }
}
