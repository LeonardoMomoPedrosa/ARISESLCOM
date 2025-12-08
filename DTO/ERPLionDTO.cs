namespace ARISESLCOM.DTO
{
    /// <summary>
    /// DTO para informações do cliente retornadas pela API GetClientByOrder do ERP-Lion.
    /// A API retorna também Track e Via, mas esses campos não são utilizados neste DTO.
    /// </summary>
    public class ClientInfoDTO
    {
        public string ClientName { get; set; }
        public string Email { get; set; }
    }
}

