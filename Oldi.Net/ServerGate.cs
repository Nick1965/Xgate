using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oldi.Utility;
using System.Net;
using System.Xml.Linq;

namespace Oldi.Net
{
    public class ServerGate: GWRequest
    {

        #region defenition

        /// <summary>
        /// Результат проверки состояния запроса
        /// </summary>
        new public class Result
        {
            public int state;
            public int substate;
            public int code;
            public Result()
            {
                state = 0;
                substate = 0;
                code = 0;
            }
        }

        protected string pointid;
        new Result result;
        string Signature;
        protected Encoding Enc;

        /// <summary>
        /// Конструктор по умоляанию
        /// </summary>
        public ServerGate()
            :base()
        {
        }

        public ServerGate(GWRequest src)
            :base(src)
        {
            terminal = src.Terminal;
        }

        public override void InitializeComponents()
        {
            host = Settings.Boriska.Host;
            ContentType = Settings.Boriska.ContentType;
            commonName = Settings.Boriska.Certname;
            pointid = Settings.Boriska.Pointid;
            CodePage = Settings.Boriska.Codepage;
            result = new Result();

            base.InitializeComponents();
            Enc = CodePage == "utf-8"? Encoding.UTF8: Encoding.GetEncoding(1251);
        }

        #endregion defenition

        /// <summary>
        /// Выролнить цикл проведения/допроведения платежа
        /// </summary>
        public override void Processing(bool New)
        {

            if (New)  // Новый платёж
            {

                if (MakePayment() == 0)
                {
                    // TraceRequest("New");

                    // Проверка дневного лимита для нового плательщика
                    if (DayLimitExceeded(true)) return;

                    // Сумма болше лимита и прошло меньше времени задержки отложить обработку запроса
                    RootLog($"{Tid} [{Provider}.Processing - NEW start] {Service}/{Gateway} {AmountAll.AsCF()}");
                    if (FinancialCheck(New)) return;
                    DoPay(0, 3);
                }
                TechInfo = $"state={result.state} substate={result.substate} code={result.code}";
                // TraceRequest("End");
            }
            else // Redo
            {
                // Сумма болше лимита и прошло меньше времени задержки отложить обработку запроса
                if (State == 0)
                {
                    // Проверка дневного лимита для нового плательщика
                    if (DayLimitExceeded(false)) return;

                    RootLog($"{Tid} [{Provider}.Processing - REDO start] {Service}/{Gateway} {AmountAll.AsCF()}");
                    if (FinancialCheck(false)) return;
                }
                DoPay(state, 6);
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
                                state = old_state == 0 ? (byte)0 : (byte)3;
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
                        Wait(20); // Ждём 20 сек
                        Attempt++;
                        Log($"{Tid} - {Provider} {Attempt} / 3 Повтор после ошибки SSL");
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
            string msg = ServerGateMessages.State60;

            switch (result.state)
            {
                case -2:
                    msg = ServerGateMessages.State_minus_2;
                    break;
                case -1:
                    msg = ServerGateMessages.State_minus_1;
                    break;
                case 0:
                    msg = ServerGateMessages.State0;
                    break;
                case 10:
                    msg = ServerGateMessages.State10;
                    times = 11;
                    break;
                case 20:
                    switch (result.substate)
                    {
                        case 1:
                            msg = ServerGateMessages.State20_1;
                            break;
                        case 2:
                        case 3:
                            msg = ServerGateMessages.State20_2_3;
                            break;
                        case 4:
                            msg = ServerGateMessages.State20_4;
                            break;
                        default:
                            msg = string.Format(ServerGateMessages.State20_X, result.substate, result.code);
                            break;
                    }
                    break;
                case 40:
                    switch (result.substate)
                    {
                        case 1:
                            msg = ServerGateMessages.State40_1;
                            break;
                        case 2:
                        case 3:
                            msg = ServerGateMessages.State40_2_3;
                            break;
                        case 4:
                        case 5:
                            msg = ServerGateMessages.State40_4_5;
                            break;
                        default:
                            msg = string.Format(ServerGateMessages.State40_X, result.substate, result.code);
                            break;
                    }
                    break;
                case 60:
                    msg = ServerGateMessages.State60;
                    break;
                case 80:
                    switch (result.substate)
                    {
                        case 1:
                        case 2:
                        case 3:
                            msg = ServerGateMessages.State80_1_2_3;
                            break;
                        default:
                            switch (result.code)
                            {
                                case 1:
                                case 2:
                                    msg = ServerGateMessages.Code1;
                                    break;
                                case 3:
                                    msg = ServerGateMessages.Code3;
                                    break;
                                case 4:
                                    msg = ServerGateMessages.Code4;
                                    break;
                                case 5:
                                    msg = ServerGateMessages.Code5;
                                    break;
                                case 6:
                                    msg = ServerGateMessages.Code6;
                                    break;
                                case 7:
                                    msg = ServerGateMessages.Code7;
                                    break;
                                case 8:
                                    msg = ServerGateMessages.Code8;
                                    break;
                                case 9:
                                    msg = ServerGateMessages.Code9;
                                    break;
                                case 10:
                                    msg = ServerGateMessages.Code10;
                                    break;
                                case 12:
                                    msg = ServerGateMessages.Code12;
                                    break;
                                case 13:
                                    msg = ServerGateMessages.Code13;
                                    break;
                                case 14:
                                    msg = ServerGateMessages.Code14;
                                    break;
                                case 15:
                                    msg = ServerGateMessages.Code15;
                                    break;
                                case 16:
                                    msg = ServerGateMessages.Code16;
                                    break;
                                case 17:
                                    msg = ServerGateMessages.Code17;
                                    break;
                                case 18:
                                    msg = ServerGateMessages.Code18;
                                    break;
                                case 19:
                                    msg = ServerGateMessages.Code19;
                                    break;
                                case 20:
                                    msg = ServerGateMessages.Code20;
                                    break;
                                case 21:
                                    msg = ServerGateMessages.Code21;
                                    break;
                                case 22:
                                    msg = ServerGateMessages.Code22;
                                    break;
                                case 30:
                                    msg = ServerGateMessages.Code30;
                                    times = 51;
                                    break;
                                case 33:
                                    msg = ServerGateMessages.Code33;
                                    times = 51;
                                    break;
                                default:
                                    msg = string.Format(ServerGateMessages.State80_X, result.substate, result.code);
                                    break;
                            }
                            break;
                    }
                    break;
                default:
                    msg = $"({result.state}/{result.substate}/{result.code})";
                    break;
            }
            return !string.IsNullOrEmpty(msg) ? msg : $"({result.state}/{result.substate}/{result.code})";
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
                    errDesc = string.Format(ServerGateMessages.Err_NoDefaults, state);
                    RootLog($"Account=\"{Account}\"\r\nPhone=\"{Phone}\"\r\nCard=\"{Card}\"\r\nPointid=\"{pointid}\"\r\nGateway=\"{Gateway}\"");
                    RootLog(ErrDesc);
                    return 1;
                }

                if (Amount == decimal.MinusOne)
                {
                    state = 0;
                    errCode = 2;
                    errDesc = ServerGateMessages.Err_NoAmount;
                    RootLog($"Pointid=\"{pointid}\"\r\nGateway=\"{Gateway}\"");
                    RootLog(ServerGateMessages.Err_NoAmount);
                    return 1;
                }

            }

