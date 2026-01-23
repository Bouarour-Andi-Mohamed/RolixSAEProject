using System;

namespace RolixSAEProject.Models
{
    public class AccountResetInfo
    {
        public Guid AccountId { get; set; }
        public string Name { get; set; } = "";
        public string Identifiant { get; set; } = "";
        public string Email2 { get; set; } = "";
    }
}
