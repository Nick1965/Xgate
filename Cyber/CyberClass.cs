using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections.Specialized;
using System.Resources;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using Oldi.Net;
using Oldi.Utility;


namespace Oldi.Net.Cyber
{
	/// <summary>
	/// Клас запроса к провайдеру Киберплат
	/// </summary>
	public partial class GWCyberRequest : GWRequest
	{
        protected string stAnswer;
        /// <summary>
        /// Секретный ключ
        /// </summary>
		public string SecretKey { get { return secret_key_path; } }
		protected string secret_key_path = "";

		/// <summary>
		/// Пароль на секретный ключ
		/// </summary>
		public string Passwd { get { return passwd; } }
		protected string passwd = "";
		/// <summary>
		/// Публичный ключ
		/// </summary>
		public string PublicKyes { get { return public_key_path; } }
        protected string public_key_path = "";
		/// <summary>
		/// Номер публичного ключа
		/// </summary>
		public string BankSerial { get { return serial; } }
		protected string serial = "";

        
#if __TEST
		public string TestString1 = "";
#endif
		/// <summary>
         /// Конструктор
         /// </summary>
        public GWCyberRequest()
			: base()
        {
        }

		public GWCyberRequest(GWRequest gw)
			: base(gw)
		{
		}

		/// <summary>
		///  Установка имени LOG-файла
		/// </summary>
		/// <returns></returns>
		protected override string GetLogName()
		{
			return Settings.Cyber.LogFile;
		}

		/// <summary>
		/// Тайм на операции соединения с провайдером
		/// </summary>
		/// <returns></returns>
		public override int TimeOut()
		{
			int timeout = 40;
			int.TryParse(Settings.Cyber.Timeout, out timeout);
			return timeout;
		}
	
		/// <summary>
		/// Инициализация переменных
		/// </summary>
		public override void InitializeComponents()
		{
			// m_log = Settings.Cyber.LogFile;

			try
			{
				CodePage = "1251";
				ContentType = "application/x-www-form-urlencoded";

				SD = ProvidersSettings.Cyber.SD;
				AP = ProvidersSettings.Cyber.AP;
				OP = ProvidersSettings.Cyber.OP;

				// Пути к файлам ключей
				secret_key_path = Settings.CyberPath + ProvidersSettings.Cyber.SecretKey;
				passwd = ProvidersSettings.Cyber.Passwd;
				public_key_path = Settings.CyberPath + ProvidersSettings.Cyber.PublicKeys;
				serial = ProvidersSettings.Cyber.BankKeySerial;

				checkHost = GetCheckHost();
				payHost = GetPayHost();
				statusHost = GetStatusHost();

			}
			catch (Exception ex)
			{
				RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
				Console.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}

			// base.InitializeComponents();

		}
	
