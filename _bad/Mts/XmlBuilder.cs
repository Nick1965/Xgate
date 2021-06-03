using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Oldi.Utility;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace Oldi.Mts
{
	public class XmlTag
	{
		public string Value { get; set; }
		public string Type {get;set;}
		NameValueCollection attrs;
		public XmlTag(string Name, string Value = null, string Type = null)
		{
			this.Value = Value;
			this.Type = Type;
			attrs = new NameValueCollection();
		}
		public XmlTag AddAttr(string Name, string Value)
		{
			attrs.Add(Name, Value);
			return this;
		}
	}


	public class XmlNamespace
	{
		public string Schema { get; set; }
		public string Xmlns { get; set; }
		public string Location { get; set; }
	}
	
	public class XmlBuilder
	{
		public string Root { get { return Root; }}
		public int ErrCode { get { return errCode; } }
		public string ErrDesc { get { return errDesc; } }
		private string root = null;
		Dictionary<string, XmlTag> tags;
		int timeout;
		int errCode = 0;
		string errDesc = "";

		public XmlBuilder(string root, int timeout = 0)
		{
			this.root = root;
			tags = new Dictionary<string, XmlTag>(20);
			this.timeout = timeout;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, string value, string utype = null)
		{
			if (!string.IsNullOrEmpty(value))
				tags.Add(tag, new XmlTag(value, utype));
			return this;
		}


		/// <summary>
		/// Вывод Xml-блока
		/// </summary>
		/// <returns>string</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			
			// Построить заголовок
			sb.AppendFormat(Properties.Settings.Default.Header, Root, timeout > 0? string.Format("\t\t\t\ta_01 = {0}>\r\n", timeout): "");

			// построить блок
			foreach (string tag in tags.Keys)
			{
				XmlTag t = tags[tag];
				if (!string.IsNullOrEmpty(t.Type))
					sb.AppendFormat(Properties.Settings.Default.Tag1, tag, t.Value, t.Type);
				else
					sb.AppendFormat(Properties.Settings.Default.Tag2, tag, t.Value);
				sb.Append("\r\n");
			}
			sb.AppendFormat(Properties.Settings.Default.Footer, Root);

			return errCode == 0? sb.ToString(): null;
		}


		private void ValidationCallback(object sender, ValidationEventArgs args)
		{
			errCode = 400;
			errDesc = args.Message;
		}

		public int Check(string stRequest)
		{
			// Сделаем запрос 0104010
			errDesc = "";
			errCode = 0;

			// Проверка документа
			try
			{
				errDesc = "OK";
				errCode = 0;

				XmlSchemaSet cs = new XmlSchemaSet();
				cs.Add(Properties.Settings.Default.UrlMessages, Properties.Settings.Default.XsdMessages);
				cs.Add(Properties.Settings.Default.UrlConstraints, Properties.Settings.Default.XsdConstraints);

				XmlReaderSettings settings = new XmlReaderSettings();
				settings.ValidationType = ValidationType.Schema;
				settings.Schemas = cs;
				settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

				StringReader sr = new StringReader(stRequest);
				XmlReader reader = XmlReader.Create(sr, settings);

				while (reader.Read()) ;

				reader.Close();
				sr.Close();

			}
			catch (Exception ex)
			{
				errDesc = ex.Message;
				errCode = 400;
			}

			return errCode;

		}
	}
}
