using Microsoft.Data.SqlClient;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;
using System.Data;

namespace ARISESLCOM.Models.Domains.DB
{
    public class AcrDB (IRedisCacheService redis) : DBDomain(redis)
    {
       private readonly IRedisCacheService _cache = redis;  

        public async Task<DataTable> GetOrderInfoDBAsync(int orderId)
        {
            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    SELECT s.aa,
                           s.val,
                           s.nome,
                           c.PKId,
                           c.PKIdUsuario,
                           c.parc,
                           ISNULL(c.parcVal, 0) AS parcVal,
                           c.frete,
                           ISNULL(c.REDESTATUS, '') AS REDESTATUS,
                           c.REDESTATUSDESC,
                           c.TID,
                           ISNULL(c.STEP, 1) AS STEP,
                           c.AUTHCODE,
                           ISNULL(c.REDESTATUS_SHIP, '') AS REDESTATUS_SHIP,
                           c.REDESTATUSDESC_SHIP,
                           c.TID_SHIP,
                           ISNULL(c.STEP_SHIP, 1) AS STEP_SHIP,
                           c.AUTHCODE_SHIP
                    FROM sysalloc s
                    JOIN tbCompra c ON s.PKId = c.idCC
                    WHERE c.PKId = @orderId";

                command.Parameters.AddWithValue("orderId", orderId);
                command.Transaction = _dbContext.GetTrx();

                var dataTable = new DataTable();
                using var adapter = new SqlDataAdapter(command);
                await Task.Run(() => adapter.Fill(dataTable));

                return dataTable;
            }
            catch
            {
                return new DataTable();
            }
        }

