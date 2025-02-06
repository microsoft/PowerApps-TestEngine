
using System.Xml.Linq;
using System.Xml;
using System.Text.Json;

namespace testengine.plugin.copilot
{
    public static class XmlJsonConvertor
    {
        public static string ConvertXmlToJson(string xmlString)
        {
            // Parse the XML string
            XDocument xmlDoc = XDocument.Parse(xmlString);

            // Convert XDocument to XmlDocument
            XmlDocument xmlDocument = new XmlDocument();
            using (var xmlReader = xmlDoc.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }

            // Convert XmlDocument to JSON string
            string jsonString = JsonSerializer.Serialize(ToJson(xmlDocument));

            return jsonString;
        }

        private static object ToJson(XmlNode node)
        {
            // Convert XmlNode to JSON-compatible object
            // Convert XmlNode to JSON-compatible object
            if (node is XmlDocument document)
            {
                // Handle the case where the node is a document
                return ToJson(document.DocumentElement);
            }
            else if (node is XmlElement element)
            {
                var jsonObject = new JsonObject();
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    jsonObject[attribute.Name] = attribute.Value;
                }
                foreach (XmlNode childNode in element.ChildNodes)
                {
                    jsonObject[childNode.Name] = ToJson(childNode);
                }
                return jsonObject;
            }
            else if (node is XmlText text)
            {
                return text.Value;
            }
            return null;
        }
    }
}
