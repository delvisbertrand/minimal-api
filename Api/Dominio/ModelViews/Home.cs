using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinimalApi.Dominio.ModelViews
{
    public struct Home
    {
        public readonly string Mensagem { get => "Bem vindo a API de veículos - Minimal API"; }

        public readonly string Doc { get => "/swagger"; }
    }
}