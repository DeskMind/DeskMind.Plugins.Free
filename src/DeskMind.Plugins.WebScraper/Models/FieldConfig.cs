using System.Xml.Serialization;

namespace DeskMind.Plugins.WebScraper.Models
{
    public class FieldConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("selector")]
        public string Selector { get; set; }

        [XmlAttribute("attribute")]
        public string Attribute { get; set; } = "innerText";
    }
}

