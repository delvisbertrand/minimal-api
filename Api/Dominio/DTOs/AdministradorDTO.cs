using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinimalApi.Dominio.Enums;

namespace MinimalApi.Dominio.DTOs
{
    public class AdministradorDTO
    {
        public String Email { get; set; } = default;
        public String Senha { get; set; } = default;
        public Perfil Perfil { get; set; } = default;
    }
}