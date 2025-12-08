namespace ARISESLCOM.Helpers
{
    public static class TrackingHelper
    {
        public static string GetViaDescription(string via)
        {
            return via switch
            {
                "V" => "JAD",
                "G" => "GOL",
                "B" => "BUS",
                "C" => "CORREIOS",
                _ => via
            };
        }

        public static string GetViaStyle(string via)
        {
            return via switch
            {
                "V" => "background-color: #fd7e14; color: #fff;", // JAD - laranja
                "G" => "background-color: #dc3545; color: #fff;", // GOL - vermelho
                "B" => "background-color: #0d6efd; color: #fff;", // BUS - azul
                "C" => "background-color: #ffc107; color: #212529;", // CORREIOS - amarelo
                _ => "background-color: #6c757d; color: #fff;"
            };
        }

        public static string GetViaIconColor(string via)
        {
            return via switch
            {
                "V" => "#fd7e14",
                "G" => "#dc3545",
                "B" => "#0d6efd",
                "C" => "#ffc107",
                _ => "#6c757d"
            };
        }

        public static string GetSourceDescription(string source)
        {
            return source switch
            {
                "E" => "E-Commerce",
                "L" => "ERP (Lion)",
                "E-commerce" => "E-Commerce",
                "ERP-Lion" => "ERP-Lion",
                _ => source ?? "E-Commerce"
            };
        }

        public static string GetSourceBadgeStyle(string source)
        {
            return source switch
            {
                "E" => "background-color: #cfe2ff; color: #084298; border: 1px solid #b6d4fe;",
                "L" => "background-color: #d1e7dd; color: #0f5132; border: 1px solid #badbcc;",
                "E-commerce" => "background-color: #cfe2ff; color: #084298; border: 1px solid #b6d4fe;",
                "ERP-Lion" => "background-color: #d1e7dd; color: #0f5132; border: 1px solid #badbcc;",
                _ => "background-color: #e2e3e5; color: #383d41; border: 1px solid #d6d8db;"
            };
        }
    }
}

