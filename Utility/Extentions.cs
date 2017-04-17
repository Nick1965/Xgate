using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Globalization;
using System.Xml.XPath;
using System.IO;

namespace Oldi.Utility
{
	public static class Extentions
		{
		/// <summary>
		/// Метод расширения SqlCommand. Добавляет ненулевой параметр в процедуру.
		/// </summary>
		/// <param name="myCmd"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		public static void Add(this SqlCommand myCmd, String name, Object value)
			{
			if (value == null)
				return;

			switch (value.GetType().Name)
				{
				case "String":
					if ((string)value == "")
						return;
					break;
				case "Char":
					if ((char)value == (char)0)
						return;
					break;
				case "Byte":
					if ((byte)value == 255)
						return;
					break;
				case "Int16":
					if ((short)value == short.MinValue)
						return;
					break;
				case "Int32":
					if ((int)value == int.MinValue)
						return;
					break;
				case "Int64":
					if ((long)value == long.MinValue)
						return;
					break;
				case "Decimal":
					if ((decimal)value == decimal.MinusOne)
						return;
					break;
				case "DateTime":
					if ((DateTime)value == DateTime.MinValue)
						return;
					break;
				}

			if (Settings.LogLevel.IndexOf("SQL") != -1)
				Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "DEBUG: {0}, {1}, {2}", name, value.GetType().Name, value);
			myCmd.Parameters.AddWithValue(name, value);

			}

		/// <summary>
		/// Метод расширения. Добавлени ненулевого параметра в строку
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		public static StringBuilder Append(this StringBuilder sb, string name, Object value = null)
			{
			if (value == null)
				return sb;

			switch (value.GetType().Name)
				{
				case "String":
					if ((string)value == "")
						return sb;
					break;
				case "Char":
					if ((char)value == (char)0)
						return sb;
					break;
				case "Byte":
					if ((byte)value == 255)
						return sb;
					break;
				case "Int16":
					if ((short)value == short.MinValue)
						return sb;
					break;
				case "Int32":
					if ((int)value == int.MinValue)
						return sb;
					break;
				case "Int64":
					if ((long)value == long.MinValue)
						return sb;
					break;
				case "Decimal":
					if ((decimal)value == decimal.MinusOne)
						return sb;
					value = ((decimal)value).ToString("0.00", CultureInfo.InvariantCulture);
					break;
				case "DateTime":
					if ((DateTime)value == DateTime.MinValue)
						return sb;
					value = ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
					break;
				}

			return sb.AppendFormat(" {0}={1}", name, value);
			}

		/// <summary>
		/// Метод расширения. Добавлени ненулевого параметра в строку
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		public static StringBuilder Append2(this StringBuilder sb, string name, Object value = null)
			{
			if (value == null)
				return sb;

			switch (value.GetType().Name)
				{
				case "String":
					if ((string)value == "")
						return sb;
					break;
				case "Char":
					if ((char)value == (char)0)
						return sb;
					break;
				case "Byte":
					if ((byte)value == 255)
						return sb;
					break;
				case "Int16":
					if ((short)value == short.MinValue)
						return sb;
					break;
				case "Int32":
					if ((int)value == int.MinValue)
						return sb;
					break;
				case "Int64":
					if ((long)value == long.MinValue)
						return sb;
					break;
				case "Decimal":
					if ((decimal)value == decimal.MinusOne)
						return sb;
					value = ((decimal)value).ToString("0.00", CultureInfo.InvariantCulture);
					break;
				case "DateTime":
					if ((DateTime)value == DateTime.MinValue)
						return sb;
					value = ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
					break;
				}

			return sb.AppendFormat("{0}-{1}", name, value);
			}

