using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Base
{
	public class BaseClass
	{
		Object LockObj = new Object();

		/// <summary>
		/// Возвращает имя Log-файла
		/// </summary>
		/// <returns></returns>
		public virtual string GetLogFileName()
		{
			return "base.log";
		}

		/// <summary>
		/// Название и версия продукта.
		/// </summary>
		public void AssemplyInfo()
		{
			//Assembly asm = Assembly.GetExecutingAssembly();
			Assembly asm = Assembly.GetCallingAssembly();
			AssemblyName asmName = asm.GetName();
			string version = string.Format("v{0}.{1}.{2}.{3}", asmName.Version.Major, asmName.Version.Minor, asmName.Version.Build, asmName.Version.Revision);
			AssemblyProductAttribute[] asmProduct =
				(System.Reflection.AssemblyProductAttribute[])asm.GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), false);
			AssemblyCopyrightAttribute[] asmCopyright =
				(System.Reflection.AssemblyCopyrightAttribute[])asm.GetCustomAttributes(typeof(System.Reflection.AssemblyCopyrightAttribute), false);
			Console.WriteLine("{0} {1} {2}", asmProduct[0].Product, version, asmCopyright[0].Copyright);
			Log(GetLogFileName(), "{0} {1} {2}", asmProduct[0].Product, version, asmCopyright[0].Copyright);

		}

		/// <summary>
		/// Поместить сообщение в очередь лога.
		/// </summary>
		/// <param name="str"></param>
		public void Log(string fmt, params object[] _params)
		{
			string log = GetLogFileName();

			lock (LockObj)
			{
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
					using (sw = new StreamWriter(new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read)))
					{
						if (!string.IsNullOrEmpty(text))
						{
							if (text.Substring(0, 2) == "\r\n")
							{
								sw.WriteLine("\r\n");
								text = text.Remove(0, 2);
								// text = text.Substring(2, text.Length - 3);
							}
							sw.WriteLine("{0} [{1}] {2}",
								DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
								Thread.CurrentThread.ManagedThreadId, text);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("{2} Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}",
						log, ex.Message, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
					Console.WriteLine(ex.StackTrace);
				}

			}
		}


		/// <summary>
		/// Запись строки или буфера в файл лога
		/// </summary>
		/// <param name="msg">Записываемая структура</param>
		public void Log(byte[] buf)
		{
			string log = GetLogFileName();

			lock (LockObj)
			{
				try
				{
					using (FileStream fs = new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read))
					{
						fs.WriteByte((byte)'\r');
						fs.WriteByte((byte)'\n');
						fs.Write(buf, 0, buf.Length);
						fs.WriteByte((byte)'\r');
						fs.WriteByte((byte)'\n');
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("{2} Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}",
						log, ex.Message, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
					Console.WriteLine(ex.StackTrace);
				}
			}

		} //Log

	}
}
