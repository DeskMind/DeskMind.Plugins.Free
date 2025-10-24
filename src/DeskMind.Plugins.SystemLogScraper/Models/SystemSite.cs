using System.Collections.Generic;

namespace DeskMind.Plugins.SystemLogScraper.Models
{
    public class SystemSite
    {
        public string Name { get; set; }               // e.g., "SystemLogs", "MotorLogs"
        public string RelativePath { get; set; }       // e.g., "/sys/logs"
        public string Description { get; set; }        // Human-readable description for AI
        public List<string> DataTypes { get; set; } = new();  // e.g., ["Errors", "Warnings", "MotorStatus"]

        public string RulesFile { get; set; }    // Optional: Name of the site containing rules for web scraping
    }
}

