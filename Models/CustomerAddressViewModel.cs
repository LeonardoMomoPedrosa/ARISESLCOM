namespace ARISESLCOM.Models
{
    public class CustomerAddressViewModel
    {
        public int PKId { get; set; }
        public string Nome { get; set; }
        public string Sobrenome { get; set; }

        public string GetFullName
        {
            get => Nome + " " + Sobrenome;
            set;
        }
        public string Ruav { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Lcidade { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string CEP { get; set; }
        public string Telefone { get; set; }
        public string Celular { get; set; }
        public string CodMun { get; set; }
        public string CPF { get; set; }
    }
}
