using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.Dominio.ModelViews;
using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enums;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi
{
    public class Startup
    {
        private string key;

        public Startup(IConfiguration configuration)
        {
            if (configuration != null)
            {
                Configuration = configuration;
                key = Configuration.GetSection("Jwt").ToString();
            }
        }

        public IConfiguration Configuration { get; set; } = default;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(option =>
            {
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddAuthorization();

            services.AddScoped<IAdministradorServico, AdministradorServico>();
            services.AddScoped<IVeiculoServico, VeiculoServico>();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT aqui"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            services.AddDbContext<DbContexto>(options =>
            {
                options.UseMySql(
                    Configuration.GetConnectionString("Mysql"),
                    ServerVersion.AutoDetect(Configuration.GetConnectionString("Mysql"))
                );
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                #region Home
                endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
                #endregion

                #region Administradors
                string GerarTokenJwt(Administrador administrador)
                {
                    if (string.IsNullOrEmpty(key)) return string.Empty;
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var claims = new List<Claim>() {
                    new Claim("Email", administrador.Email),
                    new Claim("Perfil", administrador.Perfil),
                    new Claim(ClaimTypes.Role, administrador.Perfil)
                };
                    var token = new JwtSecurityToken(
                        claims: claims,
                        expires: DateTime.Now.AddDays(1),
                        signingCredentials: credentials
                        );
                    return new JwtSecurityTokenHandler().WriteToken(token);
                }
                endpoints.MapPost("/Administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
                {
                    Administrador adm = administradorServico.Login(loginDTO);
                    if (adm != null)
                    {
                        string token = GerarTokenJwt(adm);
                        return Results.Ok(new AdministradorLogado
                        {
                            Email = adm.Email,
                            Perfil = adm.Perfil,
                            Token = token
                        });
                    }
                    else
                        return Results.Unauthorized();
                }).AllowAnonymous().WithTags("Administradores");

                endpoints.MapGet("/Administradores", ([FromQuery] int pagina, IAdministradorServico administradorServico) =>
                {
                    var administradores = administradorServico.Todos(pagina);
                    var adms = new List<AdministradorModelView>();
                    foreach (var adm in administradores)
                    {
                        adms.Add(new AdministradorModelView
                        {
                            Id = adm.Id,
                            Email = adm.Email,
                            Perfil = adm.Perfil
                        });
                    }
                    return Results.Ok(adms);
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Administradores");

                endpoints.MapGet("/Administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
                {
                    var administrador = administradorServico.BuscaPorId(id);

                    if (administrador == null) return Results.NotFound();
                    return Results.Ok(new AdministradorModelView
                    {
                        Id = administrador.Id,
                        Email = administrador.Email,
                        Perfil = administrador.Perfil
                    });
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Administradores");

                endpoints.MapPost("/Administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
                {
                    ErrosDeValidacao validacao = new ErrosDeValidacao
                    {
                        Mensagens = new List<string>()
                    };
                    if (string.IsNullOrEmpty(administradorDTO.Email)) validacao.Mensagens.Add("Email não pode ser vazio");
                    if (string.IsNullOrEmpty(administradorDTO.Senha)) validacao.Mensagens.Add("Senha não pode ser vazia");

                    if (validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);

                    Administrador administrador = new()
                    {
                        Email = administradorDTO.Email,
                        Senha = administradorDTO.Senha,
                        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
                    };
                    administradorServico.Incluir(administrador);
                    return Results.Created($"/Administradores/{administrador.Id}", new AdministradorModelView
                    {
                        Id = administrador.Id,
                        Email = administrador.Email,
                        Perfil = administrador.Perfil
                    });
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Administradores");
                #endregion

                #region Veiculos
                endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
                {
                    var validacao = validaDTO(veiculoDTO);
                    if (validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);

                    Veiculo veiculo = new()
                    {
                        Nome = veiculoDTO.Nome,
                        Marca = veiculoDTO.Marca,
                        Ano = veiculoDTO.Ano
                    };
                    veiculoServico.Incluir(veiculo);
                    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
                .WithTags("Veiculos");

                endpoints.MapGet("/veiculos", (int pagina, IVeiculoServico veiculoServico) =>
                {
                    var veiculos = veiculoServico.Todos(pagina);
                    return Results.Ok(veiculos);
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
                .WithTags("Veiculos");

                endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
                {
                    var veiculo = veiculoServico.BuscaPorId(id);

                    if (veiculo == null) return Results.NotFound();
                    return Results.Ok(veiculo);
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
                .WithTags("Veiculos");

                endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
                {
                    var validacao = validaDTO(veiculoDTO);
                    if (validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);

                    var veiculo = veiculoServico.BuscaPorId(id);

                    if (veiculo == null) return Results.NotFound();

                    veiculo.Nome = veiculoDTO.Nome;
                    veiculo.Marca = veiculoDTO.Marca;
                    veiculo.Ano = veiculoDTO.Ano;

                    veiculoServico.Atualizar(veiculo);

                    return Results.Ok(veiculo);
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Veiculos");

                endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
                {
                    var veiculo = veiculoServico.BuscaPorId(id);

                    if (veiculo == null) return Results.NotFound();

                    veiculoServico.Apagar(veiculo);

                    return Results.NoContent();
                })
                .RequireAuthorization()
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
                .WithTags("Veiculos");

                ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
                {
                    ErrosDeValidacao validacao = new ErrosDeValidacao
                    {
                        Mensagens = new List<string>()
                    };
                    if (string.IsNullOrEmpty(veiculoDTO.Nome)) validacao.Mensagens.Add("O nome não pode estar vazio");
                    if (string.IsNullOrEmpty(veiculoDTO.Marca)) validacao.Mensagens.Add("A marca não pode ficar vazia");
                    if (veiculoDTO.Ano < 1950) validacao.Mensagens.Add("Veículo muito antigo. Aceitos somente anos superiores a 1950");
                    return validacao;
                }
                #endregion
            });
        }
    }
}