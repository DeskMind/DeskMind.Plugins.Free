using Microsoft.Office.Interop.Outlook;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeskMind.Plugins.Office.Outlook
{
    public class OutlookEmailPlugin
    {
        [KernelFunction, Description("Send an email via Outlook. The function will handle opening of Outlook if not started yet.")]
        public string SendEmail(
            [Description("Recipient email address"), Required] string to,
            [Description("Subject"), Required] string subject,
            [Description("Email body"), Required] string body)
        {
            var app = OutlookHelper.GetOrStartOutlook();
            var mail = (MailItem)app.CreateItem(OlItemType.olMailItem);
            mail.To = to;
            mail.Subject = subject;
            mail.Body = body;
            mail.Send();
            return $"Email sent to {to} with subject '{subject}'.";
        }

        [KernelFunction, Description("List unread emails in the inbox. The function will handle opening of Outlook if not started yet.")]
        public string ListUnreadEmails()
        {
            var app = OutlookHelper.GetOrStartOutlook();
            var inbox = app.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            var unread = inbox.Items.Cast<MailItem>().Where(m => m.UnRead).Take(10);
            return string.Join("\n", unread.Select(m => $"From: {m.SenderName}, Subject: {m.Subject}"));
        }

        [KernelFunction, Description("Mark an email as read by subject. The function will handle opening of Outlook if not started yet.")]
        public string MarkEmailAsRead([Description("Email subject"), Required] string subject)
        {
            var app = OutlookHelper.GetOrStartOutlook();
            var inbox = app.Session.GetDefaultFolder(OlDefaultFolders.olFolderInbox);
            var mail = inbox.Items.Cast<MailItem>().FirstOrDefault(m => m.Subject == subject);
            if (mail == null) return $"Email with subject '{subject}' not found.";
            mail.UnRead = false;
            mail.Save();
            return $"Email '{subject}' marked as read.";
        }
    }
}

