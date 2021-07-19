using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Reflection;
using Oldi.Utility;
using System.Xml.XPath;
using System.Xml;
using System.Text.RegularExpressions;

namespace Oldi.Net
{

    public static class XPath
    {
        /// <summary>
        /// Извлекает параметр из ответа в виде XPath запроса
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static String GetString(string answer, string expr)
        {

            string result = "";

            try
            {

                string e = expr.ToLower();

                XmlDocument x = new XmlDocument();
                x.LoadXml(answer.ToLower());

                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                XmlTextWriter xw = new XmlTextWriter(sw);

                xw.Formatting = Formatting.Indented;

                x.WriteContentTo(xw);
                // x.Save(xw);


                string pattern = @"xmlns=""(.*)""";
                Regex regex = new Regex(pattern);
                string xDoc = sb.ToString();

                // Console.WriteLine($"Поиск для образца: {pattern}");
                foreach (Match match in regex.Matches(sb.ToString()))
                {
                    // Console.WriteLine("Found '{0}' at position {1}", match.Value, match.Index);
                    xDoc = xDoc.Replace(match.Value, "");
                    // Console.WriteLine($"Traget: \r\n{xDoc}\r\n");
                    break;
                }

                // XPathDocument doc = new XPathDocument(new StringReader(answer.ToLower()));


                XPathDocument doc = new XPathDocument(new StringReader(xDoc));
                XPathNavigator nav = doc.CreateNavigator();
                // XPathNodeIterator items = nav.Select(expr);
                XPathNavigator node = nav.SelectSingleNode(e);

                // Log("****************************************************");
                // Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
                // Log("****************************************************");


                // Console.WriteLine("{0}={1}", expr, node.Select(expr).Current.Value);
                // Console.WriteLine("{0} of {1}", expr, node.Select(expr));
                result = node.Select(e).Current.Value;

            }
            catch (Exception ex)
            {
                Utility.Log("Log\\oldigw.log", $"{expr}: {ex.ToString()}");
                Utility.Log("Log\\oldigw.log", $"{answer}");
            }

            return result;
        }


        /// <summary>
        /// Извлекает параметр int? из ответа в виде XPath запроса
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static int? GetInt(string answer, string expr)
        {

            int? result = null;
            string sResult = GetString(answer, expr);
            if (!string.IsNullOrEmpty(sResult))
                result = sResult.ToInt();

            return result;
        }

        /// <summary>
        /// Извлекает параметр long? из ответа в виде XPath запроса
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static long? GetLong(string answer, string expr)
        {

            long? result = null;

            string sResult = GetString(answer, expr);
            if (!string.IsNullOrEmpty(sResult))
                result = sResult.ToLong();

            return result;
        }

        /// <summary>
        /// Извлекает параметр decimal? из ответа в виде XPath запроса
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static decimal? GetDec(string answer, string expr)
        {

            decimal? result = null;

            string sResult = GetString(answer, expr);
            if (!string.IsNullOrEmpty(sResult))
                result = sResult.ToDecimal();

            return result;
        }


    }

    /// <summary>
    /// Клас функций логгера
    /// </summary>
    public static class Utility
    {

        static Object LockObj = new Object();

        public static int timeStamp()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static void Log(string log, string text)
		{
            if (log.Substring(0, 1) == "{")
				throw new ApplicationException($"Не задан файл журнала {log}");

			lock (LockObj)
			{
				StreamWriter sw = null;
				try
				{
					using (sw = new StreamWriter(new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read)))
					{
						if (string.IsNullOrEmpty(text))
						{
							text = "\r\n";
							sw.WriteLine("\r\n");
						}
						else
						{
							if (text.Substring(0, 2) == "\r\n")
							{
								sw.WriteLine("\r\n");
								text = text.Remove(0, 2);
								// text = text.Substring(2, text.Length - 3);
							}
							sw.WriteLine("{0} [{1:D2}] {2}",
								DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
								Thread.CurrentThread.ManagedThreadId, text);
						}
					}
				}
				catch (Exception ex)
				{
					// Console.WriteLine("{2} Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}", log, ex.Message, XConvert.AsDate2(DateTime.Now));
					// Console.WriteLine(ex.StackTrace);
				lock (LockObj)
					{
					Log(".\\error.log", "Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}\r\n{2}", log, ex.Message, ex.StackTrace);
					Log(".\\error.log", "Оригинальное сообщение:\r\n{0}", text);
					}
				}
			}
		}

		/// <summary>
        /// Поместить сообщение в очередь лога.
        /// </summary>
        /// <param name="str"></param>
        public static void Log(string log, string fmt, params object[] _params)
        {
			if (log.Substring(0, 1) == "{")
				throw new ApplicationException("Не задан файл журнала");

            string text = "";
            try
            {
                text = string.Format(fmt, _params);
            }
            catch (Exception ex)
            {
                Console.WriteLine(fmt);
                Console.WriteLine(ex.ToString());
            }
            StreamWriter sw = null;
                
			try
				{

				lock (LockObj)
					{
					using (FileStream fs = new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read))
					using (sw = new StreamWriter(fs))
						{
						if (!string.IsNullOrEmpty(text))
							{
							if (text.Substring(0, 2) == "\r\n")
								{
								sw.WriteLine("\r\n");
								text = text.Remove(0, 2);
								// text = text.Substring(2, text.Length - 3);
								}
							sw.WriteLine("{0} [{1:D2}] {2}",
								DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
								Thread.CurrentThread.ManagedThreadId, text);
							}
						}
					}
	
				}
				catch (Exception ex)
				{
					// Console.WriteLine("{2} Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}", log, ex.Message, XConvert.AsDate2(DateTime.Now));
					// Console.WriteLine(ex.StackTrace);
					lock(LockObj)
						{
						Log(".\\error.log", "Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}\r\n{2}", log, ex.Message, ex.StackTrace);
						Log(".\\error.log", "Оригинальное сообщение:\r\n{0}", text);
						}
						
				}
        }


        /// <summary>
        /// Запись строки или буфера в файл лога
        /// </summary>
        /// <param name="msg">Записываемая структура</param>
        public static void Log(string log, byte[] buf)
        {

            lock (LockObj)
            {
                try
                {
                    using (FileStream fs = new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        // fs.WriteByte((byte)'\r');
                        // fs.WriteByte((byte)'\n');
                        fs.Write(buf, 0, buf.Length);
                        // fs.WriteByte((byte)'\r');
                        // fs.WriteByte((byte)'\n');
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}", log, ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

        } //Log
    }

}
