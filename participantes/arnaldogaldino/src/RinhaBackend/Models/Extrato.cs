namespace RinhaBackend.Models
{
    public class Extrato
    {
        public Extrato()
        {
            Saldo = new Saldo();
            UltimasTransacoes = new List<Transacao>();
        }

        public Saldo Saldo { get; set; }
        public List<Transacao> UltimasTransacoes { get; set; }
    }
}
