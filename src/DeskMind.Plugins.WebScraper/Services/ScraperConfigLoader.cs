using System.IO;
using System.Xml.Serialization;

using DeskMind.Plugins.WebScraper.Models;

namespace DeskMind.Plugins.WebScraper.Services
{
    public static class ScraperConfigLoader
    {
        public static ScraperConfig Load(string filePath)
        {
            var serializer = new XmlSerializer(typeof(ScraperConfig));
            using var reader = new StreamReader(filePath);
            return (ScraperConfig)serializer.Deserialize(reader);
        }
    }
}

