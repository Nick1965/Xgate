using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Oldi.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using System.Xml;
using Oldi.Utility;
using System.Data.SqlClient;
using System.Globalization;

namespace Oldi.Mts
{
    public partial class GWMtsRequest : GWRequest
    {
		string terminalPrefix;
		protected XmlSchemaSet Schemas;
		protected XmlReaderSettings settings;
		string checkTemplate;
		string payTemplate;
		string statusTemplate;

		/// <summary>
		/// Номер терминала для МТС
		/// </summary>
		public string MtsTerminalId { get { return terminalPrefix + "." + terminal.ToString(); } }
	
		public GWMtsRequest():
            base()
        {
		}

        public GWMtsRequest(GWRequest src) :
            base(src)
        {
		}

		/// <summary>
		/// Инициализация базовых переменных класса
		/// </summary>
		public override void InitializeComponents()
		{
			base.InitializeComponents();

			CodePage = Settings.Mts.Codepage;
			ContentType = Settings.Mts.ContentType;
			asvps = Settings.Mts.AsVps;
			contract = Settings.Mts.Contract;
			security = Settings.Mts.Security;
			vpscode = Settings.Mts.VpsCode;

			commonName = vpscode + ";" + security + ";" + asvps;

			try
			{

				Schemas = new XmlSchemaSet();
				Schemas.Add(string.Format(Properties.Settings.Default.UrlMessages, ProvidersSettings.Mts.Xsd),
					string.Format(ProvidersSettings.Mts.Schemas + Properties.Settings.Default.XsdMessages, ProvidersSettings.Mts.Xsd));
				Schemas.Add(string.Format(Properties.Settings.Default.UrlConstraints, ProvidersSettings.Mts.Xsd),
					string.Format(ProvidersSettings.Mts.Schemas + Properties.Settings.Default.XsdConstraints, ProvidersSettings.Mts.Xsd));

				settings = new XmlReaderSettings();
				settings.ValidationType = ValidationType.Schema;
				settings.Schemas = Schemas;
				settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

			}
			catch (Exception ex)
			{
				errCode = 400;
				state = 12;
				errDesc = ex.Message;
				RootLog("Mts.Init {0}", errDesc);
			}

			checkHost = Properties.Settings.Default.CheckTemplate;
			payHost = Properties.Settings.Default.PayTemplate;
			statusHost = Properties.Settings.Default.StatusTemplate;
			host = Settings.Mts.Host;

			terminalPrefix = ProvidersSettings.Mts.TerminalPrefix;
		}
	
		protected override string GetLogName()
		{
			return Settings.Mts.LogFile;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Schemas = null;
			if (settings != null)
				settings.ValidationEventHandler -= ValidationCallback;
			settings = null;
		}

		/// <summary>
		/// Read timeout
		/// </summary>
		/// <returns>Timeout in sec</returns>
		public override int TimeOut()
		{
			return int.Parse(Settings.Mts.Timeout);
		}
		
