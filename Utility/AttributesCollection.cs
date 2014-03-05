using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Oldi.Utility
{
	public class AttributesCollection : Dictionary<string, string>
	{

		/// <summary>
		/// Добавляет пару ключ/значение
		/// </summary>
		/// <param name="attr">Атрибут XAttribute</param>
		public void Add(XAttribute attr)
		{
			Add((string)attr.Name.LocalName, (string)attr.Value);
		}

		/// <summary>
		/// Добавляет коллецию из AttributesCollection
		/// </summary>
		/// <param name="src">Колленкция AttributeCollection</param>
		public void Add(AttributesCollection src)
		{
			if (src != null)
				foreach (string key in src.Keys)
					Add(key, src[key]);
		}
	
		/// <summary>
		/// Сериализация коллекции атрибутов
		/// </summary>
		/// <returns>Строка XML</returns>
		public string SaveToXml()
		{
			StringBuilder at = new StringBuilder();
			if (Count > 0)
			{
				at.Append("<attributes>\r\n");
				foreach (string key in Keys)
					at.AppendFormat("<attribute name=\"{0}\" value=\"{1}\" />\r\n", key, this[key]);
				at.Append("</attributes>\r\n");
			}

			return Count > 0 ? at.ToString() : null;
		}

		/// <summary>
		/// Загрузка коллекции из XML
		/// </summary>
		/// <param name="src"></param>
		public void LoadFromXml(string src)
		{
			if (!string.IsNullOrEmpty(src))
			{
				XDocument doc = XDocument.Parse(src);

				if (doc != null)
					foreach (XElement el in doc.Element("attributes").Elements())
					{
						if ((string)el.Name.ToString() == "attribute")
							Add((string)el.Attribute("name").Value, (string)el.Attribute("value").Value);
					}
			}
		}
	
	}
}
