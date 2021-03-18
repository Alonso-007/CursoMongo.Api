using CursoMongo.Api.Data.Schemas;
using CursoMongo.Api.Domain.Entities;
using CursoMongo.Api.Domain.ValueObjects;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CursoMongo.Api.Domain.Enums;
using MongoDB.Driver.Linq;

namespace CursoMongo.Api.Data.Repositories
{
    public class RestauranteRepository
    {
        IMongoCollection<RestauranteSchema> _restaurantes;
        IMongoCollection<AvaliacaoSchema> _avaliacoes;

        public RestauranteRepository(MongoDB mongoDB)
        {
            _restaurantes = mongoDB.DB.GetCollection<RestauranteSchema>("restaurantes");
            _avaliacoes = mongoDB.DB.GetCollection<AvaliacaoSchema>("avaliacoes");
        }


        public void Inserir(Restaurante restaurante)
        {
            var document = new RestauranteSchema
            {
                Nome = restaurante.Nome,
                Cozinha = restaurante.Cozinha,
                Endereco = new EnderecoSchema
                {
                    Logradouro = restaurante.Endereco.Logradouro,
                    Numero = restaurante.Endereco.Numero,
                    Cidade = restaurante.Endereco.Cidade,
                    Cep = restaurante.Endereco.Cep,
                    UF = restaurante.Endereco.UF
                }
            };

            _restaurantes.InsertOne(document);
        }

        public async Task<IEnumerable<Restaurante>> ObterTodos()
        {
            var restuarantes = new List<Restaurante>();

            //com filtro
            /*var filter = Builders<RestauranteSchema>.Filter.Empty; //Empty pq vai trazer todos
            await _restaurantes.Find(filter).ForEachAsync(d =>
            {
                var r = new Restaurante(d.Id, d.Nome, d.Cozinha);
                var e = new Endereco(d.Endereco.Logradouro, d.Endereco.Numero, d.Endereco.Cidade, d.Endereco.UF, d.Endereco.Cep);
                r.AtribuirEndereco(e);
                restuarantes.Add(r);
            });*/

            //sem filtro mas trazendo tudo com true
            /*await _restaurantes.Find(r=> true).ForEachAsync(d =>
            {
                var r = new Restaurante(d.Id, d.Nome, d.Cozinha);
                var e = new Endereco(d.Endereco.Logradouro, d.Endereco.Numero, d.Endereco.Cidade, d.Endereco.UF, d.Endereco.Cep);
                r.AtribuirEndereco(e);
                restuarantes.Add(r);
            });*/

            //com linq
            await _restaurantes.AsQueryable().ForEachAsync(d =>
            {
                var r = new Restaurante(d.Id, d.Nome, d.Cozinha);
                var e = new Endereco(d.Endereco.Logradouro, d.Endereco.Numero, d.Endereco.Cidade, d.Endereco.UF, d.Endereco.Cep);
                r.AtribuirEndereco(e);
                restuarantes.Add(r);
            });

            return restuarantes;
        }

        public Restaurante ObterPorId(string id)
        {
            var document = _restaurantes.AsQueryable().FirstOrDefault(c => c.Id == id);

            if (document == null) return null;

            return document.ConvertParaDomain();
        }

        public bool AlterarCompleto(Restaurante restaurante)
        {
            var document = new RestauranteSchema
            {
                Id = restaurante.Id,
                Nome = restaurante.Nome,
                Cozinha = restaurante.Cozinha,
                Endereco = new EnderecoSchema
                {
                    Logradouro = restaurante.Endereco.Logradouro,
                    Numero = restaurante.Endereco.Numero,
                    Cidade = restaurante.Endereco.Cidade,
                    Cep = restaurante.Endereco.Cep,
                    UF = restaurante.Endereco.UF
                }
            };

            var resultado = _restaurantes.ReplaceOne(r => r.Id == document.Id, document);

            return resultado.ModifiedCount > 0;
        }

        public bool AlterarCozinha(string id, ECozinha cozinha)
        {
            var atualizacao = Builders<RestauranteSchema>.Update.Set(c => c.Cozinha, cozinha);

            var resultado = _restaurantes.UpdateOne(r => r.Id == id, atualizacao);

            return resultado.ModifiedCount > 0;
        }

