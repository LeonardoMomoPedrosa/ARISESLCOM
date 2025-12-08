using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Mappers.interfaces;

namespace ARISESLCOM.Models.Mappers
{
    public class CustomerViewMapper : ICustomerViewMapper
    {
        public CustomerAddressViewModel MapCustomerAddressViewModel(CustomerAddressModel model)
        {
            CustomerAddressViewModel viewModel = new()
            {
                Bairro = model.Bairro,
                Celular = model.Celular,
                CEP = model.CEP,
                Cidade = model.Cidade,
                CodMun = model.CodMun,
                Complemento = model.Complemento,
                CPF = model.CPF,
                Estado = model.Estado,
                Lcidade = model.Lcidade,
                Nome = model.Nome,
                Numero = model.Numero,
                PKId = model.PKId,
                Ruav = model.Ruav,
                Sobrenome = model.Sobrenome,
                Telefone = model.Telefone
            };
            
            return viewModel;
        }
    }
}
