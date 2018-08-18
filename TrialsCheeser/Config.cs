using System;
using System.IO;
using System.Xml;

namespace TrialsCheeser
{
    public static class Config
    {
        private static readonly string Path = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\TrialsCheeserConfig.xml");
        private static XmlDocument Document;

        public static string Get(string xpath)
        {
            return Document.SelectSingleNode($"settings/{xpath}").InnerText;
        }

        public static void Set(string xpath, string value)
        {
            Document.SelectSingleNode($"settings/{xpath}").InnerText = value;
            Save();
        }

        private static bool FileExists()
        {
            return File.Exists(Path);
        }

        public static void Load()
        {
            if (FileExists())
            {
                Document = new XmlDocument();
                Document.Load(Path);
            }
            else
            {
                LoadDefault();
            }
        }

        private static void Save()
        {
            Document.Save(Path);
        }

        private static void LoadDefault()
        {
            Document = new XmlDocument();
            var settings = Document.CreateNode(XmlNodeType.Element, "settings", string.Empty);
            Document.AppendChild(settings);
            var lastSession = Document.CreateNode(XmlNodeType.Element, "lastSession", string.Empty);
            settings.AppendChild(lastSession);
            lastSession.AppendChild(Document.CreateNode(XmlNodeType.Element, "deviceName", string.Empty));
            lastSession.AppendChild(Document.CreateNode(XmlNodeType.Element, "ip", string.Empty));
            var threshold = Document.CreateNode(XmlNodeType.Element, "threshold", string.Empty);
            threshold.InnerText = "5";
            lastSession.AppendChild(threshold);
        }
    }
}
