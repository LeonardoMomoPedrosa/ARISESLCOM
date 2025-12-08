using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Entities;
using System.Data.Common;
using System.Transactions;

namespace ARISESLCOM.Data
{
    public class ApplicationDbContext : IDBContext
    {
        private readonly SqlConnection _sqlConnection;
        private readonly IConfiguration _configuration;
        private SqlTransaction? _dbTransaction;

        public ApplicationDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _sqlConnection = new(_configuration.GetConnectionString("ARISESLCOM"));
        }

        public SqlConnection GetSqlConnection()
        {
            return _sqlConnection;
        }

        public async Task OpenAsync()
        {
            await _sqlConnection.OpenAsync();
        }

        public SqlTransaction GetTrx()
        {
            return _dbTransaction;
        }

        public async Task CloseAsync()
        {
            await _sqlConnection.CloseAsync();
        }

        public void StartTrxAsync()
        {
            _dbTransaction = _sqlConnection.BeginTransaction();
        }

        public async Task CommitTrxAsync()
        {
            if (_dbTransaction != null)
            {
                await _dbTransaction.CommitAsync();
            }
        }

        public async Task CheckTrxAsync(ActionResultModel model)
        {
            if (_dbTransaction != null && model.IsSuccess)
            {
                await _dbTransaction.CommitAsync();
            }
            else if (_dbTransaction != null && !model.IsSuccess)
            {
                await _dbTransaction.RollbackAsync();
            }
        }

        public async Task RollbackTrxAsync()
        {
            if (_dbTransaction != null)
            {
                await _dbTransaction.RollbackAsync();
            }
        }


    }
}
