using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using Oldi.Utility;
using System.Net;
using System.Resources;
using System.Web;
using System.Threading;
using System.Globalization;
using System.Collections.Specialized;

namespace RT
{
	public partial class RTRequest : GWRequest
	{
		const int MaxStrings = 1500;

		int? ReqStatus = null;		// Статус операции: началась, выполняется, выполнена...
		int? PayStatus = null;		// Статус платежа: зачислен, отклонён, отмененён...
		int? DupFlag = null;		// Флаг повторного проведения платежа
		DateTime? ReqTime = null;	// Время запроса
		string ReqNote = "";		// Сообщение об ошибке
		string ReqType = "";
		string SvcTypeID = "0";		// 0 или "" - федеральный номер телефона
		string SvcNum = "";				// Номер телефона / лицевого счёта
		string SvcSubNum = "";	// Субсчёт контрагента, V1 - Ростелеком
		string SvcComment = "";
		string[] states;
		string queryFlag = "";
		string agentAccount = ""; // Счёт учёта поставщика услуг

		new decimal? Balance = null;
		decimal? Recpay = null;
		
		public RTRequest()
			: base()
		{
		}

		public RTRequest(GWRequest src)
			: base(src)
		{
		}

		/// <summary>
		/// Инициализация значений класса
		/// </summary>
		public override void InitializeComponents()
		{
			base.InitializeComponents();
			CodePage = "utf-8";
			tz = Settings.Tz;

			if (Provider == "rtm")
				{
				commonName = Settings.Rtm.CN;
				host = Settings.Rtm.Host;
				agentAccount = Gateway;
				}
			else
				{
				commonName = Settings.Rt.CN;
				host = Settings.Rt.Host;
				if (Gateway != "0" && Gateway != "")
					{
					int filial = 10;
					if (!int.TryParse(Gateway, out filial))
						filial = 10;
					SvcTypeID = string.Format("RT.SIBIR.F{0:d3}.ACCOUNT_NUM", filial);
					}
				else
					SvcTypeID = Gateway;
				}
			
			if (string.IsNullOrEmpty(Phone))
			{
				SvcNum = Account;
				if (!string.IsNullOrEmpty(AccountParam))
					SvcSubNum = AccountParam;
			}
			else
			{
				SvcNum = Phone;
				if (!string.IsNullOrEmpty(PhoneParam))
					SvcSubNum = PhoneParam;
			}
			if (!string.IsNullOrEmpty(Comment))
				SvcComment = Comment;

			if (pcdate == DateTime.MinValue)
				pcdate = DateTime.Now;

			switch (RequestType.ToLower())
			{
				case "status":
					ReqType = "getPaymentStatus";
					break;
				case "payment":
					ReqType = "createPayment";
					break;
				case "undo":
					ReqType = "abandonPayment";
					break;
				case "check":
					ReqType = "checkPaymentParams";
					break;
				default:
					ReqType = RequestType;
					break;
			}

			if (!string.IsNullOrEmpty(Number))
				queryFlag = Number;

		}

		/// <summary>
		/// Установка имени лог-файла запросов к провайдеру
		/// </summary>
		/// <returns></returns>
		protected override string GetLogName()
		{
			return Provider == "rt"? Settings.Rt.LogFile: Settings.Rtm.LogFile;
		}

		/// <summary>
		/// Добавление HTTP-заголовков при отправке запроса
		/// </summary>
		/// <param name="request"></param>
		public override void AddHeaders(System.Net.HttpWebRequest request)
		{
			request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			request.Accept = "application/x-www-form-urlencoded";
			request.Headers.Add("Accept-Charset", "UTF-8");
		}

		/// <summary>
		/// Установка состояния платежа
		/// </summary>
		void GetErrorDescription()
		{
			int r = 0;
			ResourceManager res = Properties.Resources.ResourceManager;

			if (ReqStatus > 0)
				r = ReqStatus.Value + 100;
			else if (ReqStatus < 0)
				r = -ReqStatus.Value;
			if (ReqStatus != 0)
				ErrDesc = string.Format("{0}: {1}", ReqStatus, string.IsNullOrEmpty(ReqNote)? res.GetString("ERR_" + r.ToString()): ReqNote);
			else
				ErrDesc = string.Format("{0}: {1}", PayStatus, res.GetString("STS_" + PayStatus.ToString()));
		}

