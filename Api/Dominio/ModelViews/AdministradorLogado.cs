using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Dominio.ModelViews
{
    public class AdministradorLogado
    {
        public String Email { get; set; } = default;
        public string Perfil { get; set; } = default;
        public String Token { get; set; } = default;
    }
}