		/// <summary>
		/// Создать запрос к серверу ЕСПП. Скрывает базовый запрос.
		/// </summary>
		/// <param name="old_state">Статус (если не прошёл проверку 12)</param>
		/// <returns>0 - ОК, 1 - нет</returns>
		public new int MakeRequest(int old_state)
        {

			if (Settings.LogLevel.IndexOf("PARS") != -1)
			{
				RootLog("Параметры запроса:\r\nTimeout={0}\r\nPhone={1}\r\nAmount={2}\r\nCUR={3}\r\nService={4}\r\nTerminal={5}\r\nType={6}\r\nAS-VPS={7}\r\nContract={8}\r\nAccount={10}\r\nPrincipal={11}",
					TimeOut(), Phone, XConvert.AsAmount(Amount), Cur, Service, MtsTerminalId, TerminalType, Asvps, Contract, Account, Gateway);
			}
			
			switch (old_state)
			{
				case 0:
					using (StreamReader check = new StreamReader(Settings.Templates + (string.IsNullOrEmpty(Phone) ? "a": "p") + "-0104010-" + ProvidersSettings.Mts.Ott + "-" + ProvidersSettings.Mts.Xsd + ".tpl"))
					{
						checkTemplate = check.ReadToEnd();
					}
					if (string.IsNullOrEmpty(Phone))
						stRequest = string.Format(checkTemplate, TimeOut(), XConvert.AsAmount(Amount), Cur, Service, MtsTerminalId, TerminalType, 
							Asvps, Contract, Account, Gateway);
					else
						stRequest = string.Format(checkTemplate, TimeOut(), Phone, XConvert.AsAmount(Amount), Cur, Service, MtsTerminalId, TerminalType, Asvps, Contract, Gateway);
					break;
				
				case 1:
					using (StreamReader pay = new StreamReader(Settings.Templates + (string.IsNullOrEmpty(Phone) ? "a" : "p") + "-0104090-" + ProvidersSettings.Mts.Ott + "-" + ProvidersSettings.Mts.Xsd + ".tpl"))
					{
						payTemplate = pay.ReadToEnd();
					}
					
					controlCode = String.Format("{0}&{1}&{2}&{3}&{4}&{5}&{6}&{7}&{8}&{9}&{10}&{11}&{12}",
						Phone, XConvert.AsAmount(Amount), Cur, Service, Oid, Tid, 
						XConvert.AsDate(Pcdate), Asvps, MtsTerminalId, TerminalType, 
						XConvert.AsDateTZ(TerminalDate, Tz), string.IsNullOrEmpty(Phone)? Account: "", Gateway);

					
					if (Settings.LogLevel.IndexOf("DEBUG") != -1)
						Log("DoPay: Cc=\"{0}\"", ControlCode);

					using (Crypto sign = new Crypto(CommonName))
						controlCode = sign.Sign(ControlCode);
					
					if (string.IsNullOrEmpty(Phone))
						stRequest = string.Format(payTemplate, XConvert.AsAmount(Amount), Cur, Service, Oid, MakeCheckNumber(), Tid, 
							XConvert.AsDate(Pcdate), Outtid, Asvps, MtsTerminalId, TerminalType, Opcode, 
							XConvert.AsDateTZ(TerminalDate, Tz), ControlCode, Contract, Account, Gateway);
					else
						stRequest = string.Format(payTemplate, Phone, XConvert.AsAmount(Amount), Cur, Service, Oid, MakeCheckNumber(), Tid, 
							XConvert.AsDate(Pcdate), Outtid, Asvps, MtsTerminalId, TerminalType, Opcode, 
							XConvert.AsDateTZ(TerminalDate, Tz), ControlCode, Contract, Gateway);
					break;

				case 3:
					using (StreamReader status = new StreamReader(Settings.Templates + "0104085-" + ProvidersSettings.Mts.Ott + "-" + ProvidersSettings.Mts.Xsd + ".tpl"))
					{
						statusTemplate = status.ReadToEnd();
					}
					stRequest = string.Format(statusTemplate, TimeOut(), Outtid, Asvps, Contract);
					break;
			}

			if (CheckXML(stRequest) != 0)
			{
				RootLog("Mts.MakeRequest {0}", ErrDesc);
				state = (byte)old_state;
				errCode = 11;
			}

			return state == 12 ? 1 : 0;

        }

		/// <summary>
		/// Обработка ошибки XML-файла
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		internal void ValidationCallback(object sender, ValidationEventArgs args)
		{
			Log("XML: {0}", args.Message);
			errCode = 400;
			state = 12;
			errDesc = "(400) " + args.Message;
		}


		/// <summary>
		/// Проверка XML схемой XSD
		/// </summary>
		/// <param name="xml"></param>
		public int CheckXML(string xml)
		{
			int retcode = 0;
			StringBuilder sb = new StringBuilder();

			using (StringReader sr = new StringReader(xml))
			using (XmlReader reader = XmlReader.Create(sr, settings))
				while (true)
				{
					try
					{
						if (!reader.Read())
							break;
					}
					catch (XmlException xe)
					{
						errDesc = "(400) " + xe.Message;
						errCode = 400;
						state = 12;
						RootLog("[CXML - stop] {5} Mts.CheckXML: state={0} error={1} {2} at ({3}, {4})", state, errCode, xe.Message, xe.LineNumber, xe.LinePosition, Tid);
						sb.AppendFormat("{0}(400) {1}", sb.Length > 0? "\r\n": "", xe.Message);
						retcode = 1;
					}
				}

			errDesc = sb.ToString();
			return retcode;
			
		}

