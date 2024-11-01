using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Index]
        [Required]
        [StringLength(200)]
        public string UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(200)]
        public string PasswordHash { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
        public virtual ICollection<Bill> CustomerBills { get; set; } = new List<Bill>();
        public virtual ICollection<Bill> EmploymentBills { get; set; } = new List<Bill>();
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();
    }
}