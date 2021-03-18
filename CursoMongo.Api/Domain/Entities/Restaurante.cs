using CursoMongo.Api.Domain.Enums;
using CursoMongo.Api.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;

namespace CursoMongo.Api.Domain.Entities
{
    public class Restaurante : AbstractValidator<Restaurante>
    {
        public Restaurante(string nome, ECozinha cozinha)
        {
            Nome = nome;
            Cozinha = cozinha;
            //Avaliacoes = new List<Avaliacao>();
        }

        public Restaurante(string id, string nome, ECozinha cozinha)
        {
            Id = id;
            Nome = nome;
            Cozinha = cozinha;
            //Avaliacoes = new List<Avaliacao>();
        }

        public string Id { get; private set; }
        public string Nome { get; private set; }
        public ECozinha Cozinha { get; private set; }
        public Endereco Endereco { get; private set; }
        public List<Avaliacao> Avaliacoes { get; private set; } = new List<Avaliacao>();

        public ValidationResult ValidationResult { get; set; }

        public void AtribuirEndereco(Endereco endereco) => Endereco = endereco;

        public void InserirAvaliacao(Avaliacao avaliacao) => Avaliacoes.Add(avaliacao);

        public bool Validar()
        {
            ValidarNome();
            ValidationResult = Validate(this);

            ValidarEndereco();

            return ValidationResult.IsValid;
        }

        private void ValidarNome()
        {
            RuleFor(c => c.Nome)
                .NotEmpty().WithMessage("Nome não pode ser vazio")
                .MaximumLength(30).WithMessage("Nome pode ter no maximo 30 caracteres");
        }

        private void ValidarEndereco()
        {
            if (Endereco.Validar())
                return;

            foreach (var erro in Endereco.ValidationResult.Errors)
                ValidationResult.Errors.Add(erro);
        }
    }
}