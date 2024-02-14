using FluentValidation;

namespace RinhaBackend.Models
{
    public class Transacao
    {
        public int Valor { get; set; }
        public string Tipo { get; set; }
        public string Descricao { get; set; }
        public DateTime RealizadaEm { get; set; }
    }

    public class TransacaoValidator : AbstractValidator<Transacao>
    {
        public TransacaoValidator()
        {
            RuleFor(x => x.Valor)
                .NotNull().WithMessage("campo valor é obrigatório");

            var tiposPermitidos = new List<string>() { "c", "d" };
            RuleFor(x => x.Tipo)
                .Length(1, 1)
                .NotNull().WithMessage("campo valor é obrigatório")
                .Must(x => tiposPermitidos.Contains(x)).WithMessage("informe um tipo: " + String.Join(", ", tiposPermitidos));

            RuleFor(x => x.Descricao)
                .Length(1, 10).WithMessage("campo descrição deve ter entre 1 e 10 caracteres")
                .NotNull().WithMessage("campo descrição é obrigatório");
        }
    }
}
