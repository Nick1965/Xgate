using System.Xml.Linq;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Globalization;

namespace Oldi.Utility
{
	public class Config
	{
		public static NameValueCollection AppSettings { get { return appSettings; } }
		public static Dictionary<string, NameValueCollection> Providers { get { return providers; } }

		private static NameValueCollection appSettings = new NameValueCollection();
		private static Dictionary<string, NameValueCollection> providers = new Dictionary<string, NameValueCollection>();
		private static int code = 0;

		/// <summary>
		/// Разбор входного XML-запроса
		/// </summary>
		/// <param name="stResponse"></param>
		/// <returns></returns>
		public static int Load()
		{
			// Log("\r\nРазбирается запрос\r\n{0}\r\n", stSource);

			try
			{
				XDocument doc = XDocument.Load(".\\OldiGW.xml");
				string provider = "";

				// Очистить колекцию параметров
				appSettings.Clear();
				// Очитска списка поставщиков услуг
				providers.Clear();
				// Финансовый контроль
				Settings.checkedProviders.Clear();
				
				// Выносится в отдельный файл
				// Settings.excludes.Clear();

				if (doc.Element("Configuration").HasElements)
				{
					foreach (XElement el in doc.Root.Elements())
					{
						string name = el.Name.LocalName;
						string value = el.Value;
						// Console.WriteLine("Section: {0}", name);
						switch (el.Name.LocalName)
						{
							case "Provider":
								NameValueCollection nav = new NameValueCollection();
								foreach (XAttribute a in el.Attributes())
								{
									// Console.WriteLine("\t{0}={1}", a.Name.LocalName, a.Value);
									if (a.Name.LocalName == "name") provider = a.Value;
									nav.Set(a.Name.LocalName, a.Value);
								}
								if (string.IsNullOrEmpty(provider))
								{
									Console.WriteLine(Properties.Resources.ErrMissingProviderName);
									throw new ApplicationException(Properties.Resources.ErrMissingProviderName);
								}
								providers.Add(provider, nav);
								break;
							case "AppSettings":
								foreach (XElement add in el.Elements())
								{
									string add_name = add.Name.LocalName;
									string add_key = (string)add.Attribute("key");
									string add_value = (string)add.Attribute("value");
									// Console.WriteLine("\t{0}={1}", add_key, add_value);
									appSettings.Set(add_key, add_value);
								}
								break;
							case "Smtp":
								foreach (XElement s in el.Elements())
								{
									switch (s.Name.LocalName)
									{
										case "host":
											Settings.Smtp.Host = s.Value.ToString();
											break;
										case "port":
											Settings.Smtp.Port = s.Value.ToString();
											break;
										case "user":
											Settings.Smtp.User = s.Value.ToString();
											break;
										case "password":
											Settings.Smtp.Password = s.Value.ToString();
											break;
									}
								}
								break;
							case "FinancialCheck":
								foreach (XElement s in el.Elements())
									{
									switch (s.Name.LocalName)
										{
										case "AmountLimit":
											Settings.amountLimit = XConvert.ToDecimal(s.Value.ToString());
											break;
										case "AmountDelay":
											Settings.amountDelay = int.Parse(s.Value.ToString());
											break;
										case "Providers":
											IEnumerable<XElement> elements =
												from e in s.Elements("Provider")
												select e;
											foreach (XElement e in elements)
												{
												// Console.WriteLine(e.Name.LocalName);
												if (e.Name.LocalName == "Provider")
													{
													string Name = "";
													string Service = "";
													string Gateway = "";
													decimal Limit = decimal.MinusOne;
													foreach (var item in e.Attributes())
														{
														if (item.Name.LocalName == "Name")
															Name = item.Value.ToString();
														if (item.Name.LocalName == "Service")
															Service = item.Value.ToString();
														if (item.Name.LocalName == "Gateway")
															Gateway = item.Value.ToString();
														if (item.Name.LocalName == "Limit")
															Limit = XConvert.ToDecimal(item.Value.ToString());
														}
													Settings.checkedProviders.Add(new ProviderItem(Name, Service, Gateway, Limit));
													}
												}

											break;
										/*
										Выносится в отдельный файл
										 case "Exclude":
											foreach (XAttribute a in s.Attributes())
												{
												if (a.Name.LocalName == "Prefix")
													Settings.Excludes.Add(a.Value);
												}
											break;
										*/
										}
									}
								break;
						}
					}
				}
				else
				{
					Console.WriteLine("Нет секции Configuration");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Config: {0}\r\n{1}", ex.Message, ex.StackTrace);
				code = 1;
			}


			return code;
		}

	}