		/// <summary>
		/// Выролнить цикл проведения/допроведения платежа
		/// </summary>
		public override void Processing(bool New = true)
		{

			if (New)  // Новый платёж
			{
				if (MakePayment() != 0)
				{
					errCode = 6;
					if (string.IsNullOrEmpty(ErrDesc))
						errDesc = "Не могу создать новый платёж";
					Log("{0} {1}", Tid, ErrDesc);
				}
				else
				{
					DoPay(1, 6);
					// Если платёж не завершён, попытатья допровести
					/*
					if (state == 3)
					{
						UpdateState(Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, locked: 1);
						Log("{0} Ожидается завершение выполнения запроса", Tid);
						Thread.Sleep(30000); // Повтор не ранее чем через 30 сек.
						DoPay(3, 6);
						UpdateState(Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, locked: 0);
					}
					*/
					UpdateState(Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, locked: 0);
				}

				// Сделать ответ, если он вызван не из цикла REDO
				MakeAnswer();
			}
			else // Redo
			{
				/* REDO выполняется в OE

				// Log("{0} допроведение из статуса {1}", Tid, state);
				DoPay(state, 6);
				ParseAnswer(stResponse);
	
				// Log("{0} Проверка состояния: reqStatus = {1} payStatus = {2}", Tid, ReqStatus, PayStatus);

				if (ReqStatus == 0 && PayStatus == 2)
				{
					state = 6;
					errCode = 3;
				}
				else if (ReqStatus == 0 && (PayStatus == 3 || PayStatus == 4))
				{
					state = PayStatus == 3 ? (byte)12 : (byte)10;
					errCode = 6;
					errDesc = "Платёж отменён";
				}
				 
				 */
				UpdateState(Tid, locked: 0, state: State);
			}

			// Трассировка запроса ЕСПП
			// TraceRequest();
		}


		/// <summary>
		/// Создание нового платежа
		/// </summary>
		/// <returns></returns>
		/*
		public override int MakePayment()
		{
			if (state != 255) // Платёж существует
			{
				// Log("{0} - Платёж существует. Статус - {1}. Повторный платёж.", Tid, State);
				RootLog("{0} - Платёж существует. Статус - {1}. Повторный платёж.", Tid, State);
				// DoPay(3, 6);
				state = 1;
				UpdateState(Tid, state: 1, locked: 1);
				return 0;
			}
			else
			{
				// Log("{0} - Платёж не существует.", Tid);
				// RootLog("{0} - Платёж не существует.", Tid);
				return base.MakePayment();
			}
		}
		*/

		public override int Undo()
		{
			byte old_state = state;
			int OK = 1;
			string x = !string.IsNullOrEmpty(Phone)? Phone: 
								!string.IsNullOrEmpty(Account)? Account: 
								!string.IsNullOrEmpty(Number)? Number: "";

			// Log("{0} Попытка отмены платежа", Tid);
			Log("{0} [UNDO - начало] Num = {1} State = {2}", Tid, State != 255? State.ToString(): "<None>", x);
			if (MakeRequest(8) == 0 && SendRequest(Host) == 0 && ParseAnswer(stResponse) == 0)
				{
					ParseAnswer(stResponse);
					Log("{0} [UNDO - конец] reqStatus = {1} payStatus = {2}", Tid, ReqStatus, PayStatus);
					if (PayStatus == 3)
					{
						errCode = 6;
						errDesc = "Платёж отменён оператором";
						state = 10;
					}
					else if (PayStatus == 103)
					{
						errCode = 1;
						state = 3;
						errDesc = String.Format("Платёж {0} отменяется: {1}", Tid, ReqNote);
					}
					else
					{
						if (old_state == 6)
							errCode = 3;
						else if (old_state == 12)
							errCode = 6;
						else
							errCode = 1;
						errDesc = String.Format("Ошибка отмены платежа: {0}", ReqNote);
					}

					// Log("{0} err={1} desc={2} st={3}", Tid, ErrCode, ErrDesc, State);

					OK = 0;
				}	
			else
				{
				errCode = 2;
				state = 11;
				}

			UpdateState(Tid, errCode: ErrCode, errDesc: ErrDesc, locked: 0, state: state);
			MakeAnswer();
			return OK;
		}
		
