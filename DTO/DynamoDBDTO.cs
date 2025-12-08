using System.Text.Json;

namespace ARISESLCOM.DTO
{
    public class TrackerPedidoDTO
    {
        public string? IdPedido { get; set; }
        public string? CodRastreamento { get; set; }
        public string? Nome { get; set; }
        public string? TipoEnvio { get; set; }
        public string? RastreamentoJson { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TrackerPedidoViewModel
    {
        public string NumPedido { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public string Rastreamento { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DataAtualizacao { get; set; }
        public string TipoEnvio { get; set; } = string.Empty;
        public string Via { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
    }

    public class CorreiosRastreamentoEvento
    {
        public string? Descricao { get; set; }
        public string? Detalhe { get; set; }
        public DateTime? DtHrCriado { get; set; }
    }

    public class BuslogRastreamentoEvento
    {
        public string? Status { get; set; }
        public string? Descricao { get; set; }
        public DateTime? Data { get; set; }
    }
}

