using System.ComponentModel.DataAnnotations;

namespace RinhaBackend.Models
{
    public class Transacao
    {
        [Required(ErrorMessage = "Campo valor é obrigatório")]
        public int Valor { get; set; }

        [Required(ErrorMessage = "Campo tipo é obrigatório")]
        [StringLength(1)]
        [AllowedValues("c", "d")]
        public string Tipo { get; set; }

        [Required(ErrorMessage = "Campo descrição é obrigatório")]
        [Length(1, 10)]
        public string Descricao { get; set; }
    }
}
