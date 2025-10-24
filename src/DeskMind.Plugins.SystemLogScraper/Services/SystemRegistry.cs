using DeskMind.Plugins.SystemLogScraper.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DeskMind.Plugins.SystemLogScraper.Services
{
    public class SystemRegistry
    {
        private readonly List<SystemInfo> _systems = new List<SystemInfo>();

        public void LoadFromXml(string xmlPath)
        {
            if (!File.Exists(xmlPath)) return;

            var serializer = new XmlSerializer(typeof(List<SystemInfo>), new XmlRootAttribute("Systems"));
            using var stream = File.OpenRead(xmlPath);
            var systems = (List<SystemInfo>)serializer.Deserialize(stream);
            _systems.Clear();
            _systems.AddRange(systems);
        }

        public void SaveToXml(string xmlPath)
        {
            var serializer = new XmlSerializer(typeof(List<SystemInfo>), new XmlRootAttribute("Systems"));
            using var stream = File.Create(xmlPath);
            serializer.Serialize(stream, _systems);
        }

        public SystemInfo GetSystem(string name)
            => _systems.FirstOrDefault(s => s.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

        public IEnumerable<SystemInfo> GetAllSystems() => _systems;
    }
}