        public IEnumerable<Restaurante> ObterPorNome(string nome)
        {
            var restaurantes = new List<Restaurante>();

            //com filtro
            //$options i para ser case insentitive
            /*var filter = new BsonDocument { { "nome", new BsonDocument { { "$regex", nome }, {"$options", "i" } } } };
            _restaurantes.Find(filter)
                .ToList()
                .ForEach(d => restaurantes.Add(d.ConvertParaDomain()));*/

            //parecido com like usando linq
            _restaurantes.AsQueryable()
                .Where(w => w.Nome.ToLower().Contains(nome.ToLower()))
                .ToList()
                .ForEach(d => restaurantes.Add(d.ConvertParaDomain()));

            return restaurantes;
        }

        public void Avaliar(string restauranteId, Avaliacao avaliacao)
        {
            var document = new AvaliacaoSchema
            {
                RestauranteId = restauranteId,
                Estrelas = avaliacao.Estrelas,
                Comentario = avaliacao.Comentario
            };

            _avaliacoes.InsertOne(document);
        }

        public async Task<Dictionary<Restaurante, double>> ObterTop3()
        {
            var retorno = new Dictionary<Restaurante, double>();

            var top3 = _avaliacoes.Aggregate()
                .Group(g => g.RestauranteId, e => new { RestauranteId = e.Key, MediaEstrelas = e.Average(a => a.Estrelas) })
                .SortByDescending(s => s.MediaEstrelas)
                .Limit(3);

            await top3.ForEachAsync(t =>
            {
                var restaurante = ObterPorId(t.RestauranteId);

                _avaliacoes.AsQueryable()
                .Where(a => a.RestauranteId == t.RestauranteId)
                .ToList()
                .ForEach(a => restaurante.InserirAvaliacao(a.ConverterParaDomain()));

                retorno.Add(restaurante, t.MediaEstrelas);
            });

            return retorno;
        }

        public (long totalRestauranteRemovido, long totalAvaliacoesRemovidas) Remover(string restauranteId)
        {
            var resultadoAvaliacoes = _avaliacoes.DeleteMany(d => d.RestauranteId == restauranteId);
            var resultadoRestaurante = _restaurantes.DeleteOne(d => d.Id == restauranteId);

            return (resultadoRestaurante.DeletedCount, resultadoAvaliacoes.DeletedCount);
        }

        public async Task<IEnumerable<Restaurante>> ObterPorBuscaTextual(string texto)
        {
            var restaurantes = new List<Restaurante>();

            //usa o indice textual
            var filter = Builders<RestauranteSchema>.Filter.Text(texto);

            await _restaurantes
                .AsQueryable()
                .Where(w => filter.Inject()) //injeta o filtro
                .ForEachAsync(d => restaurantes.Add(d.ConvertParaDomain()));

            return restaurantes;
        }

        public async Task<Dictionary<Restaurante, double>> ObterTop3_ComLookup()
        {
            var retorno = new Dictionary<Restaurante, double>();

            var top3 = _avaliacoes.Aggregate()
                .Group(_ => _.RestauranteId, g => new { RestauranteId = g.Key, MediaEstrelas = g.Average(a => a.Estrelas) })
                .SortByDescending(_ => _.MediaEstrelas)
                .Limit(3)
                .Lookup<RestauranteSchema, RestauranteAvaliacaoSchema>("restaurantes", "RestauranteId", "Id", "Restaurante")
                .Lookup<AvaliacaoSchema, RestauranteAvaliacaoSchema>("avaliacoes", "Id", "RestauranteId", "Avaliacoes");

            await top3.ForEachAsync(_ =>
            {
                if (!_.Restaurante.Any()) return;//caso nao ache restaurante

                var restaurante = new Restaurante(_.Id, _.Restaurante[0].Nome, _.Restaurante[0].Cozinha);
                var endereco = new Endereco(
                    _.Restaurante[0].Endereco.Logradouro,
                    _.Restaurante[0].Endereco.Numero,
                    _.Restaurante[0].Endereco.Cidade,
                    _.Restaurante[0].Endereco.UF,
                    _.Restaurante[0].Endereco.Cep);

                restaurante.AtribuirEndereco(endereco);

                retorno.Add(restaurante, _.MediaEstrelas);
            });

            return retorno;
        }
    }
}