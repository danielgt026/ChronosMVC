﻿using ChronosMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ChronosMVC.Controllers
{
    public class CorporacaoController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string apiUrl = "http://localhost:5027/api/Corporacao/";

        public CorporacaoController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult LoginCorporacao()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CadastroCorporacao()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AdicionarDadosCorporacao()
        {
            var id = User.FindFirst("idCorporacao")?.Value;

            if (!string.IsNullOrEmpty(id))
            {
                var model = new CorporacaoModel { idCorporacao = int.Parse(id) };
                return View(model);
            }

            TempData["MensagemErro"] = "Corporação não encontrada.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CorporacaoModel model)
        {
            var loginData = new { emailCorporacao = model.emailCorporacao, passwordString = model.PasswordString };
            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl + "Autenticar", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Aqui, você deve deserializar a resposta da API para obter o ID da corporação
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                var token = tokenResponse.Token;
                var idCorporacao = tokenResponse.IdCorporacao; // Capturando o ID da corporação

                // Configure a claims identity
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.emailCorporacao),
                    new Claim("Token", token), // Adicione o token como uma claim
                    new Claim("idCorporacao", idCorporacao.ToString()), // Use o ID capturado
                    new Claim(ClaimTypes.Role, "Corporacao") // Adicionando a role
                };

                var claimsIdentity = new ClaimsIdentity(claims, "login");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(claimsPrincipal); // Autentica o usuário

                TempData["MensagemSucesso"] = "Login realizado com sucesso!";
                return RedirectToAction("Index", "Home");
            }

            TempData["MensagemErro"] = $"Erro: {responseContent}";
            return View("LoginCorporacao", model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCorporacao(CorporacaoModel model)
        {

            var registerData = new { emailCorporacao = model.emailCorporacao, passwordString = model.PasswordString };
            var json = JsonConvert.SerializeObject(registerData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl + "Registrar", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["MensagemSucesso"] = "Registro realizado com sucesso!";
                return RedirectToAction("LoginCorporacao"); 
            }

            TempData["MensagemErro"] = $"Erro: {responseContent}";
            return View(model); 
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarInformacoes(CorporacaoModel model)
        {
            // Certifique-se de que o idCorporacao está no modelo
            if (model.idCorporacao <= 0)
            {
                TempData["MensagemErro"] = "ID da corporação não é válido.";
                return View("AdicionarDadosCorporacao", model);
            }

            // Serializa o modelo para JSON
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Envia o pedido para a API
            var response = await _httpClient.PutAsync(apiUrl + "Put/" + model.idCorporacao, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Verifica se a resposta foi bem-sucedida
            if (response.IsSuccessStatusCode)
            {
                // Se a resposta contiver um corpo, tente deserializá-lo
                if (!string.IsNullOrEmpty(responseContent))
                {
                    try
                    {
                        var registeredCorporacao = JsonConvert.DeserializeObject<CorporacaoModel>(responseContent);
                        TempData["idCorporacao"] = registeredCorporacao.idCorporacao;
                        TempData["MensagemSucesso"] = "Informações atualizadas com sucesso!";
                    }
                    catch (JsonException ex)
                    {
                        // Se houver um erro ao deserializar, registre o erro
                        TempData["MensagemErro"] = $"Erro ao processar a resposta: {ex.Message}";
                    }
                }
                else
                {
                    TempData["MensagemSucesso"] = "Informações atualizadas com sucesso!";
                }
                return RedirectToAction("Index", "Home");
            }

            // Se a resposta não for bem-sucedida, registre o erro
            TempData["MensagemErro"] = $"Erro: {responseContent}";
            return View("AdicionarDadosCorporacao", model);
        }




        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(); 
            TempData["MensagemSucesso"] = "Logout realizado com sucesso!";
            return RedirectToAction("Index", "Home");
        }




        public class TokenResponse
        {
            public string Token { get; set; }
            public int IdCorporacao { get; set; } 
        }

    }
}