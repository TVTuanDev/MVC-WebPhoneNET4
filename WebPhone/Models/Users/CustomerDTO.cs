using System;
using System.ComponentModel.DataAnnotations;

namespace WebPhone.Models.Users
{
    public class CustomerDTO
    {
        public Guid Id { get; set; }
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Address { get; set; }
    }
}