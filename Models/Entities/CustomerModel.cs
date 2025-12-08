using ARISESLCOM.Models.Domains.DB;

namespace ARISESLCOM.Models.Entities
{
    public class CustomerModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime UltimoAcesso { get; set; }
        public string Ip { get; set; }
        public string MlNickname { get; set; }
        public string Login { get; set; }
        public string Senha { get; set; }
        public byte Confiavel { get; set; }
        public decimal Desconto { get; set; }
        public decimal Credito { get; set; }
        public List<CustomerAddressModel> CustomerAddressModelList { get; set; }
    }
}