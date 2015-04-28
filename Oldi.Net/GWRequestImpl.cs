using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Resources;
using System.Xml.Linq;
using System.ServiceModel;
using Oldi.Net.Proxy;
using System.IO.Compression;

namespace Oldi.Net
{
	public partial class GWRequest
	{
		/// <summary>
		/// Конструктор
		/// </summary>
		public GWRequest()
		{
			attributes = new AttributesCollection();
			errCode = 6;
			state = 12;
			InitializeComponents();
		}

		public GWRequest(GWRequest src)
		{
			provider = src.Provider;
			templateName = src.TemplateName;

			tid = src.Tid;
			transaction = src.Transaction;
			agentId = src.AgentId;
			terminal = src.Terminal;
			terminalType = src.TerminalType;
			realTerminalId = src.RealTerminalId;
			service = src.Service;
			gateway = src.Gateway;
			operdate = src.Operdate;
			pcdate = src.Pcdate;
			terminalDate = src.TerminalDate;
			tz = src.Tz;
			transaction = src.Transaction;
			checkNumber = src.CheckNumber;
			oid = src.Oid;
			cur = src.Cur;
			session = Properties.Settings.Default.SessionPrefix + tid.ToString();
			state = src.State;

			lastcode = src.Lastcode;
			lastdesc = src.Lastdesc;
			times = src.Times;

			phone = src.Phone;
			phoneParam = src.PhoneParam;
			account = src.Account;
			accountParam = src.AccountParam;
			card = src.Card;
			filial = src.Filial;

			amount = src.Amount;
			amountAll = src.AmountAll;
			number = src.Number;
			orgname = src.Orgname;
			docnum = src.Docnum;
			docdate = src.Docdate;
			purpose = src.Purpose;
			fio = src.Fio;
			address = src.Address;
			agree = 1;
			contact = src.Contact;
			inn = src.Inn;
			comment = src.Comment;

			acceptdate = src.Acceptdate;
			acceptCode = src.AcceptCode;
			outtid = src.outtid;
			addinfo = src.AddInfo;
			errMsg = src.ErrMsg;
			opname = src.Opname;
			opcode = src.Opcode;

			pause = src.Pause;

			kpp = src.Kpp;
			payerInn = src.PayerInn;
			ben = src.Ben;
			bik = src.Bik;
			tax = src.Tax;
			kbk = src.KBK;
			okato = src.OKATO;
			payType = src.PayType;
			reason = src.reason;

			statusType = src.StatusType;
			startDate = src.StartDate;
			endDate = src.EndDate;

			attributes = new AttributesCollection();
			attributes.Add(src.Attributes);

			InitializeComponents();

		}