	public class ProviderItem
		{
		public string Name { get; set; }
		public string Service { get; set; }
		public string Gateway { get; set; }
		public decimal Limit { get; set; }
		public ProviderItem(string Name, string Service, string Gateway, decimal Limit)
			{
			this.Name = Name;
			this.Service = Service;
			this.Gateway = Gateway;
			this.Limit = Limit;
			}
		public override string ToString()
			{
			return $"Name = \"{Name}\" Service = \"{Service}\" Gateway = \"{Gateway}\"";
			}
		}
	
	/// <summary>
	/// Провайдеры
	/// </summary>

	public static class ProvidersSettings
	{
		public static class Rt
		{
			public static string Name { get { return Config.Providers["rt"]["name"]; } }
			public static string Host { get { return Config.Providers["rt"]["host"]; } }
			public static string Log { get { return Config.Providers["rt"]["log"]; } }
			public static string CN { get { return Config.Providers["rt"]["cn"]; } }
			public static string Hash { get { return Config.Providers["rt"]["hash"]; } }
            public static string agentId { get { return Config.Providers["rt"]["agentId"]; } }
            public static string agentAccount { get { return Config.Providers["rt"]["agentAccount"]; } }
        }

        #region Rtm
        /// <summary>
        /// RT-Mobile
        /// </summary>
        public static class RtTest
			{
			public static string Name { get { return Config.Providers["rttest"]["name"]; } }
			public static string Host { get { return Config.Providers["rttest"]["host"]; } }
			public static string Log
				{
				get
					{
					return Config.Providers["rttest"]["log"];
					}
				}
			public static string CN
				{
				get
					{
					return Config.Providers["rttest"]["cn"];
					}
				}
			public static string Hash
				{
				get
					{
					return Config.Providers["rttest"]["hash"];
					}
				}
			}
        #endregion Rtm

        #region Pcc
        public static class Pcc
		{
			public static string Name { get { return Config.Providers["pcc"]["name"]; } }
			public static string Host { get { return Config.Providers["pcc"]["host"]; } }
			public static string Log { get { return Config.Providers["pcc"]["log"]; } }
			public static string Certname { get { return Config.Providers["pcc"]["certname"]; } }
		}
        #endregion Pcc

        #region Ekt
        public static class Ekt
		{
			public static string Name { get { return Config.Providers["ekt"]["name"]; } }
			public static string Host { get { return Config.Providers["ekt"]["host"]; } }
			public static string Log { get { return Config.Providers["ekt"]["log"]; } }
			public static string Certname { get { return Config.Providers["ekt"]["certname"]; } }
			public static string Timeout { get { return Config.Providers["ekt"]["timeout"]; } }
			public static string Codepage { get { return Config.Providers["ekt"]["code-page"]; } }
			public static string ContentType { get { return Config.Providers["ekt"]["content-type"]; } }
			public static string Pointid { get { return Config.Providers["ekt"]["point-id"]; } }
		}
        #endregion Ekt

        #region Boriska
        public static class Boriska
        {
            const string key = "boriska";
            public static string Name { get { return Config.Providers[key]["name"]; } }
            public static string Host { get { return Config.Providers[key]["host"]; } }
            public static string Log { get { return Config.Providers[key]["log"]; } }
            public static string Certname { get { return Config.Providers[key]["certname"]; } }
            public static string Timeout { get { return Config.Providers[key]["timeout"]; } }
            public static string Codepage { get { return Config.Providers[key]["code-page"]; } }
            public static string ContentType { get { return Config.Providers[key]["content-type"]; } }
            public static string Pointid { get { return Config.Providers[key]["point-id"]; } }
        }
        #endregion Boriska

        #region Mts
        public static class Mts
		{
			public static string Name { get { return Config.Providers["mts"]["name"]; } }
			public static string Host { get { return Config.Providers["mts"]["host"]; } }
			public static string Log { get { return Config.Providers["mts"]["log"]; } }

