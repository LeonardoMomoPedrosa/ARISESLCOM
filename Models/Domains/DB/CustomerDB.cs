using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.DB
{
    public class CustomerDB(IRedisCacheService redis) : DBDomain(redis)
    {
        public async Task<List<CustomerModel>> GetCustomerListByNameDBAsync(string name)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
	                        select top 100 u.id,
                                    u.nome,
                                    u.email,
                                    u.login,
                                    u.senha,
                                    u.confiavel,
                                    isnull(u.desconto,0) as desconto, 
                                    isnull(sum(amount),0) as credito 
                            from tbusuarios u 
                            left outer join tbcredeb cd on cd.iduser = u.id
	                        where u.nome like @nome 
	                        group by u.id,u.nome,u.endereco,u.cidade,u.estado,u.email,u.interesse,u.login,u.senha,
                                        u.pagina,u.comentario,u.desconto,u.confiavel
	                        order by u.id desc
                        ");

            command.Parameters.AddWithValue("nome", $"%{name}%");

            using SLDataReader rd = new(await command.ExecuteReaderAsync());

            return GetCustomerModelList(rd);
        }

        public async Task<List<CustomerModel>> GetCustomerByIdDBAsync(int id)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
	                        select  u.id,
                                    u.nome,
                                    u.email,
                                    u.login,
                                    u.senha,
                                    u.confiavel,
                                    isnull(u.desconto,0) as desconto, 
                                    isnull(sum(amount),0) as credito 
                            from tbusuarios u 
                            left outer join tbcredeb cd on cd.iduser = u.id
	                        where u.id = @id 
	                        group by u.id,u.nome,u.endereco,u.cidade,u.estado,u.email,u.interesse,u.login,u.senha,
                                        u.pagina,u.comentario,u.desconto,u.confiavel
	                        order by u.id desc
                        ");

            command.Parameters.AddWithValue("id", id);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());

            return GetCustomerModelList(rd);
        }

        public async Task<List<CustomerModel>> GetCustomerByEmailDBAsync(string email)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
	                        select  u.id,
                                    u.nome,
                                    u.email,
                                    u.login,
                                    u.senha,
                                    u.confiavel,
                                    isnull(u.desconto,0) as desconto, 
                                    isnull(sum(amount),0) as credito 
                            from tbusuarios u 
                            left outer join tbcredeb cd on cd.iduser = u.id
	                        where u.email like @email
	                        group by u.id,u.nome,u.endereco,u.cidade,u.estado,u.email,u.interesse,u.login,u.senha,
                                        u.pagina,u.comentario,u.desconto,u.confiavel
	                        order by u.id desc
                        ");

            command.Parameters.AddWithValue("email", $"%{email}%");

            using SLDataReader rd = new(await command.ExecuteReaderAsync());

            return GetCustomerModelList(rd);
        }

        private static List<CustomerModel> GetCustomerModelList(SLDataReader rd)
        {
            var modelList = new List<CustomerModel>();

            while (rd.Read())
            {
                modelList.Add(GetCustomerModel(rd));
            }

            return modelList;
        }

        private static CustomerModel GetCustomerModel(SLDataReader rd)
        {
            return new CustomerModel()
            {
                Id = rd.GetInt("id"),
                Name = rd.GetStr("nome"),
                Email = rd.GetStr("email"),
                Login = rd.GetStr("login"),
                Senha = rd.GetStr("senha"),
                Confiavel = rd.GetByte("confiavel"),
                Desconto = rd.GetDecFromDouble("desconto"),
                Credito = rd.GetDecFromDouble("credito")
            };
        }

        public async Task UpdateCustomerDBTrust(int customerId, bool trustInd)
        {
            var trust = trustInd ? 2 : 0;
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
                                                    UPDATE tbUsuarios SET confiavel = @trust WHERE id = @id
                                                ");

            command.Parameters.AddWithValue("id", customerId);
            command.Parameters.AddWithValue("trust", trust);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateCustomerDBDiscount(int customerId, decimal discount)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
                                                    UPDATE tbUsuarios SET desconto = @disc WHERE id = @id
                                                ");

            command.Parameters.AddWithValue("id", customerId);
            command.Parameters.AddWithValue("disc", discount);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<decimal> InsertCustomerDBCredit(int customerId, decimal creditAmount)
        {
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
                                                    INSERT INTO tbCreDeb (credeb,amount,date,iduser) VALUES (1,@amount,getDate(),@id)
                                                ");

            command.Parameters.AddWithValue("id", customerId);
            command.Parameters.AddWithValue("amount", creditAmount);

            await command.ExecuteNonQueryAsync();

            return await GetCustomerDBCredit(customerId);
        }

        public async Task<decimal> GetCustomerDBCredit(int customerId)
        {
            decimal amt = 0;
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
                                                    select sum(amount) as credit from tbCreDeb where iduser = @id
                                                ");

            command.Parameters.AddWithValue("id", customerId);
            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                amt = rd.GetDecFromDouble("credit");
            }
            return amt;
        }


    }
}
