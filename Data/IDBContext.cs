using Microsoft.Data.SqlClient;
using ARISESLCOM.Models;
using System.Data.Common;

namespace ARISESLCOM.Data
{
    public interface IDBContext
    {
        public SqlConnection GetSqlConnection();
        public Task OpenAsync();
        public Task CloseAsync();
        public void StartTrxAsync();
        Task CommitTrxAsync();
        Task CheckTrxAsync(ActionResultModel model);
        Task RollbackTrxAsync();
        public SqlTransaction GetTrx();
    }
}