			public static string AsVps { get { return Config.Providers["mts"]["as-vps"]; } }
			public static string VpsCode { get { return Config.Providers["mts"]["vps-code"]; } }
			public static string Contract { get { return Config.Providers["mts"]["contract"]; } }
			public static string Security { get { return Config.Providers["mts"]["security-code"]; } }
			public static string Certname { get { return Config.Providers["mts"]["certname"]; } }

			public static string Ott { get { return Config.Providers["mts"]["ott"]; } }
			public static string Xsd { get { return Config.Providers["mts"]["xsd"]; } }
			public static string Schemas { get { return Config.Providers["mts"]["schemas"]; } }

			public static string TerminalPrefix { get { return Config.Providers["mts"]["terminal-prefix"]; } }
			
			public static string Codepage { get { return Config.Providers["mts"]["code-page"]; } }
			public static string ContentType { get { return Config.Providers["mts"]["content-type"]; } }

			public static string Timeout { get { return Config.Providers["mts"]["timeout"]; } }
		}
        #endregion Mts

        #region Cyber
        public static class Cyber
		{
			public static string Name { get { return Config.Providers["cyber"]["name"]; } }
			public static string Host { get { return Config.Providers["cyber"]["host"]; } }
			public static string PayCheck { get { return Config.Providers["cyber"]["pay-check"]; } }
			public static string ApiCheck { get { return Config.Providers["cyber"]["api-check"]; } }
			public static string Pay { get { return Config.Providers["cyber"]["pay"]; } }
			public static string ApiPay { get { return Config.Providers["cyber"]["api-pay"]; } }
			public static string PayStatus { get { return Config.Providers["cyber"]["pay-status"]; } }
			public static string ApiStatus { get { return Config.Providers["cyber"]["api-status"]; } }
			public static string Log { get { return Config.Providers["cyber"]["log"]; } }

			public static string SD { get { return Config.Providers["cyber"]["SD"]; } }
			public static string AP { get { return Config.Providers["cyber"]["AP"]; } }
			public static string OP { get { return Config.Providers["cyber"]["OP"]; } }
			public static string SecretKey { get { return Config.Providers["cyber"]["SecretKey"]; } }
			public static string PublicKeys { get { return Config.Providers["cyber"]["PublicKeys"]; } }
			public static string Passwd { get { return Config.Providers["cyber"]["Passwd"]; } }
			public static string BankKeySerial { get { return Config.Providers["cyber"]["BankKeySerial"]; } }
			public static string Timeout { get { return Config.Providers["cyber"]["timeout"]; } }
		}
        #endregion Cyber

        #region Rapida
        public static class Rapida
        {
            public static string Name { get { return Config.Providers["rapida"]["name"]; } }
            public static string Host { get { return Config.Providers["rapida"]["host"]; } }
            public static string CN { get { return Config.Providers["rapida"]["cn"]; } }
            public static string Log { get { return Config.Providers["rapida"]["log"]; } }

        }
        #endregion Rapida

        #region Xsolla
        public static class Xsolla
        {
            public static string Name { get { return Config.Providers["xsolla"]["name"]; } }
            public static string Host { get { return Config.Providers["xsolla"]["host"]; } }
            public static string CodePage { get { return Config.Providers["xsolla"]["codepage"]; } }
            public static string Agent { get { return Config.Providers["xsolla"]["agent"]; } }
            public static string AgentKey { get { return Config.Providers["xsolla"]["agent-key"]; } }
            public static string Agent1 { get { return Config.Providers["xsolla"]["agent1"]; } }
            public static string AgentKey1 { get { return Config.Providers["xsolla"]["agent-key1"]; } }
            public static string Log { get { return Config.Providers["xsolla"]["log"]; } }

        }
        #endregion Xsolla

        #region Smtp
        public static class Smtp
		{
			public static string Log { get { return Config.Providers["smtp"]["log"]; } }
		}
        #endregion Smtp

    }


    /// <summary>
    /// Установки
    /// </summary>
    public static class Settings
	{

		static string root = "";
		static string logPath ="";
		static string templates = "";
		static string registers = "";
		static string cyber = "";
		static int tz = 7;
		static int siteTppId = 215;
		static int fakeTppId = 1;
		static int fakeTppType = 2;
		static string attachments = "";
		static int delivery = 0;
		internal static decimal amountLimit = decimal.MinusOne;
		internal static int amountDelay = 0;
		static string lists = "";
		
