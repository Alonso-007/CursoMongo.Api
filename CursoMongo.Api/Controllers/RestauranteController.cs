using CursoMongo.Api.Controllers.Inputs;
using CursoMongo.Api.Controllers.Outputs;
using CursoMongo.Api.Data.Repositories;
using CursoMongo.Api.Domain.Entities;
using CursoMongo.Api.Domain.Enums;
using CursoMongo.Api.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CursoMongo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestauranteController : ControllerBase
    {

        private readonly RestauranteRepository _restauranteRepository;

        public RestauranteController(RestauranteRepository restauranteRepository)
        {
            _restauranteRepository = restauranteRepository;
        }

        [HttpPost("IncluirRestaurante")]
        public ActionResult IncluirRestaurante([FromBody] RestauranteInclusao restauranteInclusao)
        {
            var cozinha = ECozinhaHelper.ConverterDeInteiro(restauranteInclusao.Cozinha);

            var restaurante = new Restaurante(restauranteInclusao.Nome, cozinha);
            var endereco = new Endereco(
                restauranteInclusao.Logradouro,
                restauranteInclusao.Numero,
                restauranteInclusao.Cidade,
                restauranteInclusao.UF,
                restauranteInclusao.Cep);

            restaurante.AtribuirEndereco(endereco);


            if (!restaurante.Validar())
            {
                return BadRequest(
                    new
                    {
                        errors = restaurante.ValidationResult.Errors.Select(_ => _.ErrorMessage)
                    });
            }

            _restauranteRepository.Inserir(restaurante);

            return Ok(
                new
                {
                    data = "Restaurante inserido com sucesso"
                }
            );
        }

        [HttpGet("todos")]
        public async Task<ActionResult> ObterRestaurantes()
        {
            var restaurantes = await _restauranteRepository.ObterTodos();

            var listagem = restaurantes.Select(r => new RestauranteListagem
            {
                Id = r.Id,
                Nome = r.Nome,
                Cozinha = (int)r.Cozinha,
                Cidade = r.Endereco.Cidade
            });

            return Ok(
                new
                {
                    data = listagem
                }
            );
        }

        [HttpGet("porId/{id}")]
        public ActionResult ObterRestaurantesPorId(string id)
        {
            var restaurante = _restauranteRepository.ObterPorId(id);

            if (restaurante == null) return NotFound();

            var exibicao = new RestauranteExibicao
            {
                Id = restaurante.Id,
                Nome = restaurante.Nome,
                Cozinha = (int)restaurante.Cozinha,
                Endereco = new EnderecoExibicao
                {
                    Logradouro = restaurante.Endereco.Logradouro,
                    Numero = restaurante.Endereco.Numero,
                    Cidade = restaurante.Endereco.Cidade,
                    Cep = restaurante.Endereco.Cep,
                    UF = restaurante.Endereco.UF
                }
            };

            return Ok(
                new
                {
                    data = exibicao
                }
            );
        }

        [HttpPut("alteracaoCompleta")]
        public ActionResult AlterarRestaurante([FromBody] RestauranteAlteracaoCompleta restauranteAlteracaoCompleta)
        {
            var restaurante = _restauranteRepository.ObterPorId(restauranteAlteracaoCompleta.Id);

            if (restaurante == null) return NotFound();

            var cozinha = ECozinhaHelper.ConverterDeInteiro(restauranteAlteracaoCompleta.Cozinha);
            restaurante = new Restaurante(restauranteAlteracaoCompleta.Id, restauranteAlteracaoCompleta.Nome, cozinha);
            var endereco = new Endereco(
                restauranteAlteracaoCompleta.Logradouro,
                restauranteAlteracaoCompleta.Numero,
                restauranteAlteracaoCompleta.Cidade,
                restauranteAlteracaoCompleta.UF,
                restauranteAlteracaoCompleta.Cep);

            restaurante.AtribuirEndereco(endereco);

            if (!restaurante.Validar())
            {
                return BadRequest(
                    new
                    {
                        errors = restaurante.ValidationResult.Errors.Select(e => e.ErrorMessage)
                    }
                );
            }

            if (!_restauranteRepository.AlterarCompleto(restaurante))
            {
                return BadRequest(new
                {
                    errors = "Nenhum documento foi alterado"
                });
            }

            return Ok(
                new
                {
                    data = "Restaurante alterado com sucesso"
                }
            );
        }

        [HttpPatch("alteracaoParcial")]
        public ActionResult AlterarCozinha(string id, [FromBody] RestauranteAlteracaoParcial restauranteAlteracaoParcial)
        {
            var restaurante = _restauranteRepository.ObterPorId(id);

            if (restaurante == null) return NotFound();

            var cozinha = ECozinhaHelper.ConverterDeInteiro(restauranteAlteracaoParcial.Cozinha);

            if (!_restauranteRepository.AlterarCozinha(id, cozinha))
            {
                return BadRequest(new
                {
                    error = "Nenhum documento foi alterado"
                });
            }

            return Ok(
                new
                {
                    data = "Restaurante alterado com sucesso"
                }
            );
        }

        [HttpGet("obterPorNome")]
        public ActionResult ObterRestaurantePorNome([FromQuery] string nome)
        {
            var restaurantes = _restauranteRepository.ObterPorNome(nome);

            var listagem = restaurantes.Select(r => new RestauranteListagem
            {
                Id = r.Id,
                Nome = r.Nome,
                Cozinha = (int)r.Cozinha,
                Cidade = r.Endereco.Cidade
            });

            return Ok(
                new
                {
                    data = listagem
                }
            );
        }

        [HttpPatch("avaliaInclusao/{id}")]
        public ActionResult AvaliarRestaurante(string id, [FromBody] AvaliacaoInclusao avaliacaoInclusao)
        {
            var restaurante = _restauranteRepository.ObterPorId(id);

            if (restaurante == null) return NotFound();

            var avaliacao = new Avaliacao(avaliacaoInclusao.Estrelas, avaliacaoInclusao.Comentario);

            if (!avaliacao.Validar())
            {
                return BadRequest(
                    new
                    {
                        errors = avaliacao.ValidationResult.Errors.Select(e => e.ErrorMessage)
                    }
                );
            }

            _restauranteRepository.Avaliar(id, avaliacao);

            return Ok(
                new
                {
                    data = "Restaurante avaliado com sucesso"
                }
            );
        }

        [HttpGet("top3")]
        public async Task<ActionResult> Top3()
        {
            var top3 = await _restauranteRepository.ObterTop3();

            var listagem = top3.Select(e => new RestauranteTop3
            {
                Id = e.Key.Id,
                Nome = e.Key.Nome,
                Cozinha = (int)e.Key.Cozinha,
                Cidade = e.Key.Endereco.Cidade,
                Estrelas = e.Value
            });

            return Ok(
                new
                {
                    data = listagem
                }
            );
        }

        [HttpGet("top3Lookup")]
        public async Task<ActionResult> Top3LookUp()
        {
            var top3 = await _restauranteRepository.ObterTop3_ComLookup();

            var listagem = top3.Select(e => new RestauranteTop3
            {
                Id = e.Key.Id,
                Nome = e.Key.Nome,
                Cozinha = (int)e.Key.Cozinha,
                Cidade = e.Key.Endereco.Cidade,
                Estrelas = e.Value
            });

            return Ok(
                new
                {
                    data = listagem
                }
            );
        }

        [HttpDelete("remover/{id}")]
        public ActionResult Remover(string id)
        {
            var restaurante = _restauranteRepository.ObterPorId(id);

            if (restaurante == null) return NotFound();

            (var totalRestauranteRemovido, var totalAvaliacoesRemovidas) = _restauranteRepository.Remover(id);
            //com parametro nomeado
            /*var item = _restauranteRepository.Remover(id);
            var totalRestauranteRemovido = item.totalRestauranteRemovido;
            var totalAvaliacoesRemovidas = item.totalAvaliacoesRemovidas;*/
            

            return Ok(
                new
                {
                    data = $@"Total de exclusões: {totalRestauranteRemovido} restaurante com {totalAvaliacoesRemovidas} avaliações"
                }
            );
        }

        [HttpGet("textual")]
        public async Task<ActionResult> ObterRestaurantePorBuscaTextual([FromQuery] string texto)
        {
            var restaurantes = await _restauranteRepository.ObterPorBuscaTextual(texto);

            var listagem = restaurantes.ToList().Select(t => new RestauranteListagem
            {
                Id = t.Id,
                Nome = t.Nome,
                Cozinha = (int)t.Cozinha,
                Cidade = t.Endereco.Cidade
            });

            return Ok(
                new
                {
                    data = listagem
                }
            );
        }
    }
}
