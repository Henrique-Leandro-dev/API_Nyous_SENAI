﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nyous.Contexts;
using Nyous.Domains;

namespace Nyous.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        NyousContext _context = new NyousContext();

        // Capturar as infos do token do appsetting.json
        // Definimos uma variável para percorrer nossos métodos com as configurações obtidas no appsettings.json
        private IConfiguration _config;

        // Definimos um método construtor para poder passar essas configs
        public LoginController(IConfiguration config)
        {
            _config = config;
        }

        // Método para validar o usuario na aplicação
        private Usuario AuthenticateUser(Usuario login)
        {
            return _context.Usuario.Include(a => a.IdAcessoNavigation).FirstOrDefault(u => u.Email == login.Email && u.Senha == login.Senha);
        }


        // Método para gerar o token
        private string GenerateJSONWebToken(Usuario userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Definimos nossas Claims (dados da sessão) para poderem ser capturadas
            // a qualquer momento enquanto o Token for ativo
                var claims = new[] {
                new Claim(JwtRegisteredClaimNames.NameId, userInfo.Nome),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Pegar info de acesso  por escrito, ex: Padrao ou administrador
                new Claim(ClaimTypes.Role, userInfo.IdAcessoNavigation.Tipo)
    };

            // Configuramos nosso Token e seu tempo de vida
            var token = new JwtSecurityToken
                (
                    _config["Jwt:Issuer"],
                    _config["Jwt:Issuer"],
                    claims,
                    expires: DateTime.Now.AddMinutes(120),
                    signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Usamos a anotação "AllowAnonymous" para 
        // ignorar a autenticação neste método, já que é ele quem fará isso
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] Usuario login)
        {
            // Definimos logo de cara como não autorizado
            IActionResult response = Unauthorized();

            // Autenticamos o usuário da API
            var user = AuthenticateUser(login);
            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }
    }
}
