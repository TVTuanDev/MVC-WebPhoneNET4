using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using MailKit.Security;
using MimeKit.Text;
using MimeKit;
using MailKit.Net.Smtp;

namespace WebPhone.Services
{
    public class SendMailService
    {
        public async Task SendMailAsync(string emailTo, string subject, string htmlMessage)
        {
            var conf = ConfigurationManager.AppSettings[""];
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("WebPhone.vn", ConfigurationManager.AppSettings["Mail"]));
            email.To.Add(MailboxAddress.Parse(emailTo));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            using (var smtp = new SmtpClient())
            {
                try
                {
                    smtp.Connect(ConfigurationManager.AppSettings["Host"], int.Parse(ConfigurationManager.AppSettings["Port"] ?? ""), SecureSocketOptions.StartTls);
                    smtp.Authenticate(ConfigurationManager.AppSettings["Mail"], ConfigurationManager.AppSettings["Password"]);
                    await smtp.SendAsync(email);
                }
                catch (Exception)
                {
                    // Gửi mail thất bại, nội dung email sẽ lưu vào thư mục mailssave
                    System.IO.Directory.CreateDirectory("mailssave");
                    var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid());
                    await email.WriteToAsync(emailsavefile);
                }

                smtp.Disconnect(true);
            }
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Cài đặt dịch vụ gửi SMS tại đây
            //System.IO.Directory.CreateDirectory("smssave");
            //var emailsavefile = string.Format(@"smssave/{0}-{1}.txt", number, Guid.NewGuid());
            //System.IO.File.WriteAllTextAsync(emailsavefile, message);
            return Task.FromResult(0);
        }
    }
}