		/// <summary>
		/// Выполнение запроса
		/// </summary>
		/// <param name="host">Хост (не используется)</param>
		/// <param name="old_state">Текущее состояние 0 - check, 1 - pay, 3 - staus</param>
		/// <param name="try_state">Состояние, присваиваемое при успешном завершении операции</param>
		/// <returns></returns>
		public override int DoPay(byte old_state, byte try_state)
		{
			// Создадим платеж с статусом=0 и locId = 1
			// state 0 - новый
			// state 1 - получено разрешение

			int retcode = 0;
            // Создание шаблона сообщения check/pay/status

            pcdate = DateTime.Now.AddHours(-1);
            terminalDate = pcdate;
            tz = 6;

            
            RootLog("{3} Корректировка времени PC={0} old TD={1} new TD={2}",
                XConvert.AsDateTZ(Pcdate, Settings.Tz),
                XConvert.AsDateTZ(TerminalDate, Tz),
                XConvert.AsDateTZ(pcdate + TimeSpan.FromHours(Tz - Settings.Tz), Tz),
                Tid);
            if (Exec("UpdateTime", Tid, pcdate: Pcdate, terminalDate: terminalDate) != 0)
                return -1;
            

            // Проверка времени создания платежа
            // Если платёж кис больше часа, скорректировать время

            /*
            if (old_state == 0 && DateTime.Now.Ticks - Pcdate.Ticks > TimeSpan.TicksPerHour * 1)
				{
                pcdate = DateTime.Now;

                // Корректировка времени -1
                pcdate = pcdate.AddHours(-1);

                Random rnd = new Random((int)DateTime.Now.Ticks);
				DateTime time = new DateTime(pcdate.Ticks - TimeSpan.TicksPerSecond * (long)(rnd.NextDouble() * 10.0));
				RootLog("{3} Корректировка времени PC={0} old TD={1} new TD={2}",
					XConvert.AsDateTZ(Pcdate, Settings.Tz),
					XConvert.AsDateTZ(TerminalDate, Tz),
					XConvert.AsDateTZ(time + TimeSpan.FromHours(Tz - Settings.Tz), Tz),
					Tid);
				if (Exec("UpdateTime", Tid, pcdate :Pcdate, terminalDate :terminalDate) != 0)
					return -1;
				}
            */
			
			// БД уже доступна, не будем её проверять
			if (MakeRequest(old_state) == 0)
			{
				// retcode = 0 - OK
				// retcode = 1 - TCP-error
				// retcode = 2 - Message sends, but no answer recieved.
				// retcode < 0 - System error
				if ((retcode = SendRequest(Host)) == 0)
					if ((retcode = ParseAnswer(stResponse)) == 0) // Ответ получен и разобран; 1 - Timeout
					{
						if (old_state == 0)
						{
							errDesc = "Разрешение на платёж получено";
							state = 1;
							errCode = 1;
							// ReportRequest("check  ");
							// ReportCheck();
						}
						else
						{
							state = 6;
							errCode = 3;
							// errCode = 3;
							errDesc = "Платёж проведён";
						}
						UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc,
						opname: Opname, opcode: Opcode, fio: fio, outtid: Outtid, account: Account, 
							limit: Limit, limitEnd: XConvert.AsDate(LimitDate),
							acceptdate: XConvert.AsDate2(Acceptdate), acceptCode: AcceptCode,
							locked: state == 6 ? (byte)0 : (byte)1); // Разблокировать если проведён

						// ReportRequest("DoPay");

						return 0;
					}
			}

			// Запрос на разрешение платежа

