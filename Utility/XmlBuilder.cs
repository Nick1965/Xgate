using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace Oldi.Utility
{
	public class XmlNameSpace
	{
		public string Name { get; set; }
		public string Value { get; set; }
		public string Location{get;set;}
		public XmlNameSpace(string Name, string Value = null, string Location = null)
		{
			this.Name = Name;
			this.Value = Value;
			this.Location = Location;
		}
	}


	public class XmlBuilder
	{
		public string Root { get { return Root; }}
		private string root = null;
		Dictionary<string, string> tags;
		int timeout;

		public XmlBuilder(string root, int timeout = 0)
		{
			this.root = root;
			tags = new Dictionary<string, string>(20);
			this.timeout = timeout;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, string value)
		{
			if (!string.IsNullOrEmpty(value))
				tags.Add(tag, value);
			return this;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, byte value)
		{
			if (value != 255)
				tags.Add(tag, value.ToString());
			return this;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, short value)
		{
			if (value != -1)
				tags.Add(tag, value.ToString());
			return this;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, int value)
		{
			if (value != -1)
				tags.Add(tag, value.ToString());
			return this;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, long value)
		{
			if (value != -1)
				tags.Add(tag, value.ToString());
			return this;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, decimal value)
		{
			if (value != decimal.MinusOne)
				tags.Add(tag, XConvert.AsAmount(value));
			return this;
		}

		/// <summary>
		/// Добавить тэг
		/// </summary>
		/// <param name="tag">Имя тэга</param>
		/// <param name="value">значение</param>
		/// <returns>XmlBuilder</returns>
		public XmlBuilder AddTag(string tag, DateTime value)
		{
			if (value != DateTime.MinValue)
				tags.Add(tag, XConvert.AsDate(value));
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
			sb.Append("<?xml version = \"1.0\" encoding = \"UTF-8\"?>");
			sb.AppendFormat("<{0}\txmlns = \"http://schema.mts.ru/ESPP/AgentPayments/Protocol/Messages/v5_02\"\r\n", Root);
			sb.Append("\t\t\t\txmlns:espp-constraints = \"http://schema.mts.ru/ESPP/Core/Constraints/v5_02\"\r\n");
			sb.Append("\t\t\t\txmlns:xsi = \"http://www.w3.org/2001/XMLSchema-instance\"\r\n");
			sb.Append("\t\t\t\txsi:schemaLocation = \"http://schema.mts.ru/ESPP/AgentPayments/Protocol/Messages/v5_02 ESPP_AgentPayments_Protocol_Messages_v5_02.xsd\r\n");
			sb.Append("\t\t\t\t\t\t\t\t\t\thttp://schema.mts.ru/ESPP/Core/Constraints/v5_02 ESPP_Core_Constraints_v5_02.xs\"\r\n");
			if (timeout != 0 && timeout != -1)
				sb.AppendFormat("\t\t\t\ta_01 = \"{0}\">\r\n", timeout);
			else
				sb.Append(">\r\n");
			return base.ToString();
		}


	}
}
