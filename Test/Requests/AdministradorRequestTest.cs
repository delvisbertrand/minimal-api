using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using minimal_api.Dominio.ModelViews;
using MinimalApi.DTOs;
using Test.Helpers;

namespace Test.Requests
{
    [TestClass]
    public class AdministradorRequestTest
    {
        [ClassInitialize]
        public static void ClassInit(TestContext testContext)
        {
            Setup.ClassInit(testContext);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Setup.CloseCleanup();
        }

        [TestMethod]
        public async Task TestLogin()
        {
            // Arrange
            var loginDTO = new LoginDTO{
                Email = "adm@teste.com",
                Senha = "123456"
            };

            var content = new StringContent(JsonSerializer.Serialize(loginDTO), Encoding.UTF8, "Application/json");

            // Act
            var response = await Setup.client.PostAsync("/Administradores/login", content);

            // Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadAsStreamAsync();
            var admLogado = JsonSerializer.Deserialize<AdministradorLogado>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.IsNotNull(admLogado.Email ?? "");
            Assert.IsNotNull(admLogado.Perfil ?? "");
            Assert.IsNotNull(admLogado.Token ?? "");
        }
    }
}