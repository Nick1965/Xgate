using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using Oldi.Utility;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Net;

namespace Oldi.Ekt
{

	public partial class GWEktRequest : GWRequest
	{

		/// <summary>
		/// Допроведение платежа
		/// </summary>
		/// <param name="State"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/*
		public override void Processing(byte State, int ErrCode, string ErrDesc)
		{
			state = State;
			errCode = ErrCode;
			errDesc = ErrDesc;
			Processing(false);
		}
		*/

		/// <summary>
		/// Выролнить цикл проведения/допроведения платежа
		/// </summary>
		public override void Processing(bool New)
		{

            int oldstate = State;

            if (New)  // Новый платёж
			{
                // QIWI:
                // Скорретируем AmountAll: + 2.%
                // Корректировать QIWI больше не будем
                /*
                if (Gateway == qiwiGateway && AmountAll > Amount)
                {
                    RootLog($"{Tid} [CHNG AmountAll] {Service}/{Gateway} корретировка");

                    decimal oldAmount = AmountAll;
                    /\*
                    if (TerminalType == 1)
                    {
                        amountAll = Amount / (1m - 0.025m);
                        amountAll = Math.Round(AmountAll / 10m, 0) * 10m;
                        if ((1m - Amount / AmountAll) * 100m > 3.5m)
                            amountAll = Amount;
                    }
                    else
                        amountAll = Math.Round(Amount / (1m - 0.035m), 2);
                    *\/

                    if ((1m - Amount / AmountAll) * 100m > 4.4m)
                        amountAll = Math.Round(Amount / (1m - 4.4m / 100m), 0);

                    if (AmountAll > oldAmount)
                    {
                        amountAll = oldAmount; // Откат к старому значению. Корретировка не нужна
                        RootLog($"{Tid} [CHNG AmountAll] {Service}/{Gateway} корретировка не требуется");
                    }
                    else
                    {
                        RootLog($"{Tid} [CHNG AmountAll] {Service}/{Gateway} корретировка");
                        RootLog($"{Tid} [CHNG]  A={Amount.AsCurrency()} Old={oldAmount.AsCurrency()} New={AmountAll.AsCurrency()} Perc={Math.Round(1 - Amount / AmountAll, 2)} TType={TerminalType}");
                    }
                }
                */

                if (MakePayment() == 0)
				{
                    // TraceRequest("New");

                    // Проверка дневного лимита для нового плательщика если это не QIWI.
                    if (!Gateway.Equals(qiwiGateway))
                    {
                        if (DayLimitExceeded(true)) return;
                        // Сумма болше лимита и прошло меньше времени задержки отложить обработку запроса
                        if (FinancialCheck(New)) return;
                    }
				    DoPay(0, 3);
				}
                RootLog($"{Tid} [EktProcessing - NEW start] {Provider} {Service}/{Gateway} {AmountAll.AsCF()} state={State}");
				// TraceRequest("End");
			}
			else // Redo
			{
                // Сумма болше лимита и прошло меньше времени задержки отложить обработку запроса
                if (State == 0)
                {
                    // Проверка дневного лимита для нового плательщика если это не QIWI.
                    if (!Gateway.Equals(qiwiGateway))
                    {
                        if (DayLimitExceeded(false)) return;
                        if (FinancialCheck(false)) return;
                    }
                }
				DoPay(state, 6);
                RootLog($"{Tid} oldstate={oldstate} [EktProcessing - REDO start] {Provider} {Service}/{Gateway} {AmountAll.AsCF()} state={State}");
            }
        }


        /// <summary>
        /// Количество повторов при ошибке SSL
        /// </summary>
        int Attempt = 0;

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
			state = 3;
			errCode = 1;
			// Создание шаблона сообщения check/pay/status