		/// <summary>
		/// Создаёт запрос check/pay/status
		/// </summary>
		/// <param name="old_state"></param>
		/// <returns></returns>
		public override int MakeRequest(int old_state)
		{

			StringBuilder p = new StringBuilder();

			// выполнить проверку возможности платежа и вернуть баланс счёта
			if (old_state == 0) 
			{
				ReqType = "queryPayeeInfo";
				p.AppendFormat("reqType={0}", ReqType);
				if (!string.IsNullOrEmpty(SvcTypeID))
					p.AppendFormat("&svcTypeId={0}", HttpUtility.UrlEncode(SvcTypeID));

				p.AppendFormat("&svcNum={0}", SvcNum);
				if (!string.IsNullOrEmpty(Phone) && Provider == "rt")
					p.AppendFormat("&svcNum=7{0}", SvcNum);
				
				if (!string.IsNullOrEmpty(SvcSubNum))
					p.AppendFormat("&svcSubNum={0}", SvcSubNum);
				p.Append("&queryFlags=13");	// Остаток на счёте
			}
			// Создать запрос Pay
			else if (old_state == 1) 
			{
				ReqType = "createPayment";
				p.AppendFormat("reqType={0}", ReqType);
				p.AppendFormat("&srcPayId={0}", Tid);
				if (!string.IsNullOrEmpty(SvcTypeID))
					p.AppendFormat("&svcTypeId={0}", HttpUtility.UrlEncode(SvcTypeID));

				p.AppendFormat("&svcNum={0}", SvcNum);
				if (!string.IsNullOrEmpty(Phone) && Provider == "rt")
					p.AppendFormat("&svcNum=7{0}", SvcNum);
					
				if (!string.IsNullOrEmpty(SvcSubNum))
					p.AppendFormat("&svcSubNum={0}", SvcSubNum);

				// Непустой для РТ-Мобайл
				if (!string.IsNullOrEmpty(agentAccount))
					p.AppendFormat("&agentAccount={0}", agentAccount);

				p.AppendFormat("&payTime={0}", HttpUtility.UrlEncode(XConvert.AsDateTZ(Pcdate, Settings.Tz)));
				p.AppendFormat("&reqTime={0}", HttpUtility.UrlEncode(XConvert.AsDateTZ(Pcdate, Settings.Tz)));
				p.AppendFormat("&payAmount={0}", (int)(Amount * 100m));
				// PayDetails = string.Format("{0}|{1}|{3}", SvcSubNum, PayAmount, PayPurpose);
				
				p.Append("&payCurrId=RUB");
				p.Append("&payPurpose=0");
				p.AppendFormat("&payComment={0}", HttpUtility.UrlEncode(Comment));

			}
			// Создать запрос Status
			else if (old_state == 3) 
			{
				if (queryFlag != "")
				{
					p.Append("reqType=getPaymentStatus");
					if (!string.IsNullOrEmpty(Account))
						p.AppendFormat("&svcNum={0}", Account);
					if (!string.IsNullOrEmpty(AccountParam))
						p.AppendFormat("&svcSubNum={0}", AccountParam);
					if (SvcTypeID != "")
						p.AppendFormat("&svcTypeId={0}", HttpUtility.UrlEncode(SvcTypeID));
					p.AppendFormat("&queryFlags={0}", queryFlag);
				}
				else if (Tid != 0 && Tid != int.MinValue)
					p.Append("reqType=getPaymentStatus");
				else
					p.Append("reqType=getPaymentsStatus");
				if (Tid != 0 && Tid != int.MinValue)
					p.AppendFormat("&srcPayId={0}", Tid);
				if (StatusType != null)
					p.AppendFormat("&statusType={0}", StatusType);
				if (StartDate != null)
					p.AppendFormat("&startDate={0}", HttpUtility.UrlEncode(XConvert.AsDateTZ(StartDate, Settings.Tz)));
				if (EndDate != null)
					p.AppendFormat("&endDate={0}", HttpUtility.UrlEncode(XConvert.AsDateTZ(EndDate, Settings.Tz)));

				// Непустой для РТ-Мобайл
				if (!string.IsNullOrEmpty(agentAccount))
					p.AppendFormat("&agentAccount={0}", agentAccount);

			}
			// Запрос на отмену платежа
			else if (old_state == 8) 
			{
				ReqType = "abandonPayment";
				p.AppendFormat("reqType={0}", ReqType);
				p.AppendFormat("&srcPayId={0}", Tid);
				p.AppendFormat("&reqTime={0}", HttpUtility.UrlEncode(XConvert.AsDateTZ(Pcdate, Settings.Tz)));

				// Непустой для РТ-Мобайл
				if (!string.IsNullOrEmpty(agentAccount))
					p.AppendFormat("&agentAccount={0}", agentAccount);

			}
			else
			{
				errCode = 2;
				errDesc = string.Format("Неизвестное состояние ({0}) платежа", old_state);
				state = 11;
				Log(ErrDesc);
				RootLog(ErrDesc);
				return 1;
			}

			// p.AppendFormat("&reqTime={0}", UrlEncode(XConvert.AsDateTZ(Pcdate)));
			// stRequest = HttpUtility.UrlEncode(p.ToString()).Replace(HttpUtility.UrlEncode("&"), "&").Replace(HttpUtility.UrlEncode("="), "=");
			stRequest = p.ToString();

			Log("\r\nComment: {0}", SvcComment);
			
			// Log("Запрос к {0}\r\n=======================================\r\n{1}", Host, stRequest.Replace("&", "\r\n"));
			// Log(stRequest);
			// stRequest = HttpUtility.UrlEncode(stRequest);
			// Log(" \r\n--------------------------------------\r\n{0}", stRequest.Replace("%26", "\r\n"));
			
			return 0;
		}

