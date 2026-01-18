using System.Collections.Generic;

namespace RolixSAEProject.Models
{
    public class AccountPageViewModel
    {
        public AccountProfile Profile { get; set; } = new AccountProfile();

        // onglet commandes
        public List<OrderSummary> Orders { get; set; } = new List<OrderSummary>();
    }
}