			// БД уже доступна, не будем её проверять
			if ((retcode = MakeRequest(old_state)) == 0)
			{
				// retcode = 0 - OK
				// retcode = 1 - TCP-error
				// retcode = 2 - Message sends, but no answer recieved.
                // retcode = 10 - SSL error
				// retcode < 0 - System error
				// if (Settings.LogLevel.IndexOf("REQ") != -1)
				//	Log("\r\nПодготовлен запрос к: {0}\r\n{1}", Host, stRequest);

				if ((retcode = SendRequest(Host)) == 0)
				{
					// Разберем ответ
					ParseAnswer(stResponse);

					switch (result.state)
					{
						case 10: // Финансовый контроль
							errCode = 11;
							state = 3;
							break;
                        /*
                        case 40:
							if (result.substate == 7)
								errCode = 11;
							else
								state = 3;
								errCode = 1;
							break;
						*/
                        case 80:
							if (result.code == 30) // Нет денег - длиный интервал
							{
								errCode = 12;
								state = old_state == 0? (byte)0: (byte)3;
							}
							else 
							{
								errCode = 6;
								state = 12;
							}
							break;
						case -2: // Платёж не найден при проверке статуса. Отменяем платёж
							errCode = 6;
							state = 12;
							break;
						case 60:
							errCode = 3;
							state = 6;
							break;
						default:
							errCode = 1;
							state = 3;
							break;
					}
				}
                else if ((WebExceptionStatus)retcode == WebExceptionStatus.SecureChannelFailure) // Ошибка SSL
                {
                    if (Attempt < 3)
                    {
                        Attempt++;
                        Log("{0} - {1} / 3 Повтор после ошибки SSL", Tid, Attempt);
                        return DoPay(old_state, try_state);
                    }
                    errCode = 11;
                    state = old_state;
                }
                else if (retcode > 0) // Ошибка TCP или таймаут
				{
					errCode = 11;
					state = old_state;
				}
				else
				{
					errCode = 2;
					state = 11;
				}
			}
			else
			{
				errCode = retcode;
				state = 12;
			}

			if (retcode == 0)
			{
				byte l = 0;

				if (old_state == 0 && errCode == 1)
				{
					retcode = 0;
					l = 1;
				}
				else
					retcode = 1;

				UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc,
				opname: Opname, opcode: Opcode, fio: fio, outtid: Outtid, account: Account,
					limit: Limit, limitEnd: XConvert.AsDate(LimitDate),
					acceptdate: XConvert.AsDate2(Acceptdate), acceptCode: AcceptCode,
					locked: l); // Разблокировать если проведён
				return retcode;
			}
			else
			{
				// if (Settings.LogLevel.IndexOf("DEBUG") != -1)
				//	RootLog("Ответ провайдера: Tid={0} State={1}, Substate={2}, Code={3}", Tid, result.state, result.substate, result.code);
				UpdateState(tid, state: state, errCode: retcode, errDesc: errDesc, locked: 0);
				return 1;
			}

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

			// Возможно 2 варианта: result и error
			if (doc.Root.Name.LocalName.ToString() == "response")
			{
				XElement root = XElement.Parse(stResponse);
				IEnumerable<XElement> fields =
					from el in root.Elements("result")
					select el;

				foreach (XAttribute el in fields.Attributes())
				{

					switch (el.Name.LocalName.ToString().ToLower())
					{
						case "state": // Состояние платежа
							int.TryParse(el.Value, out result.state);
							if (result.state == 60)
								acceptdate = DateTime.Now;
							break;
						case "substate": // Сообщение об ошибке
							int.TryParse(el.Value, out result.substate);
							break;
						case "code": // Сообщение об ошибке
							int.TryParse(el.Value, out result.code);
							break;
						case "trans": // Сообщение об ошибке
							outtid = (string)el.Value;
							break;
						default:
							// Error = ErrTemplatelInvalid;
							break;
					}
				}

				// Извлеяём описание ошибки
				errDesc = GetErrDesc();

			}
			else // error
			{
				errDesc = (string)doc.Root.Value;
				result.state = -2;
			}

