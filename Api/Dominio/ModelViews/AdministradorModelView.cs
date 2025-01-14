using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinimalApi.Dominio.Enums;

namespace minimal_api.Dominio.ModelViews
{
    public record AdministradorModelView
    {
        public int Id { get; set; } = default;
        public String Email { get; set; } = default;
        public string Perfil { get; set; } = default;
    }
}