		/// <summary>
		/// Запрс статуса
		/// </summary>
		/// <returns></returns>
		public override void GetPaymentStatus()
		{
			// Создать запрос
			MakeRequest(3);

			// Log(stRequest);

			// Отправить запрос
			if (SendRequest(Host) == 0)
			{
				// Log(" \r\n\t\t\t{0}", HttpUtility.UrlDecode(stRequest));
				// Log("------------------------------------------\r\n\t\t\t{0}", /*HttpUtility.UrlDecode*/(stResponse));

			ParseAnswer(stResponse);

				// Log("Количество строк: {0}", states.Length);
				// Log("------------------------------------------\r\n\t\t\tReqStatus={0}", ReqStatus);

				// int i = 0;
				// foreach (string item in states)
				//	Log("Строка {0}\t{1}", ++i, item);

				if (ReqStatus == 0)
				{
					switch (PayStatus)
					{
						case 2:
							errCode = 3;
							state = 6;
							errDesc = "Платёж проведён";
							break;
						case 3:
						case 4:
							errCode = 6;
							state = 12;
							errDesc = "Платёж отменён";
							break;
						case 102:
						case 103:
							errCode = 1;
							state = 3;
							errDesc = "Платёж обрабатывается";
							break;
						default:
							// Log("PayStatus = {0}, ReqStatus = {1}", PayStatus, ReqStatus);
							errCode = 1;
							state = 3;
							errDesc = "Получение статуса лицевого счёта";
							break;
					}
				}
				else if (ReqStatus == -12)
				{
					errCode = 6;
					errDesc = string.Format("Платёж не возможен или л/с не существует. reqStatus = {0}", ReqStatus);
					state = 12;
				}
				else
				{
					errCode = 6;
					errDesc = string.Format("Ошибка обработки запроса. reqStatus = {0}", ReqStatus);
					state = 12;
				}

			}
			// Создать ответ
			MakeAnswer();

			/*
			if (Settings.LogLevel.IndexOf("OEREQ") != -1)
			{
				Log("Ответ для OE:\r\n\t{0}", stResponse.Replace("\r\n", "\r\n\t"));
				RootLog("Ответ для OE:\r\n\t{0}", stResponse.Replace("\r\n", "\r\n\t"));
			}
			*/

			// Трассировка запроса ЕСПП
			// TraceRequest();

		}
		
		/// <summary>
		/// Выполнить запрос Check
		/// </summary>
		public override void DoCheck(bool session = false)
		{
			// Log(stRequest);
			
			// Создать запрос
			MakeRequest(0);

			// Отправить запрос
			if (SendRequest(Host) == 0)
			{
				ParseAnswer(stResponse);
				if (ReqStatus == 0)
				{
					errCode = 1;
					state = 1;
					errDesc = "Абонент найден";
				}
				else
				{
					errCode = 6;
					state = 12;
					errDesc = string.Format("{0}: {1}", ReqStatus, ReqNote != "" ? ReqNote : "Абонент не найден");
					fio = ReqNote != "" ? ReqNote : "Абонент не найден";
				}

			}

			// Создать ответ
			MakeAnswer();

		}

