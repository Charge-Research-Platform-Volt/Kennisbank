using MailKit.Net.Smtp;
using MimeKit;

namespace KnowledgeBank.Utils
{
    public class MailUtils(string smtpHost, int tlsPort, string address, string password, string fromName)
    {
        public virtual void SendMail(string to, string subject, string body)
        {
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, address));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using (SmtpClient client = new SmtpClient())
            {
                client.Connect(smtpHost, tlsPort, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(address, password);

                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


