using System;
using System.Collections.Generic;

namespace WebPhone.Models.Users
{
    public class UserRoleDTO
    {
        public Guid UserId { get; set; }
        public List<Guid> SelectedRole { get; set; } = new List<Guid>();
    }
}