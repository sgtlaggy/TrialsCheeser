using System;
using System.IO;
using System.Xml.Linq;

namespace TrialsCheeser
{
    public static class Config
    {
        private static readonly string Path = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\TrialsCheeserConfig.xml");
        private static XDocument Document;

        private static XElement GetOrCreateElement(XContainer container, string name)
        {
            XElement element = container.Element(name);
            if (element == null)
            {
                element = new XElement(name);
                container.Add(element);
            }
            return element;
        }

        private static XElement GetElementByXpath(string xpath)
        {
            var element = Document.Element("settings");
            foreach (string next in xpath.Split('/'))
            {
                element = GetOrCreateElement(element, next);
            }
            return element;
        }

        public static string Get(string xpath)
        {
            return GetElementByXpath(xpath).Value;
        }

        public static void Set(string xpath, string value)
        {
            var element = GetElementByXpath(xpath);
            element.Value = value.ToString();
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
                Document = XDocument.Load(Path);
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
            Document = new XDocument(
                new XElement("settings",
                    new XElement("lastSession",
                        new XElement("deviceName"),
                        new XElement("onTop"),
                        new XElement("ip"),
                        new XElement("threshold", "5")
                    )
                )
            );
        }
    }
}