		/// <summary>
		/// Контролируемые поставщики
		/// </summary>
		public static List<ProviderItem> CheckedProviders { get { return checkedProviders; } }
		internal static List<ProviderItem> checkedProviders = new List<ProviderItem>();

        static int connectionLimit = 2;
		// static int deliveryStart;
		// static int deliveryStop;

		/// <summary>
		/// Наименование продукта
		/// </summary>
		public static string Title { get; set; }
		/// <summary>
		/// Версия продукта
		/// </summary>
		public static string Version { get; set; }
		/// <summary>
		/// Название задачи в спске задач
		/// </summary>
		public static string Jobname { get; set; }

		public static string Root
		{
			get { return root; }
			set 
				{ 
				root = !string.IsNullOrEmpty(value)? value: ".\\";
				if (root.Substring(root.Length - 1, 1) != "\\") root += "\\";
				}
		}
		public static string LogPath
		{
			get { return logPath; }
			set { logPath = root + value; }
		}
		// Шаблоны
		public static string Templates
		{
			get { return templates; }
			set { templates = root + value; }
		}
		// Реестры
		public static string Registers
		{
			get { return registers; }
			set { registers = root + value; }
		}
		public static string CyberRoot
		{
			get { return cyber; }
			set { cyber = root + value; }
		}

		/// <summary>
		/// Папка для хранения аттачей
		/// </summary>
		public static string Attachments
		{
			get { return attachments; }
			set { attachments = root + value; }
		}

		/// <summary>
		/// Http-порт
		/// </summary>
		public static int Port { get { return port; } }
		static int port = 0;
		/// <summary>
		/// Https-порт
		/// </summary>
		public static int SslPort { get { return sslPort; } }
		static int sslPort = 0;
		public static string GWHost { get; set; }

        /// <summary>
        /// Количество одновременно открытых соединений
        /// </summary>
        public static int ConnectionLimit { get { return connectionLimit; } }

		/// <summary>
		/// TZ по умолчанию, если терминал не определён
		/// </summary>
		public static int Tz { get { return tz; } }

		/// <summary>
		/// Номер чточки сайта
		/// </summary>
		public static int SiteTppId { get { return siteTppId; } }
		
		/// <summary>
		/// ID терминала поумолчанию, если он не зарегистрирован
		/// </summary>
		public static int FakeTppId { get { return fakeTppId; } }
		/// <summary>
		/// Тип терминала по умочяанию, если он не зарегистрирован
		/// </summary>
		public static int FakeTppType { get { return fakeTppType; } }

		// LogLeve="DEBUG" -- выводится трассировка запросаов
		public static string LogLevel { get { return logLevel; } }
		public static string logLevel;

		/// <summary>
		/// Строка подключения к БД
		/// </summary>
		public static string ConnectionString { get; set; }

		/// <summary>
		/// Подключение к БД ГОРОД
		/// </summary>
		public static string GorodConnectionString { get; set; }

		/// <summary>
		/// Время попытки проверки БД при старте шлюза
		/// </summary>
		public static int DbCheckTimeout { get; set; }

		// Общая секция для шлюза
		public static class OldiGW
		{
			public static string LogFile { get { return LogPath + "OldiGW.log"; } }
		}

		internal static int conveyorSize;
		/// <summary>
		/// Размер конвейера обработки входящих запросов
		/// </summary>
		public static int ConveyorSize { get { return conveyorSize; } }

		/// <summary>
		/// Номер рассылки. 0 - не рассылать
		/// </summary>
		public static int Delivery { get { return delivery; } }

		/// <summary>
		/// Предел суммы для финансового контроля
		/// </summary>
		public static decimal AmountLimit { get { return amountLimit; } }
		/// <summary>
		/// Звдержка (в часах) для финансового контроля
		/// </summary>
		public static int AmountDelay { get { return amountDelay; } }

		/// <summary>
		/// Белый и чёрный списки
		/// </summary>
		public static string Lists { get { return root + lists; } }

		/// <summary>
		/// SMTP-сервер
		/// </summary>
		public static class Smtp
		{
			public static string Host;
			public static string Port;
			public static string User;
			public static string Password;
			public static string LogFile { get { return LogPath + ProvidersSettings.Smtp.Log; } }
		}
		// public static Smtp SmtpInfo;

