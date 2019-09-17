using Dapper;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using Oldi.Net;
using Oldi.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RT
{

    class PayDetails
    {
        public string svcSubNum;
        public decimal payAmount = 100M;
        public int payPurpose = 0;
    }

    class JasonRequest
    {
        public string reqType;
        public string svcTypeId;
        public string svcNum;
        public string svcSubNum;
        public string payCurId = "RUB";
        public decimal payAmount = 100M;
        public DateTime? payTime;
        public int payPurpose = 0;
        public string payComment;
        public PayDetails[] payDetails;
        public string agentId = "65423";
        public string agentAccount = "CASH_CMSN"; // Счёт учёта поставщика услуг
        public int? queryFlags;
        public string srcPayId;
        public DateTime? reqTime;
    }

    class JasonReponse
    {
        public DateTime? reqType;
        public int? reqStatus;
        public DateTime? reqTime;
        public string reqNote;
        public string errUsrMsg;
        public string clientType;
        public string clientInn;
        public string clientOrgName;
        public string esppPayId;
        public string reqUserMsg;
        public int? dupFlag;
        public int? payStatus;
        public string dstDepCode;
        public decimal? payeeRecPay;
        public decimal? payeeRemain;
    }

    [Table("OLDIGW.filial")]
    class RTFilial
    {
        public int Num;
        public string SvcTypeId;
        public string Comment;
    }


    public partial class RTTest : GWRequest
    {
        const int MaxStrings = 1500;

        int? ReqStatus = null;      // Статус операции: началась, выполняется, выполнена...
        int? PayStatus = null;      // Статус платежа: зачислен, отклонён, отмененён...
        int? DupFlag = null;        // Флаг повторного проведения платежа
        DateTime? ReqTime = null;   // Время запроса
        string ReqNote = "";        // Сообщение об ошибке
        string ReqType = "";
        string SvcTypeID = "0";     // 0 или "" - федеральный номер телефона
        string SvcNum = "";             // Номер телефона / лицевого счёта
        string SvcSubNum = "";      // Субсчёт контрагента, V1 - Ростелеком
        string SvcComment = "";
        string[] states;
        string queryFlag = "";

        // int AgentId = 65423; // Id агента
        // string agentAccount = "CASH_CMSN"; // Счёт учёта поставщика услуг

        new decimal? Balance = null;
        decimal? Recpay = null;
        JasonRequest jreq = new JasonRequest();
        JasonReponse resp = new JasonReponse();

        string ProviderName = "rt-test";

        public RTTest()
            : base()
        {
        }

        public RTTest(GWRequest src)
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
            // tz = Settings.Tz;
            tz = 7;

            commonName = "ESPP-TEST";
            host = "https://test-rt.rt.ru:8443/uni_new";
            Log($"****CHECK: Service={Service} Filial={Filial}");
            if (Service.ToLower() == "rtk-acc")
            {
                // Выберем филиал из таблицы
                using (IDbConnection db = new SqlConnection(Settings.ConnectionString))
                {
                    filial = db.Query(@"select svcTypeId from oldigw.oldigw.filial where Num={0}", Filial).FirstOrDefault();
                    if (!string.IsNullOrEmpty(filial)) SvcTypeID = filial;
                    RootLog("Филилал={0} Найден код филиала={1}", Filial, SvcTypeID);
                }
            }

            if (string.IsNullOrEmpty(Phone))
            {
                jreq.svcNum = Account;
                if (!string.IsNullOrEmpty(AccountParam))
                    jreq.svcSubNum = AccountParam;
            }
            else
            {
                jreq.svcNum = "7" + Phone;
                if (!string.IsNullOrEmpty(PhoneParam))
                    jreq.svcSubNum = PhoneParam;
            }
            if (!string.IsNullOrEmpty(Comment))
                jreq.payComment = Comment;

            if (pcdate == DateTime.MinValue)
                pcdate = DateTime.Now;

            switch (RequestType.ToLower())
            {
                case "status":
                    jreq.reqType = "getPaymentStatus";
                    break;
                case "payment":
                    jreq.reqType = "createPayment";
                    break;
                case "undo":
                    jreq.reqType = "abandonPayment";
                    break;
                case "check":
                    jreq.reqType = "checkPaymentParams";
                    break;
                default:
                    jreq.reqType = RequestType;
                    break;
            }

            if (!string.IsNullOrEmpty(Number))
                jreq.queryFlags = Number.ToInt();

            if (Tid != long.MinValue)
                jreq.srcPayId = Tid.ToString();

        }

        /// <summary>
        /// Установка имени лог-файла запросов к провайдеру
        /// </summary>
        /// <returns></returns>
        protected override string GetLogName()
        {
            return Provider == ProviderName ? Properties.Settings.Default.LogFileName : Settings.Rtm.LogFile;
        }

        /// <summary>
        /// Установка состояния платежа
        /// </summary>
        void GetErrorDescription()
        {
            int r = 0;
            ResourceManager res = null;
            res = Properties.Resources.ResourceManager;

            if (ReqStatus > 0)
                r = ReqStatus.Value + 100;
            else if (ReqStatus < 0)
                r = -ReqStatus.Value;
            if (ReqStatus != 0)
                ErrDesc = $"{res.GetString("ERR_" + r.ToString())}: {ReqNote}";
            else
                ErrDesc = $"{PayStatus}: {res.GetString("STS_" + PayStatus.ToString())}";

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
                    Sync(false);
                    if (State < 6)
                    {
                        DoPay(1, 6);
                        UpdateState(Tid, state: state, errCode: ErrCode, errDesc: ErrDesc, locked: 0);
                    }
                }

                // Сделать ответ, если он вызван не из цикла REDO
                MakeAnswer();
            }
            else // Redo
            {
                // REDO выполняется в OE

                /*
				Log("{0} допроведение из статуса {1}", Tid, state);
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

        public override int Undo()
        {
            byte old_state = state;
            int OK = 1;
            string x = !string.IsNullOrEmpty(Phone) ? Phone :
                                !string.IsNullOrEmpty(Account) ? Account :
                                !string.IsNullOrEmpty(Number) ? Number : "";

            // Log("{0} Попытка отмены платежа", Tid);
            Log("{0} [UNDO - начало] Num = {1} State = {2}", Tid, State != 255 ? State.ToString() : "<None>", x);

            stRequest = JsonConvert.SerializeObject(jreq);

            if (MakeRequest(8) == 0 && PostJson(Host, stRequest) == 0 && ParseAnswer(stResponse) == 0)
            {
                Log("{0} [UNDO - конец] reqStatus = {1} payStatus = {2}", Tid, ReqStatus, PayStatus);
                if (resp.payStatus == 3)
                {
                    errCode = 6;
                    errDesc = "Платёж отменён оператором";
                    state = 10;
                }
                else if (resp.payStatus == 103)
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

            try
            {

                // выполнить проверку возможности платежа и вернуть баланс счёта
                if (old_state == 0)
                {
                    // checkParam
                    jreq.payAmount = 100M; // 1 RUB
                    jreq.payComment = $"Проверка параметров для {Phone}{Account}";

                    // CheckUser
                    jreq.queryFlags = 13;
                }
                // Создать запрос Pay
                else if (old_state == 1)
                {
                    jreq.payTime = Operdate;
                    jreq.reqTime = Pcdate;
                    jreq.payAmount = Amount * 100m;
                    jreq.payComment = $"Оплата {jreq.payAmount} {jreq.srcPayId} {jreq.svcNum}";
                }
                // Создать запрос Status
                else if (old_state == 3)
                {
                    jreq.srcPayId = Tid.ToString();
                }
                // Запрос на отмену платежа
                else if (old_state == 8)
                {
                    jreq.srcPayId = !string.IsNullOrEmpty(Phone) ? Phone : Account;
                    jreq.reqType = XConvert.AsDateTZ(Pcdate, 7);
                    jreq.srcPayId = Tid.ToString();
                    jreq.reqTime = Pcdate;
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
            }
            catch (Exception ex)
            {
                RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
            }

            stRequest = JsonConvert.SerializeObject(jreq);
            Log($"Создан запрос\r\n{stResponse}");

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

            stResponse = JsonConvert.SerializeObject(jreq);
            Log(stRequest);
            // Отправить запрос
            if (PostJson(Host, stResponse) == 0)
            {
                ParseAnswer(stResponse);

                if (resp.reqStatus == 0)
                {
                    switch (resp.payStatus)
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
                else if (resp.reqStatus == -12)
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
            Log(stRequest);

            // Создать запрос
            MakeRequest(0);
            stRequest = JsonConvert.SerializeObject(jreq);
            // Отправить запрос
            if (PostJson(Host, stRequest) == 0)
            {
                ParseAnswer(stResponse);
                if (resp.reqStatus == 0)
                {
                    errCode = 1;
                    state = 1;
                    errDesc = "Абонент найден";
                }
                else
                {
                    errCode = 6;
                    state = 12;
                    errDesc = string.Format("{0}: {1}", resp.reqStatus, resp.reqNote != "" ? resp.reqNote : "Абонент не найден");
                    fio = resp.reqNote != "" ? resp.reqNote : "Абонент не найден";
                }

            }

            // Создать ответ
            MakeAnswer();

        }

        /// <summary>
        /// Разбор ответ
        /// </summary>
        /// <param name="stResponse"></param>
        /// <returns></returns>
        public override int ParseAnswer(string stResponse)
        {

            resp = JsonConvert.DeserializeObject<JasonReponse>(stResponse);

            SetCurrentState();

            return 0;
        }

        /// <summary>
        /// Установка состояния платежа на основании ответа провайдера
        /// </summary>
        void SetCurrentState()
        {
            if (resp.reqStatus == 0)
            {
                switch (resp.payStatus)
                {
                    case 2:     // Платёж выполнен
                        state = 6;
                        errCode = 3;
                        if (ReqTime != null)
                            acceptdate = ReqTime.Value;
                        else
                            acceptdate = DateTime.Now;
                        break;
                    case 3:     // Платёж отменён
                        state = 8;
                        errCode = 3;
                        break;
                    case 102:   // Проводится
                    case 103:   // Отменяется
                        state = 3;
                        errCode = 1;
                        break;
                    case 4:     // Отклонён
                        errCode = 6; // Платёж отклонён / отменёт. Финал.
                        state = 12;
                        break;
                }
            }
            else if (resp.reqStatus == 1 || resp.reqStatus == -1) // Платёж может быть допроведён
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
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "info", resp.errUsrMsg.Length <= 250 ? HttpUtility.HtmlEncode(resp.errUsrMsg) : HttpUtility.HtmlEncode(resp.errUsrMsg.Substring(0, 250)));

            if (resp.payeeRecPay != null)
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "recpay", XConvert.AsAmount(resp.payeeRecPay.Value));
            if (resp.payeeRemain != null)
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "balance", XConvert.AsAmount(resp.payeeRemain.Value));

            sb.AppendFormat("<{0}>\r\n{1}\r\n</{0}>\r\n", "response", sb1.ToString());

            stResponse = JsonConvert.SerializeObject(resp);
            Log($"Получен ответ от ЕСПП\r\n{stResponse}");

            stResponse = sb.ToString();

            if (tid > 0)
                UpdateState(Tid, state: State, locked: 1);


            Log("Подготовлен ответ:\r\n{0}", stResponse);
        }

            /// <summary>
            /// Обработчик входного запроса
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="certificate"></param>
            /// <param name="chain"></param>
            /// <param name="policyErrors"></param>
            /// <returns></returns>
            private bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
            {
                // allow any certificate...
                // Log("[SendRequest]: Получен сертификат выданый {0}", certificate.Issuer);

                return true;
            }

            /// <summary>
            /// Добавление клиентского сертификата
            /// </summary>
            /// <param name="request"></param>
            public int FindCertificate(HttpWebRequest request)
            {
                errCode = 0;

                // Добавление сертификата
                if (!string.IsNullOrEmpty(commonName))
                {
                    using (Crypto cert = new Crypto(CommonName))
                    {
                        if (cert != null)
                            if (cert.HasPrivateKey)
                                request.ClientCertificates.Add(cert.Certificate);
                            else
                            {
                                errDesc = Properties.Resources.MsgCHNPK;
                                errCode = -1;
                            }
                        else
                        {
                            errDesc = $"{Properties.Resources.MsgCNF} {CommonName}";
                            errCode = -1;
                        }
                    }
                }

                return errCode;

            }

        /// <summary>
        /// Отправка Json
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public int PostJson(string Host, string json)
            {
                HttpWebRequest request = null;
                HttpWebResponse response = null;
                // StreamReader reader = null;
                byte[] buf;
                Encoding enc = Encoding.UTF8;

                try
                {

                    // Использем только протоколы семейства TLS
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    ServicePointManager.CheckCertificateRevocationList = true;
                    ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemote);
                    ServicePointManager.DefaultConnectionLimit = 10;

                    request = (HttpWebRequest)WebRequest.Create(Host);
                    request.ProtocolVersion = HttpVersion.Version11;
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.KeepAlive = true;
                    request.Timeout = 30 * 1000;
                    request.Method = "POST";
                    request.Headers.Set("Accept-Encoding", "identity");
                    request.AllowAutoRedirect = false;
                    request.UserAgent = Settings.Title;

                    request.ContentType = "application/jason; charset=UTF-8";
                    request.Accept = "application/json";
                    request.Headers.Add("Accept-Charset", "UTF-8");

                    // Добавление сертификата
                    if (AddCertificate(request) != 0)
                        return errCode;

                    // ServicePoint sp = request.ServicePoint;
                    // Всё устанавливаем по-умолчанию
                    // sp.ConnectionLeaseTimeout = 900000; //Timeout.Infinite; // 15 мин максимум держится соединение
                    // sp.MaxIdleTime = Timeout.Infinite;


                    request.UserAgent = Settings.ClientName;

                    if (request.Method == WebRequestMethods.Http.Post)
                    {
                    buf = enc.GetBytes(json);
                    request.ContentLength = buf.Length;
                    using (Stream requestStream = request.GetRequestStream())
                        requestStream.Write(buf, 0, buf.Length);
                    }

                    // Получим дескриптор ответа
                    using (response = (HttpWebResponse)request.GetResponse())
                        if (request.HaveResponse)
                        {
                        // Перехватим поток от сервера
                        using (Stream dataStream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(dataStream, enc))
                            stResponse = reader.ReadToEnd();
                        return (int)response.StatusCode;
                        }
                        else
                        {
                        return (int)response.StatusCode;
                        }
                }
                catch (WebException we)
                {
                errCode = (int)we.Status;
                errDesc = we.Message;
                }
                catch (Exception ex)
                {
                errCode = -1;
                errDesc = ex.Message;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= new RemoteCertificateValidationCallback(ValidateRemote);
                if (request != null && request.ClientCertificates != null)
                    request.ClientCertificates.Clear();
                if (response != null) response.Close();
            }

            return errCode;

            } // PostJson

        }
    }