			switch (retcode)
			{
				case 1:
				case 502: // Истекло время обработки
					state = old_state;
					break;
				case 358: // Нет запроса на прием платежа
					state = 0;
					break;
				case 103: // "Домашняя" БС абонента на профилактике или недоступна.
				case 106: // ЦМ не доступен
					state = times > 20 ? (byte)12 : old_state;
					break;
				case 200: // Прием платежа невозможен. Прием платежа запрещен (без детализации причины)
				case 201: // Номер телефона в БС не найден.
				case 202: // Приложение обслуживания не найдено.
				case 203: // Счет неактивен.
				case 205: // Описание клиента не найдено.
				case 206: // Абонент не найден
					// state = Pcdate.AddDays(1.0) <= DateTime.Now ? (byte)12 : old_state;
					// state = times > 20 ? (byte)12 : old_state;
					state = 12;
					break;
				case 301: // Нет денег
					state = 0;
					break;
				case 354: // Терминал не зарегистрирован
					// state = Pcdate.AddDays(1.0) <= DateTime.Now ? (byte)12 : old_state;
					state = old_state == 0? (byte)0: (byte)12;
					break;
				case 66:  // ПЦ ЕСПП. Техническая ошибка сервера приложений
					state = 12;
					break;
				case 362:
				case 501: // Превышена пропускная способность
				case 367: // Номер уже обработан в ЕСПП
				case -20367:
					state = 11;
					break;
				case 357: // Платеж уже существует
				case 398: // Истекло время акцептования документа
				case 651: // Платеж в требуемом состоянии не найден
					if (old_state == 3 && lastcode == 357)
						state = 12;
					else
						state = times > 20? (byte)12: (byte)3;
					break;
				case 363: // Запрещен прием платежей с будущей датой операции вне допустимого диапазона
					RootLog("Дата вне диапазона Now={0} Pc={1} TD={2}", 
						XConvert.AsDate2(DateTime.Now), XConvert.AsDate2(Pcdate), 
						TerminalDate != null? XConvert.AsDate2(TerminalDate.Value): XConvert.AsDate2(DateTime.Now)
						);
					state = 0;
					break;
				case 650: // Не определено состояние платежа
					state = 1; // разрешение получено, отпарвить зфн
					break;
				default:
					state = 12;
					break;
			}

			UpdateState(tid, state: state, errCode: retcode, errDesc: errDesc, locked: 0);
			return 1;

		}


		/// <summary>
		/// Разбор ответа
		/// </summary>
		/// <param name="stResponse">Ответ сервера ЕСПП</param>
		/// <returns>0 - успешно, 1 - таймаут, -1 - неудача, ошибка в errCode</returns>
		public override int ParseAnswer(string stResponse)
        {

            XDocument doc = XDocument.Parse(stResponse);
			errCode = 400; // Ответ не получен
			errDesc = "Ответ не получен";

            templateName = doc.Root.Name.LocalName.ToString().ToUpper();

			// Log("Получен ответ: {0}\r\n{1}", templateName, stResponse);

            if (templateName == "ESPP_2204050" ||
                templateName == "ESPP_2204051" ||
                templateName == "ESPP_2204010" ||
                templateName == "ESPP_2204090" ||
                templateName == "ESPP_2204085")
            {
                XElement root = XElement.Parse(stResponse);
                IEnumerable<XElement> fields =
                    from el in root.Elements()
                    select el;

                foreach (XElement el in fields)
                {

                    switch (el.Name.LocalName.ToString().ToLower())
                    {
                        case "f_01": // Код ошибки
                            int.TryParse(el.Value, out errCode);
                            break;
                        case "f_02": // Сообщение об ошибке
                            ErrDesc = el.Value;
                            break;
                        default:
                            // Error = ErrTemplatelInvalid;
                            break;
                    }
                }

				doc = null;
				return ErrCode;
            }
            else
			{
				XElement root = XElement.Parse(stResponse);
				IEnumerable<XElement> fields =
					from el in root.Elements()
					select el;
				if (templateName.ToUpper() == "ESPP_1204010")
				{
					foreach (XElement el in fields)
					{
						switch (el.Name.LocalName.ToString().ToLower())
						{
							case "f_01": // Код доманего оператора
								opcode = el.Value;
								break;
							case "f_02": // Домашний оператор
								opname = el.Value;
								break;
							case "f_03": // Л/с абонента
								account = el.Value;
								break;
							case "f_04": // Инициалы
								fio = el.Value;
								break;
							case "f_05": // Outtid
								outtid = el.Value;
								break;
							case "f_06": // Сумма лимита
								Decimal.TryParse(el.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out limit);
								break;
							case "f_08": // Сумма лимита
								DateTime.TryParse(el.Value, out limitend);
								break;
							default:
								// Error = ErrTemplatelInvalid;
								break;
						}
					}
				}
				else if (templateName.ToUpper() == "ESPP_1204090")
				{
					foreach (XElement el in fields)
					{
						switch (el.Name.LocalName.ToString().ToLower())
						{
							case "f_13": // Дата акцепта
								DateTime.TryParse(el.Value, out acceptdate);
								break;
							case "f_20": // Код акцептования
								acceptCode = el.Value;
								break;
							default:
								// Error = ErrTemplatelInvalid;
								break;
						}
					}
				}
				else if (templateName.ToUpper() == "ESPP_1204085")
				{
					foreach (XElement el in fields)
					{
						switch (el.Name.LocalName.ToString().ToLower())
						{
							case "f_01": // Состояние обработки платежа
								int.TryParse(el.Value, out errCode);
								break;
							case "f_14": // Дата акцепта
								DateTime.TryParse(el.Value, out acceptdate);
								break;
							case "f_21": // Код акцептования
								acceptCode = el.Value;
								break;
							default:
								// Error = ErrTemplatelInvalid;
								break;
						}
					}
				}
				else if (templateName.ToUpper() == "ESPP_1204050")
				{
					long rid = 0;
					foreach (XElement el in fields)
					{
						switch (el.Name.LocalName.ToString().ToLower())
						{
							case "f_01": // Состояние обработки платежа
								long.TryParse(el.Value, out rid);
								break;
							default:
								// Error = ErrTemplatelInvalid;
								break;
						}
					}
					errCode = 0;
					errDesc = "Реестр " + rid.ToString() + " отправлен.";
				}
				else if (templateName.ToUpper() == "ESPP_1204051")
				{
					foreach (XElement el in fields)
					{
						switch (el.Name.LocalName.ToString().ToLower())
						{
							case "f_04": // Результат сверки 1 - расхождений нет, всё остальное ошибки
								if (el.Value != "1")
									int.TryParse(el.Value, out errCode);
								else
									ErrCode = 0;
								break;
							case "f_05": // Резюме
								ErrDesc = el.Value;
								break;
							case "f_06": // Присоединенный XML-файл
								xmlFile = el.Value;
								break;
							default:
								// Error = ErrTemplatelInvalid;
								break;
						}
					}
					// Log("1204051: f_04={0} f_05={1}", ErrCode, ErrDesc);
					doc = null;
					return ErrCode;
				}

			}

			doc = null;
			return 0;
        
        }

		
		/// <summary>
		/// Создание нового платежа
		/// Переопределён. Делает проверку выполняется ли платёж по номеру телефона или л/с
		/// </summary>
		/// <returns></returns>
		public override int MakePayment()
		{
			
			if (!string.IsNullOrEmpty(phone))
			{
				if (phone.Length > 10)
				{
					account = phone;
					phone = "";
				}
				else if (phone.Length == 10 && phone.Substring(0, 1) == "1")
				{
					account = phone;
					phone = "";
				}
			}
			else
			{
				account = phone;
			}

			// Нельзя допускать чтобы время процессинга было раньше текущего
			// В этой функции время корректируется
			// GetTerminalInfo();

			return base.MakePayment();
		}
	
