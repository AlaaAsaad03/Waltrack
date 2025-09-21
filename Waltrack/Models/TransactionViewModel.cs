namespace Waltrack.Models
{
    public class TransactionViewModel
    {
        public int TransactionId { get; set; }
        public int CategoryId { get; set; }
        public int Amount { get; set; }
        public string? Note { get; set; }
        public DateTime Date { get; set; }
    }
}
