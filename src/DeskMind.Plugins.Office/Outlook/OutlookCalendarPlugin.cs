using Microsoft.Office.Interop.Outlook;
using Microsoft.SemanticKernel;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeskMind.Plugins.Office.Outlook
{
    public class OutlookCalendarPlugin
    {
        [KernelFunction, Description("Create a calendar appointment. The function will handle opening of Outlook if not started yet.")]
        public string CreateAppointment(
            [Description("Subject"), Required] string subject,
            [Description("Start date/time"), Required] DateTime start,
            [Description("End date/time"), Required] DateTime end,
            [Description("Location")] string location = "",
            [Description("Body")] string body = "")
        {
            var app = OutlookHelper.GetOrStartOutlook();
            var appt = (AppointmentItem)app.CreateItem(OlItemType.olAppointmentItem);
            appt.Subject = subject;
            appt.Start = start;
            appt.End = end;
            appt.Location = location;
            appt.Body = body;
            appt.Save();
            return $"Appointment '{subject}' created from {start} to {end}.";
        }

        [KernelFunction, Description("List upcoming appointments. The function will handle opening of Outlook if not started yet.")]
        public string ListUpcomingAppointments([Description("Number of appointments"), Required] int count = 10)
        {
            var app = OutlookHelper.GetOrStartOutlook();
            var calendar = app.Session.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
            var items = calendar.Items.Cast<AppointmentItem>()
                .OrderBy(i => i.Start)
                .Take(count);
            return string.Join("\n", items.Select(i => $"{i.Subject} at {i.Start}"));
        }

        [KernelFunction, Description("Delete an appointment by subject. The function will handle opening of Outlook if not started yet.")]
        public string DeleteAppointment([Description("Subject"), Required] string subject)
        {
            var app = OutlookHelper.GetOrStartOutlook();
            var calendar = app.Session.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
            var appt = calendar.Items.Cast<AppointmentItem>().FirstOrDefault(a => a.Subject == subject);
            if (appt == null) return $"Appointment '{subject}' not found.";
            appt.Delete();
            return $"Appointment '{subject}' deleted.";
        }
    }
}

