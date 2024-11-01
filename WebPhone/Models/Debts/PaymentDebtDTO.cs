using WebPhone.EF;

namespace WebPhone.Models.Debts
{
    public class PaymentDebtDTO
    {
        public Bill Bill { get; set; }
        public int PaymentTotal { get; set; }
    }
}