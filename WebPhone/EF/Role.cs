using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string RoleName { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}