		/// <summary>
		/// Выролнить цикл проведения/допроведения платежа
		/// </summary>
		public override void Processing(bool New = true)
		{

			try
				{
				SetLock(1);

				if (New)  // Новый платёж
					{
					if (MakePayment() == 0)
						{
		
						// Проверка дневного лимита для данного плательщика
                        if (DayLimitExceeded(true)) return;

						// Сумма болше лимита и прошло меньше времени задержки отложить обработку запроса
						if (FinancialCheck(New)) return;

						// ReportRequest("Begin");
						if (DoPay(0, 1) == 0)
							{
							// ReportRequest("Check");
							DoPay(1, 6); // Платеж МТС не надо проверять статус, он сразу идёт в 6-й статус
							}
						}
					// ReportRequest("End");
					}

				else // Redo
					{
					ReportRequest("Redo start");

					// Синхронизация с Городом
					Sync(false);
					if (State < 6)
						{

						if (State == 0) // Новый платеж, получить разрешение
							{
							// Проверим время
							GetTerminalInfo();

							if (FinancialCheck(New)) return;
							if (DoPay(0, 1) == 0)
								DoPay(1, 6);
							}
						else if (State == 1) // Разрешение получено
							{
							DoPay(1, 6);
							}
						else if (State == 3) // Платеж отправлен
							DoPay(3, 6);

						ReportRequest("Redo stop");
					
						}
					}

				}
			catch(Exception x)
				{
				RootLog("Tid={0} {1}", Tid, x.ToString());
				}
			finally
				{
				SetLock(0);
				}

			}

	
	
	}
}
