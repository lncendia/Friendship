using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Friendship
{
    public class Settings
    {
        public string TgToken { get; set; }
        public string QiwiToken { get; set; }

        public static Settings Read()
        {
            FileStream fileStream = new FileStream("cfg.xml", FileMode.Open);
            XmlSerializer xml = new XmlSerializer(typeof(Settings));
            return (Settings)xml.Deserialize(fileStream);
        }
    }
}
