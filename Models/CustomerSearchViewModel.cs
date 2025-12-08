using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models
{
    public class CustomerSearchViewModel
    {
        [BindProperty]
        public string? Name { get; set; }

        [EmailAddress]
        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public int? CustomerId { get; set; }
    }
}