		// Секции провайдеров
		/// <summary>
		/// MTS
		/// </summary>
		public static class Mts
		{
			public static string Name { get { return ProvidersSettings.Mts.Name; } }
			public static string Host { get { return ProvidersSettings.Mts.Host; } }
			public static string LogFile { get { return logPath + ProvidersSettings.Mts.Log; } }
			public static string AsVps { get { return ProvidersSettings.Mts.AsVps; } }
			public static string VpsCode { get { return ProvidersSettings.Mts.VpsCode; } }
			public static string Contract { get { return ProvidersSettings.Mts.Contract; } }
			public static string Security { get { return ProvidersSettings.Mts.Security; } }
			public static string Codepage { get { return ProvidersSettings.Mts.Codepage; } }
			public static string ContentType { get { return ProvidersSettings.Mts.ContentType; } }
			public static string Timeout { get { return ProvidersSettings.Mts.Timeout; } }
		}

		/// <summary>
		/// ЦУП
		/// </summary>
		public static class Pcc
		{
			public static string Name { get { return ProvidersSettings.Pcc.Name; } }
			public static string Host { get; set; }
			public static string LogFile { get; set; }
			public static string Certname { get; set; }
		}

		/// <summary>
		/// Ростелеком
		/// </summary>
		public static class Rt
		{
			public static string Name { get { return ProvidersSettings.Rt.Name; } }
			public static string Host { get { return ProvidersSettings.Rt.Host; } }
			public static string LogFile { get { return logPath + ProvidersSettings.Rt.Log; } }
			public static string CN { get { return ProvidersSettings.Rt.CN; } }
			public static string Hash { get { return ProvidersSettings.Rt.Hash.Replace(" ", ""); } }
            public static string AgentId { get { return ProvidersSettings.Rt.agentId; } }
            public static string AgentAccount { get { return ProvidersSettings.Rt.agentAccount; } }
        }

        /// <summary>
        /// Rt-mobile
        /// </summary>
        public static class RtTest {
            public static string Name { get { return ProvidersSettings.RtTest.Name; } }
			public static string Host { get { return ProvidersSettings.RtTest.Host; } }
			public static string LogFile { get { return logPath + ProvidersSettings.RtTest.Log; } }
			public static string CN { get { return ProvidersSettings.RtTest.CN; } }
			public static string Hash{get{return ProvidersSettings.RtTest.Hash.Replace(" ", "");}}
			}

		/// <summary>
		/// Ект
		/// </summary>
		public static class Ekt
		{
			public static string Name { get { return ProvidersSettings.Ekt.Name; } }
			public static string Host { get { return ProvidersSettings.Ekt.Host; } }
			public static string LogFile { get { return logPath + ProvidersSettings.Ekt.Log; } }
			public static string Certname { get { return ProvidersSettings.Ekt.Certname; } }
			public static string Pointid { get { return ProvidersSettings.Ekt.Pointid; } }
			public static string Codepage { get { return ProvidersSettings.Ekt.Codepage; } }
			public static string ContentType { get { return ProvidersSettings.Ekt.ContentType; } }
			public static string Timeout { get { return ProvidersSettings.Ekt.Timeout; } }
		}

        /// <summary>
        /// Boriska
        /// </summary>
        public static class Boriska
        {
            public static string Name { get { return ProvidersSettings.Boriska.Name; } }
            public static string Host { get { return ProvidersSettings.Boriska.Host; } }
            public static string LogFile { get { return logPath + ProvidersSettings.Boriska.Log; } }
            public static string Certname { get { return ProvidersSettings.Boriska.Certname; } }
            public static string Pointid { get { return ProvidersSettings.Boriska.Pointid; } }
            public static string Codepage { get { return ProvidersSettings.Boriska.Codepage; } }
            public static string ContentType { get { return ProvidersSettings.Boriska.ContentType; } }
            public static string Timeout { get { return ProvidersSettings.Boriska.Timeout; } }
        }

        /// <summary>
        /// CyberPlat
        /// </summary>
        public static class Cyber
		{
			public static string Name = "cyber";


			public static string Timeout { get { return ProvidersSettings.Cyber.Timeout; } }
			public static string LogFile
			{
				// get { return LogPath + ProvidersSettings.Cyber.Log; }
				get { return LogPath + "cyber.log"; }
			}
		}


		/// <summary>
		/// Путь к папке КиберПлат
		/// </summary>
		public static string CyberPath { get { return cyber; } }

