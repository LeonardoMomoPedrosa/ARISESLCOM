using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ARISESLCOM.Models.Entities
{
    public class UserModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Digite o usu�rio")]
        [BindProperty]
        public String Name { get; set; }

        public List<String> Roles { get; set; }

        [Required(ErrorMessage = "Digite a senha")]
        [BindProperty]
        public string Password { get; set; }
    }
}