		void TraceRequest()
		{
			StringBuilder sb = new StringBuilder();

			if (Tid > 0)
				sb.AppendFormat("srcPayId = {0} ", Tid);
			sb.AppendFormat("reqType = {0} svcTypeId = {1} svcNum = {2} svcSubNum = {3} svcPurpose = 0 amount = {4}\r\n\t\t\tsvcComment = {5}\r\n",
				ReqType, SvcTypeID, SvcNum, SvcSubNum, XConvert.AsAmount(Amount), SvcComment);

			// Непустой для РТ-Мобайл
			if (!string.IsNullOrEmpty(agentAccount))
				sb.AppendFormat("\t\t\tagentAccount = {0}\r\n", agentAccount);
	
			if (ReqTime != null)
				sb.AppendFormat("\t\t\treqTime = {0}\r\n", XConvert.AsDateTZ(ReqTime.Value));
			if (Acceptdate != DateTime.MinValue)
				sb.AppendFormat("\t\t\tacceptedTme/abandonedTime = {0}\r\n", XConvert.AsDateTZ(Acceptdate));
			if (!string.IsNullOrEmpty(Outtid))
				sb.AppendFormat("\t\t\tesppPayId = {0}\r\n", Outtid);
			if (ReqStatus != null)
				sb.AppendFormat("\t\t\treqStatus = {0}\r\n", ReqStatus);
			if (PayStatus != null)
				sb.AppendFormat("\t\t\tpayStatus = {0}\r\n", PayStatus);
			if (DupFlag != null)
				sb.AppendFormat("\t\t\tdupFlag = {0}\r\n", DupFlag);
			if (!string.IsNullOrEmpty(ReqNote))
				sb.AppendFormat("\t\t\treqNote = {0}\r\n", ReqNote);
			if (!string.IsNullOrEmpty(AddInfo))
				sb.AppendFormat("\t\t\treqUsrMsg = {0}\r\n", AddInfo);
			if (!string.IsNullOrEmpty(Opname))
				sb.AppendFormat("dstDepCode = {0}\r\n", Opname);
			if (!string.IsNullOrEmpty(Fio))
				sb.AppendFormat("\t\t\tpayeeName = {0}\r\n", Fio);
			if (Price != decimal.MinusOne)
				sb.AppendFormat("\t\t\tpayeeRemain = {0}\r\n", XConvert.AsAmount(Price));

			if (sb.ToString().Length > 0)
				Log(sb.ToString());
		}

		/// <summary>
		/// Разбор ответ
		/// </summary>
		/// <param name="stResponse"></param>
		/// <returns></returns>
		public override int ParseAnswer(string stResponse)
		{
			string[] keys = stResponse.Split(new char[] { '&', '\r', '\n' }, MaxStrings, StringSplitOptions.RemoveEmptyEntries);
			int val;

			Log("********************");
			Log(stResponse);
			Log("********************");
	
			states = new string[0]; // Строки состояний
			int statep = 0;
			decimal decValue;
			ReqStatus = null;
			PayStatus = null;

			foreach (string key in keys)
			{
				// Log("Pare: {0}", HttpUtility.UrlDecode(key));
				string[] pare = key.Split(new Char[] { '=' });
				string name;
				string value;
				if (pare != null && pare.Length == 2)
				{
					name = pare[0];
					value = pare[1];
					switch (name)
					{
						case "esppPayId":
							outtid = value;
							break;
						case "reqTime":
							DateTime dt;
							if (DateTime.TryParse(HttpUtility.UrlDecode(value.Replace('T', ' ')), out dt))
								ReqTime = dt;
							break;
						case "reqStatus":
							if (int.TryParse(value, out val))
								ReqStatus = val;
							else
								ReqStatus = null;
							break;
						case "payStatus":
							if (int.TryParse(value, out val))
								PayStatus = val;
							else
								PayStatus = null;
							break;
						case "reqNote":			// Сообщение об ошибке
							ReqNote = HttpUtility.UrlDecode(value).Replace("{", "\\[").Replace("}", "\\]");
							break;
						case "reqUsrMsg":		// Сообщение для клиента
							addinfo = HttpUtility.UrlDecode(value);
							break;
						case "dupFlag":
							if (int.TryParse(value, out val))
							{
								DupFlag = val;
								addinfo = "Дублирование платежа!";
							}
							else
								DupFlag = null;
							break;
						case "dstDepcode":
							opname = value;
							break;
						case "accceptedTime":
						case "abandonedTime":
							acceptdate = DateTime.MinValue;
							DateTime.TryParse(HttpUtility.UrlDecode(value.Replace('T', ' ')), out acceptdate);
							break;
						case "reqType":
							ReqType = value;
							break;
						case "payeeRemain":		// Остаток средств на л/с
							decValue = 0;
							decimal.TryParse(value, out decValue);
							Balance = decValue / 100M;
							break;
						case "payeeRecPay":		// Рекомендуемый платёж
							decValue = 0m;
							decimal.TryParse(value, out decValue);
							Recpay = decValue / 100M;
							break;
						case "payeeName":
							fio = HttpUtility.UrlDecode(value);		// Инициалы абонента
							break;
					}
				}
				else if (pare != null && pare.Length == 1) // Строка массива
				{
					Array.Resize<string>(ref states, statep + 1);
					states.SetValue(pare[0], statep++);
				}
			}

			// Log("Количество строк: {0}, ответ:\r\n{1}", statep, stResponse);
			
			SetCurrentState();
			
			return 0;
		}

