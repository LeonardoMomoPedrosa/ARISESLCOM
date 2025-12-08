using ARISESLCOM.Data;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using Microsoft.Data.SqlClient;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class CustomerDomainModel(IRedisCacheService redis) : CustomerDB(redis), ICustomerDomainModel
    {
        public async Task<CustomerModel> GetCustomerModelAsync(int customerId)
        {
            return await GetCustomerModelAsync(customerId, 0);
        }
        public async Task<CustomerModel> GetCustomerModelAsync(int customerId, int addressId)
        {
            CustomerModel model = new()
            {
                CustomerAddressModelList = await GetCustomerAddressModelListAsync(customerId, addressId)
            };

            using SqlCommand command = new("", _dbContext.GetSqlConnection());
            command.CommandText = @"
                                            SELECT u.ip,
                                                    u.email,
                                                    u.id,
                                                    u.ultimoAcesso,
                                                    u.ml_nickname
                                            FROM tbUsuarios u
                                            WHERE u.id = @customerId
                                        ";

            command.Parameters.AddWithValue("customerId", customerId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                model.Ip = rd.GetStr("ip");
                model.Email = rd.GetStr("email");
                model.Id = rd.GetInt("id");
                model.UltimoAcesso = rd.GetDate("ultimoAcesso");
                model.MlNickname = rd.GetStr("ml_nickname");
            }

            return model;
        }

        public async Task<List<CustomerProfileModel>> GetCustomerProfileAsync(int customerId, int orderId)
        {
            var modelList = new List<CustomerProfileModel>();
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = @"
                                        select status,
                                                count(status) as cont 
                                        from tbcompra 
                                        where pkidusuario = @customerId
                                        and pkid <> @orderId
                                        group by status
                                    ";

            command.Parameters.AddWithValue("customerId", customerId);
            command.Parameters.AddWithValue("orderId", orderId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                modelList.Add(new CustomerProfileModel()
                {
                    OrderStatus = rd.GetStr("status"),
                    Count = rd.GetInt("cont")
                });
            }
            return modelList;
        }

        public async Task<FraudModel> GetFraudModelAsync(int orderId)
        {
            int idUsuario = 0;
            var senha = "";
            var cep = "";
            byte? confiavel = null;

            var model = new FraudModel();
            model.OrderId = orderId;
            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = @"
                                        select  c.pkidusuario,
                                                dc.cep,
                                                u.senha,
                                                u.confiavel 
                                        from tbdadoscompra dc 
                                        join tbcompra c on c.iddados = dc.pkid 
                                        join tbusuarios u on u.id = c.pkidusuario 
                                        where c.pkid=@orderId
                                    ";

            command.Parameters.AddWithValue("orderId", orderId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                idUsuario = rd.GetInt("pkidusuario");
                senha = rd.GetStr("senha");
                cep = rd.GetStr("cep").Replace("-", "");
                confiavel = rd.GetNullableByte("confiavel");
                model.CustomerId = idUsuario;
                model.Confiavel = confiavel;
            }
            rd.Dispose();

            using SqlCommand commandIP = new("", connection: _dbContext.GetSqlConnection());
            commandIP.CommandText = @"
                                        select id,
                                                nome,
                                                email,
                                                ultimoAcesso,
                                                ip 
                                        from tbusuarios 
                                        where ip in (select ip from tbusuarios where id = @idUser)
                                        and id != @idUser
                                    ";
            commandIP.Parameters.AddWithValue("idUser", idUsuario);
            using SLDataReader rdIP = new(await commandIP.ExecuteReaderAsync());
            while (rdIP.Read())
            {
                model.FraudIPList.Add(new()
                {
                    IdUsuario = rdIP.GetInt("id"),
                    email = rdIP.GetStr("email"),
                    IP = rdIP.GetStr("ip"),
                    Nome = rdIP.GetStr("nome"),
                    UltimoAcesso = rdIP.GetDate("ultimoAcesso")
                });
            }
            rdIP.Dispose();

            using SqlCommand commandPWD = new("", connection: _dbContext.GetSqlConnection());
            commandPWD.CommandText = @"
                                        select id,
                                                nome,
                                                email,
                                                ultimoAcesso,
                                                ip 
                                        from tbusuarios 
                                        where senha=@senha 
                                        and id != @id 
                                        order by ultimoAcesso desc
                                    ";
            commandPWD.Parameters.AddWithValue("id", idUsuario);
            commandPWD.Parameters.AddWithValue("senha", senha);

            using SLDataReader rdPWD = new(await commandPWD.ExecuteReaderAsync());

            while (rdPWD.Read())
            {
                model.FraudPWDList.Add(new()
                {
                    IdUsuario = rdPWD.GetInt("id"),
                    email = rdPWD.GetStr("email"),
                    IP = rdPWD.GetStr("ip"),
                    Nome = rdPWD.GetStr("nome"),
                    UltimoAcesso = rdPWD.GetDate("ultimoAcesso"),
                });
            }
            rdPWD.Dispose();

            using SqlCommand commandCEP = new("", connection: _dbContext.GetSqlConnection());
            commandCEP.CommandText = @"
                                        select u.id as idu,
                                                c.pkid as pedido,
                                                c.data as dc,
                                                u.nome,
                                                u.email,
                                                u.ultimoacesso,
                                                u.ip,
                                                dc.cep,
                                                dc.ruav 
                                        from tbdadoscompra dc 
                                        join tbcompra c on dc.pkid = c.iddados 
                                        join tbusuarios u on u.id = dc.id_user 
                                        and replace(dc.cep,'-','') = @cep
                                        and u.id != @idUser
                                        and c.data > getdate() - 90 
                                        order by c.data desc
                                    ";
            commandCEP.Parameters.AddWithValue("idUser", idUsuario);
            commandCEP.Parameters.AddWithValue("cep", cep);

            using SLDataReader rdCEP = new(await commandCEP.ExecuteReaderAsync());
            while (rdCEP.Read())
            {
                model.FraudCEPList.Add(new()
                {
                    IdUsuario = rdCEP.GetInt("idu"),
                    email = rdCEP.GetStr("email"),
                    IP = rdCEP.GetStr("ip"),
                    Nome = rdCEP.GetStr("nome"),
                    UltimoAcesso = rdCEP.GetDate("ultimoAcesso"),
                    CEP = rdCEP.GetStr("cep"),
                    OrderDate = rdCEP.GetDate("dc"),
                    OrderId = rdCEP.GetInt("pedido"),
                    Ruav = rdCEP.GetStr("ruav")
                });
            }
            return model;
        }

        public async Task<List<CustomerAddressModel>> GetCustomerAddressModelListAsync(int customerId, int addressId)
        {
            List<CustomerAddressModel> modelList = [];

            using SqlCommand command = new("", connection: _dbContext.GetSqlConnection());
            command.CommandText = string.Format(@"
                                        SELECT dc.id_user,
                                                dc.ruav,
                                                dc.cidade,
                                                dc.estado,
                                                dc.cep,
                                                dc.telefone,
                                                dc.cpf,
                                                dc.PKId,
                                                dc.nome,
                                                dc.sobrenome,
                                                dc.numero,
                                                dc.complemento,
                                                dc.bairro,
                                                dc.lcidade,
                                                dc.telcom,
                                                dc.telcel,
                                                dc.codmun,
                                                dc.new_format,
                                                dc.lion_id,
                                                dc.ativo,
                                                dc.ml_email
                                          FROM tbDadosCompra dc
                                          WHERE dc.id_user = @customerId {0} 
                                          ORDER by dc.PKId desc
                                        ", addressId > 0 ? GetAndEqSQL("dc.", "PKId") : ""
                                        );

            command.Parameters.AddWithValue("customerId", customerId);
            command.Parameters.AddWithValue("PKId", addressId);

            using SLDataReader rd = new(await command.ExecuteReaderAsync());
            while (rd.Read())
            {
                modelList.Add(new CustomerAddressModel()
                {
                    Ruav = rd.GetStr("ruav"),
                    Cidade = rd.GetStr("cidade"),
                    Estado = rd.GetStr("estado"),
                    CEP = rd.GetStr("cep"),
                    Telefone = rd.GetStr("telcel"),
                    CPF = rd.GetStr("cpf"),
                    PKId = rd.GetInt("PKId"),
                    Nome = rd.GetStr("nome"),
                    Sobrenome = rd.GetStr("sobrenome"),
                    Numero = rd.GetStr("numero"),
                    Complemento = rd.GetStr("complemento"),
                    Bairro = rd.GetStr("bairro"),
                    Lcidade = rd.GetStr("lcidade"),
                    Celular = rd.GetStr("telcel"),
                    CodMun = rd.GetStr("codmun"),
                    LionId = rd.GetInt("lion_id"),
                    ML_Email = rd.GetStr("ml_email")
                });
            }

            return modelList;
        }

        public async Task<List<CustomerModel>> GetCustomerListAsync(CustomerSearchViewModel model)
        {
            List<CustomerModel> outModel = [];
            if (!string.IsNullOrEmpty(model.Name))
            {
                outModel = await GetCustomerListByNameDBAsync(model.Name);
            }
            else if (!string.IsNullOrEmpty(model.Email))
            {
                outModel = await GetCustomerByEmailDBAsync(model.Email);
            }
            else if (model.CustomerId > 0)
            {
                outModel = await GetCustomerByIdDBAsync(model.CustomerId ?? 0);
            }

            return outModel;
        }


    }
}
