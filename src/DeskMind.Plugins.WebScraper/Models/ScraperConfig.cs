using System.Collections.Generic;
using System.Xml.Serialization;

namespace DeskMind.Plugins.WebScraper.Models
{
    [XmlRoot("ScraperConfig")]
    public class ScraperConfig
    {
        public string Url { get; set; }
        public bool RequiresJavascript { get; set; }
        public string WaitForSelector { get; set; }

        [XmlArray("Fields")]
        [XmlArrayItem("Field")]
        public List<FieldConfig> Fields { get; set; } = new();

        [XmlArray("Collections")]
        [XmlArrayItem("Collection")]
        public List<CollectionConfig> Collections { get; set; } = new();
    }
}