			doc = null;
			return 0;
		}

		/// <summary>
		/// Формирование сообщения об ошибке
		/// </summary>
		/// <returns></returns>
		string GetErrDesc()
		{
		string msg = Messages.State60;

			switch (result.state)
			{
				case -2:
					msg = Messages.State_minus_2;
					break;
				case -1:
					msg = Messages.State_minus_1;
					break;
				case 0:
					msg = Messages.State0;
					break;
				case 10:
					msg = Messages.State10;
					times = 11;
					break;
				case 20:
					switch(result.substate)
					{
						case 1:
							msg = Messages.State20_1;
							break;
						case 2:
						case 3:
							msg = Messages.State20_2_3;
							break;
						case 4:
							msg = Messages.State20_4;
							break;
						default:
							msg = string.Format(Messages.State20_X, result.substate, result.code);
							break;
					}
					break;
				case 40:
					switch (result.substate)
					{
						case 1:
							msg = Messages.State40_1;
							break;
						case 2:
						case 3:
							msg = Messages.State40_2_3;
							break;
						case 4:
						case 5:
							msg = Messages.State40_4_5;
							break;
						default:
							msg = string.Format(Messages.State40_X, result.substate, result.code);
							break;
					}
					break;
				case 60:
					msg = Messages.State60;
					break;
				case 80:
					switch (result.substate)
					{
						case 1:
						case 2:
						case 3:
							msg = Messages.State80_1_2_3;
							break;
						default:
							switch (result.code)
							{
								case 1:
								case 2:
									msg = Messages.Code1;
									break;
								case 3:
									msg = Messages.Code3;
									break;
								case 4:
									msg = Messages.Code4;
									break;
								case 5:
									msg = Messages.Code5;
									break;
								case 6:
									msg = Messages.Code6;
									break;
								case 7:
									msg = Messages.Code7;
									break;
								case 8:
									msg = Messages.Code8;
									break;
								case 9:
									msg = Messages.Code9;
									break;
								case 10:
									msg = Messages.Code10;
									break;
								case 12:
									msg = Messages.Code12;
									break;
								case 13:
									msg = Messages.Code13;
									break;
								case 14:
									msg = Messages.Code14;
									break;
								case 15:
									msg = Messages.Code15;
									break;
								case 16:
									msg = Messages.Code16;
									break;
								case 17:
									msg = Messages.Code17;
									break;
								case 18:
									msg = Messages.Code18;
									break;
								case 19:
									msg = Messages.Code19;
									break;
								case 20:
									msg = Messages.Code20;
									break;
								case 21:
									msg = Messages.Code21;
									break;
								case 22:
									msg = Messages.Code22;
									break;
								case 30:
									msg = Messages.Code30;
									times = 51;
									break;
								case 33:
									msg = Messages.Code33;
									times = 51;
									break;
								default:
									msg = string.Format(Messages.State80_X, result.substate, result.code);
									break;
							}
							break;
					}
					break;
				default:
					msg = string.Format("({0}/{1}/{2})", result.state, result.substate, result.code);
					break;
			}
			return !string.IsNullOrEmpty(msg)? msg: string.Format("({0}/{1}/{2})", result.state, result.substate, result.code);
		}

		/// <summary>
		/// Создание запроса. Скрывает базовый код.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public override int MakeRequest(int state)
		{
            string acnt = !string.IsNullOrEmpty(Account) ? Account : !string.IsNullOrEmpty(Card) ? Card : Phone;

            if (State == 0)
            {

                if (string.IsNullOrEmpty(acnt) || string.IsNullOrEmpty(pointid) || string.IsNullOrEmpty(Gateway))
                {
                    state = 0;
                    errCode = 2;
                    errDesc = string.Format(Messages.Err_NoDefaults, state);
                    RootLog($"Account=\"{Account}\"\r\nPhone=\"{Phone}\"\r\nCard=\"{Card}\"\r\nPointid=\"{pointid}\"\r\nGateway=\"{Gateway}\"");
                    RootLog(ErrDesc);
                    return 1;
                }

                if (Amount == decimal.MinusOne)
                {
                    state = 0;
                    errCode = 2;
                    errDesc = Messages.Err_NoAmount;
                    RootLog($"Pointid=\"{pointid}\"\r\nGateway=\"{Gateway}\"");
                    RootLog(Messages.Err_NoAmount);
                    return 1;
                }

            }

			if (Pcdate == DateTime.MinValue)
				pcdate = DateTime.Now;
			int lTz = Tz != -1? Tz: Settings.Tz;
			string sDate = XConvert.AsDate(pcdate) + string.Format("+{0:D2}00", lTz);

            string sAmount = Math.Round(Amount * 100).ToString();
            string sAmountAll = Math.Round(AmountAll * 100).ToString();
            string check = MakeCheckNumber();
            string sAttributes = "";

            stRequest = Properties.Resources.Template_XmlHeader + "\r\n";

            if (state == 0) // Payment
				{
                /*
                <request point="{0}">
	                <payment account="{1}" service="{2}" sum="{3}" sum-in="{4}" id="{5}" check="{6}" date="{7}">
                        <attribute name="id2" value="{8}" />
	                </payment>
                </request>
                 */

                if (!string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(Number)) // Атрибуты: id1 = account, id2 = number
                {
                    if (Attributes == null)
                        attributes = new AttributesCollection();
                    attributes.Add("id2", Number);
                }
                else if (!string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(Phone)) // Id1 и phone
                {
                    if (Attributes == null)
                        attributes = new AttributesCollection();
                    attributes.Add("phone", Phone);
                }

                foreach (string name in Attributes.Keys)
                    sAttributes += $@"<attribute name=""{name}"" value=""{Attributes[name]}"" />";

                stRequest += $@"<request point=""{pointid}"">";
                stRequest += $@"<payment id=""{Tid}"" sum=""{sAmount}"" sum-in=""{sAmountAll}"" check=""{check}"" service=""{Gateway}"" account=""{acnt}"" date=""{sDate}""";
                if (Terminal != int.MinValue)
                    stRequest += $@" terminal-vps-id=""{Terminal}""";
                stRequest += ">";
                if (!string.IsNullOrEmpty(sAttributes))
                    stRequest += $"{sAttributes}";
                stRequest += "</payment></request>";

                /*
                if (Gateway == "1668")
					stRequest += string.Format(Properties.Resources.Template_Payment_1668, pointid, Card, Gateway, sAmount, sAmountAll, Tid, MakeCheckNumber(), sDate, Phone);
				// Единый кошелёк
				else if (Gateway == "458")
					{
					if (Account.Length > 10 || (Account.Length == 10 && Account.Substring(0) != "9")) // Лицевой счёт
						stRequest += string.Format(Properties.Resources.Template_Id1, pointid, Account, Gateway, sAmount, sAmountAll, Tid, MakeCheckNumber(), sDate);
					else // Номер телефона
						stRequest += string.Format(Properties.Resources.Template_Id1, pointid, Gateway, sAmount, sAmountAll, Tid, MakeCheckNumber(), sDate, Account);
					}
				else if (!string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(Number)) // Атрибуты: id1 = account, id2 = number
                    stRequest += string.Format(Properties.Resources.Template_Payment_Id1Id2, pointid, Gateway, sAmount, sAmountAll, Tid, MakeCheckNumber(), sDate, Account, Number);
				else
                if (!string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(Phone)) // Id1 и phone
					stRequest += string.Format(Properties.Resources.Template_Payment_Id1Phone, pointid, Gateway, sAmount, sAmountAll, Tid, MakeCheckNumber(), sDate, Account, Phone);
				else if (string.IsNullOrEmpty(Account) && string.IsNullOrEmpty(Phone) && Attributes?.Count > 0) // Есть дополнительные атрибуты
					{
					StringBuilder sb = new StringBuilder();
					sb.Append($"<request point=\"{pointid}\">\r\n");
					sb.AppendFormat("\t<payment account=\"{0}\" service=\"{1}\" sum=\"{2}\" sum-in=\"{3}\" id=\"{4}\" check=\"{5}\" date={6}>\r\n",
						string.IsNullOrEmpty(Phone) ? Account : Phone,	// Номер телефона или счёта
						Gateway,										// Номер шлюза ЕКТ
						sAmount,					                	// amount
						sAmountAll,                 					// summary_amount
						Tid.ToString(),
						MakeCheckNumber(),								// Номер чека
						sDate											// Время платежа
						);
					// Добавляется коллекция атрибутов
					foreach (string name in Attributes.Keys)
                        sb.Append($"\t\t<attribute name=\"{name}\" value=\"{Attributes[name]}\" />\r\n");
                    sb.Append("\t/<payment>\r\n");
					sb.Append("/<request>");
					stRequest += sb.ToString();
					}
				else
					{
					StringBuilder sb = new StringBuilder();
					// Добавляется коллекция атрибутов
					foreach (string name in Attributes.Keys)
						sb.Append($"\t\t<attribute name=\"{name}\" value=\"{Attributes[name]}\" />\r\n");
					stRequest += string.Format(Properties.Resources.Template_Payment, pointid,
						string.IsNullOrEmpty(Phone) ? Account : Phone, Gateway, // Номер сервиса ЕКТ, 
						sAmount, 
                        AmountAll == -1? sAmount: sAmountAll, 
						Tid, 
                        MakeCheckNumber(), 
                        sDate, 
                        sb.ToString());
					}
                    */
				}
			else if (state == 3) // Status
				stRequest += string.Format(Properties.Resources.Template_Status, pointid, Tid);
			else
				{
				errDesc = string.Format(Messages.Err_UnknownState, state);
				state = 0;
				errCode = 2;
				RootLog(ErrDesc);
				return 1;
				}

			// ReportRequest();

			return 0;
		}

		/// <summary>
		/// Добвляет подпись в запрос
		/// </summary>
		/// <param name="request">Запрос HttpWebRequest</param>
		public override void AddHeaders(System.Net.HttpWebRequest request)
		{
			// Подпись в заголовок
			using (Crypto crypto = new Crypto(CommonName))
				Signature = crypto.Sign(stRequest, 1, Enc);
			
			request.Headers.Add("PayLogic-Signature", Signature);
			// request.Host = "217.199.242.228:8181";

			// Log("SHA1: {0}", Signature);
		}

	}
}
