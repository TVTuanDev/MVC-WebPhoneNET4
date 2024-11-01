﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPhone.EF
{
    [Table("UserRoles")]
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public virtual User User { get; set; }
        public virtual Role Role { get; set; }
    }
}