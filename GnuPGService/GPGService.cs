using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Diagnostics;
using System.Globalization;

namespace GPG
	{
	
	class GPGService: IGPGService
		{
		public GPGService()
			{
			}

		/// <summary>
		/// Создаёт gpg-подпись документа
		/// </summary>
		/// <param name="PrivateKeyName"></param>
		/// <param name="PublicKeyName"></param>
		/// <param name="IncludeData"></param>
		/// <param name="Text"></param>
		/// <returns></returns>
		GpgResult CreateSign(string PrivateKeyName, string PublicKeyName, bool IncludeData, string Text)
			{
			GpgResult Result = new GpgResult();

			// Сформируем имя файла данных
			int num = 0;
			string DataSetName = "";
			// Создадим файл
			while (true)
				{
				DataSetName = string.Format("data\\{0}{1,d3}.dat", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.zzz", CultureInfo.InvariantCulture), num++);
				try
					{
					using (FileStream fs = new FileStream(DataSetName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
					using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding(1251)))
						sw.Write(Text);
					break;
					}
				catch
					{
					// Произойдёт если файл уже существуем, тгда увеличим num...
					}
				}
	
			// Создаём подпись.
			ProcessStartInfo Psi = new ProcessStartInfo();
			Psi.FileName = "cmd.exe";
			Psi.Arguments = string.Format("/C CreateSign0.cmd {0}", DataSetName);
			Process.Start(Psi);

			Process[] Proc = Process.GetProcessesByName("gpg");
			Proc[0].WaitForExit(10000);
			int ExitCode = Proc[0].ExitCode;

			// Читаем подпись
			using (StreamReader sr = new StreamReader(DataSetName + ".asc"))
				Result.Message = HttpUtility.UrlEncode(sr.ReadToEnd());
	
			Log("Sign={0}", Result.Message);

			return Result;
			}


		/// <summary>
		/// Запись в журнал
		/// </summary>
		/// <param name="fmt"></param>
		/// <param name="items"></param>
		void Log(string fmt, params object[] items)
			{
			Oldi.Net.Utility.Log("Log\\GpfService.log", fmt, items);
			}

		}

	}
