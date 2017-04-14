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

                XPathDocument doc = new XPathDocument(new StringReader(answer.ToLower()));
                XPathNavigator nav = doc.CreateNavigator();
                // XPathNodeIterator items = nav.Select(expr);
                XPathNavigator node = nav.SelectSingleNode(expr.ToLower());

                // Log("****************************************************");
                // Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
                // Log("****************************************************");


                // Console.WriteLine("{0}={1}", expr, node.Select(expr.ToLower()).Current.Value);
                result = node.Select(expr.ToLower()).Current.Value;

            }
            catch (Exception) { }

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

            try
            {

                XPathDocument doc = new XPathDocument(new StringReader(answer.ToLower()));
                XPathNavigator nav = doc.CreateNavigator();
                // XPathNodeIterator items = nav.Select(expr);
                XPathNavigator node = nav.SelectSingleNode(expr.ToLower());

                // Log("****************************************************");
                // Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
                // Log("****************************************************");


                // Console.WriteLine("{0}={1}", expr, node.Select(expr.ToLower()).Current.Value);
                if (!string.IsNullOrEmpty(node.Select(expr.ToLower()).Current.Value))
                    result = node.Select(expr.ToLower()).Current.Value.ToInt();

            }
            catch (Exception) { }

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

            try
            {

                XPathDocument doc = new XPathDocument(new StringReader(answer.ToLower()));
                XPathNavigator nav = doc.CreateNavigator();
                // XPathNodeIterator items = nav.Select(expr);
                XPathNavigator node = nav.SelectSingleNode(expr.ToLower());

                // Log("****************************************************");
                // Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
                // Log("****************************************************");


                // Console.WriteLine("{0}={1}", expr, node.Select(expr.ToLower()).Current.Value);
                if (!string.IsNullOrEmpty(node.Select(expr.ToLower()).Current.Value))
                    result = node.Select(expr.ToLower()).Current.Value.ToInt();

            }
            catch (Exception) { }

            return result;
        }
    }

    public static class Utility
    {

        static Object LockObj = new Object();

        public static void Log(string log, string text)
		{
			if (log.Substring(0, 1) == "{")
				throw new ApplicationException("Не задан файл журнала");

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
			
            string text = string.Format(fmt, _params.Length > 0 ? _params[0] : "",
                _params.Length > 1 ? _params[1] : "",
                _params.Length > 2 ? _params[2] : "",
                _params.Length > 3 ? _params[3] : "",
                _params.Length > 4 ? _params[4] : "",
                _params.Length > 5 ? _params[5] : "",
                _params.Length > 6 ? _params[6] : "",
                _params.Length > 7 ? _params[7] : "",
                _params.Length > 8 ? _params[8] : "",
                _params.Length > 9 ? _params[9] : "",
                _params.Length > 10 ? _params[10] : "",
                _params.Length > 11 ? _params[11] : "",
                _params.Length > 12 ? _params[12] : "",
                _params.Length > 13 ? _params[13] : "",
                _params.Length > 14 ? _params[14] : "",
                _params.Length > 15 ? _params[15] : "");
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