            if (Pcdate == DateTime.MinValue)
                pcdate = DateTime.Now;
            int lTz = Tz != -1 ? Tz : Settings.Tz;
            string sDate = XConvert.AsDate(pcdate) + string.Format("+{0:D2}00", lTz);

            string sAmount = Math.Round(Amount * 100).ToString();
            string sAmountAll = Math.Round(AmountAll * 100).ToString();
            string check = MakeCheckNumber();
            string sAttributes = "";

            stRequest = @"<?xml version=""1.0"" encoding=""utf-8""?>";

            if (state == 0) // Payment
            {
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
                stRequest += $@"<payment id=""{Tid}"" sum=""{sAmount}"" check=""{check}"" service=""{Gateway}"" account=""{acnt}"" date=""{sDate}"">";
                if (!string.IsNullOrEmpty(sAttributes))
                    stRequest += $"{sAttributes}";
                stRequest += "</payment></request>";
            }
            else if (state == 3) // Status
                stRequest += $"<request point=\"{pointid}\"><status id=\"{Tid}\" /></request>";
            else
            {
                errDesc = string.Format(ServerGateMessages.Err_UnknownState, state);
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
        public override void AddHeaders(HttpWebRequest request)
        {
            // Подпись в заголовок
            using (Crypto crypto = new Crypto(CommonName))
                Signature = crypto.Sign(stRequest, 1, Enc);

            request.Headers.Add("PayLogic-Signature", Signature);
            // request.Host = "217.199.242.228:8181";

            // Log("SHA1: {0}", Signature);
        }

        /// <summary>
        /// Добавление клиентского сертификата. В этом провайдере ничего не добавляется
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override int AddCertificate(HttpWebRequest request)
        {
            return 0;
        }

        public override int TimeOut()
        {
            return Settings.Boriska.Timeout.ToInt();
        }

        protected override string GetLogName()
        {
            return Settings.Boriska.LogFile;
        }

    }
}
