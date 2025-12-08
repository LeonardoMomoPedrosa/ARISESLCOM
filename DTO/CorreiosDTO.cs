using System.Diagnostics.Contracts;

namespace ARISESLCOM.DTO
{
    /* DTO */
    public class CorreiosDTO
    {
        public CorreiosPrecoDTO CorreiosPrecoDTO{ get; set; }
        public CorreiosPrazoDTO CorreiosPrazoDTO { get; set; }
    }

    public class CorreiosPrecoDTO
    {
        public string coProduto { get; set; }
        public string pcBase { get; set; }
        public string pcBaseGeral { get; set; }
        public string peVariacao { get; set; }
        public string pcReferencia { get; set; }
        public string vlBaseCalculoImposto { get; set; }
        public string inPesoCubico { get; set; }
        public string psCobrado { get; set; }
        public string peAdValorem { get; set; }
        public string vlSeguroAutomatico { get; set; }
        public string qtAdicional { get; set; }
        public string pcFaixa { get; set; }
        public string pcFaixaVariacao { get; set; }
        public string pcProduto { get; set; }
        public string pcFinal { get; set; }
    }

    public class CorreiosPrazoDTO
    {
        public string coProduto { get; set; }
        public int prazoEntrega { get; set; }
        public DateTime dataMaxima { get; set; }
        public string entregaDomiciliar { get; set; }
        public string entregaSabado { get; set; }
    }

    public class AuthDTO
    {
        public string ambiente { get; set; }
        public string id { get; set; }
        public string ip { get; set; }
        public string perfil { get; set; }
        public string cnpj { get; set; }
        public int pjInternacional { get; set; }
        public string cpf { get; set; }
        public string cie { get; set; }
        public CartaoPostagemDTO cartaoPostagem { get; set; }
        public ContratoDTO contrato { get; set; }
        public List<int> api { get; set; }
        public List<string> paths { get; set; }
        public DateTime emissao { get; set; }
        public DateTime expiraEm { get; set; }
        public string zoneOffset { get; set; }
        public string token { get; set; }
    }

    public class CartaoPostagemDTO
    {
        public string numero { get; set; }
        public string contrato { get; set; }
        public int dr { get; set; }
        public List<int> api { get; set; }
    }

    public class ContratoDTO
    {
        public string numero { get; set; }
        public int dr { get; set; }
        public List<int> api { get; set; }
    }

    public class CorreiosRastreamentoDTO
    {
        public string versao { get; set; }
        public int quantidade { get; set; }
        public List<ObjetoRastreamentoDTO> objetos { get; set; }
        public string tipoResultado { get; set; }
    }

    public class ObjetoRastreamentoDTO
    {
        public string codObjeto { get; set; }
        public TipoPostalDTO tipoPostal { get; set; }
        public DateTime? dtPrevista { get; set; }
        public string contrato { get; set; }
        public int? largura { get; set; }
        public int? comprimento { get; set; }
        public int? altura { get; set; }
        public double? peso { get; set; }
        public string formato { get; set; }
        public string modalidade { get; set; }
        public List<EventoDTO> eventos { get; set; }
    }

    public class TipoPostalDTO
    {
        public string sigla { get; set; }
        public string descricao { get; set; }
        public string categoria { get; set; }
    }

    public class EventoDTO
    {
        public string codigo { get; set; }
        public string tipo { get; set; }
        public DateTime dtHrCriado { get; set; }
        public string descricao { get; set; }
        public string detalhe { get; set; }
        public UnidadeDTO unidade { get; set; }
        public UnidadeDTO unidadeDestino { get; set; }
    }

    public class UnidadeDTO
    {
        public string codSro { get; set; }
        public string tipo { get; set; }
        public EnderecoDTO endereco { get; set; }
    }

    public class EnderecoDTO
    {
        public string cep { get; set; }
        public string logradouro { get; set; }
        public string numero { get; set; }
        public string bairro { get; set; }
        public string cidade { get; set; }
        public string uf { get; set; }
    }
}
