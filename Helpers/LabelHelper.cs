using SLCOMLIB.Helpers;
namespace ARISESLCOM.Helpers
{
    public static class PageLabelHelper
    {
        public static readonly Dictionary<string, string> HeaderNewOrderDict = new()
        {
            { LibConsts.ORDER_STATUS_NOVO,"Pedidos Novos" },
            { LibConsts.ORDER_STATUS_AG_COMPROV,"Aguardando comprovante (via dep�sito)" },
            { LibConsts.ORDER_STATUS_CCREDITO,"Pedidos com cart�o de cr�dito" },
            { LibConsts.ORDER_STATUS_BOLETO,"Aguardando confirma��o de boleto" },
            { LibConsts.ORDER_STATUS_PAGTO_CONF_PREP,"Pagamento confirmado, em prepara��o" },
            { LibConsts.ORDER_STATUS_ENVIADO,"Pedido(s) enviado(s)" },
            { LibConsts.ORDER_STATUS_CANC,"Pedido(s) cancelado(s)" },
            { LibConsts.ORDER_STATUS_N_AUTORIZ,"Pedido(s) n�o autorizado(s)" },
            { LibConsts.ORDER_STATUS_AG_ESTOQUE,"Pedido(s) esperando estoque" },
            { LibConsts.ORDER_STATUS_SEM_PREV,"Pedido(s) em espera sem previs�o" },
            { LibConsts.ORDER_STATUS_QUARENTENA,"Pedido(s) em quarentena" },
            { LibConsts.ORDER_STATUS_RETIRADA,"Pedido pronto para retirada" }
        };

        public static string ShowDtLong(DateTime dt)
        {
            var retVal = "";
            if (dt > DateTime.MinValue)
            {
                retVal = dt.ToString("yyyy/MM/dd hh:mm");
            }
            return retVal;
        }

        public static string ShowDtShort(DateTime dt)
        {
            var retVal = "";
            if (dt > DateTime.MinValue)
            {
                retVal = dt.ToString("yyyy/MM/dd");
            }
            return retVal;
        }

        public static string FormatProductName (string input)
        {
            return input.Replace("<BR>", "<br>").Split("<br>")[0];
        }
    }
}

