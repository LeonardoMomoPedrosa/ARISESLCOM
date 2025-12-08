using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models
{
    public class DateModel
    {
        [Required(ErrorMessage = "Data necess�ria")]
        [DataType(DataType.Date)]
        [BindProperty, DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Data1 { get; set; }

        [Required(ErrorMessage = "Data necess�ria")]
        [DataType(DataType.Date)]
        [BindProperty, DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Data2 { get; set; }

        [Required(ErrorMessage = "M�s necess�rio")]
        [DataType("month")]
        [BindProperty]
        public DateTime Month { get; set; }
    }
}
