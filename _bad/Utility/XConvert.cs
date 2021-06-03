using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Oldi.Utility
{
	public static class XConvert
	{
		/// <summary>
		/// Целое - 6 знаков
		/// </summary>
		/// <param name="x">Короткое (In16) целое</param>
		/// <returns>Строка</returns>
		public static string AsInteger(Int16 x)
		{
			return x.ToString("D6");
		}

		/// <summary>
		/// Длинное целое - 11 знаков
		/// </summary>
		/// <param name="x">Длинное (Int32) целое</param>
		/// <returns>Строка</returns>
		public static string AsLongInt(Int32 x)
		{
			return x == int.MinValue? "": x.ToString("D11");
		}

		/// <summary>
		/// Двойное длинное целое - 20 знаков
		/// </summary>
		/// <param name="x">Двойное длинное (Int64) целое</param>
		/// <returns>Строка</returns>
		public static string AsLongLong(Int64 x)
		{
			return x == long.MinValue? "": x.ToString("D20");
		}

		/// <summary>
		/// С плавающей точкой - 21 знак
		/// </summary>
		/// <param name="x">Двойное с плавающей точкой (double)</param>
		/// <returns>Строка</returns>
		public static string AsFloat(double x)
		{
			string r = x.ToString("G", CultureInfo.InvariantCulture);
			StringBuilder rb = new StringBuilder();
			rb.Append('0', 21);
			rb.Remove(21 - r.Length, r.Length);
			rb.Append(r);
			return rb.ToString();
		}

		/// <summary>
		/// С фиксированной точкой - 21 знак с лидирующими нулями, 2 знака после точки
		/// </summary>
		/// <param name="x">Двойное с плавающей точкой (double)</param>
		/// <returns>Строка</returns>
		public static string AsDecimal(double x)
		{
			return x.ToString("000000000000000000.00", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// С фиксированной точкой, без лидирующих нулей, 2 знака после точки
		/// </summary>
		/// <param name="x">Десятичное (decimal)</param>
		/// <returns>Строка</returns>
		public static string AsAmount(decimal x)
		{
			return x == decimal.MinusOne? "": x.ToString("0.00", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// С фиксированной точкой, без лидирующих нулей, 2 знака после точки
		/// </summary>
		/// <param name="x">object</param>
		/// <returns>Строка</returns>
		public static string AsAmount(object x)
			{
			decimal y;
			try
				{
				y = Convert.ToDecimal(x);
				}
			catch (Exception)
				{
				y = decimal.MinusOne;
				}
			return AsAmount(y);
			}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDate(DateTime x)
		{
			return x != DateTime.MinValue ? x.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) : "";
		}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDate(DateTime? x)
		{
			return x != null ? x.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) : "";
		}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss.mmmm
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDate2(DateTime x)
		{
			return x != DateTime.MinValue ? x.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) : "";
		}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss+07:00
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDateTZ(DateTime x)
		{
			return x != DateTime.MinValue ? x.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture) : "";
		}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss+07:00
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDateTZ(DateTime x, int tz)
		{
			return x != DateTime.MinValue? 
				string.Format("{0}{1}{2:D2}:00", x.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), tz >= 0 ? "+" : "-", tz): 
				"";
		}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss+HH:00
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDateTZ(DateTime? x, int tz)
		{
			return x != null? 
				string.Format("{0}{1}{2:D2}:00", x.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), tz >= 0 ? "+" : "-", tz): 
				"";
		}

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss+06:00 (Урал)
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public static string AsDateTZ6(DateTime x)
		{
			string s = "";
			if (x != DateTime.MinValue)
			{
				DateTime y = x.AddHours(-1);
				s = y.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + "+06:00";
			}
			return s;
		}

		/// <summary>
		/// Строка переменной длины (первые 3 знака - дина строки)
		/// </summary>
		/// <param name="x">Строка</param>
		/// <returns>Строка</returns>
		public static string AsVarString(string x)
		{
			return string.IsNullOrEmpty(x)? "": x.Length.ToString("D3") + x;
		}

		/// <summary>
		/// Строка фиксированной длины
		/// </summary>
		/// <param name="x">Строка</param>
		/// <param name="length">Длина</param>
		/// <returns>Строка</returns>
		public static string AsString(String x, int length)
		{
			if (!string.IsNullOrEmpty(x) && length > 0)
			{
				string format = "{0," + length.ToString() + "}";
				return String.Format(format, x);
			}
			else
				return "";
		}

		/// <summary>
		/// Преобразует строку вида dddddd.dddd в decimal
		/// </summary>
		/// <param name="s">строка цифр с точкой с возможным символом +-</param>
		/// <returns>результат преобразования</returns>
		public static decimal ToDecimal(string s)
			{
			decimal a;
			if (!decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out a))
				a = decimal.MinusOne;
			return a;
			}
	}

}