		/// <summary>
		/// Установка состояния платежа на основании ответа провайдера
		/// </summary>
		void SetCurrentState()
		{
			if (ReqStatus == 0)
			{
				switch (PayStatus)
				{
					case 2:		// Платёж выполнен
						state = 6;
						errCode = 3;
						if (ReqTime != null)
							acceptdate = ReqTime.Value;
						else
							acceptdate = DateTime.Now;
						break;
					case 3:		// Платёж отменён
						state = 8;
						errCode = 3;
						break;
					case 102:	// Проводится
					case 103:	// Отменяется
						state = 3;
						errCode = 1;
						break;
					case 4:		// Отклонён
						errCode = 6; // Платёж отклонён / отменёт. Финал.
						state = 12;
						break;
				}
			}
			else if (ReqStatus == 1 || ReqStatus == -1) // Платёж может быть допроведён
			{
				if (State == 1)
				{
					errCode = 6;
					errDesc = "";
					state = 12;
				}
				else
				{
					errCode = 1;
					state = 1;
					errDesc = "";
				}
			}
			else
			{
				errCode = 6;
				state = 12;
				errDesc = "";
			}
			
			GetErrorDescription();

		}

		/// <summary>
		/// Ответ OE
		/// </summary>
		public void MakeAnswer()
		{
			StringBuilder sb = new StringBuilder();
			StringBuilder sb1 = new StringBuilder();

			sb1.AppendFormat("\t<{0} code=\"{1}\">{2}</{0}>", "error", ErrCode, HttpUtility.HtmlEncode(ErrDesc));
			if (!string.IsNullOrEmpty(Outtid))
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "transaction", Outtid);
			if (Acceptdate != DateTime.MinValue && (State == 6 || State == 10))
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "accept-date", XConvert.AsDate(Acceptdate).Replace('T', ' '));
			if (!string.IsNullOrEmpty(Fio))
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "fio", Fio);
			if (!string.IsNullOrEmpty(Opname))
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "opname", Opname);
			if (!string.IsNullOrEmpty(Account))
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "account", Account);
			if (!string.IsNullOrEmpty(AddInfo))
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "info", AddInfo.Length <= 250 ? HttpUtility.HtmlEncode(AddInfo) : HttpUtility.HtmlEncode(AddInfo.Substring(0, 250)));
			
			if (Recpay != null)
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "recpay", XConvert.AsAmount(Recpay.Value));
			if (Balance != null)
				sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "balance", XConvert.AsAmount(Balance.Value));

			sb.AppendFormat("<{0}>\r\n{1}\r\n</{0}>\r\n", "response", sb1.ToString());
			
			stResponse = sb.ToString();

			if (tid > 0)
				UpdateState(Tid, state: State, locked: 1);

			// Log("Подготовлен ответ:\r\n{0}", stResponse);
		}

	}
}
