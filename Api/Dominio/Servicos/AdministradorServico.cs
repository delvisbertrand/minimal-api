using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {
        private readonly DbContexto _contexto;

        public AdministradorServico(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador BuscaPorId(int id)
        {
            return _contexto.Administradores.Where(v => v.Id == id).FirstOrDefault();
        }

        public Administrador Incluir(Administrador administrador)
        {
            _contexto.Administradores.Add(administrador);
            _contexto.SaveChanges();
            return administrador;
        }

        public Administrador Login(LoginDTO loginDTO)
        {
            return _contexto.Administradores.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
        }

        public List<Administrador> Todos(int pagina)
        {
            var query = _contexto.Administradores.AsQueryable();
            int itensPorPagina = 10;
            query = query.Skip((pagina - 1) * itensPorPagina).Take(itensPorPagina);
            return [.. query];
        }
    }
}