		/// <summary>
		/// Метод расширения. Добавлени ненулевого параметра в строку
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		public static StringBuilder Append(this StringBuilder sb, string name, DateTime? value)
			{
			if (value == null)
				return sb;
			return sb.AppendFormat(" {0}={1}", name, ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.zzz", CultureInfo.InvariantCulture));
			}

		/// <summary>
		/// Метод расширения. Добавлени ненулевого параметра в строку
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		public static StringBuilder Append2(this StringBuilder sb, string name, DateTime? value)
			{
			if (value == null)
				return sb;
			return sb.AppendFormat("{0}-{1}", name, ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.zzz", CultureInfo.InvariantCulture));
			}

		/// <summary>
		/// Метод расширения. Добавление ненулевой строки параметров в строку с \r\n на конце
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		/// <returns></returns>
		public static StringBuilder AppendLine(this StringBuilder sb, string name, Object value = null)
			{
			if (value == null)
				return sb;

			switch (value.GetType().Name)
				{
				case "String":
					if ((string)value == "")
						return sb;
					break;
				case "Char":
					if ((char)value == (char)0)
						return sb;
					break;
				case "Byte":
					if ((byte)value == 255)
						return sb;
					break;
				case "Int16":
					if ((short)value == short.MinValue)
						return sb;
					break;
				case "Int32":
					if ((int)value == int.MinValue)
						return sb;
					break;
				case "Int64":
					if ((long)value == long.MinValue)
						return sb;
					break;
				case "Decimal":
					if ((decimal)value == decimal.MinusOne)
						return sb;
					value = ((decimal)value).ToString("0.00", CultureInfo.InvariantCulture);
					break;
				case "DateTime":
					if ((DateTime)value == DateTime.MinValue)
						return sb;
					value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
					break;
				}

			return sb.AppendFormat("{0}={1}\r\n", name, value);
			}


		/// <summary>
		/// Метод расширения. Добавление ненулевой строки параметров в строку с \r\n на конце
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="name">Имя параметра</param>
		/// <param name="value">Значение параметра</param>
		/// <returns></returns>
		public static StringBuilder AppendLine(this StringBuilder sb, string name, DateTime? value)
			{
			if (value == null)
				return sb;
			return sb.AppendFormat("{0}={1}\r\n", name, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.zzz", CultureInfo.InvariantCulture));
			}


		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа byte</param>
		public static void GetValue(this SqlDataReader dr, string pname, out byte value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetByte(c);
			else
				value = 255;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа short</param>
		public static void GetValue(this SqlDataReader dr, string pname, out short value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetInt16(c);
			else
				value = short.MinValue;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа int</param>
		public static void GetValue(this SqlDataReader dr, string pname, out int value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetInt32(c);
			else
				value = int.MinValue;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа long</param>
		public static void GetValue(this SqlDataReader dr, string pname, out long value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetInt64(c);
			else
				value = long.MinValue;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа decimal</param>
		public static void GetValue(this SqlDataReader dr, string pname, out decimal value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetDecimal(c);
			else
				value = decimal.MinValue;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа bool</param>
		public static void GetValue(this SqlDataReader dr, string pname, out bool value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetBoolean(c);
			else
				value = false;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа DateTime</param>
		public static void GetValue(this SqlDataReader dr, string pname, out DateTime value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetDateTime(c);
			else
				value = DateTime.MinValue;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа DateTime</param>
		public static void GetValue(this SqlDataReader dr, string pname, out DateTime? value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetDateTime(c);
			else
				value = null;
			}
		/// <summary>
		/// Читает значение из столбца pname
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <param name="pname">Имя столбца</param>
		/// <param name="value">Возвращаемое значения типа srting</param>
		public static void GetValue(this SqlDataReader dr, string pname, out string value)
			{
			int c = dr.GetOrdinal(pname);
			if (!dr.IsDBNull(c))
				value = dr.GetString(c);
			else
				value = null;
			}


		/// <summary>
		/// Преобразовывает в целое число
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static long ToLong(this string s)
			{
			long x;
			if (long.TryParse(s, out x))
				return x;
			else
				return -1;
			}

        /// <summary>
        /// Преобразовывает в целое число
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int ToInt(this string s)
        {
            int x;
            if (int.TryParse(s, out x))
                return x;
            else
                return -1;
        }


        /// <summary>
        /// Преобразует в decimal
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static decimal ToDecimal (this string s)
        {
            decimal x;
            if (!decimal.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out x))
                return decimal.MinusOne;
            else
                return x;
        }

        /// <summary>
        /// Преобразовывает в строку число с десятичной точкой
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string AsCurrency(this decimal d)
			{
			return d.ToString("0.00", CultureInfo.InvariantCulture);
			}


        /// <summary>
        /// Преобразовывает Дату в Строку канонического вида
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string AsCF(this DateTime d)
        {
            return d.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Преобразовывает Сумму в строку канонического вида
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string AsCF(this decimal d)
        {
            return d.ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Преобразовывает Сумму в строку канонического вида
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string AsCF(this decimal? d)
        {
            return d != null? d.Value.ToString("0.00", CultureInfo.InvariantCulture): "0.00";
        }

        /// <summary>
        /// Проверяет входит ли символ с в строку s
        /// </summary>
        /// <param name="s">Исходная строка</param>
        /// <param name="c">Массив символов</param>
        /// <returns></returns>
        public static bool Contains(this string s, Char[] c)
        {
            if (!string.IsNullOrEmpty(s))
                foreach (var cc in c)
                    if (s.Contains<char>(c[0]))
                        return true;
            return false;
        }
        
        /// <summary>
        /// Извлекает выражение Xpath[expression] из self
        /// </summary>
        /// <param name="self">Xml-документ. содержащий expression</param>
        /// <param name="Expression">Выражение Xpath, которое нужно ихзвлечь</param>
        /// <returns></returns>
        public static string XPath(this string self, string Expression)
        {

            if (string.IsNullOrEmpty(self))
                return "";

            try
            {

                XPathDocument doc = new XPathDocument(new StringReader(self.ToLower()));
                XPathNavigator nav = doc.CreateNavigator();
                // XPathNodeIterator items = nav.Select(expr);
                XPathNavigator node = nav.SelectSingleNode(Expression.ToLower());

                // Log("****************************************************");
                // Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
                // Log("****************************************************");


                // Console.WriteLine("{0}={1}", expr, node.Select(expr.ToLower()).Current.Value);
                return node.Select(Expression.ToLower()).Current.Value;

            }
            catch (Exception /*ex*/)
            {
                // Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
                // Log(answer);
            }

            return "";

        }

    }
}
