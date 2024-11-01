using WebPhone.EF;

namespace WebPhone.Models.Debts
{
    public class CustomerDebtDTO
    {
        public User Customer { get; set; }
        public int Debt { get; set; }
    }
}