		/// <summary>
		/// Имя Web-клиента
		/// </summary>
		public static string ClientName { get; set; }

        public static class Rapida
        {
            public static string Name { get { return ProvidersSettings.Rapida.Name; } }
            public static string Host { get { return ProvidersSettings.Rapida.Host; } }
            public static string CN { get { return ProvidersSettings.Rapida.CN; } }
            public static string Log { get { return LogPath + ProvidersSettings.Rapida.Log; } }

        }


        public static class Xsolla
        {
            public static string Name { get { return ProvidersSettings.Xsolla.Name; } }
            public static string Host { get { return ProvidersSettings.Xsolla.Host; } }
            public static string Agent { get { return ProvidersSettings.Xsolla.Agent; } }
            public static string AgentKey { get { return ProvidersSettings.Xsolla.AgentKey; } }
            public static string Agent1 { get { return ProvidersSettings.Xsolla.Agent1; } }
            public static string AgentKey1 { get { return ProvidersSettings.Xsolla.AgentKey1; } }
            public static string CodePage { get { return ProvidersSettings.Xsolla.CodePage; } }
            public static string Log { get { return LogPath + ProvidersSettings.Xsolla.Log; } }

        }

        /// <summary>
        /// Чтение файла *.exe.config
        /// </summary>
        public static void ReadConfig()
		{

			Assembly assem = Assembly.GetCallingAssembly();
			AssemblyName assemName = assem.GetName();
			//Assembly asm = Assembly.GetExecutingAssembly();
			Assembly asm = Assembly.GetCallingAssembly();
			AssemblyName asmName = asm.GetName();
			string Version = string.Format("v{0}.{1}.{2}.{3}", asmName.Version.Major, asmName.Version.Minor, asmName.Version.Build, asmName.Version.Revision);
			AssemblyProductAttribute[] asmProduct =
				(System.Reflection.AssemblyProductAttribute[])asm.GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), false);
			AssemblyDescriptionAttribute[] asmDesc =
				(System.Reflection.AssemblyDescriptionAttribute[])asm.GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), false);
			AssemblyTitleAttribute[] asmTitle =
				(System.Reflection.AssemblyTitleAttribute[])asm.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);
	
			Settings.Jobname = assemName.Name;
			//Settings.Title = /*assemName.Name;*/ myFileVersionInfo.ProductName;

			Title = string.Format("{0} {1}. {2}", asmProduct[0].Product, Version, asmDesc[0].Description);
			ClientName = asmTitle[0].Title + " Client " + Version;

			// Config config = new Config();
			Config.Load();

			Root = Config.AppSettings["Root"];
			LogPath = Config.AppSettings["LogPath"];
			Templates = Config.AppSettings["Templates"];
			registers = Config.AppSettings["Registers"];
			CyberRoot = Config.AppSettings["Cyber"];
			Attachments = Config.AppSettings["Attachments"];
			
//			if (string.IsNullOrEmpty(Config.AppSettings["Port"]))
//				port = 300;
//			else
            //Порт должен быть задан!
				int.TryParse(Config.AppSettings["Port"], out port);
			
