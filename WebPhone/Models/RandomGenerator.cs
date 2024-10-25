using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WebPhone.Models
{
    public class RandomGenerator
    {
        private static readonly Random random = new Random();
        private const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string number = "0123456789";

        public static string RandomCode(int count)
        {
            StringBuilder code = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                code.Append(characters[random.Next(characters.Length)]);
            }
            return code.ToString();
        }

        public static string RandomNumber(int count)
        {
            StringBuilder code = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                code.Append(number[random.Next(number.Length)]);
            }
            return code.ToString();
        }
    }
}