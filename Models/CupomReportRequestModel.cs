using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models
{
    public class CupomReportRequestModel
    {
        [Required(ErrorMessage = "Mês necessário")]
        [DataType("month")]
        [BindProperty]
        public DateTime Month { get; set; }

        [Required(ErrorMessage = "Cupom necessário")]
        [BindProperty]
        public string Cupom { get; set; } = "AQUAFLOW";
    }
}

