using System.Collections.Generic;
using System.Xml.Serialization;

namespace DeskMind.Plugins.WebScraper.Models
{
    public class CollectionConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("selector")]
        public string Selector { get; set; }

        [XmlElement("Field")]
        public List<FieldConfig> Fields { get; set; } = new();
    }
}

