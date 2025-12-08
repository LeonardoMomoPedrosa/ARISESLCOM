using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;

namespace ARISESLCOM.Models.Domains.DB
{
    public class OrderDB(IRedisCacheService redis) : DBDomain(redis)
    {
        private readonly static string _orderFields = @"
                                                   c.PKIdUsuario,
                                                    c.PKId,
                                                    c.status,
                                                    c.metodoPagto,
                                                    c.data,
                                                    c.dataMdSt,
                                                    c.parc,
                                                    c.idDados,
                                                    c.frete,
                                                    c.via,
                                                    c.track,
                                                    c.desconto,
                                                    c.taxaJuros,
                                                    c.minParcJuros,
                                                    c.credito,
                                                    c.boletoConf,
                                                    c.fretetp,
                                                    c.lionOrderId,
                                                    c.parcVal,
                                                    c.idAeroporto,
                                                    dc.lcidade,
                                                    u.nome,
                                                    RTRIM(LTRIM(ISNULL(dc.nome, '') + ' ' + ISNULL(dc.sobrenome, ''))) AS dnome,
                                                    dc.cidade,
                                                    dc.estado,
                                                    u.email,
                                                    c.TID,
                                                    c.NSU,
                                                    c.NSU_SHIP,
                                                    c.AUTHCODE,
                                                    c.TID_SHIP,
                                                    c.AUTHCODE_SHIP,
                                                    c.metodoPagto,
                                                    c.REDESTATUS,
                                                    c.REDESTATUS_SHIP,
                                                    c.REDESTATUSDESC,
                                                    c.REDESTATUSDESC_SHIP,
                                                    c.First6,
                                                    c.Last4,
                                                    s.nome as nomecc
        ";

        private readonly static string _orderItemsFields = @"
                                                    idProduto,
                                                    quantidade,
                                                    PKId,
                                                    PKIdCompra,
                                                    preco,
                                                    atrelado,
                                                    nome,
                                                    id_subtipo,
                                                    peso,
                                                    estoque,
                                                    sys_creation_date,
                                                    minParcJuros,
                                                    sys_update_date
        ";

        private readonly static string _orderTodayFields = @"
                                                    c.PKId,
                                                    c.status,
                                                    c.metodoPagto,
                                                    c.data,
                                                    c.dataMdSt,
                                                    c.parc,
                                                    c.lionOrderId,
                                                    c.fretetp,
                                                    c.TID,
                                                    c.NSU,
                                                    c.NSU_SHIP,
                                                    c.AUTHCODE,
                                                    c.TID_SHIP,
                                                    c.AUTHCODE_SHIP,
                                                    c.REDESTATUS,
                                                    c.REDESTATUS_SHIP,
                                                    c.First6,
                                                    c.Last4,
                                                    u.nome,
                                                    d.nome as dnome,
                                                    d.sobrenome,
                                                    s.nome as nomecc
        ";

        protected async Task ChangeOrderStatusDBAsync(int orderId, string status)
        {
            var cacheKey = RedisCacheService.GetOrderReditKey(orderId);
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = @"
                                    UPDATE tbCompra 
                                    SET status=@status,
                                    dataMdSt = dateadd(HH,1,getdate())
                                    WHERE PKId = @orderId
                                        ";

            command.Parameters.AddWithValue("status", status);
            command.Parameters.AddWithValue("orderId", orderId);

            await command.ExecuteNonQueryAsync();
            await _redis.DeleteCacheValueAsync(cacheKey);
        }

