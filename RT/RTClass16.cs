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

    class JsonRequest
    {
        public string reqType;
        public string svcTypeId="0";
        public string svcNum;
        public string svcSubNum;
        public string payCurId = "RUB";
        public decimal payAmount = 100M;
        public string payTime;
        public int payPurpose = 0;
        public string payComment;
        public PayDetails[] payDetails;
        public string agentId = "65423";
        public string agentAccount = "CASH_CMSN"; // Счёт учёта поставщика услуг
        public int? queryFlags;
        public string srcPayId;
        public string reqTime;

        public override string ToString()
        {
            return $@"reqType={reqType}
                    svcTypeId={svcTypeId}
                    svcNum={svcNum}
                    svcSubNum={svcSubNum}
                    payCurId={payCurId}
                    payAmount={payAmount}
                    payTime={payTime}
                    payPurpose={payPurpose}
                    payComment={payComment}
                    agentId={agentId}
                    agentAccount={agentAccount}
                    queryFlags={queryFlags}
                    srcPayId={srcPayId}
                    reqTime={reqTime}";
        }
    }

    class FindRequest
    {
        public string reqType = "queryPayeeInfo";
        public string svcTypeId = "0";
        public string svcNum;
        public string svcSubNum = "";
        public int? queryFlags = 13;
        public string agentId = "65423";

        public override string ToString()
        {
            return $"reqType={reqType} svcTypeId={svcTypeId} svcNum={svcNum}svcSubNum={svcSubNum} queryFlags={queryFlags} agentId={agentId}";
        }
    }

    class PayRequest
    {
        public string reqType = "createPayment";
        public string svcTypeId = "0";
        public string svcNum;
        public string svcSubNum;
        public string srcPayId;
        public string payTime;
        public string payCurrId = "RUB";
        public int? payAmount;
        public int payPurpose = 0;
        public string payComment;
        public string agentId = Settings.Rt.AgentId; // "65423";
        public string agentAccount = Settings.Rt.AgentAccount; // "CASH_NCMSN";
        public string reqTime;
        public string payerContact;
        public int registerCheck = 1;
    }

    class StatusRequest
    {
        public string reqType = "getPaymentStatus";
        public string srcPayId;
        public string agentId = Settings.Rt.AgentId; // "65423";
    }

    class UndoRequest
    {
        public string reqType = "abandonPayment";
        public string srcPayId;
        public string reqTime;
        public string agentId = Settings.Rt.AgentId; //"65423";
    }

    class CheckRequest
    {
        public string reqType = "checkPaymentParams";
        public string svcTypeId = "0";
        public string svcNum;
        public string svcSubNum;
        public string payCurrId = "RUB";
        public int? payAmount = 100;
        public int payPurpose = 0;
        public string payComment = $"Check params";
        public string agentId = Settings.Rt.AgentId; // "65423";
        public string agentAccount = Settings.Rt.AgentAccount; // "CASH_NCMSN";
    }

    class JsonReponse
    {
        public int? reqStatus;
        public DateTime? reqTime;
        public string reqNote;
        public string errUsrMsg;
        public string payeeName;
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
        public DateTime? acceptTime;
        public DateTime? acceptedTime;
        public DateTime? abandonTime;
        public DateTime? abandonedTime;
        public string reqType;
    }

    [Table("OLDIGW.filial")]
    class RTFilial
    {
        public int Num;
        public string SvcTypeId;
        public string Comment;
    }


    public partial class RTClass16 : GWRequest
    {
        const int MaxStrings = 1500;

        int? ReqStatus = null;      // Статус операции: началась, выполняется, выполнена...
        int? PayStatus = null;      // Статус платежа: зачислен, отклонён, отмененён...
        int? DupFlag = null;        // Флаг повторного проведения платежа
        DateTime? ReqTime = null;   // Время запроса
        string ReqNote = "";        // Сообщение об ошибке
        string SvcTypeID = "0";     // 0 или "" - федеральный номер телефона

        int Attempt = 0;
        // int AgentId = 65423; // Id агента
        // string agentAccount = "CASH_CMSN"; // Счёт учёта поставщика услуг

        JsonRequest jreq = new JsonRequest();
        JsonReponse resp = new JsonReponse();

        FindRequest freq = new FindRequest();
        PayRequest preq = new PayRequest();
        StatusRequest sreq = new StatusRequest();
        UndoRequest ureq = new UndoRequest();

        public RTClass16()
            : base()
        {
        }

        public RTClass16(GWRequest src)
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

            commonName = Settings.Rt.CN;
            host = Settings.Rt.Host;


            if (string.IsNullOrEmpty(Phone))
            {
                jreq.svcTypeId = FindFilial();
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
                    sreq.srcPayId = Tid.ToString();
                    break;
                case "payment":
                    DoPay();
                    break;
                case "undo":
                    ureq.reqType = "abandonPayment";
                    break;
                case "check":
                    jreq.reqType = "checkPaymentParams";
                    break;
                case "find":
                    DoCheck();
                    return;
                    
                default:
                    jreq.reqType = RequestType;
                    break;
            }

            if (!string.IsNullOrEmpty(Number))
                jreq.queryFlags = Number.ToInt();

            if (Tid != long.MinValue)
                jreq.srcPayId = Tid.ToString();

        }

        string FindFilial()
        {
            SvcTypeID = "0";
            if (Service.ToLower() == "rt-acc" && SvcTypeID == "0") // чтоб лишний раз не читать из БД
            {
                // Выберем филиал из таблицы

                try
                {
                    using (IDbConnection db = new SqlConnection(Settings.ConnectionString))
                    {
                        Log($"[FindFilial] select svcTypeId from oldigw.oldigw.filial where Num={Filial.ToInt()}");
                        SvcTypeID = db.Query<string>($"select svcTypeId from oldigw.oldigw.filial where Num={Filial.ToInt()}").FirstOrDefault();
                        Log($"Филилал={Filial} Найден код филиала={SvcTypeID}");
                    }
                }
                catch(SqlException se)
                {
                    RootLog($"{se.Message}, {se.LineNumber}");
                    RootLog($"[FindFilial]: {filial}");
                    return "0";
                }

            }

            Log($"[FindFilial]: {SvcTypeID}");

            return SvcTypeID;

        }

        /// <summary>
        /// Установка имени лог-файла запросов к провайдеру
        /// </summary>
        /// <returns></returns>
        protected override string GetLogName()
        {
            return Properties.Settings.Default.LogFileName;
        }

        /// <summary>
        /// Установка состояния платежа
        /// </summary>
        void GetErrorDescription()
        {
            int r = 0;
            ResourceManager res = null;
            res = Properties.Resources.ResourceManager;

            if (freq.reqType != "queryPayeeInfo")
            {
                if (resp.reqStatus > 0)
                    r = resp.reqStatus.Value + 100;
                else if (resp.reqStatus < 0)
                    r = -resp.reqStatus.Value;
                if (resp.reqStatus != 0)
                    ErrDesc = $"{res.GetString("ERR_" + r.ToString())}: {resp.reqNote}";
                else
                    ErrDesc = $"{resp.reqStatus}: {res.GetString("STS_" + resp.payStatus.ToString())}";
            }

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
                    Log($"[{Tid}] Ph={Phone} Acc={Account} Amount={Amount.AsCF()}");
                }
                else
                {
                    Sync(false);
                    if (State < 6)
                    {
                        // DoPay(1, 6);
                        DoPay();
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
            RootLog($"{Tid} [UNDO - начало] Num = {x} State = {State}");

            ureq.srcPayId = Tid.ToString();
            ureq.reqTime = XConvert.AsDateTZ(Pcdate, Settings.Tz);
            stRequest = JsonConvert.SerializeObject(ureq);

            if (MakeRequest(8) == 0 && PostJson(Host, stRequest) == 0 && ParseAnswer(stResponse) == 0)
            {
                Log($"{Tid} [UNDO - конец] reqStatus = {resp.reqStatus} payStatus = {resp.payStatus}");
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
                    errDesc = String.Format($"Платёж {Tid} отменяется: {ReqNote}");
                }
                else
                {
                    if (old_state == 6)
                        errCode = 3;
                    else if (old_state == 12)
                        errCode = 6;
                    else
                        errCode = 1;
                    errDesc = String.Format($"Ошибка отмены платежа: {ReqNote}");
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

                if (old_state == 0)
                {
                    // jreq.reqTime = Pcdate.AsCF();
                    // checkParam
                    // freq.reqType = "queryPayeeInfo";
                    // Log($"MakeRequest for {freq.ToString()}");
                }
                // Создать запрос Pay
                else if (old_state == 1)
                {
                    if (Service.ToLower() == "rt-acc")
                    {
                        preq.svcTypeId = FindFilial();
                        preq.svcNum = Account;
                    }
                    else
                    {
                        preq.svcNum = '7' + Phone;
                    }
                    // preq.svcSubNum = jreq.svcSubNum;
                    preq.payTime = XConvert.AsDateTZ(DateTime.Now, Settings.Tz);
                    preq.reqTime = XConvert.AsDateTZ(Pcdate, Settings.Tz);
                    preq.payAmount = (int?)(Amount * 100m);
                    preq.payComment = $"Оплата {preq.payAmount} {preq.srcPayId} {preq.svcNum}";
                    preq.srcPayId = Tid.ToString();
                    preq.payerContact = Contact;
                    stRequest = JsonConvert.SerializeObject(preq);
                    Log($"Создан запрос\r\n{stRequest}");

                    return 0;
                }
                // Создать запрос Status
                else if (old_state == 3)
                {
                    sreq.srcPayId = Tid.ToString();
                    stRequest = JsonConvert.SerializeObject(sreq);
                    Log($"Создан запрос\r\n{stRequest}");
                }
                // Запрос на отмену платежа
                else if (old_state == 8)
                {
                    stRequest = JsonConvert.SerializeObject(ureq);
                    Log($"Создан запрос\r\n{stRequest}");
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
                RootLog(ex.ToString());
                return 1;
            }

            // stRequest = JsonConvert.SerializeObject(jreq);
            // Log($"Создан запрос\r\n{stRequest}");

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

            stRequest = JsonConvert.SerializeObject(sreq);
            Log(stRequest);
            // Отправить запрос
            if (PostJson(Host, stRequest) == 0)
            {
                ParseAnswer(stResponse);
                outtid = resp.esppPayId;

                if (resp.reqStatus == 0)
                {
                    switch (resp.payStatus)
                    {
                        case 2:
                            errCode = 3;
                            state = 6;
                            errDesc = "Платёж проведён";
                            acceptdate = resp.acceptedTime.Value;
                            break;
                        case 3:
                        case 4:
                            errCode = 6;
                            state = 12;
                            errDesc = "Платёж отменён";
                            acceptdate = resp.abandonedTime.Value;
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
                    acceptdate = resp.abandonedTime.Value;
                }
                else
                {
                    errCode = 6;
                    errDesc = string.Format("Ошибка обработки запроса. reqStatus = {0}", ReqStatus);
                    state = 12;
                    acceptdate = resp.abandonedTime.Value;
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

            if (CheckParams() != 0)
                return;

            // Создать запрос
            freq.reqType = "queryPayeeInfo";
            freq.svcNum = jreq.svcNum;
            freq.svcTypeId = FindFilial();
            freq.svcSubNum = "";
            freq.queryFlags = 13;
            // MakeRequest(0);
            stRequest = JsonConvert.SerializeObject(freq);
            // Отправить запрос
            Log($"[DoCheck] host={Host}\r\nstRequest={stRequest}");
            if (PostJson(Host, stRequest) == 0)
            {
                ParseAnswer(stResponse);
                if (resp.reqStatus == 0)
                {
                    errCode = 0;
                    state = 1;
                    errDesc = "Абонент найден";
                }
                else if (resp.reqStatus == -12)
                {
                    errCode = 6;
                    state = 12;
                    // errDesc = resp.reqNote != "" ? resp.reqNote : "Абонент не найден";
                    errDesc = "Абонент не существует";
                    // fio =  "Абонент не существует";
                }
                else
                {
                    errCode = 6;
                    state = 12;
                    errDesc =  "Абонент не существует";
                    // fio =  "Абонент не существует";
                }

            }

            // Создать ответ
            MakeAnswer();

        }

        int CheckParams()
        {
            CheckRequest req = new CheckRequest();


            Log($"[CHECK] Req={RequestType} Ph={Phone} Acc{Account} Service={Service} Filial={Filial}");
            if (Service.ToLower() == "rt-acc")
            {
                req.svcTypeId = FindFilial();
                req.svcNum = Account;
            }
            else
                req.svcNum = '7' + Phone;

            req.svcSubNum = "";
            stRequest = JsonConvert.SerializeObject(req);
            Log($"[CheckParams] host={Host}\r\nstRequest={stRequest}");
            if (PostJson(Host, stRequest) == 0)
            {
                Log($"[CheckParams] stResponse={stResponse}");
                ParseAnswer(stResponse);
                if (resp.reqStatus == 0)
                    return 0;
            }
            errCode = 6;
            state = 12;
            errDesc = $"[CHECK] Bad params for: Req={RequestType} Ph={Phone} Acc{Account} Service={Service} Filial={Filial}");

            MakeAnswer();
            
            return 1;
        }

            public void DoPay()
        {
            // Log(stRequest);
            // Создать запрос
            
            if (Service.ToLower() == "rt-acc")
            {
                preq.svcTypeId = FindFilial();
                preq.svcNum = Account;
            }
            else
                preq.svcNum = '7' + Phone;
            preq.svcSubNum = "";
            preq.payerContact = Contact;
            MakeRequest(1);
            // stRequest = JsonConvert.SerializeObject(preq);
            // Отправить запрос
            Log($"[DoPay] host={Host}\r\nstRequest={stRequest}");
            if (PostJson(Host, stRequest) == 0)
            {
                ParseAnswer(stResponse);
                /*
                if (resp.reqStatus == 0)
                {
                    errCode = 0;
                    state = 3;
                    errDesc = "Абонент найден";
                }
                else if (resp.reqStatus == -12)
                {
                    errCode = 6;
                    state = 12;
                    // errDesc = resp.reqNote != "" ? resp.reqNote : "Абонент не найден";
                    errDesc = resp.reqNote != "" ? resp.reqNote : resp.errUsrMsg + " " + resp.reqStatus;
                    fio = resp.reqNote != "" ? resp.reqNote : resp.errUsrMsg;
                }
                else
                {
                    errCode = 6;
                    state = 12;
                    errDesc = resp.reqNote != "" ? resp.reqNote : resp.errUsrMsg + " " + resp.reqStatus;
                    fio = resp.reqNote != "" ? resp.reqNote : resp.errUsrMsg;
                }
                */
            }

            Log($"[DoPay] stResponse={stResponse}");
            
            UpdateState(Tid, errCode: ErrCode, errDesc: ErrDesc, locked: 0, state: state);

            // Создать ответ
            // MakeAnswer();

        }

        /// <summary>
        /// Разбор ответ
        /// </summary>
        /// <param name="stResponse"></param>
        /// <returns></returns>
        public override int ParseAnswer(string stResponse)
        {

            resp = JsonConvert.DeserializeObject<JsonReponse>(stResponse);

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
                fio = resp.payeeName;
                transaction = resp.esppPayId;
                balance = (resp.payeeRemain != null)? resp.payeeRemain / 100m: null;
                resp.payeeRecPay = resp.payeeRecPay != null ? resp.payeeRecPay.Value / 100m : resp.payeeRecPay = -1; ;
                errCode = 0;
                errDesc = "Абонент найден";

                Log($"fio = {fio}");

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
            else if (resp.reqStatus == -24)
            {
                errCode = 6;
                state = 12;
                errDesc = "Не достаточно средств для проведения платежа";
            }
            else if (resp.reqStatus == -12)
            {
                errCode = 6;
                state = 12;
                errDesc = "Абонент не найден";
            }
            else if (resp.reqStatus == 1 || resp.reqStatus == -1) // Платёж может быть допроведён
            {
                if (State == 1)
                {
                    errCode = 6;
                    errDesc = resp.reqNote;
                    state = 12;
                }
                else
                {
                    errCode = 1;
                    state = 1;
                    errDesc = "Абонент найден";
                }
            }
            else
            {
                errCode = 6;
                state = 12;
                errDesc = "Абонент не найден";
            }

            GetErrorDescription();

        }

        /// <summary>
        /// Ответ OE
        /// </summary>
        public void MakeAnswer()
        {

            Log($"[MakeAnser] errCode={errCode} errDesc={errDesc}");

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            sb1.AppendFormat("\t<{0} code=\"{1}\">{2}</{0}>", "error", ErrCode, HttpUtility.HtmlEncode(ErrDesc));
            if (!string.IsNullOrEmpty(resp.esppPayId))
            {
                outtid = resp.esppPayId;
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "transaction", resp.esppPayId);
            }
            if (resp.payStatus != null)
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "payStatus", XConvert.AsAmount(resp.payStatus));
            if (Acceptdate != DateTime.MinValue && (State == 6 || State == 10))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "accept-date", XConvert.AsDate(Acceptdate).Replace('T', ' '));
            if (!string.IsNullOrEmpty(Fio))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "fio", Fio);
            if (!string.IsNullOrEmpty(Account))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "account", Account);
            if (!string.IsNullOrEmpty(resp.errUsrMsg))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "addinfo", resp.errUsrMsg.Length <= 250 ? HttpUtility.HtmlEncode(resp.errUsrMsg) : HttpUtility.HtmlEncode(resp.errUsrMsg.Substring(0, 250)));

            if (resp.payeeRecPay != null)
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "recpay", XConvert.AsAmount(resp.payeeRecPay));
            if (Balance != null)
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "balance", XConvert.AsAmount(balance));
            if (!string.IsNullOrEmpty(resp.clientInn))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "clientInn", resp.clientInn);
            if (!string.IsNullOrEmpty(resp.clientType))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "clientType", resp.clientType);
            if (!string.IsNullOrEmpty(resp.clientOrgName))
                sb1.AppendFormat("\r\n\t<{0}>{1}</{0}>", "clientOrgName", resp.clientOrgName);

            sb.AppendFormat("<{0}>\r\n{1}\r\n</{0}>\r\n", "response", sb1.ToString());

            // stResponse = JsonConvert.SerializeObject(resp);
            // Log($"Получен ответ от ЕСПП\r\n{stResponse}");

            stResponse = sb.ToString();

            if (tid > 0)
                UpdateState(Tid, state: State, locked: 1);


            Log($"[MakeAnser] Подготовлен ответ:\r\n{stResponse}");
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
            request.Timeout = 120 * 1000;
            request.Method = "POST";
            request.Headers.Set("Accept-Encoding", "identity");
            request.AllowAutoRedirect = false;
            request.UserAgent = Settings.Title;

            request.ContentType = "application/json; charset=UTF-8";
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
                Log($"[PostJson]: Получен ответ\r\n{stResponse}");
                return 0;
                }
            else
                {
                return (int)response.StatusCode;
                }
            }
            catch (WebException we)
                {
                /*
                if (we.Status == WebExceptionStatus.Timeout)
                {
                Log("PostJson: Wait 60 sec");
                Wait(60);
                return PostJson(Host, json);
                }
                */
                if (we.Status == WebExceptionStatus.SecureChannelFailure)
                {
                    if (Attempt < 3)
                    {
                        Attempt++;
                        Log("{Tid} - {Attempt} / 3 Повтор после ошибки SSL", Tid, Attempt);
                        return PostJson(Host, json);
                    }
                    errCode = (int)we.Status;
                    errDesc = we.Message;
                }
                else
                {
                    errCode = (int)we.Status;
                    errDesc = we.Message;
                }
                RootLog($"PostJson: {we.Status} {we.ToString()}");
            }
            catch (Exception ex)
                {
                errCode = -1;
                errDesc = ex.Message;
                RootLog($"PostJson: {ex.ToString()}");
                }
            finally
            {
                // ServicePointManager.ServerCertificateValidationCallback -= new RemoteCertificateValidationCallback(ValidateRemote);
                if (request != null && request.ClientCertificates != null)
                    request.ClientCertificates.Clear();
                if (response != null) response.Close();
            }


            return errCode;

            } // PostJson

    }
}