        public async Task<List<AcrOrderViewModel>> GetAcrCardsDBAsync()
        {
            var result = new List<AcrOrderViewModel>();

            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    SELECT c.PKId,
                           RTRIM(LTRIM(ISNULL(e.nome, '') + ' ' + ISNULL(e.sobrenome, ''))) AS nome,
                           e.cidade,
                           e.estado,
                           c.data,
                           c.parc,
                           ISNULL(c.frete, 0) AS frete,
                           (SELECT SUM(p.quantidade * p.preco) FROM tbCompraItem p WHERE p.PKIdCompra = c.PKId) AS amt,
                           ISNULL(c.REDESTATUS, '') AS REDESTATUS,
                           c.REDESTATUSDESC,
                           c.TID,
                           ISNULL(c.STEP, 1) AS STEP,
                           c.AUTHCODE,
                           ISNULL(c.REDESTATUS_SHIP, '') AS REDESTATUS_SHIP,
                           c.REDESTATUSDESC_SHIP,
                           c.TID_SHIP,
                           ISNULL(c.STEP_SHIP, 1) AS STEP_SHIP,
                           c.AUTHCODE_SHIP
                    FROM tbCompra c
                    JOIN tbUsuarios u ON u.id = c.PKIdUsuario
                    JOIN tbDadosCompra e ON e.PKId = c.IdDados
                    LEFT JOIN sysalloc s ON s.PKId = c.idCC
                    WHERE c.metodoPagto = 'C'
                    AND c.status = 'G'
                    AND LEN(s.aa) > 5
                    ORDER BY c.data DESC";

                command.Transaction = _dbContext.GetTrx();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var vm = new AcrOrderViewModel
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("PKId")),
                        CustomerName = reader["nome"]?.ToString() ?? string.Empty,
                        City = reader["cidade"]?.ToString() ?? string.Empty,
                        State = reader["estado"]?.ToString() ?? string.Empty,
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("data")),
                        Installments = reader.GetInt32(reader.GetOrdinal("parc")),
                        ShippingAmount = reader["frete"] != DBNull.Value ? Convert.ToDecimal(reader["frete"]) : 0,
                        Amount = reader["amt"] != DBNull.Value ? Convert.ToDecimal(reader["amt"]) : 0,

                        PaymentStatus = reader["REDESTATUS"]?.ToString() ?? string.Empty,
                        PaymentStatusDescription = reader["REDESTATUSDESC"]?.ToString() ?? string.Empty,
                        PaymentTid = reader["TID"]?.ToString() ?? string.Empty,
                        PaymentAuthCode = reader["AUTHCODE"]?.ToString() ?? string.Empty,
                        PaymentStep = reader["STEP"] != DBNull.Value ? Convert.ToInt32(reader["STEP"]) : 1,

                        ShippingStatus = reader["REDESTATUS_SHIP"]?.ToString() ?? string.Empty,
                        ShippingStatusDescription = reader["REDESTATUSDESC_SHIP"]?.ToString() ?? string.Empty,
                        ShippingTid = reader["TID_SHIP"]?.ToString() ?? string.Empty,
                        ShippingAuthCode = reader["AUTHCODE_SHIP"]?.ToString() ?? string.Empty,
                        ShippingStep = reader["STEP_SHIP"] != DBNull.Value ? Convert.ToInt32(reader["STEP_SHIP"]) : 1
                    };

                    result.Add(vm);
                }
            }
            catch
            {
                // return empty list on failure
            }

            return result;
        }


        public async Task SavePaymentTransactionDBAsync(int orderId, string status, string description, string tid, string authCode, string nsu, int step)
        {
            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    UPDATE tbCompra 
                    SET REDESTATUS = @status,
                        REDESTATUSDESC = @description,
                        TID = @tid,
                        AUTHCODE = @authCode,
                        NSU = @nsu,
                        STEP = @step,
                        DataMdSt = GETDATE()
                    WHERE PKId = @orderId";

                command.Parameters.AddWithValue("orderId", orderId);
                command.Parameters.AddWithValue("status", status);
                command.Parameters.AddWithValue("description", description ?? "");
                command.Parameters.AddWithValue("tid", tid ?? "");
                command.Parameters.AddWithValue("authCode", authCode ?? "");
                command.Parameters.AddWithValue("nsu", nsu ?? "");
                command.Parameters.AddWithValue("step", step);
                command.Transaction = _dbContext.GetTrx();

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar transação de pagamento: {ex.Message}", ex);
            }
        }

        public async Task SavePaymentTransactionErrorDBAsync(int orderId, string status, string description, int step)
        {
            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    UPDATE tbCompra 
                    SET REDESTATUS = @status,
                        REDESTATUSDESC = @description,
                        STEP = @step,
                        DataMdSt = GETDATE()
                    WHERE PKId = @orderId";

                command.Parameters.AddWithValue("orderId", orderId);
                command.Parameters.AddWithValue("status", status);
                command.Parameters.AddWithValue("description", description ?? "");
                command.Parameters.AddWithValue("step", step);
                command.Transaction = _dbContext.GetTrx();

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar erro de transação de pagamento: {ex.Message}", ex);
            }
        }

        public async Task SaveShippingTransactionDBAsync(int orderId, string status, string description, string tid, string authCode, string nsu, int step)
        {
            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    UPDATE tbCompra 
                    SET REDESTATUS_SHIP = @status,
                        REDESTATUSDESC_SHIP = @description,
                        TID_SHIP = @tid,
                        AUTHCODE_SHIP = @authCode,
                        NSU_SHIP = @nsu,
                        STEP_SHIP = @step,
                        DataMdSt = GETDATE()
                    WHERE PKId = @orderId";

                command.Parameters.AddWithValue("orderId", orderId);
                command.Parameters.AddWithValue("status", status);
                command.Parameters.AddWithValue("description", description ?? "");
                command.Parameters.AddWithValue("tid", tid ?? "");
                command.Parameters.AddWithValue("authCode", authCode ?? "");
                command.Parameters.AddWithValue("nsu", nsu ?? "");
                command.Parameters.AddWithValue("step", step);
                command.Transaction = _dbContext.GetTrx();

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar transação de frete: {ex.Message}", ex);
            }
        }

        public async Task SaveShippingTransactionErrorDBAsync(int orderId, string status, string description, int step)
        {
            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    UPDATE tbCompra 
                    SET REDESTATUS_SHIP = @status,
                        REDESTATUSDESC_SHIP = @description,
                        STEP_SHIP = @step,
                        DataMdSt = GETDATE()
                    WHERE PKId = @orderId";

                command.Parameters.AddWithValue("orderId", orderId);
                command.Parameters.AddWithValue("status", status);
                command.Parameters.AddWithValue("description", description ?? "");
                command.Parameters.AddWithValue("step", step);
                command.Transaction = _dbContext.GetTrx();

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar erro de transação de frete: {ex.Message}", ex);
            }
        }

        public async Task<string?> GetErrorDescriptionDBAsync(string errorCode)
        {
            var cacheKey = RedisCacheService.GetRedeErrorsKey(errorCode);
            var model = await _redis.GetCacheValueAsync<string>(cacheKey);

            if (!string.IsNullOrEmpty(model))
            {
                return model;
            }

            try
            {
                using var command = new SqlCommand("", connection: _dbContext.GetSqlConnection());
                command.CommandText = @"
                    SELECT Descricao 
                    FROM tbRedeErros 
                    WHERE Codigo = @errorCode";

                command.Parameters.AddWithValue("errorCode", errorCode ?? "");
                command.Transaction = _dbContext.GetTrx();

                var result = await command.ExecuteScalarAsync();
                var description = result?.ToString();

                if (!string.IsNullOrEmpty(description))
                {
                    await SetCacheAsync(cacheKey, description, int.MaxValue);
                }

                return description;
            }
            catch
            {
                return null;
            }
        }

        public SqlConnection GetSqlConnection()
        {
            return _dbContext.GetSqlConnection();
        }

        public SqlTransaction? GetTrx()
        {
            return _dbContext.GetTrx();
        }
    }
}
