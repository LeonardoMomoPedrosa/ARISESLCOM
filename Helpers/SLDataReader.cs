using Microsoft.Data.SqlClient;

namespace ARISESLCOM.Helpers
{
    public class SLDataReader(SqlDataReader reader) : IDisposable
    {
        private readonly SqlDataReader _reader = reader;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _reader.Dispose();
        }

        public DateTime GetDate(string field)
        {
            return _reader[field] == DBNull.Value ? DateTime.MinValue : (DateTime)_reader[field];
        }

        public int GetInt(string field)
        {
            return _reader[field] == DBNull.Value? -1 : (int)_reader[field];
        }

        public byte GetByte(string field)
        {
            return _reader[field] == DBNull.Value ? (byte)0 : (byte)_reader[field];
        }

        public byte? GetNullableByte(string field)
        {
            return _reader[field] == DBNull.Value ? null : (byte?)_reader[field];
        }

        public int GetInt16(string field)
        {
            return _reader[field] == DBNull.Value ? -1 : (Int16)_reader[field];
        }

        public string GetStr(string field)
        {
            return _reader[field] == DBNull.Value ? "" : (string)_reader[field];
        }

        public bool GetBool(string field)
        {
            return _reader[field] != DBNull.Value && (bool)_reader[field];
        }

        public double GetDouble(string field)
        {
            return (double)_reader[field];
        }

        public decimal GetDecimal(string field)
        {
            return (decimal)_reader[field];
        }

        public decimal GetDecFromDouble(string field)
        {
            var d = (double) _reader[field];
            return (decimal)d;
        }

        public bool Read()
        {
            return _reader.Read();
        }


    }
}