		/// <summary>
		/// check
		/// </summary>
		/// <returns></returns>
		public override string GetCheckHost()
		{
			string host;

			if (string.IsNullOrEmpty(service))
				return "";
			
			if (service.Length == 2)
			{
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.PayCheck, service, service);
				if (!string.IsNullOrEmpty(gateway))
					host += "/" + gateway;
			}
			else if (string.IsNullOrEmpty(gateway))
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.ApiCheck, service);
			else
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.PayCheck, service, gateway);

			return host;

			/*
			return service.Length == 2 ?
				ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.PayCheck, service, service) :
				ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.ApiCheck, service);
			*/
		}

		/// <summary>
		/// pay
		/// </summary>
		/// <returns></returns>
		public override string GetPayHost()
		{
			string host;

			if (string.IsNullOrEmpty(service))
				return "";
			
			if (service.Length == 2)
			{
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.Pay, service, service);
				if (!string.IsNullOrEmpty(gateway))
					host += "/" + gateway;
			}
			else if (string.IsNullOrEmpty(gateway))
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.ApiPay, service);
			else
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.Pay, service, gateway);

			return host;

			/*
			return service.Length == 2 ?
				ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.Pay, service, service) :
				ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.ApiPay, service);
			 */
		}

		/// <summary>
		/// status
		/// </summary>
		/// <returns></returns>
		public override string GetStatusHost()
		{
			string host;

			if (string.IsNullOrEmpty(service))
				return "";

			if (service.Length == 2)
			{
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.PayStatus, service, service);
			}
			else if (string.IsNullOrEmpty(gateway))
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.ApiStatus, service);
			else
				host = ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.PayStatus, service, gateway);

			return host;

			/*
			return service.Length == 2 ?
				ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.PayStatus, service, service) :
				ProvidersSettings.Cyber.Host + string.Format(ProvidersSettings.Cyber.ApiStatus, service);
			*/
		}
		
		/// <summary>
		/// Создание запроса CHECK/PAY/STATUS
		/// </summary>
		/// <returns></returns>
		public int MakeRequest(bool status = false)
		{
			// ParamBuilder pb = new ParamBuilder(null, true);
			StringBuilder prm = new StringBuilder();
            string s_text = "";

            try
            {
				prm.AppendLine("SD", SD);
				prm.AppendLine("AP", AP);
				prm.AppendLine("OP", OP);
				prm.AppendLine("SESSION", "OLDIGW" + tid.ToString());

				if (!status)
				{
					if (!string.IsNullOrEmpty(ben)) // Бенефициар
					{
						//1839361
						GetReason();
						prm.AppendLine("NUMBER", "");
						prm.AppendLine("ACCOUNT", fio);
						StringBuilder purpose = new StringBuilder();
						purpose.Append("BENTYPE-corporate", "")
							.Append("||BEN-{0}", ben)
							.Append("||BENINN-{0}", inn)
							.Append("||BENKPP-{0}", kpp)
							.Append("||BENACC-{0}", account)
							.Append("||BENBANKBIK-{0}", bik)
							.Append("||REASON-{0}", reason)
							.Append("||REASON_FAT-{0}", tax == 0 ? "no_FAT" : tax == 10 ? "FAT10" : "FAT18")
							.Append("||PAYER_ADDRESS-{0}", address)
							.Append("||PRIORITY-06")
							.Append("||KBK-{0}", kbk)
							.Append("||OKATO-{0}", okato);
						prm.AppendLine("PURPOSE", purpose.ToString());
					}
					else
					{
						prm.AppendLine("NUMBER", number);
						prm.AppendLine("CARD", card);
						prm.AppendLine("FIO", fio);

						if (Service == "ge".ToLower() && Gateway == "484")
						{
							if (Account.Substring(0, 3) == "1||")
							{
								account = "21||";;
								RootLog("{0} Триколор. Замена услуги 1 на 21");
							}
							else if (Account.Substring(0, 3) == "7||")
							{
								account = "21||"; ;
								RootLog("{0} Триколор. Замена услуги 7 на 21");
							}
						}
						prm.AppendLine("ACCOUNT", Account);
						
						prm.AppendLine("DOCNUM", docnum);
						prm.AppendLine("DOCDATE", docdate);
						prm.AppendLine("PURPOSE", purpose);
						prm.AppendLine("ADDRESS", address);
					}

					prm.AppendLine("CONTACT", contact);
					prm.AppendLine("AMOUNT", XConvert.AsAmount(amount));
					prm.AppendLine("AMOUNT_ALL", XConvert.AsAmount(amountAll));
					prm.AppendLine("COMMENT", Comment);
					if (agree != 0) prm.AppendLine("AGREE", agree);
				}

				prm.AppendLine("ACCEPT_KEYS", ProvidersSettings.Cyber.BankKeySerial);

				IPriv.SignMessage(prm.ToString(), out stRequest, out s_text, secret_key_path, passwd);

				if (Settings.LogLevel.IndexOf("REQ") != -1)
					Log("\r\nПодготовлен запрос:\r\n{0}\r\n", s_text);

				errCode = 0;
				errDesc = null;
				return 0;

            }
            catch (IPrivException ie)
            {
                errDesc = string.Format("IPRIV: Sign={0} {1}", ie.code, ie.ToString());
				errCode = ie.code;
				/*
				if (state == 1)
					state = 0;
				*/
				state = 11;
			}
            catch (Exception ex)
            {
				// errCode = 11;
                errDesc = ex.Message;
				// if (state == 1)
				// state = 0;
				errCode = -1;
				state = 11;
            }

			RootLog("Cyber: {0}", errDesc);

			return 1;

		}

        /// <summary>
        /// Разбор ответа
        /// </summary>
        /// <param name="stResponse">Блок ответа</param>
        public override int  ParseAnswer(string stResponse)
        {
            // int pos;
            // int begin;

            // Разбор ответа
            if (string.IsNullOrEmpty(stResponse))
            {
				errDesc = Properties.Resources.MsgEmptyAnswer;
				errCode = 1;
				state = state == 1 ? (byte)0 : (byte)3;
				return 1;
            }

            // Проверка подписи сервера
			try
			{
				IPriv.VerifyMessage(stResponse, public_key_path, serial);
				// Log("Подпись проверена");

				// Ответы Киберплат.ком
				// BEGIN
				/*
				Код сессии (SESSION);
				Номер транзакции (TRANSID);
				Внешний номер платежа (RRN);
				Дата и время платежа в системе дилера (DATE);
				Код авторизации провайдера (AUTHCODE);
				Номер телефона (NUMBER);
				Номер счета (ACCOUNT);
				Сумма платежа (AMOUNT);
				Код ответа КиберПлат (ERROR, если ответ был получен);
				Результат операции (RESULT), по мнению дилера (0 — успех, 1 — ошибка).                
				*/
				// NameValueCollection nav = new NameValueCollection();
				string s = "";
				if (GetAnswer() == 0)
				{
					// Код ошибки
					if (!Int32.TryParse(GetValue("Error"), out errCode))
					{
						errCode = 400;
						errDesc = "Невозможно разобрать ответ провайдера";
						state = 12;
						return 100;
					}
					
					// Результат обработки
					if (!Int32.TryParse(GetValue("Result"), out result)) 
						result = 0;

					// Код авторизации платежа
					if (string.IsNullOrEmpty(acceptCode)) 
						acceptCode = GetValue("authcode");

					// Код транзакции в Кибере
					if (string.IsNullOrEmpty(outtid))
						outtid = GetValue("transID");

					// Дата проведения платежа
					if (acceptdate == DateTime.MinValue)
						DateTime.TryParse(GetValue("Date"), out acceptdate);

					// Дополнительная информация о ПУ
					if (string.IsNullOrEmpty(addinfo))
						addinfo = GetValue("addinfo");

					// Номер счета у ПУ
					if (string.IsNullOrEmpty(account)) 
						account = GetValue("account");

					// Остаток на счете
					if (limit == decimal.MinusOne)
						if (!string.IsNullOrEmpty(s = GetValue("rest")))
							decimal.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out limit);

					// Наименование ПУ
					if (string.IsNullOrEmpty(opname))
						opname = GetValue("opname");

					// Требуемая сумма
					if (price == decimal.MinusOne)
						if (!string.IsNullOrEmpty(s = GetValue("price")))
							decimal.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price);

					errMsg = GetValue("errmsg");
					return 0;
				}
			}
			catch (ApplicationException ae)
			{
				errCode = 400;
				errDesc = ae.Message;
				state = state == 1 ? (byte)0 : (byte)3;
			}
			catch (IPrivException ie)
			{
				ErrCode = ie.code;
				ErrDesc = string.Format("({0}) {1}", ie.code, ie.ToString());
				state = state == 1 ? (byte)0 : (byte)3;
			}
			catch (Exception ex)
			{
				ErrCode = 6;
				ErrDesc = string.Format("ParseAnswer: ({0}) {1}", errCode, ex.Message);
				RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
				state = 12;
			}

			return errCode;

        }

        /// <summary>
        /// Выбор из ответа блока Begin ... End
        /// </summary>
        /// <returns>-1 - ошибка разбора (ответ не полный); длина блока</returns>
        int GetAnswer()
        {
            int pos1;
            int pos2;

            try
            {
                // Выделим тело ответа между BEGIN .. END
                pos1 = stResponse.ToLower().IndexOf("begin");
                if (pos1 == -1) return -1;
                // Найдем = после ключевого слова
                pos2 = stResponse.ToLower().IndexOf("end", pos1);
                if (pos2 == -1) return -1;
                // Добавим еще 1 <CR><LF> для последнего аргумента
				stAnswer = stResponse.Substring(pos1 + 5, pos2 - pos1 - 6).Trim() + "\r\n";

                // return stAnswer.Length;
				return 0;
            }
            catch (Exception ex)
                {
					errCode = 100;	
					errDesc = string.Format("GetAnswer: {0}", ex.Message);
					return 1;
                }

            // Log("GetAnswer: stAnswer= {0}", stAnswer);
            
        }

		/// <summary>
        /// Выбор параметра из ответа
        /// </summary>
        /// <param name="key">Наименование параметра</param>
        /// <returns>Значение параметра</returns>
        string GetValue(string key)
        {
            string value = null;
            int pos2 = -1;
            int len = 0;

            int pos1 = stAnswer.IndexOf(key.ToUpper(), 0);
            try
            {
                if (pos1 != -1) // key найден в теле ответа
                {
                    pos1 += key.Length + 1;
                    pos2 = stAnswer.IndexOf("\r\n", pos1) - 1;
                    if (pos1 != -1 && pos2 == -2) pos2 = pos1;

                    // Log("Key={3} pos1={0} pos2={1} v={2}", pos1, pos2, pos2 - pos1, key.ToUpper());

                    len = pos2 - pos1 + 1;

                    // Log("{3}: pos1={0} pos2={1} len={2}", pos1, pos2, len, key);

                    // if (stAnswer.Length < pos2) return "";

                    if (pos1 < stAnswer.Length)
                        value = stAnswer.Substring(pos1, len);
                }
            }
            catch (Exception)
            {
                throw new ApplicationException (string.Format("GetValue: key={0}, pos1={1}, pos2={2}, len={3}, lenall={4}", key, pos1, pos2, len, stAnswer.Length));
            }

			// Log("GetValue: {0}={1}", key, value);
			return value;
        }

        /// <summary>
        /// Запрос на проведение платежа
        /// </summary>
        /// <returns></returns>
        public override int DoPay(byte old_state, byte try_state)
        {
            // Создадим платеж с статусом=0 и locId = 1
			// state 0 - новый
			// state 1 - получено разрешение

			string host;
			state = old_state;

			switch (old_state)
			{
				case 0:
					host = CheckHost;
					break;
				case 1:
					host = PayHost;
					break;
				case 3:
					host = StatusHost;
					break;
				default:
					host = StatusHost;
					break;
			}

			Log("\r\nHost: {0}", host);

			// Создание шаблона сообщения pay_check

			try
			{
				if (MakeRequest(status: old_state == 3? true: false) == 0)
					if (SendRequest(host) == 0)
						if (ParseAnswer(stResponse) == 0)
						{
							// Log("DoPay: error={0} result={1}", errCode, result);
							if (old_state == 0 || old_state == 1)
							{
								if (result == 0 && errCode == 0)
								{
									errCode = 0;
									state = try_state;
									errDesc = old_state == 0 ? "Разрешение на платеж получено" : "Платеж отправлен";
									// PrintParams("DoPay");
									UpdateState(tid: Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, result: result,
										outtid: outtid, acceptdate: XConvert.AsDate2(acceptdate),
										price: price, addinfo: addinfo);
									// Log("Обработан: {0}", ToString());
									return 0;
								}
							}
							else if (old_state == 3)
							{
								if (errCode == 0)
								{
									if (result == 7)
									{
										state = 6;
										errDesc = "Платеж проведен";
										// PrintParams("DoPay");
										UpdateState(tid: Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, result: result,
											acceptCode: acceptCode, acceptdate: XConvert.AsDate2(acceptdate));
										// Log("Обработан: {0}", ToString());
									}
									if (result == 1) // Платеж не зарегистраирован
									{
										state = 0;
										errDesc = "Платеж не зарегистрирован";
										// PrintParams("DoPay");
										UpdateState(tid: Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, result: result);
										// Log("Обработан: {0}", ToString());
									}
									else if (result != 7)
									{
										state = 3;
										errDesc = "Платеж проводится";
										// PrintParams("DoPay");
										UpdateState(tid: Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, result: result);
										// Log("Обработан: {0}", ToString());
									}
									errCode = 0;
									return 0;
								}
							}

							ChangeState(old_state); // state 1 или 0 или 12
							// PrintParams("DoPay");
							UpdateState(tid: Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, result: result);
							// Log("Не обработан: {0}", ToString());
							return 1;
						}
			}
			catch (Exception ex)
			{
				errCode = 6;
				errDesc = string.Format("Check: {0}", ex.Message);
				state = 12;
			}

			// Изменим состояние платежа
			ChangeState(old_state);

			if (old_state == 0 && ErrCode != 0 && !string.IsNullOrEmpty(AddInfo))
				errDesc += errDesc + ": " + AddInfo;
			
			UpdateState(tid: Tid, state: state, errCode: ErrCode, errDesc: ErrDesc,
				acceptCode: acceptCode, outtid: outtid, acceptdate: XConvert.AsDate2(acceptdate),
				price: price, addinfo: addinfo);

			errCode = state == 12 ? 6 : 1;

			return 1;
            
        }

		/// <summary>
		/// Назначение платежа
		/// </summary>
		public void GetReason()
		{
			try
			{
				using (SqlParamCommand pcmd = new SqlParamCommand(Settings.ConnectionString, "OldiGW.Ver3_GetReason"))
				{
					pcmd.AddParam("Tid", tid);
					pcmd.ConnectionOpen();
					using (SqlDataReader dr = pcmd.ExecuteReader(CommandBehavior.SingleRow))
						if (dr.Read())
							if (dr.GetInt32(dr.GetOrdinal("ErrCode")) == 0)
								reason = dr.GetString(dr.GetOrdinal("Reason"));
							else
								reason = null;
				}

			}
			catch (Exception ex)
			{
				reason = null;
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}

		}
	
		/// <summary>
		/// Установка состояния платежа и задержки (pause) если получена ошибка
		/// </summary>
		/// <param name="oldState"></param>
		public void ChangeState(byte oldState)
		{
			DateTime dateend = Operdate.AddHours(24);
			
			// Сетевая ошибка, либо таймаут
			if (errCode == 502)
			{
				state = oldState;
				return;
			}
			if (errCode < 0)
			{
				state = 11;
				return;
			}

			int lastError = errCode;

			if (oldState == 0)
				switch (errCode)
				{
					case 1: // сессия с таким номерорм уже существует. установить state = 1
						state = 3;
						break;
					case 6: // Неверная АСП (устарел ключ)
						state = 11;
						errCode = 2;
						break;
					case 21: // Недостаточно средств для проведения платежа
					case 25: // Работа шлюза приостановлена
					case 26: // Платежи данного Контрагента временно блокированы
					case 30: // Общая ошибка системы.
					case 50: // Проведение платежей в системе временно невозможно
					case 52: // Возможно, контрагент заблокирован.
					case 53: // Возможно, точка приема заблокирована.
					case 54: // Возможно, оператор точки приема заблокирован.
					// case 81: // Превышена максимальная сумма платежа по Контрагенту -- Отмеять!
					case 82: // Превышена сумма списаний за день по Контрагенту
					// case 83: // Превышена максимальная сумма платежа по точке -- Отменять!
					case 84: // Превышена сумма списаний за день по точке
						pause = 60;
						// state = 0;
						// После 24ч - отменить платёж
						state = 11;
						errCode = 2;
						break;
					case 24: // Ошибка связи с сервером Получателя.
								// На этапе проверки (chek) любой неверный код -- отмена платежа
						state = 12;
						// if (state == 0)
						//	RootLog("{1} Ожидание завершения технологического перерыва до: {0} ", XConvert.AsDate2(dateend), Tid);
						break;
					case 33: // Карты указанного номинала на данный момент в системе отсутствуют
					case 35: // Ошибка при изменении состояния платежа
					case 41: // Ошибка добавления данных платежа в базу данных
					case 51: // Не найдены данные в системе
						// pause = 10;
						state = 11;
						errCode = 2;
						break;
					case 20: // Завершается
					case 48: // Ошибка при сохранении в системе данных сессии
						// pause = 0;
						state = 12;
						break;
					default:
						state = 12;
						break;
				}
			else
				switch (errCode)
				{
					case 24: // Ошибка связи с сервером Получателя.
						// После 48 часов отменить платёж
						state = 12;
						/*
						if (operdate.AddHours(48) < DateTime.Now)
						{
							state = 12;
							RootLog("{0} Время выполнения запроса превысило 48 часов. Платёж отменён. Старт={1} стоп={2}", 
								Tid, XConvert.AsDate(Operdate), XConvert.AsDate(DateTime.Now));
						}
						else
							state = 3;
						*/
						break;
					case 6: // Неверная АСП (устарел ключ)
					case 21: // Недостаточно средств для проведения платежа
					case 25: // Работа шлюза приостановлена
					case 26: // Платежи данного Контрагента временно блокированы
					case 30: // Общая ошибка системы.
					case 36: // Транзакция уже завершена или находится в обработке
					case 50: // Проведение платежей в системе временно невозможно
					case 52: // Возможно, контрагент заблокирован.
					case 53: // Возможно, точка приема заблокирована.
					case 54: // Возможно, оператор точки приема заблокирован.
					case 81: // Превышена максимальная сумма платежа по Контрагенту
					case 82: // Превышена сумма списаний за день по Контрагенту
					case 83: // Превышена максимальная сумма платежа по точке
					case 84: // Превышена сумма списаний за день по точке
						pause = 60;
						state = 3;
						break;
					case 33: // Карты указанного номинала на данный момент в системе отсутствуют
					case 35: // Ошибка при изменении состояния платежа
					case 41: // Ошибка добавления данных платежа в базу данных
					case 46: // Не удалось завершить ошибочный платеж	
					case 48: // Ошибка при сохранении в системе данных сессии
					case 51: // Не найдены данные в системе
						// pause = 10;
						state = 11;
						errCode = 2;
						break;
					case 11: // Сессия с таким номером не существует. Платёж нужно повторить.
					case 34: // Транзакция с таким номером не найдена
						state = 0;
						break;
					case 20: // Завершается
					case 22: // Платеж не принят. Ошибка при переводе средств.
						// pause = 0;
						state = 3;
						break;
					default:
						if (errCode < 0)
						{
							state = 11;
							errCode = 2;
							errDesc = "Неизвестная ошибка. Обработка запроса отложена";
							RootLog("{0} Неизвестная ошибка state={1}\r\nОтвет Кибера:\r\n{2}", Tid, oldState, stAnswer);
							return;
						}
						else
							state = 12;
						break;
				}
			if ((oldState == 0 || oldState == 1) && ErrCode == 7 && Price != decimal.MinusOne && Price != 0M)
				errDesc = string.Format("({0}) Неверная сумма, ожидается: {1}", errCode, XConvert.AsAmount(Price));
			else
				errDesc = CyberError(lastError);
		}
    
	}

}