		/// <summary>
		/// Инициализация компонентов
		/// </summary>
		/// <returns></returns>
		public virtual void InitializeComponents()
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool disposed = false;
		/// <summary>
		/// Метод очситки памяти при завершении процесса
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;
			}
		}

		/// <summary>
		/// Хост для запроса Check
		/// </summary>
		/// <returns>string</returns>
		public virtual string GetCheckHost()
		{
			return Host;
		}

		/// <summary>
		/// Хост для запроса Pay
		/// </summary>
		/// <returns>string</returns>
		public virtual string GetPayHost()
		{
			return Host;
		}

		/// <summary>
		/// Хост для запроса Status
		/// </summary>
		/// <returns>string</returns>
		public virtual string GetStatusHost()
		{
			return Host;
		}

		/// <summary>
		/// Read timeout 
		/// </summary>
		/// <returns>Timeout sec.</returns>
		public virtual int TimeOut()
		{
			return 40;
		}
	
		public virtual string ParseError()
		{
			return "";
		}

		/// <summary>
		/// Запрос на поиск информации о платеже: Баланс и т.п.
		/// </summary>
		/// <returns>0 - OK; 1 - Fail</returns>
		public virtual int Find()
		{
			errCode = Properties.Settings.Default.CodeLongLongWait;
			errDesc = String.Format(Properties.Resources.MsgFNI, "Find");
			return 1;
		}

		/// <summary>
		/// Запишем состояние платежа, состояние 6, запись разблокирована
		/// </summary>
		public virtual void PaymentOK()
		{
		}

		/// <summary>
		/// Запишем состояние платежа, состояние 3, запись разблокирована
		/// </summary>
		public virtual void PaymentFail()
		{
		}

		/// <summary>
		/// Выполняет отсылку платежа
		/// </summary>
		/// <param name="host">Хост</param>
		/// <param name="old_state">Старое состояние</param>
		/// <param name="try_state">Требуемое состояние</param>
		/// <returns>0 - успех; 1 - неуспех</returns>
		public virtual int DoPay(byte old_state, byte try_state)
		{
			int retcode = 0;

			// Создание запроса
			if ((retcode = MakeRequest(old_state)) != 0)
			{
				errCode = retcode;
				state = 12;
				UpdateState(tid, state: state, errCode: retcode, errDesc: errDesc, locked: 0);
				return retcode;
			}

			// Отправка заппроса провайдеру
			if ((retcode = SendRequest(Host)) != 0)
			{
				if (retcode > 0) // TCP
				{
					errCode = 1;
					state = old_state;
				}
				else // Отложить запрос
				{
					errCode = 2;
					state = 11;
				}
				UpdateState(tid, state: state, errCode: retcode, errDesc: errDesc, locked: 0);
				return retcode;
			}

			// Разобрать ответ провайдера
			ParseAnswer(stResponse);

			byte lockRecord = 1;
			if (state == 6 || state == 11 || state == 12)
				lockRecord = 0; // Операция завершена

			// Фиксирование состояния в БД
			UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc,
							opname: Opname, opcode: Opcode, fio: fio, outtid: Outtid, account: Account,
							limit: Limit, limitEnd: XConvert.AsDate(LimitDate),
							acceptdate: XConvert.AsDate2(Acceptdate), acceptCode: AcceptCode,
							locked: lockRecord); // Разблокировать если проведён

			return retcode;
		}

		/// <summary>
		/// Абстрактный метод создания запроса. Должен быть реализован в дочернем классе.
		/// </summary>
		/// <param name="old_state"></param>
		/// <returns></returns>
		public virtual int MakeRequest(int old_state)
		{
			throw new NotImplementedException(string.Format("Не реализован метод MakeRequest. Provider={0}", Provider));
		}

		/// <summary>
		/// Задержка процесса на sec секунд
		/// </summary>
		/// <param name="sec">int секунды</param>
		public void Wait(int sec)
		{
			Thread.Sleep(sec * 1000);
		}

		/// <summary>
		/// Проверяет возможность платежа по указанным реквизитам с минимальной суммой
		/// </summary>
		/// <param name="OpenSession">
		/// Если true - то при последующем запросе Pay, 
		/// надо указать номер сессии, вринятом запросе. 
		/// По умолчанию false - сессия не открывается
		/// </param>
		/// <returns></returns>
		public virtual void DoCheck(bool OpenSession = false)
		{
		}

		/// <summary>
		/// Проверка возможности проведения платежа
		/// </summary>
		public virtual void Check()
		{
			try
			{
				DoCheck();
			}
			catch (Exception ex)
			{
				RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
				price = decimal.MinusOne;
				errCode = 400;
				errDesc = ex.Message;
				state = 12;
			}
		}

		/// <summary>
		/// Выролнить цикл проведения/допроведения платежа
		/// </summary>
		public virtual void Processing(bool New /*= true*/)
		{

			if (New)  // Новый платёж
			{
				if (MakePayment() != 0) return;

				// Сумма болше лимита и прошло меньше времени задержки отложить обработку запроса
				if (FinancialCheck(New)) return;

				if (DoPay(0, 1) != 0) return;
				if (DoPay(1, 3) != 0) return;
				DoPay(3, 6);
			}
			else // Redo
			{
				if (State == 0)
					{
					if (FinancialCheck(New)) return;
					if (DoPay(0, 1) != 0) return;
					if (DoPay(1, 3) != 0) return;
					}

				if (State == 1)
					{
					if (DoPay(1, 3) != 0) return;
					}
	
				DoPay(3, 6);
			}

		}

		/// <summary>
		/// Финансовы контроль
		/// </summary>
		protected virtual bool FinancialCheck(bool newPay)
			{

			string x = null;
			decimal AmountLimit = Settings.AmountLimit;
			int AmountDelay = Settings.AmountDelay;
			string Notify = "";

			if (!string.IsNullOrEmpty(Phone))
				x = Phone;
			else if (!string.IsNullOrEmpty(Account) && string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если задан Number, то используется он
				x = Account;
			else if (!string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если только не задан Card
				x = Number;
			else if (!string.IsNullOrEmpty(Card))
				x = Card;
			else
				{
				RootLog("{0} [FCHK] {1}/{2} Не задан номер счёта", Tid, Service, Gateway);
				return false;
				}

			// Если тип терминала не определён: считаем терминал и включаем финюконтроль
			if (State == 0 && TerminalType == 1) // Если только новый платёж
				{

				string trm = Terminal != int.MinValue? Terminal.ToString(): "NOREG";

				// Проверим в чёрном списке
				if (FindInBlackList(x))
					return true;
				
				// Если меньше допустимого лимита, не ставить на контроль
				if (AmountAll < AmountLimit)
					{
					RootLog("{0} [FCHK - stop] {1}/{2} Num=\"{3}\" сумма платежа меньше общего лимита {4}, завершение проверки", 
						Tid, Service, Gateway, x,XConvert.AsAmount(AmountLimit));
					return false;
					}

				// Если номер телефона в списке исключаемых завершить финансовый контроль
				if (FindInLists(Settings.Lists, x, 1) == 1) // Найден в белом списке
					{
					RootLog("{0} [FCHK - stop] {1}/{2} Num=\"{3}\" найден в белом списке, завершение проверки", Tid, Service, Gateway, x);
					return false;
					}

				foreach (var item in Settings.CheckedProviders)
					{
					// Если реквизиты платежа (провайдер/сервис/получатель) совпадают с эталонным
					if (item.Name.ToLower() == Provider.ToLower() && item.Service.ToLower() == Service.ToLower() && item.Gateway.ToLower() == Gateway.ToLower())
						{
						// Ищем переопределения для агента
						if (!FindAgentInList(out AmountLimit, out AmountDelay, out Notify))
							{
							AmountLimit = item.Limit;
							AmountDelay = Settings.AmountDelay;
							}

						if (AmountAll >= AmountLimit && Pcdate.AddHours(AmountDelay) >= DateTime.Now) // Проверка отправки СМС
							{
							RootLog("{0} [FCHK] Для агента AgentID=\"{1}\" заданы параметры: Limit={2} Delay={3} Notify={4}",
								Tid, AgentId < 0? "*": AgentId.ToString(), AmountLimit, AmountDelay, Notify);

							state = 0;
							errCode = 11;
							errDesc = string.Format("[Фин.контроль] Отложен до {0}",
								XConvert.AsDate(Pcdate.AddHours(AmountDelay)));
							UpdateState(Tid, state :State, errCode :ErrCode, errDesc :ErrDesc, locked :0);
							RootLog("{0} [FCHK - stop] {1}/{2} Trm={7} Num={6} A={3} S={4} - Платёж отложен до {5}",
								Tid, Service, Gateway, XConvert.AsAmount(Amount), XConvert.AsAmount(AmountAll), XConvert.AsDate(Pcdate.AddHours(AmountDelay)), x, Terminal);

							// Отправить СМС-уведомление, усли список уведомлений не пуст
							if (newPay && !string.IsNullOrEmpty(Notify))
								{
								this.Notify(Notify, string.Format("Num={0} S={1} Trm={2} блок до {3}",
									x, XConvert.AsAmount(AmountAll), Terminal, XConvert.AsDate(Pcdate.AddHours(AmountDelay))));
								}

							// Не найден  в белом списке - на контроль!
							return true;
							}
						}
		
					}
		
				}

			return false;
			}

		bool FindInBlackList(string x)
			{
			// RootLog("{0} [FCHK - strt] {1}/{2} Num=\"{3}\" поиск в чёрном списке", Tid, Service, Gateway, x);

			foreach (var item in Settings.CheckedProviders)
				{

				// Проверка любой суммы в чёрном списке
				if (item.Name.ToLower() == Provider.ToLower() 
						&& item.Service.ToLower() == Service.ToLower() 
						&& item.Gateway.ToLower() == Gateway.ToLower() 
						// && AmountAll >= item.Limit -- не проверяем лимит
						&& Pcdate.AddHours(Settings.AmountDelay) >= DateTime.Now)
					{

					// Если номер телефона в списке исключаемых завершить финансовый контроль
					if (FindInLists(Settings.Lists, x, 2) == 2) // Найден в чёрном списке
						{
						state = 12;
						errCode = 6;
						errDesc = string.Format("[BLACK] Отменён вручную");
						UpdateState(Tid, state :State, errCode :ErrCode, errDesc :ErrDesc, locked :0);
						RootLog("{0} [FCHK - BLCK] {1}/{2} Num={5} A={3} S={4} - Найден в чёрном списке. Отменён.",
							Tid, Service, Gateway, XConvert.AsAmount(Amount), XConvert.AsAmount(AmountAll), x);
						return true;
						}

					}

				}

			// RootLog("{0} [FCHK - strt] {1}/{2} Num=\"{3}\" в чёрном списке не найден", Tid, Service, Gateway, x);
			return false;
			}
	
		/// <summary>
		/// Поиск переопределений для агента
		/// </summary>
		/// <param name="Limit"></param>
		/// <param name="Notify"></param>
		/// <returns></returns>
		bool FindAgentInList(out decimal Limit, out int AmountDelay, out string Notify)
			{
			XDocument doc = null;
			Limit = decimal.MinusOne;
			AmountDelay = int.MinValue;
			Notify = "";
			int Id = 0;
			string NotifyDefault = "";
			bool IsDefault = false;

			// Открывает файл спсиков с разрешением чтения и записи несколькими процессами
			try
				{
				using (FileStream fs = new FileStream(Settings.Lists, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
					doc = XDocument.Load(fs);

					foreach (XElement el in doc.Root.Elements())
						{
						string name = el.Name.LocalName;
						string value = el.Value;
						switch (el.Name.LocalName)
							{
							// Секция переопределения агентов
							case "Agents":
								// Ищем агента
								foreach (XElement s in el.Elements())
									{
									switch (s.Name.LocalName)
										{
										case "Agent":
											foreach (var item in s.Attributes())
												{
												if (item.Name.LocalName == "ID")
													if (item.Value == "*") 
														IsDefault = true;
													else
														{
														Id = int.Parse(item.Value);
														IsDefault = false;
														}
												if (item.Name.LocalName == "Limit") Limit = decimal.Parse(item.Value);
												if (item.Name.LocalName == "AmountDelay") AmountDelay = int.Parse(item.Value);
												if (item.Name.LocalName == "Notify") Notify = item.Value;
												}
											if (Id == AgentId)
												return true;
											if (IsDefault)
												NotifyDefault = Notify;
											break;
										}
									}
								break;
							}
						}
					}
				}
			catch (Exception ex)
				{
				RootLog("[FCHK] Agents lists: {0}", ex.Message);
				}

			Notify = NotifyDefault;
			return false; // Агент не найден

			}

		/// <summary>
		/// Открывает чёрно-белый список и ищет в нём номер
		/// </summary>
		/// <param name="Listpath">Путь к списку</param>
		/// <param name="Number">Номер телефона/счёта</param>
		/// <returns>
		/// 0 - не найден; 
		/// 1 - найден в белом списке;
		/// 2 - найден в чёрном списке
		/// </returns>
		int FindInLists(string Listpath, string Number, int ListType)
			{

			XDocument doc = null;

			// Открывает файл спсиков с разрешением чтения и записи несколькими процессами
				try
					{
					using (FileStream fs = new FileStream(Listpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						doc = XDocument.Load(fs);

						// Выносится в отдельный файл
						// Settings.excludes.Clear();


						foreach (XElement el in doc.Root.Elements())
							{
							string name = el.Name.LocalName;
							string value = el.Value;
							// Console.WriteLine("Section: {0}", name);
							
							switch (el.Name.LocalName)
								{
								// Белый список
								case "White":
									if (ListType == 1)
										{
										if (FindInList(Number, el))
											return 1;
										}
									break;
								// Чёрный список
								case "Black":
									if (ListType == 2)
										if (FindInList(Number, el))
											return 2;
									break;
								}
							}
						}
					}
				catch (Exception ex)
					{
					RootLog("[FCHK] White/Black lists: {0}", ex.Message);
					}

			return 0; // Номер не найден
			}


		/// <summary>
		/// Поиск номера в чёрном или белом списке
		/// </summary>
		/// <param name="Number">Номер, string</param>
		/// <param name="el">Список, XElement</param>
		/// <returns>true - найден</returns>
		bool FindInList(string Number, XElement el)
			{
			foreach (XElement s in el.Elements())
				{
				switch (s.Name.LocalName)
					{
					case "Number":
						string Prefix = "";
						foreach (var item in s.Attributes())
							{
							if (item.Name.LocalName == "Prefix")
								{
								Prefix = item.Value.ToString();
								// Log("{0} [FCHK - found]  {1}", Tid, Prefix);
								if (Prefix != "" && Number.Length >= Prefix.Length && Number.Substring(0, Prefix.Length) == Prefix)
									{
									// Номер найден в списке
									return true;
									}
								}
							}
						break;
					}
				}
			return false;
			}

		public virtual int UpdateState(long tid, byte state = 255, int errCode = -1, string errDesc = null, int result = -1,
								string opname = null, string opcode = null, string fio = null, string account = null,
								string outtid = null, decimal limit = decimal.MinusOne, string limitEnd = null, byte locked = 255,
								string acceptdate = null, string acceptCode = null, string addinfo = null, decimal price = decimal.MinusOne)
		{

			if (state == 6)
				SendSMS();

			return Exec(sp: "UpdateState", tid: tid,
									state: state,
									errCode: errCode,
									errDesc: errDesc,
									opname: opname,
									opcode: opcode,
									fio: fio,
									account: account,
									outtid: outtid,
									limit: limit,
									limitend: limitEnd,
									locked: locked,
									acceptdate: acceptdate,
									acceptcode: acceptCode,
									addinfo: addinfo,
									price: price,
									result: result);
		}

		/// <summary>
		/// Создание новой записи в таблице Queue и Payment
		/// </summary>
		public virtual int MakePayment()
		{

			// ReportRequest("MakePayment");

			// Установка даты операции
			operdate = DateTime.Now;
			
			GetTerminalInfo();

			State = 0; // Новый

			return Exec(sp :"MakePayment2", tid :Tid,
										provider: provider,
										phone: phone,
										phoneParam: PhoneParam,
										account: account,
										accountParam: AccountParam,
										filial: filial,
										card: card,
										number: number,
										fio: fio,
										session: session,
										amount: amount,
										amountAll: amountAll,
										orgname: orgname,
										docnum: docnum,
										docdate: docdate,
										purpose: purpose,
										contact: contact,
										comment: comment,
										inn: inn,
										address: address,
										service: service,
										gateway: gateway,
										agentId: agentId,
										terminal: terminal,
										terminalType: terminalType,
										realTerminalId: realTerminalId,
										transaction: transaction,
										pcdate: (pcdate == DateTime.MinValue) ? null : (DateTime?)pcdate,
										terminalDate: terminalDate,
										tz: Tz,
										bik: bik,
										kpp: kpp,
										payerInn: payerInn,
										ben: ben,
										tax: tax,
										kbk: kbk,
										okato: okato,
										payType: payType,
										reason: reason,
										attributes: Attributes.SaveToXml());
		}

		/// <summary>
		/// Обновление параметров платежа для перепроведения
		/// </summary>
		/// <returns></returns>
		public virtual int UpdatePayment()
			{

			// ReportRequest("MakePayment");

			// Установка даты операции
			operdate = DateTime.Now;

			GetTerminalInfo();

			State = 0; // Новый

			return Exec(sp: "UpdatePayment", tid :Tid,
										provider :provider,
										operdate: operdate,
										phone :phone,
										phoneParam :PhoneParam,
										account :account,
										accountParam :AccountParam,
										filial :filial,
										card :card,
										number :number,
										fio :fio,
										session :session,
										amount :amount,
										amountAll :amountAll,
										orgname :orgname,
										docnum :docnum,
										docdate :docdate,
										purpose :purpose,
										contact :contact,
										comment :comment,
										inn :inn,
										address :address,
										service :service,
										gateway :gateway,
										agentId: agentId,
										terminal :terminal,
										terminalType :terminalType,
										realTerminalId :realTerminalId,
										transaction :transaction,
										pcdate :(pcdate == DateTime.MinValue) ? null : (DateTime?)pcdate,
										terminalDate :terminalDate,
										tz :Tz,
										bik :bik,
										kpp :kpp,
										payerInn :payerInn,
										ben :ben,
										tax :tax,
										kbk :kbk,
										okato :okato,
										payType :payType,
										reason :reason,
										attributes :Attributes.SaveToXml());
			}

		/// <summary>
		/// Разбор ответа провайдера
		/// </summary>
		/// <param name="stResponse"></param>
		/// <returns></returns>
		public virtual int ParseAnswer(string stResponse)
		{
			return 1;
		}

		public virtual int Exec(string sp,
			long tid,                    // Tid
			byte state = 255,           // Состояние платежа
			string provider = null,     // Провайдер
			string phone = null,        // Телефон
			string phoneParam = null,	// Наименование параметра
			string account = null,      // Номер счета (Тип платежа для Кибера)
			string accountParam = null, // Наименование параметра
			byte filial = 255,			// Номер филиала Ростелеком Сибирь
			string number = null,       // Номер счета для Кибера
			string card = null,			// Номер банковской карты
			string fio = null,          // ФИО плательщика
			string session = null,      // Номер сессии для кибера
			decimal amount = decimal.MinusOne,        // Сумма платежа
			decimal amountAll = decimal.MinusOne,    // Сумма, удержанная с клиента
			string currency = null,     // Валюта (всегда 810)
			string service = null,      // Номер платежного инстр. для ЕСПП, RecId для ЦУП, код шлюза для Кибера
			string gateway = null,      // Номер шлюза
			int agentId = int.MinValue, // Номер агента
			int terminal = int.MinValue,     // Номер терминала (точки)
			int terminalType = int.MinValue,		// Тип терминала
			int realTerminalId = int.MinValue,
			// string terminalPfx = null,	// Префикс перед номером точки (МТС)
			// int fakeTppId = int.MinValue,	// Подстановочный номер (если ТПП не зарегистрирован)
			// int fakeTppType = int.MinValue,	// Тип подстановочного терминала
			DateTime? operdate = null,	// Время операции
			DateTime? pcdate = null,       // Дата платежа в ПЦ
			DateTime? terminalDate = null,	// Дата платежа на терминале
			int tz = int.MinValue,
			string transaction = null,  // Номер транзакции
			string outtid = null,       // Номер платежа в системе провайдера
			string opname = null,       // Домашний оператор ЕСПП
			string opcode = null,       // Код оператора ЕСПП
			string limitend = null,     // Конец лимита
			decimal limit = decimal.MinusOne,         // Сумма лимита
			int errCode = int.MinValue,           // Код ошибки
			string errDesc = null,      // Описание ошибки
			int result = int.MinValue,			// Result
			string acceptdate = null,   // Дата акцепта платежа
			string orgname = null,      // Организация (Кибер)
			string docnum = null,       // Номер документа/постановления/квитанции и пр.
			string docdate = null,      // Дата (без времени) этого документа
			string purpose = null,      // Назначение платежа
			string address = null,      // Адрес плательщика
			byte agree = 255,           // Согласие на проведение платежа
			string inn = null,          // ИНН принципала
			string contact = null,      // Номер контактного телефона (возможно будет всегда записываться в поле Phone, а это поле будет пустым.
			string comment = null,      // Комментарий
			string acceptcode = null,   // Код авторизации у ПУ
			string addinfo = null,      // Реквизиты ПУ
			decimal price = decimal.MinusOne,		// Ожидаемая сумма
			string bik = null,			// БИК банка бенефициара
			string kpp = null,			// КПП
			string payerInn = null,		// ИНН плательщика
			string ben = null,			// Бенифициар
			int tax = int.MinValue,				// НДС
			string kbk = null,			// КБК
			string okato = null,		// ОКАТО
			int payType = int.MinValue,	// Тип платежа из рубрикатора
			string reason = null,		// Назначение платежа
			string attributes = null,	// Дополнительные атрибуты запроса
			byte locked = 255)
		{
			SqlDataReader dataReader = null;
			string sp_name = "OldiGW.ver3_" + sp;
			using (SqlParamCommand cmd = new SqlParamCommand(Settings.ConnectionString, sp_name))
			{
				cmd.AddParam("Tid", tid);

				cmd.AddParam("Provider", provider);
				cmd.AddParam("Phone", phone);
				cmd.AddParam("PhoneParam", phoneParam);
				cmd.AddParam("Account", account);
				cmd.AddParam("AccountParam", accountParam);
				cmd.AddParam("Filial", filial);
				cmd.AddParam("Number", number);
				cmd.AddParam("Card", card);
				cmd.AddParam("Session", session);
				cmd.AddParam("Amount", amount);
				cmd.AddParam("amountAll", amountAll);
				cmd.AddParam("Currency", currency);
				cmd.AddParam("Service", service);
				cmd.AddParam("AgentId", agentId);
				cmd.AddParam("TppId", terminal);
				cmd.AddParam("TppType", terminalType);
				cmd.AddParam("RealTppId", realTerminalId);
				cmd.AddParam("Pcdate", XConvert.AsDate(pcdate));
				cmd.AddParam("Operdate", XConvert.AsDate(operdate));
				cmd.AddParam("Terminaldate", terminalDate);
				cmd.AddParam("Tz", tz);
				cmd.AddParam("Transaction", transaction);
				cmd.AddParam("Opname", opname);
				cmd.AddParam("Opcode", opcode);
				cmd.AddParam("Limit", limit);
				cmd.AddParam("LimitEnd", limitend);
				cmd.AddParam("Outtid", outtid);
				cmd.AddParam("Acceptcode", acceptcode);
				cmd.AddParam("Acceptdate", acceptdate);
				cmd.AddParam("Addinfo", addinfo);
				cmd.AddParam("Price", price);
				cmd.AddParam("State", state);
				cmd.AddParam("ErrCode", errCode);
				cmd.AddParam("ErrDesc", errDesc);
				cmd.AddParam("Orgname", orgname);
				cmd.AddParam("Docnum", docnum);
				cmd.AddParam("Docdate", docdate);
				cmd.AddParam("Purpose", purpose);
				cmd.AddParam("Address", address);
				cmd.AddParam("Fio", fio);
				cmd.AddParam("Gateway", gateway);
				cmd.AddParam("agree", agree);
				cmd.AddParam("INN", inn);
				cmd.AddParam("BIK", bik);
				cmd.AddParam("contact", contact);
				cmd.AddParam("comment", comment);
				cmd.AddParam("kpp", kpp);
				cmd.AddParam("payerInn", payerInn);
				cmd.AddParam("ben", ben);
				cmd.AddParam("tax", tax);
				cmd.AddParam("kbk", kbk);
				cmd.AddParam("okato", okato);
				cmd.AddParam("payType", payType);
				cmd.AddParam("Reason", reason);

				// Дополнительные атрибуты платежа
				cmd.AddParam("Attributes", attributes);

				cmd.ConnectionOpen();
				dataReader = cmd.Execute(CommandBehavior.SingleRow | CommandBehavior.CloseConnection);
				cmd.ConnectionClose();

				errCode = cmd.ErrCode;
				errDesc = cmd.ErrDesc;
			}

			if (errCode != 0)
				RootLog("Exec.{0}: {1} {2}", sp_name, errCode, errDesc);
			return errCode;
		}

		/// <summary>
		/// Запрос дополнительной информации о терминале
		/// </summary>
		public virtual int GetTerminalInfo()
		{
			
			// Если номер терминала не задан выйти.
			if (Terminal == int.MinValue)
				{
				terminal = Settings.FakeTppId;
				terminal = Settings.FakeTppType;
				return 0;
				}

			Random rnd = new Random((int)DateTime.Now.Ticks);
			// Если PCdate не задан или старше 120 сек, скорректируем Pcdate
			if (Pcdate == DateTime.MinValue)
				pcdate = DateTime.Now;

			// Устанавливаем время с задержкой до 10 секунд
			DateTime time = new DateTime(pcdate.Ticks - TimeSpan.TicksPerSecond * rnd.Next(1, 10));


			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("[OldiGW].[ver3_GetTerminalInfo]", cnn))
			{
				cmd.Add("TppId", Terminal);
				cmd.Parameters.Add("Tz", SqlDbType.Int).Direction = ParameterDirection.Output;
				cmd.Parameters.Add("TppType", SqlDbType.Int).Direction = ParameterDirection.Output;
				cmd.Connection.Open();
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.ExecuteNonQuery();

				if (cmd.Parameters["TppType"].Value != DBNull.Value)
					terminalType = Convert.ToInt32(cmd.Parameters["TppType"].Value);
				else
					terminalType = 0;

				if (terminalType > 0)
				{
					if (cmd.Parameters["Tz"].Value != DBNull.Value)
						tz = Convert.ToInt32(cmd.Parameters["Tz"].Value);
					else
						tz = Settings.Tz;

					// Проверяем время только у МТС и статус 0
					if (Provider == ProvidersSettings.Mts.Name && state == 0)
						{
						RootLog("{0} Контроль времени терминал={1} PD = {2} TD = {3} TZ={4}", Tid, Terminal, Pcdate, TerminalDate, tz);

						if (TerminalType == 2)
							{
							RootLog("{0} Установка времени для терминала = {1} TD = {2} TZ={3}",
								Tid, Terminal, XConvert.AsDateTZ(time + TimeSpan.FromHours(Tz - Settings.Tz), Tz), tz);
							terminalDate = time - TimeSpan.FromHours(Tz - Settings.Tz);
							return 0;
							}

						if (TerminalDate == null)
							{
							RootLog("{0} Установка времени для терминала = {1} TD = {2} TZ={3}",
								Tid, Terminal, XConvert.AsDateTZ(time + TimeSpan.FromHours(Tz - Settings.Tz), Tz), Tz);
							terminalDate = time - TimeSpan.FromHours(Tz - Settings.Tz);
							}
						else
							{
							DateTime pc = Pcdate.AddHours(-1 * Settings.Tz);
							DateTime td = TerminalDate.Value.AddHours(-1 * Tz);
							if (td > pc)
								{
								RootLog("{3} Корректировка времени [-] PC={0} old TD={1} new TD={2} TZ={4}",
									XConvert.AsDateTZ(Pcdate, Settings.Tz),
									XConvert.AsDateTZ(TerminalDate, Tz),
									XConvert.AsDateTZ(time + TimeSpan.FromHours(Tz - Settings.Tz), Tz),
									Tid, Tz);
								terminalDate = time - TimeSpan.FromHours(Tz - Settings.Tz);
								}
							else if (td <= pc.AddSeconds(-10))
								{
								RootLog("{4} Корректировка времени [+] PC={0} old TD={1} new TD={2} diff={3} sec. TZ={5}",
									XConvert.AsDateTZ(Pcdate, Settings.Tz),
									XConvert.AsDateTZ(TerminalDate, Tz),
									XConvert.AsDateTZ(time + TimeSpan.FromHours(Tz - Settings.Tz), Tz),
									(pc.Ticks - td.Ticks) / TimeSpan.TicksPerSecond, Tid, Tz);
								terminalDate = time - TimeSpan.FromHours(Tz - Settings.Tz);
								}

							// terminalDate += TimeSpan.FromHours(Tz - Settings.Tz);
							}

						}
				}
				else // Терминал не зарегистрирован
				{
					// Если тип не задан установить его как тип из конфига
					RootLog("{1} GetTerminalInfo: Терминал {0} не зарегистрирован", Terminal, Tid);
					realTerminalId = terminal;
					terminal = Settings.FakeTppId;
					terminalType = Settings.FakeTppType;
					// Если терминал не зарегистрирован, время устанавливается по ПЦ
					tz = Settings.Tz;
					RootLog("{3} Корректировка времени PC={0} old TD={1} new TD={2} TZ={4}",
						XConvert.AsDateTZ(Pcdate, Settings.Tz),
						XConvert.AsDateTZ(TerminalDate, Tz),
						XConvert.AsDateTZ(time - TimeSpan.FromHours(Tz - Settings.Tz), Tz),
						Tid, Tz);
					terminalDate = time;
				}
			}

			return 0;
		}


		/// <summary>
		/// Запрос даже не был отправлен
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		public int TryAgain(string msg)
		{
			errCode = Properties.Settings.Default.CodeTryAgain;
			errDesc = msg;
			return -1;
		}

		public virtual string GetError(GWRequest gw)
		{
			return "";
		}

		/// <summary>
		/// Извлекает дополнительную информацию о запросе, и проверяет доступность БД
		/// </summary>
		public virtual int GetPaymentInfo()
		{
			SqlDataReader dataReader = null;
			// int tz;

			// try
			// {
			using (SqlParamCommand cmd = new SqlParamCommand(Settings.ConnectionString, "oldigw.ver3_GetPayInfo"))
			{
				cmd.AddParam("Tid", tid);
				cmd.ConnectionOpen();

				#region ReadParam
				using (dataReader = cmd.ExecuteReader(CommandBehavior.SingleRow))
					if (dataReader.Read())
					{
						Database.Param dp = new Database.Param(dataReader);
						dp.Read("ErrCode", out errCode);
						dp.Read("ErrDesc", out errDesc);
						if (errCode == 0)
						{
							ReadAll(dataReader);
						}
						else
						{
							ErrDesc = string.Format("GetPaymentInfo: ({0}) {1}", errCode, errDesc);
							errCode = 1;
						}
					}
					else
					{
						errCode = 1;
						ErrDesc = string.Format("GetPaymentInfo: ошибка чтения");
					}
				#endregion

				cmd.ConnectionClose();
			}
			// }
			// catch (Exception ex)
			// {
			//	ErrDesc = string.Format("GetPaymentInfo: {0}", ex.Message);
			//	errCode = 1;
			// }

			return errCode;

		}

		/// <summary>
		/// Отмена
		/// </summary>
		/// <returns></returns>
		public virtual int Undo()
		{
			state = 12;
			errCode = 6;
			errDesc = "Платёж отменён оператором";
			UpdateState(Tid, state :State, errCode :ErrCode, errDesc :ErrDesc, locked: 0);

			return 0;
		}
		
		/// <summary>
		/// Читает информацию о платеже из БД
		/// </summary>
		/// <param name="dr">SqlDataReader</param>
		/// <returns>0 - информация считана; 1 - ошибка БД</returns>
		public int ReadAll(SqlDataReader dr)
		{
			Database.Param dp = new Database.Param(dr);
			if (dr != null)
			{
				dp.Read("LastCode", out lastcode);
				dp.Read("LastDesc", out lastdesc);
				dp.Read("Provider", out provider);
				dp.Read("Times", out times);
				dp.Read("Tid", out tid);
				dp.Read("Service", out service);
				dp.Read("Gateway", out gateway);
				dp.Read("Phone", out phone);
				dp.Read("PhoneParam", out phoneParam);
				dp.Read("Account", out account);
				dp.Read("AccountParam", out accountParam);
				dp.Read("Number", out number);
				dp.Read("Card", out card);
				dp.Read("Amount", out amount);
				dp.Read("AmountAll", out amountAll);
				dp.Read("Operdate", out operdate);
				dp.Read("PCdate", out pcdate);
	
				string attrs;
				dp.Read("Attributes", out attrs);
				attributes.LoadFromXml(attrs);

				dp.Read("TerminalDate", out terminalDate);
				dp.Read("Tz", out tz);
				dp.Read("Transaction", out transaction);
				dp.Read("Session", out session);
				dp.Read("Orgname", out orgname);
				dp.Read("Docnum", out docnum);
				dp.Read("Docdate", out docdate);
				dp.Read("Purpose", out purpose);
				dp.Read("Address", out address);
				dp.Read("Fio", out fio);
				dp.Read("AgentId", out agentId);
				dp.Read("TppId", out terminal);
				dp.Read("TppType", out terminalType);
				dp.Read("RealTppId", out realTerminalId);
				//dp.Read("Agree", out agree);
				agree = 1;
				dp.Read("INN", out inn);
				dp.Read("Contact", out contact);
				dp.Read("Comment", out comment);
				dp.Read("State", out state);
				dp.Read("Lock", out locked);
				dp.Read("Outtid", out outtid);
				dp.Read("Acceptdate", out acceptdate);
				dp.Read("Addinfo", out addinfo);
				dp.Read("Filial", out filial);
				dp.Read("Opname", out opname);
				dp.Read("Opcode", out opcode);
				dp.Read("Limit", out limit);
				dp.Read("LimitEnd", out limitend);
				dp.Read("Price", out price);
					
				dp.Read("Ben", out ben);
				if (!string.IsNullOrEmpty(Ben))
					{
					dp.Read("KPP", out kpp);
					dp.Read("Reason", out reason);
					dp.Read("Tax", out tax);
					dp.Read("PayINN", out payerInn);
					dp.Read("KBK", out kbk);
					dp.Read("OKATO", out okato);
					dp.Read("PayAddress", out address);
					dp.Read("BenAcc", out account);
					}
				

				return 0;
			}

			return 1;
		}

		/// <summary>
		/// Блокировка записи
		/// </summary>
		/// <param name="newLock"></param>
		public void SetLock(int newLock)
		{
			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("OldiGW.Ver3_SetLock", cnn))
			{
				cmd.Parameters.AddWithValue("Tid", tid);
				cmd.Parameters.AddWithValue("Lock", newLock);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Connection.Open();
				cmd.ExecuteNonQuery();
			}
		}


		/// <summary>
		/// Статус платежа
		/// </summary>
		public void GetState()
		{

			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("OldiGW.Ver3_GetState", cnn))
			{
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("Tid", tid);
				cmd.Connection.Open();
				using (SqlDataReader dr = cmd.ExecuteReader())
				{
					if (dr.Read())
					{
						Database.Param p = new Database.Param(dr);
						p.Read("Provider", out provider);
						p.Read("Service", out service);
						p.Read("Gateway", out gateway);
						p.Read("Operdate", out operdate);
						p.Read("PCdate", out pcdate);
						p.Read("TerminalDate", out terminalDate);
						p.Read("Tz",out tz);
						p.Read("Tid", out tid);
						p.Read("Amount", out amount);
						p.Read("AmountAll", out amountAll);
						p.Read("State", out state);
						p.Read("ErrCode", out errCode);
						p.Read("ErrDesc", out errDesc);
						p.Read("Result", out result);
						p.Read("Times", out times);
						p.Read("Account", out account);
						p.Read("fio", out fio);
						p.Read("addinfo", out addinfo);
						p.Read("Outtid", out outtid);
						p.Read("Acceptdate", out acceptdate);
						p.Read("Acceptcode", out acceptCode);
						p.Read("Opname", out opname);
						p.Read("Opcode", out opcode);
						p.Read("Limit", out limit);
						p.Read("Limitend", out limitend);
						p.Read("Price", out price);
					}
					else
						state = 255;
				}
			}
			pause = times;
		}

		/// <summary>
		/// Получает 6-значный номер чека из номера транзакции
		/// </summary>
		/// <param name="Transaction">Номер транзакции</param>
		/// <returns>Номер чека</returns>
		public string MakeCheckNumber()
		{
			string tpl = string.IsNullOrEmpty(Transaction)? Tid.ToString(): Transaction;

			// string checknumber = tpl.Replace("-", "");
			char[] a = tpl.Replace("-", "").Reverse().ToArray();
			// char[] a = checknumber.Reverse().ToArray();
			string check = new string(a);
			if (check.Length > 6)
				check = check.Substring(0, 6);
			a = check.Reverse().ToArray();
			check = new string(a);

			// char[] b = new char[6];
			// for (int i = 0; i < 6; i++)
			//	b[i] = '0';
			// for (int i = 0; i < (a.Length < 6 ? a.Length : 6); i++)
			//	b[i] = a[i];
			// a = b.Reverse().ToArray();
			// checknumber = new string(a);

			a = null;
			// b = null;

			return check;
		}


		/// <summary>
		/// Вывод состояния платежа
		/// </summary>
		/// <param name="step"></param>
		public void ReportRequest(string step = null)
		{
			StringBuilder sb = new StringBuilder();

			if (Tid != 0 && Tid != int.MinValue)
				sb.AppendFormat("{0} ", Tid);
			if (!string.IsNullOrEmpty(step))
				sb.AppendFormat("[{0}] " , step);
			if (!string.IsNullOrEmpty(Service))
				sb.AppendFormat("{0}", Service);
			if (!string.IsNullOrEmpty(Gateway))
				sb.AppendFormat("/{0}", Gateway);

			sb.Append("A", Amount);
			sb.Append("S", AmountAll);
			sb.Append("Ph", Phone);
			sb.Append("Prm", PhoneParam);
			sb.Append("Acc", Account);
			sb.Append("Prm", AccountParam);
			sb.Append("Num", Number);
			sb.Append("Crd", Card);
			sb.Append("Con", Contact);
			sb.Append("FIO", Fio);
			sb.Append("Adr", Address);
			sb.Append("Ben", Ben);
			sb.Append("Op", Opname);
			sb.Append("Pc", XConvert.AsDate(Pcdate));
			// sb.Append("Td", XConvert.AsDateTZ(TerminalDate, Tz));
			if (State == 6)
				if (Acceptdate != DateTime.MinValue)
					sb.AppendFormat(" Ac={0}", XConvert.AsDate(Acceptdate));
			sb.Append("Trn", Transaction);
			sb.Append("Trm", Terminal);
			sb.Append("Typ", TerminalType);
			sb.Append("St", State);
			if (ErrCode != int.MinValue)
			{
				sb.Append("Err", ErrCode);
				if (string.IsNullOrEmpty(errDesc) && state == 0) errDesc = "Новый";
				if (errCode != 3)
					{
					sb.Append("Res", Result);
					sb.AppendFormat(" - {0}", ErrDesc);
					if (!string.IsNullOrEmpty(TechInfo))
						sb.AppendFormat(" ({0})", TechInfo);
					}
			}
			if (!string.IsNullOrEmpty(ErrMsg))
				sb.AppendFormat("\r\n{0}", ErrMsg);
			RootLog(sb.ToString());
		}

		/// <summary>
		/// Запрос статуса на стороне ПЦ
		/// </summary>
		public virtual void GetPaymentStatus()
		{
		}
		
		/// <summary>
		/// Отладочная информация
		/// </summary>
		/// <param name="step"></param>
		public void TraceRequest(string step = null)
		{
			if (Settings.LogLevel.IndexOf("REQ") == -1)
				return;
			
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("Prov={0} {1}/{2} ***** {3}", string.IsNullOrEmpty(step) ? RequestType : step, Provider, Service, !string.IsNullOrEmpty(Gateway) ? "/" + Gateway : "", Comment);
			sb.Append("Tid", Tid);
			sb.Append("A", XConvert.AsAmount(Amount));
			sb.Append("S", XConvert.AsAmount(AmountAll));
			sb.Append("Ph", Phone);
			sb.Append("Sub", PhoneParam);
			sb.Append("Acc", Account);
			sb.Append("AParam", AccountParam);
			sb.Append("Num", Number);
			sb.Append("Crd", Card);
			sb.Append("Con", Contact);
			sb.Append("FIO", Fio);
			sb.Append("Adr", Address);
			sb.Append("Ben", Ben);
			sb.Append("Op", Opname);
			sb.Append("Pc", XConvert.AsDate(Pcdate));
			sb.Append("Td", XConvert.AsDateTZ(TerminalDate, Tz));
			sb.AppendFormat(" Ac={0}", XConvert.AsDate(Acceptdate));
			sb.Append("Trn", Transaction);
			sb.Append("Trm", Terminal);
			sb.Append("Typ", TerminalType);
			sb.Append("St", State);
			if (ErrCode != 0)
			{
				sb.Append("Err", ErrCode);
				sb.Append("Res", Result);
				sb.AppendFormat(" - {0}", ErrDesc);
			}
			if (!string.IsNullOrEmpty(ErrMsg))
				sb.AppendFormat("\r\n{0}", ErrMsg);
			RootLog(sb.ToString());
		}
		
		/// <summary>
		/// Локальная копия лога
		/// </summary>
		/// <param name="fmt">string</param>
		/// <param name="_params">object[]</param>
		protected void Log(string fmt, params object[] _params)
		{
			Utility.Log(GetLogName(), fmt, _params);
		}
		protected void Log(string msg)
		{
			Utility.Log(GetLogName(), msg);
		}
		protected void Log(string msg, string p1)
		{
			Utility.Log(GetLogName(), msg, p1);
		}

		protected virtual string GetLogName()
		{
			return Settings.OldiGW.LogFile;
		}

		/// <summary>
		/// Ошибка Кибера
		/// </summary>
		public string CyberError(int CyberError)
		{
			ResourceManager res = Properties.Resources.ResourceManager;
			return string.Format("({0}) {1}", CyberError, res.GetString("Error" + CyberError.ToString()));
		}

		/// <summary>
		/// Отправка уведомления на один или несколько номеров
		/// </summary>
		/// <param name="List">Список номеров</param>
		/// <param name="Message">Сообщение</param>
		public void Notify(string List, string Message)
			{

			string From = "RegPlat";
			string[] phones = null;

			// Открыть TCP-канал
			// NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
			// EndpointAddress endPointAddress = new EndpointAddress("http://odbs1.regplat.ru:1101/sms");
			// ChannelFactory<IXSMPP> myChannelFactory = new ChannelFactory<IXSMPP>(binding, endPointAddress);
			// IXSMPP Client = myChannelFactory.CreateChannel();
			// Response r = null;

			if (List.IndexOf(',') != -1 || List.IndexOf(';') != -1 || List.IndexOf('|') != -1 || List.IndexOf(' ') != -1)
				{
				phones = List.Split(new Char[] { ' ', ',', ';', '|' });
				StringBuilder sb = new StringBuilder();
				foreach (string p in phones)
					{
					if (SendSMS(From, p, Message))
						RootLog("[SMSC] Уведомление {0} на {1} отправлено", Message, p);
					else
						RootLog("[SMSC] Уведомление {0} на {1} не отправлено", Message, p);
					}
				}
			else
				{
				if (SendSMS(From, List, Message))
					RootLog("[SMSC] Уведомление {0} на {1} отправлено", Message, List);
				else
					RootLog("[SMSC] Уведомление {0} на {1} не отправлено", Message, List);
				}

			// myChannelFactory.Close();

			}


		/// <summary>
		/// Отправка СМС
		/// </summary>
		/// <param name="From"></param>
		/// <param name="Phone"></param>
		/// <param name="Message"></param>
		/// <returns></returns>
		public bool SendSMS(string From, string Phone, string Message)
			{
			string ep = Config.AppSettings["SMPPEndpoint"];
			RootLog("Send SMS(\"{3}\") {0} From={1} To={2}", From, Message, Phone, ep);

			string answer = Get(ep, string.Format("from={0}&phone=7{1}&message={2}", From, Phone, Message));

			if (!string.IsNullOrEmpty(answer) && answer.IndexOf("<errCode>0</errCode>") != -1)
				return true;
			else
				return false;
			}


		/// <summary>
		/// Выполнение запроса GET
		/// </summary>
		/// <param name="Host"></param>
		/// <param name="Entry"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		string Get(string Host, params string[] x)
			{
			StringBuilder Url = new StringBuilder();
			Url.Append(Host);
			if (x != null && x.Length > 0)
				Url.AppendFormat("?{0}", x[0]);
			if (x != null && x.Length > 1)
				for (int i = 1; i < x.Length; i++)
					Url.AppendFormat("&{0}", x[i]);

			System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(new Uri(Url.ToString()));
			request.Method = "GET";
			request.Accept = "text/xml */*";
			request.UserAgent = "XNET-test";
			// request.ContentType = "application/x-www-form-urlencoded; charset=windows-1251";
			request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
			request.Headers.Add("Accept-Encoding", "identity");
			// request.Headers.Add("Accept-Encoding", "gzip, deflate, identity");

			// Set some reasonable limits on resources used by this request
			request.MaximumAutomaticRedirections = 4;
			request.MaximumResponseHeadersLength = 4;
			// Set credentials to use for this request.
			// CredentialCache myCache = new CredentialCache();
			// myCache.Add(new Uri(TargetUri), "Basic", new NetworkCredential(UserName, Password));
			// request.Credentials = myCache;

			RootLog("\r\nGet: Обращение к XSMPP: {0}", Url);
			foreach (string key in request.Headers.AllKeys)
				RootLog("{0} = {1}", key, request.Headers[key]);

			System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
			if (response == null)
				{
				RootLog("Get: Request faulted.");
				return null;
				}

			// Console.WriteLine("Content length is {0}", response.ContentLength);
			// Console.WriteLine("Content type is {0}", response.ContentType);
			// Console.WriteLine("The encoding method used is: " + response.ContentEncoding);
			// Console.WriteLine("The character set used is :" + response.CharacterSet);
			// Get the stream associated with the response.
			Stream receiveStream = response.GetResponseStream();

			RootLog("\r\nGetПолучен ответ:");
			foreach (string key in response.Headers.AllKeys)
				RootLog("{0} = {1}", key, response.Headers[key]);

			// Pipes the stream to a higher level stream reader with the required encoding format. 
			Encoding enc;
			if (!string.IsNullOrEmpty(response.CharacterSet))
				{
				if (response.CharacterSet.ToLower() == "windows-1251")
					enc = Encoding.GetEncoding(1251);
				else if (response.CharacterSet.ToLower() == "utf-8")
					enc = Encoding.UTF8;
				else
					enc = Encoding.ASCII;
				}
			else
				enc = Encoding.GetEncoding(1251);

			string buf = null;

			string[] ce = response.ContentEncoding.Split(new char[] { ',' });
			foreach (string c in ce)
				{
				if (c.ToLower() == "deflate")
					{
					using (DeflateStream dfls = new DeflateStream(receiveStream, CompressionMode.Decompress))
					using (StreamReader reader = new StreamReader(dfls, enc))
						buf = reader.ReadToEnd();
					// Log("Read Deflate. Charset=\"{0}\", Content-Length={1}", enc.WebName, buf.Length);
					break;
					}
				else if (c.ToLower() == "gzip")
					{
					using (GZipStream gzips = new GZipStream(receiveStream, CompressionMode.Decompress))
					using (StreamReader reader = new StreamReader(gzips, enc))
						buf = reader.ReadToEnd();
					// Log("Read GZip/{1}. Charset=\"{0}\"", enc.WebName, buf.Length);
					break;
					}
				else
					{
					using (StreamReader reader = new StreamReader(receiveStream, enc))
						buf = reader.ReadToEnd();
					// Log("Read uncompress. Charset=\"{0}\"", enc.WebName);
					break;
					}
				}

			receiveStream.Close();

			return buf;
			}

		/// <summary>
		/// Запись в основной лог
		/// </summary>
		/// <param name="fmt">Формат</param>
		/// <param name="_params">Параметры</param>
		protected void RootLog(string fmt, params object[] _params)
		{
			Utility.Log(Settings.OldiGW.LogFile, fmt, _params);
		}
	
	}
}