//			if (string.IsNullOrEmpty(Config.AppSettings["SslPort"]))
//				sslPort = 301;
//			else
				int.TryParse(Config.AppSettings["SslPort"], out sslPort);

			ConnectionString = Config.AppSettings["ConnectionString"];
			GorodConnectionString = Config.AppSettings["GorodConnectionString"];


			DbCheckTimeout = int.Parse(Config.AppSettings["DbCheckTimeout"]);
			GWHost = Config.AppSettings["GWHost"];
			logLevel = Config.AppSettings["logLevel"];

            if (!string.IsNullOrEmpty(Config.AppSettings["ConnectionLimit"]))
                int.TryParse(Config.AppSettings["ConnectionLimit"], out connectionLimit);

			if (!string.IsNullOrEmpty(Config.AppSettings["tz"]))
				tz = int.Parse(Config.AppSettings["tz"]);
			if (!string.IsNullOrEmpty(Config.AppSettings["site-tpp-id"]))
				siteTppId = int.Parse(Config.AppSettings["site-tpp-id"]);
			if (!string.IsNullOrEmpty(Config.AppSettings["fake-tpp-id"]))
				fakeTppId = int.Parse(Config.AppSettings["fake-tpp-id"]);
			if (!string.IsNullOrEmpty(Config.AppSettings["fake-tpp-type"]))
				fakeTppType = int.Parse(Config.AppSettings["fake-tpp-type"]);

			if (!string.IsNullOrEmpty(Config.AppSettings["Conveyor-Size"]))
				conveyorSize = int.Parse(Config.AppSettings["Conveyor-Size"]);

			// Рассылки
			if (!string.IsNullOrEmpty(Config.AppSettings["Delivery"]))
				delivery = int.Parse(Config.AppSettings["Delivery"]);

			// Белые, чёрные списки
			if (!string.IsNullOrEmpty(Config.AppSettings["Lists"]))
				lists = Config.AppSettings["Lists"];
			
			if (LogLevel.IndexOf("CONF") != -1)
				Log();
			
				// LogPath = (string)ar.GetValue("LogPath", typeof(string));
				// Templates = (string)ar.GetValue("Templates", typeof(string));
				// Reesters = (string)ar.GetValue("Reesters", typeof(string));
				// CyberRoot = (string)ar.GetValue("Cyber", typeof(string));
				// Port = Convert.ToInt32((string)ar.GetValue("Port", typeof(string)));
				// SslPort = Convert.ToInt32((string)ar.GetValue("SslPort", typeof(string)));
				// ConnectionString = (string)ar.GetValue("ConnectionString", typeof(string));
				// DbCheckTimeout = Convert.ToInt32((string)ar.GetValue("DbCheckTimeout", typeof(string)));
				// GWHost = (string)ar.GetValue("GWHost", typeof(string));
				// logLevel = (string)ar.GetValue("LogLevel", typeof(string));

		}

		/// <summary>
		/// Вывод конфига в лог-файл
		/// </summary>
		public static void Log()
		{
			string log = LogPath + "OldiGW.log";

			if (Settings.logLevel.IndexOf("CONF") != -1)
			{
				Oldi.Net.Utility.Log(log, "\r\nКонфигурация шлюза:");
				// Console.WriteLine("\r\nКонфигурация шлюза:");
				foreach (string key in Config.AppSettings.AllKeys)
				{
					Oldi.Net.Utility.Log(log, "{0}={1}", key, Config.AppSettings[key]);
					// Console.WriteLine("{0}={1}", key, Config.AppSettings[key]);
				}

				Oldi.Net.Utility.Log(log, "\r\nProviders:");
				// Console.WriteLine("\r\nProviders:");
				foreach (string k in Config.Providers.Keys)
				{
					// NameValueCollection kvp.ValueType = providers.Values;
					NameValueCollection nnn = (NameValueCollection)Config.Providers[k];
					Oldi.Net.Utility.Log(log, "providers: {0}", nnn["name"]);
					// Console.WriteLine("providers: {0}", nnn["name"]);
					foreach (string s in nnn)
					{
						if (s != "name")
							Oldi.Net.Utility.Log(log, "\t{0}={1}", s, nnn[s]);
							// Console.WriteLine("\t{0}={1}", s, nnn[s]);
					}
				}

				// SMTP-секция
				Oldi.Net.Utility.Log(log, "Smtp: host={0} port={1} user={2} password={3}",
				// Console.WriteLine("Smtp: host={0} port={1} user={2} password={3}",
					Settings.Smtp.Host, Settings.Smtp.Port, Settings.Smtp.User, Settings.Smtp.Password);

				// FinancialCheck
				Oldi.Net.Utility.Log(log, "FinancialCheck:\r\n\tAmountLimit = \"{0}\" AmountDelay = \"{1}\"", AmountLimit, AmountDelay);
				foreach(var item in CheckedProviders)
					{
					if (item.Limit == decimal.MinusOne)
						item.Limit = AmountLimit;
					Oldi.Net.Utility.Log(log, "\tProvider = \"{0}\" Service = \"{1}\" Gateway = \"{2}\" Limit = \"{3}\"",
						item.Name, item.Service, item.Gateway, item.Limit);
					}
			
				Oldi.Net.Utility.Log(log, "\tLst: \"{0}\"", Lists);
			/*
			foreach(string prefix in Excludes)
				{
				Oldi.Net.Utility.Log(log, "\tExclude prefix \"{0}\"", prefix);
				}
			*/
			}
		}

	}

}