        public async Task<List<OrderSummaryModel>> SearchOrderByCustomerNameDBAsync(string customerName)
        {
            var model = new List<OrderSummaryModel>();
            customerName = $"%{customerName}%";
            String query = @"
                                SELECT u.nome, c.PKId, c.status, c.data, c.via, c.track 
                                FROM tbCompra c, tbdadoscompra d,tbusuarios u 
                                WHERE (d.nome like @name or d.sobrenome like @name or u.nome like @name) 
                                AND c.iddados = d.pkid and u.id = c.pkidusuario order by data desc
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("name", customerName);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new OrderSummaryModel()
                {
                    PKId = rd.GetInt("PKId"),
                    Data = rd.GetDate("data"),
                    CustomerName = rd.GetStr("nome"),
                    Status = rd.GetStr("status"),
                    Via = rd.GetStr("via"),
                    Track = rd.GetStr("track")
                });
            }
            return model;
        }

        public async Task<int> UpdateTrackingDBAsync(TrackingModel model)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = @"
                                    UPDATE tbCompra 
                                    SET track=@track,
                                    via=@via
                                    WHERE PKId = @orderId;
                                    
                                    MERGE tbTrackControl AS target
                                    USING (SELECT @orderId AS orderId, @source AS source) AS sourceData
                                    ON target.orderId = sourceData.orderId
                                    WHEN NOT MATCHED THEN
                                        INSERT (orderId, status, source)
                                        VALUES (@orderId, 0, @source)
                                    WHEN MATCHED THEN
                                        UPDATE SET status = 0, source = @source;

                                    MERGE tbTrackHistory AS history
                                    USING (SELECT @orderId AS order_id, @track AS track_no, @via AS via, @source AS source) AS sourceHistory
                                    ON history.order_id = sourceHistory.order_id
                                    WHEN MATCHED THEN
                                        UPDATE SET track_no = sourceHistory.track_no,
                                                   via = sourceHistory.via,
                                                   source = sourceHistory.source,
                                                   creation_date = GETDATE()
                                    WHEN NOT MATCHED THEN
                                        INSERT (order_id, track_no, via, source, creation_date)
                                        VALUES (sourceHistory.order_id, sourceHistory.track_no, sourceHistory.via, sourceHistory.source, GETDATE());
                                        ";

            command.Parameters.AddWithValue("track", model.TrackNo);
            command.Parameters.AddWithValue("via", model.Via);
            command.Parameters.AddWithValue("source", model.Source ?? "E");
            command.Parameters.AddWithValue("orderId", model.OrderId);

            int rows = await command.ExecuteNonQueryAsync();

            return rows;
        }

        public async Task<List<TrackingModel>> GetTrackingHistoryDBAsync()
        {
            List<TrackingModel> history = [];

            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = @"
                                    SELECT order_id, track_no, via, source, creation_date
                                    FROM tbTrackHistory
                                    WHERE creation_date >= DATEADD(day, -7, GETDATE())
                                    ORDER BY creation_date DESC
                                    ";

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                history.Add(new TrackingModel
                {
                    OrderId = rd.GetInt("order_id"),
                    TrackNo = rd.GetStr("track_no"),
                    Via = rd.GetStr("via"),
                    Source = rd.GetStr("source") ?? "E",
                    InsertedAt = rd.GetDate("creation_date")
                });
            }

            return history;
        }

        public async Task<bool> OrderExistsDBAsync(int orderId)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = @"
                                    SELECT COUNT(1) 
                                    FROM tbCompra 
                                    WHERE PKId = @orderId
                                ";

            command.Parameters.AddWithValue("orderId", orderId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task UpdateShipmentDBAsync(int orderId, decimal shipmentAmount)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = @"
                                    UPDATE tbCompra 
                                    SET frete=@frete
                                    WHERE PKId = @orderId
                                        ";

            command.Parameters.AddWithValue("frete", shipmentAmount);
            command.Parameters.AddWithValue("orderId", orderId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<OrderStatusReportModel>> GetOrderStatusReportDBAsync()
        {
            var cacheKey = RedisCacheService.GetOrderStatusReportKey();
            var model = await _redis.GetCacheValueAsync<List<OrderStatusReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            String query = @"
                                select 	c.pkid,
		                                c.status,
		                                convert(varchar,c.data,103) as data,
		                                datediff(day,c.data,getDate()) as diff,
		                                sum(pc.preco * pc.quantidade) as total 
                                from tbcompra c 
                                join tbprodutoscompra pc on pc.pkidcompra = c.pkid 
                                where status in ('A','P','G','B','C','Q','S') 
                                group by c.pkid,c.status,c.data 
                                order by c.status,datediff(day,c.data,getDate()) desc
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new OrderStatusReportModel()
                {
                    Pkid = rd.GetInt("pkid"),
                    Status = rd.GetStr("status"),
                    Data = rd.GetStr("data"),
                    Diff = rd.GetInt("diff"),
                    Total = rd.GetDecFromDouble("total")
                });

            }

            await SetCacheAsync<List<OrderStatusReportModel>>(cacheKey, model, RedisCacheService.ORDER_STATUS_REPORT_MINUTES);
            return model;
        }

        public async Task<List<OrderStatusTodayReportModel>> GetOrderStatusTodayReportDBAsync()
        {
            var cacheKey = RedisCacheService.GetOrderStatusTodayReportKey();
            var model = await _redis.GetCacheValueAsync<List<OrderStatusTodayReportModel>>(cacheKey);

            if (model != null)
            {
                return model;
            }

            model = [];
            String query = @"
                                select status,isnull(count(1),0) as count 
                                from tbcompra 
                                where status in ('V','N','L') 
                                and format(datamdst,'yyyyMMdd')=format(getDate(),'yyyyMMdd')
                                group by status
                                order by status
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new OrderStatusTodayReportModel()
                {
                    Status = rd.GetStr("status"),
                    Count = rd.GetInt("count")
                });
            }

            await SetCacheAsync<List<OrderStatusTodayReportModel>>(cacheKey,
                                                                    model,
                                                                    RedisCacheService.ORDER_STATUS_TODAY_REPORT_MINUTES);
            return model;
        }

        public async Task<List<OrderSummaryModel>> GetRejectedOrdersTodayDBAsync()
        {
            var model = new List<OrderSummaryModel>();
            String query = @$"
                                SELECT DISTINCT top 100 c.PKId as idcompra, 
                                                    isnull(max(p.id_subtipo),0) as tipo,
                                                    c.lionOrderId as lid,
                                                    {_orderTodayFields}
                                     FROM tbCompra c 
                                     left join tbProdutosCompra pc on pc.pkidcompra = c.pkid 
                                     left join tbProdutos p on p.pkid = pc.idproduto and p.id_subtipo in (8,19,20,21) 
                                     join tbDadosCompra d on c.PKIdUsuario = d.id_user 
                                     join tbUsuarios u on c.PKIdUsuario = u.id AND d.PKId = c.idDados 
                                     left join sysalloc s on s.PKId = c.idCC
                                     WHERE c.status = 'N' AND CAST(c.dataMdSt as date) = CAST(GETDATE() as date)
                                     group by c.pkid,c.datamdst,d.nome,c.data,c.parc,d.sobrenome,u.nome,c.lionOrderId,c.status,c.fretetp,c.tid,c.TID_SHIP,
                                                c.AUTHCODE,c.AUTHCODE_SHIP,c.metodoPagto,c.REDESTATUS,c.REDESTATUS_SHIP,c.NSU,c.NSU_SHIP,first6,last4,s.nome
                                     ORDER BY c.data
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new OrderSummaryModel()
                {
                    PKId = rd.GetInt("idcompra"),
                    Data = rd.GetDate("data"),
                    DataMdSt = rd.GetDate("dataMdSt"),
                    CustomerName = rd.GetStr("nome"),
                    ReceiverName = rd.GetStr("dnome"),
                    ModoPagto = rd.GetStr("metodoPagto"),
                    ReceiverLastName = rd.GetStr("sobrenome"),
                    LionOrderId = rd.GetInt("lid"),
                    Status = rd.GetStr("status"),
                    Tipo = 0,
                    Parc = rd.GetInt("parc"),
                    Fretetp = rd.GetStr("fretetp"),
                    TID = rd.GetStr("TID"),
                    TID_SHIP = rd.GetStr("TID_SHIP"),
                    NSU = rd.GetStr("NSU"),
                    NSU_SHIP = rd.GetStr("NSU_SHIP"),
                    AUTHCODE = rd.GetStr("AUTHCODE"),
                    AUTHCODE_SHIP = rd.GetStr("AUTHCODE_SHIP"),
                    REDESTATUS = rd.GetStr("REDESTATUS"),
                    REDESTATUS_SHIP = rd.GetStr("REDESTATUS_SHIP"),
                    First6 = rd.GetStr("First6"),
                    Last4 = rd.GetStr("Last4"),
                    NomeCC = rd.GetStr("nomecc")
                });
            }

            return model;
        }

        public async Task<List<OrderSummaryModel>> GetSentOrdersTodayDBAsync()
        {
            var model = new List<OrderSummaryModel>();
            String query = @$"
                                SELECT DISTINCT top 100 c.PKId as idcompra, 
                                                    isnull(max(p.id_subtipo),0) as tipo,
                                                    c.lionOrderId as lid,
                                                    {_orderTodayFields}
                                     FROM tbCompra c 
                                     left join tbProdutosCompra pc on pc.pkidcompra = c.pkid 
                                     left join tbProdutos p on p.pkid = pc.idproduto and p.id_subtipo in (8,19,20,21) 
                                     join tbDadosCompra d on c.PKIdUsuario = d.id_user 
                                     join tbUsuarios u on c.PKIdUsuario = u.id AND d.PKId = c.idDados 
                                     left join sysalloc s on s.PKId = c.idCC
                                     WHERE c.status = 'V' AND CAST(c.dataMdSt as date) = CAST(GETDATE() as date)
                                     group by c.pkid,c.datamdst,d.nome,c.data,c.parc,d.sobrenome,u.nome,c.lionOrderId,c.status,c.fretetp,c.tid,c.TID_SHIP,
                                                c.AUTHCODE,c.AUTHCODE_SHIP,c.metodoPagto,c.REDESTATUS,c.REDESTATUS_SHIP,c.NSU,c.NSU_SHIP,first6,last4,s.nome
                                     ORDER BY c.data
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new OrderSummaryModel()
                {
                    PKId = rd.GetInt("idcompra"),
                    Data = rd.GetDate("data"),
                    DataMdSt = rd.GetDate("dataMdSt"),
                    CustomerName = rd.GetStr("nome"),
                    ReceiverName = rd.GetStr("dnome"),
                    ModoPagto = rd.GetStr("metodoPagto"),
                    ReceiverLastName = rd.GetStr("sobrenome"),
                    LionOrderId = rd.GetInt("lid"),
                    Status = rd.GetStr("status"),
                    Tipo = 0,
                    Parc = rd.GetInt("parc"),
                    Fretetp = rd.GetStr("fretetp"),
                    TID = rd.GetStr("TID"),
                    TID_SHIP = rd.GetStr("TID_SHIP"),
                    NSU = rd.GetStr("NSU"),
                    NSU_SHIP = rd.GetStr("NSU_SHIP"),
                    AUTHCODE = rd.GetStr("AUTHCODE"),
                    AUTHCODE_SHIP = rd.GetStr("AUTHCODE_SHIP"),
                    REDESTATUS = rd.GetStr("REDESTATUS"),
                    REDESTATUS_SHIP = rd.GetStr("REDESTATUS_SHIP"),
                    First6 = rd.GetStr("First6"),
                    Last4 = rd.GetStr("Last4"),
                    NomeCC = rd.GetStr("nomecc")
                });
            }

            return model;
        }

        public async Task<List<OrderSummaryModel>> GetCancelledOrdersTodayDBAsync()
        {
            var model = new List<OrderSummaryModel>();
            String query = @$"
                                SELECT DISTINCT top 100 c.PKId as idcompra, 
                                                    isnull(max(p.id_subtipo),0) as tipo,
                                                    c.lionOrderId as lid,
                                                    {_orderTodayFields}
                                     FROM tbCompra c 
                                     left join tbProdutosCompra pc on pc.pkidcompra = c.pkid 
                                     left join tbProdutos p on p.pkid = pc.idproduto and p.id_subtipo in (8,19,20,21) 
                                     join tbDadosCompra d on c.PKIdUsuario = d.id_user 
                                     join tbUsuarios u on c.PKIdUsuario = u.id AND d.PKId = c.idDados 
                                     left join sysalloc s on s.PKId = c.idCC
                                     WHERE c.status = 'L' AND CAST(c.dataMdSt as date) = CAST(GETDATE() as date)
                                     group by c.pkid,c.datamdst,d.nome,c.data,c.parc,d.sobrenome,u.nome,c.lionOrderId,c.status,c.fretetp,c.tid,c.TID_SHIP,
                                                c.AUTHCODE,c.AUTHCODE_SHIP,c.metodoPagto,c.REDESTATUS,c.REDESTATUS_SHIP,c.NSU,c.NSU_SHIP,first6,last4,s.nome
                                     ORDER BY c.data
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(new OrderSummaryModel()
                {
                    PKId = rd.GetInt("idcompra"),
                    Data = rd.GetDate("data"),
                    DataMdSt = rd.GetDate("dataMdSt"),
                    CustomerName = rd.GetStr("nome"),
                    ReceiverName = rd.GetStr("dnome"),
                    ModoPagto = rd.GetStr("metodoPagto"),
                    ReceiverLastName = rd.GetStr("sobrenome"),
                    LionOrderId = rd.GetInt("lid"),
                    Status = rd.GetStr("status"),
                    Tipo = 0,
                    Parc = rd.GetInt("parc"),
                    Fretetp = rd.GetStr("fretetp"),
                    TID = rd.GetStr("TID"),
                    TID_SHIP = rd.GetStr("TID_SHIP"),
                    NSU = rd.GetStr("NSU"),
                    NSU_SHIP = rd.GetStr("NSU_SHIP"),
                    AUTHCODE = rd.GetStr("AUTHCODE"),
                    AUTHCODE_SHIP = rd.GetStr("AUTHCODE_SHIP"),
                    REDESTATUS = rd.GetStr("REDESTATUS"),
                    REDESTATUS_SHIP = rd.GetStr("REDESTATUS_SHIP"),
                    First6 = rd.GetStr("First6"),
                    Last4 = rd.GetStr("Last4"),
                    NomeCC = rd.GetStr("nomecc")
                });
            }

            return model;
        }

        public async Task InsertOrderDetailDBAsync(int orderId, 
                                                    int idUsuario ,
                                                    int qtd, 
                                                    ProductModel productModel)
        {
            var model = new List<OrderSummaryModel>();
            String query = @"
                                INSERT INTO [tbProdutosCompra]
                                           ([idUsuario]
                                           ,[idProduto]
                                           ,[quantidade]
                                           ,[PKIdCompra]
                                           ,[preco]
                                           ,[atrelado]
                                           ,[nome]
                                           ,[id_subtipo]
                                           ,[peso]
                                           ,[estoque]
                                           ,[sys_creation_date]
                                           ,[minParcJuros])
                                     VALUES
                                           (@idUsuario,
                                            @idProduto,
                                            @quantidade,
                                            @PKIdCompra,
                                            @preco,
                                            @atrelado,
                                            @nome,
                                            @id_subtipo,
                                            @peso,
                                            @estoque,
                                            getDate(),
                                            @minParcJuros)
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = query;
            command.Parameters.AddWithValue("idUsuario", idUsuario);
            command.Parameters.AddWithValue("idProduto", productModel.PKId);
            command.Parameters.AddWithValue("quantidade", qtd);
            command.Parameters.AddWithValue("PKIdCompra", orderId);
            command.Parameters.AddWithValue("preco", productModel.PrecoFinal);
            command.Parameters.AddWithValue("atrelado", 0);
            command.Parameters.AddWithValue("nome", productModel.Nome);
            command.Parameters.AddWithValue("id_subtipo", productModel.SubTipo);
            command.Parameters.AddWithValue("peso", productModel.Peso);
            command.Parameters.AddWithValue("estoque", productModel.Estoque);
            command.Parameters.AddWithValue("minParcJuros", productModel.MinParcJuros);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteOrderDetailDBAsync(int odpkid)
        {
            var model = new List<OrderSummaryModel>();
            String query = @"
                                delete from tbprodutoscompra where pkid=@odpkid
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = query;
            command.Parameters.AddWithValue("odpkid", odpkid);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<(int PKId, int Quantity)?> GetOrderDetailByProductAsync(int orderId, int productId)
        {
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = @"
                                        SELECT TOP 1 pkid, quantidade 
                                        FROM tbProdutosCompra 
                                        WHERE PKIdCompra = @orderId AND idProduto = @productId
                                    ";

            command.Parameters.AddWithValue("orderId", orderId);
            command.Parameters.AddWithValue("productId", productId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            if (rd.Read())
            {
                return (rd.GetInt("pkid"), rd.GetInt("quantidade"));
            }

            return null;
        }

        public async Task UpdateOrderDetailQuantityAsync(int odpkid, int newQuantity)
        {
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = @"
                                        UPDATE tbProdutosCompra 
                                        SET quantidade = @quantity 
                                        WHERE pkid = @odpkid
                                    ";

            command.Parameters.AddWithValue("odpkid", odpkid);
            command.Parameters.AddWithValue("quantity", newQuantity);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateOrderParcValDBAsync(int oid, decimal parcVal)
        {
            String query = @"
                                update tbCompra set parcVal = @parcval where pkid=@pkid
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = query;
            command.Parameters.AddWithValue("pkid", oid);
            command.Parameters.AddWithValue("parcval", parcVal);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<OrderSummaryModel>> GetOrdersByStatusDBAsync(string status)
        {
            var model = new List<OrderSummaryModel>();
            String query = @"
                                SELECT DISTINCT top 100 c.PKId as idcompra, 
                                                    isnull(max(p.id_subtipo),0) as tipo,
                                                    c.dataMdSt,
                                                    c.lionOrderId as lid,
                                                    c.parc, 
                                                    c.data, 
                                                    d.nome as dnome,
                                                    d.sobrenome,
                                                    u.nome,
                                                    c.status,
                                                    c.fretetp,
                                                    c.TID,
                                                    c.AUTHCODE,
                                                    c.TID_SHIP,
                                                    c.AUTHCODE_SHIP,
                                                    c.NSU,
                                                    c.NSU_SHIP,
                                                    c.metodoPagto,
                                                    c.REDESTATUS,
                                                    c.REDESTATUS_SHIP,
                                                    c.REDESTATUSDESC,
                                                    c.REDESTATUSDESC_SHIP,
                                                    first6,
                                                    last4,
                                                    s.nome as nomecc
                                     FROM tbCompra c 
                                     left join tbProdutosCompra pc on pc.pkidcompra = c.pkid 
                                     left join tbProdutos p on p.pkid = pc.idproduto and p.id_subtipo in (8,19,20,21) 
                                     join tbDadosCompra d on c.PKIdUsuario = d.id_user 
                                     join tbUsuarios u on c.PKIdUsuario = u.id AND d.PKId = c.idDados 
                                     left join sysalloc s on s.PKId = c.idCC
                                     WHERE c.status = @status
                                     group by c.pkid,c.datamdst,d.nome,c.data,c.parc,d.sobrenome,u.nome,c.lionOrderId,c.status,c.fretetp,c.tid,c.TID_SHIP,
                                                c.AUTHCODE,c.AUTHCODE_SHIP,c.metodoPagto,c.REDESTATUS,c.REDESTATUS_SHIP,c.REDESTATUSDESC,c.REDESTATUSDESC_SHIP,c.NSU,c.NSU_SHIP,first6,last4,s.nome
                                     ORDER BY c.data
                            ";

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = query;
            command.Parameters.AddWithValue("status", status);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                    model.Add(new OrderSummaryModel()
                    {
                        PKId = rd.GetInt("idcompra"),
                        Data = rd.GetDate("data"),
                        DataMdSt = rd.GetDate("dataMdSt"),
                        CustomerName = rd.GetStr("nome"),
                        ReceiverName = rd.GetStr("dnome"),
                        ModoPagto = rd.GetStr("metodoPagto"),
                        ReceiverLastName = rd.GetStr("sobrenome"),
                        LionOrderId = rd.GetInt("lid"),
                        Status = rd.GetStr("status"),
                        Tipo = 0,
                        Parc = rd.GetInt("parc"),
                        Fretetp = rd.GetStr("fretetp"),
                        TID = rd.GetStr("TID"),
                        TID_SHIP = rd.GetStr("TID_SHIP"),
                        NSU = rd.GetStr("NSU"),
                        NSU_SHIP = rd.GetStr("NSU_SHIP"),
                        AUTHCODE = rd.GetStr("AUTHCODE"),
                        AUTHCODE_SHIP = rd.GetStr("AUTHCODE_SHIP"),
                        REDESTATUS = rd.GetStr("REDESTATUS"),
                        REDESTATUS_SHIP = rd.GetStr("REDESTATUS_SHIP"),
                        REDESTATUSDESC = rd.GetStr("REDESTATUSDESC"),
                        REDESTATUSDESC_SHIP = rd.GetStr("REDESTATUSDESC_SHIP"),
                        First6 = rd.GetStr("first6"),
                        Last4 = rd.GetStr("last4"),
                        NomeCC = rd.GetStr("nomecc")
                    });
            }
            return model;
        }

        public async Task<List<OrderModel>> GetCreditCardOrdersForProcessingDBAsync()
        {
            var ordersById = new Dictionary<int, OrderModel>();
            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();
            command.CommandText = @$"
                                    SELECT 
                                        {_orderFields},
                                        pc.PKId AS item_PKId,
                                        pc.PKIdCompra AS item_PKIdCompra,
                                        pc.idProduto AS item_idProduto,
                                        pc.quantidade AS item_quantidade,
                                        pc.preco AS item_preco,
                                        pc.nome AS item_nome,
                                        pc.id_subtipo AS item_id_subtipo,
                                        pc.peso AS item_peso,
                                        pc.estoque AS item_estoque,
                                        pc.sys_creation_date AS item_sys_creation_date,
                                        pc.minParcJuros AS item_minParcJuros,
                                        pc.sys_update_date AS item_sys_update_date
                                    FROM tbCompra c
                                    JOIN tbUsuarios u ON u.id = c.PKIdUsuario
                                    JOIN tbDadosCompra dc ON dc.PKId = c.idDados
                                    LEFT JOIN sysalloc s ON s.PKId = c.idCC
                                    JOIN tbProdutosCompra pc ON pc.PKIdCompra = c.PKId
                                    WHERE c.metodoPagto = 'C'
                                        AND c.status = 'G'
                                        AND LEN(s.aa) > 5
                                    ORDER BY c.data DESC, pc.PKId ASC";

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                int orderId = rd.GetInt("PKId");
                if (!ordersById.TryGetValue(orderId, out var orderModel))
                {
                    var summary = MapOrderSummaryModel(rd);

                    orderModel = new OrderModel
                    {
                        OrderSummary = summary,
                        Items = []
                    };

                    ordersById.Add(orderId, orderModel);
                }

                // Map item for the current row using the prefix aliases
                orderModel.Items.Add(MapOrderItemModel(rd, "item_"));
            }

            // Order by date desc to mirror SQL after grouping
            var ordered = ordersById.Values
                .OrderByDescending(o => o.OrderSummary.Data)
                .ToList();

            return ordered;
        }

        public async Task<OrderModel> GetOrderDBAsync(int orderId)
        {
            var model = new OrderModel
            {
                Items = await GetOrderItemsDBAsync(orderId)
            };

            var summaryModel = new OrderSummaryModel();

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = @$"
                                            SELECT {_orderFields}
                                            FROM tbCompra c
                                            JOIN tbUsuarios u ON u.id = c.PKIdUsuario
                                            JOIN tbDadosCompra dc ON dc.PKId = c.idDados
                                            LEFT JOIN sysalloc s ON s.PKId = c.idCC
                                            WHERE c.PKId = @orderId
                                        ";

            command.Parameters.AddWithValue("orderId", orderId);
            command.Transaction = _dbContext.GetTrx();

            using SLDataReader rd = new(await command.ExecuteReaderAsync());

            if (rd.Read())
            {
                model.OrderSummary = MapOrderSummaryModel(rd);
            }
            else
            {
                // Nenhum registro encontrado para o orderId; retorna modelo com Items preenchidos
                return model;
            }

            return model;
        }

        public async Task<List<OrderItemModel>> GetOrderItemsDBAsync(int orderId)
        {
            var model = new List<OrderItemModel>();
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.Transaction = _dbContext.GetTrx();

            command.CommandText = @$"
                                        SELECT {_orderItemsFields}
                                          FROM tbProdutosCompra
                                          WHERE PKIdCompra = @orderId
                                        ";

            command.Parameters.AddWithValue("orderId", orderId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Add(MapOrderItemModel(rd));
            }

            return model;
        }

        private static OrderSummaryModel MapOrderSummaryModel(SLDataReader rd)
        {
            // Maps fields selected via _orderFields
            return new OrderSummaryModel()
            {
                PKId = rd.GetInt("PKId"),
                PKIdUsuario = rd.GetInt("PKIdUsuario"),
                Status = rd.GetStr("status"),
                ModoPagto = rd.GetStr("metodoPagto"),
                Data = rd.GetDate("data"),
                DataMdSt = rd.GetDate("dataMdSt"),
                Parc = rd.GetInt("parc"),
                IdDados = rd.GetInt("idDados"),
                Frete = rd.GetDouble("frete"),
                Via = rd.GetStr("via"),
                Track = rd.GetStr("track"),
                Desconto = rd.GetDouble("desconto"),
                TaxaJuros = rd.GetDouble("taxaJuros"),
                MinParcJuros = rd.GetInt16("minParcJuros"),
                Credito = rd.GetDouble("credito"),
                Fretetp = rd.GetStr("fretetp"),
                LionOrderId = rd.GetInt("lionOrderId"),
                ParcVal = rd.GetDouble("parcVal"),
                IdAeroporto = rd.GetInt("idAeroporto"),
                Lcidade = rd.GetStr("lcidade"),
                Cidade = rd.GetStr("cidade"),
                Estado = rd.GetStr("estado"),
                CustomerName = rd.GetStr("nome"),
                CustomerDName = rd.GetStr("dnome"),
                Email = rd.GetStr("email"),
                TID = rd.GetStr("TID"),
                NSU = rd.GetStr("NSU"),
                NSU_SHIP = rd.GetStr("NSU_SHIP"),
                AUTHCODE = rd.GetStr("AUTHCODE"),
                TID_SHIP = rd.GetStr("TID_SHIP"),
                AUTHCODE_SHIP = rd.GetStr("AUTHCODE_SHIP"),
                REDESTATUS = rd.GetStr("REDESTATUS"),
                REDESTATUS_SHIP = rd.GetStr("REDESTATUS_SHIP"),
                REDESTATUSDESC = rd.GetStr("REDESTATUSDESC"),
                REDESTATUSDESC_SHIP = rd.GetStr("REDESTATUSDESC_SHIP"),
                First6 = rd.GetStr("First6"),
                Last4 = rd.GetStr("Last4"),
                NomeCC = rd.GetStr("nomecc")
            };
        }

        private static OrderItemModel MapOrderItemModel(SLDataReader rd, string itemPrefix = "")
        {
            // If a prefix was used to alias item columns, honor it; else use raw column names
            string P(string name) => string.IsNullOrEmpty(itemPrefix) ? name : itemPrefix + name;

            return new OrderItemModel()
            {
                PKId = rd.GetInt(P("PKId")),
                SiteId = rd.GetInt(P("idProduto")),
                SubTipoId = int.Parse(rd.GetStr(P("id_subtipo"))),
                CreatioDate = rd.GetDate(P("sys_creation_date")),
                ProductWeight = rd.GetInt(P("peso")),
                ProductName = rd.GetStr(P("nome")),
                Quantity = rd.GetInt(P("quantidade")),
                UnitPrice = rd.GetDecFromDouble(P("preco")),
                Weight = rd.GetInt(P("peso"))
            };
        }
    }
}
