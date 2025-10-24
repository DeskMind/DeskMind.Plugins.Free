using System;
using System.Collections.Generic;
using System.Linq;

namespace DeskMind.Plugins.SystemLogScraper.Models
{
    public class SystemInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Manufacturer { get; set; }

        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; } = "http";
        public List<SystemSite> Sites { get; set; } = new List<SystemSite>();

        public string BaseUrl => $"{Protocol}://{Hostname}:{Port}";

        public string GetSiteUrl(string siteName)
        {
            var site = Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
            if (site == null) return BaseUrl;
            return $"{BaseUrl.TrimEnd('/')}/{site.RelativePath.TrimStart('/')}";
        }

        public IEnumerable<SystemSite> GetAvailableSites() => Sites;
    }
}

