using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using System.Xml.XPath;
using System.IO;
using System.Net;
using Oldi.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Globalization;
using System.Web;

namespace Oldi.Net
{
    public class GWRapidaRequest : GWRequest
    {
        // Host = "https://tepay.rapida.ru/hypertest/";
        // const string CN = "Nickolay Plotnikov";
        // readonly string LogFileName = Settings.LogPath + ".\\Rapida.log";
        new decimal Balance = 0;

        string PPID; // Номер терминала
        string Fam; // Фамилия Имя Отчество
        string Name;
        string SName;
        string KD; // Тип документа
        new string SD; // Серия
        string ND; // Номер
        string GD; // Кем выдан
        string DD; // Дата выдачи документа
        string DR; // Дата рождения
        string MR; // Место рождения
        string CS; // Гражданство
        string AMR; // Регистрация
        string GKID; // Номер регистрации в сервисе

        string Bank = "";
        string TemplateTid = ""; // Код требования
        string PaymNumb = ""; // Номер платежа


        public GWRapidaRequest(GWRequest src) : base(src)
        {
            provider = src.Provider;

            templateName = src.TemplateName;

            tid = src.Tid;
            transaction = src.Transaction;
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
            outtid = src.Outtid;
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
            reason = src.Reason;

            statusType = src.StatusType;
            startDate = src.StartDate;
            endDate = src.EndDate;

            attributes = new AttributesCollection();
            attributes.Add(src.Attributes);


            InitializeComponents();
        }

        public override void InitializeComponents()
        {
            host = Settings.Rapida.Host;

            fio = string.Format("{0} {1} {2}", attributes["Fam"], attributes["Name"], attributes["SName"]);

            PPID = attributes["PPID"] ?? "";

            // Для платежа по коду требования
            TemplateTid = attributes["TID"] ?? "";

            Fam = HttpUtility.UrlEncode(attributes["Fam"], Encoding.GetEncoding(1251)) ?? "";
            Name = HttpUtility.UrlEncode(attributes["Name"], Encoding.GetEncoding(1251)) ?? "";
            SName = HttpUtility.UrlEncode(attributes["SName"], Encoding.GetEncoding(1251)) ?? "";

            KD = attributes["KD"] ?? "";
            SD = attributes["SD"] ?? "";
            ND = attributes["ND"] ?? "";
            GD = HttpUtility.UrlEncode(attributes["GD"], Encoding.GetEncoding(1251)) ?? "";
            DD = attributes["DD"] ?? "";
            DR = attributes["DR"] ?? "";
            MR = HttpUtility.UrlEncode(attributes["MR"], Encoding.GetEncoding(1251)) ?? "";
            CS = HttpUtility.UrlEncode(attributes["CS"], Encoding.GetEncoding(1251)) ?? "";
            AMR = HttpUtility.UrlEncode(attributes["AMR"], Encoding.GetEncoding(1251)) ?? "";

            bik = attributes["BIK"] ?? "";
            account = HttpUtility.UrlEncode(Account, Encoding.GetEncoding(1251)) ?? ""; // Вдруг кто напихает кириллицы в договор
        }
        protected override string GetLogName()
        {
            return Settings.Rapida.Log;
        }

        public override int MakeRequest(int old_state)
        {
            Log("{0} CHECK", Tid);
            // Проверка баланса агента
            Log("{0} [REGP] Начало регистрации", Tid);
            return 0;
        }

        public override int DoPay(byte old_state, byte try_state)
        {

            // Блокировать допроведение записи
            SetLock(1);

            if (old_state == 0)
            {
                if (CheckBalance())
                {
                    // Баланс удовлетворяет платежу
                    RegPayeer();
                    RegTemplate();
                    TemplatePayment();
                }
                else
                {
                    // Будем ждать когда поступят деньги
                    if (State == 0)
                    {
                        errDesc = "Шлюз временно заблокирован";
                        errCode = 12;
                    }
                }
            }
            else
                return 0;

            MakeAnswer();

            UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, opcode: GKID, acceptCode: TemplateTid, limit: Balance, locked: 0); // Разблокировать если проведён

            return ErrCode;
        }


        /// <summary>
        /// Создание строки ответа
        /// </summary>
        void MakeAnswer()
        {

            StringBuilder sb = new StringBuilder();

            sb.Append("<Response>\r\n");

            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "ErrCode", ErrCode);
            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "ErrDesc", ErrDesc);

            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "FIO", Fio);
            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "GKID", GKID);
            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "TID", TemplateTid);
            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "Balance", Balance.AsCurrency());
            sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "Bank", Bank);

            sb.Append("</Response>\r\n");

            stResponse = sb.ToString();

            Log("Подготовлен ответ:\r\n{0}", Answer);

        }
        
        /// <summary>
        /// Выполнения платежа по шаблону
        /// </summary>
        void TemplatePayment()
        {

            if (ErrCode != 0)
                return;

            stRequest = string.Format("?function=payment&PaymExtId={0}&PPID={1}&TID={2}&Amount={3}", Session, PPID, TemplateTid, Amount.AsCurrency());
            string Result = Get(Host + stRequest);
            if (string.IsNullOrEmpty(Result))
            {
                if (ErrCode == 500) // Ошибка сервиса
                    state = 11;
                return;
            }

            string ResultStatus = GetValueFromAnswer(Result, "/Response/Result");
            if (string.IsNullOrEmpty(ResultStatus))
                ResultStatus = GetValueFromAnswer(Result, "/Response/CheckResult");

            if (!string.IsNullOrEmpty(ResultStatus) && ResultStatus.ToUpper() == "OK")
            {
                // Баланс агента
                Balance = GetValueFromAnswer(Result, "/Response/Balance").ToDecimal();
                errDesc = GetValueFromAnswer(Result, "/Response/Description");
                errCode = 3;
                state = 6;
            }
            else
            {
                errCode = GetValueFromAnswer(Result, "/Response/ErrCode").ToInt();
                errDesc = string.Format("({0}) {1}", errCode, GetValueFromAnswer(Result, "/Response/Description"));
                errCode = 6;
                state = 12;
            }

        }

        /// <summary>
        /// Регистрация платёжного шаблона
        /// </summary>
        void RegTemplate()
        {

            if (Gateway.ToLower() != "regpay")
                return;
            if (ErrCode != 0)
                return;

            string Params = string.Format("{0};{1};{2} {3} {4};{5}", Bik, Account, Fam, Name, SName, Number);
            stRequest = string.Format("?function=check&PaymExtId={0}&PPID={1}&mPhone={2}&RCode=601&Params={3}", Session, PPID, Phone, Params);

            string Result = Get(Host + stRequest);
            if (string.IsNullOrEmpty(Result))
            {
                if (ErrCode == 500) // Ошибка сервиса
                    state = 11;
                return;
            }

            string ResultStatus = GetValueFromAnswer(Result, "/Response/CheckResult");
            if (!string.IsNullOrEmpty(ResultStatus) && ResultStatus.ToUpper() == "OK")
            {
                // Код требования
                TemplateTid = GetValueFromAnswer(Result, "/Response/Tid");
                // Банк
                Bank = GetValueFromAnswer(Result, "/Response/B_Name");
                // Номер платежа в системе
                PaymNumb = GetValueFromAnswer(Result, "/Response/PaymNumb");
                // Баланс агента
                Balance = GetValueFromAnswer(Result, "/Response/Balance").ToDecimal();
                errDesc = GetValueFromAnswer(Result, "/Response/Description");
                errCode = 0;
                state = 0;
            }
            else
            {
                errCode = GetValueFromAnswer(Result, "/Response/ErrCode").ToInt();
                errDesc = string.Format("({0}) {1}", errCode, GetValueFromAnswer(Result, "/Response/Description"));
                errCode = 6;
                state = 12;
            }

        }

        /// <summary>
        /// Регистрация плательщика
        /// </summary>
        void RegPayeer()
        {

            // Если какого-то параметра нет - возникнет исключение

            if (Gateway.ToLower() != "regpay")
                return;

            stRequest = string.Format("?function=reg&PaymExtId={0}&PPID={1}&mPhone={2}&fam={3}&name={4}&sname={5}&KD={6}&SD={7}&ND={8}&GD={9}&DD={10}&DR={11}&MR={12}&CS={13}&AMR={14}", 
                Session, PPID, Phone, Fam.ToUpper(), Name.ToUpper(), SName.ToUpper(), KD, SD, ND, GD, DD, DR, MR, CS, AMR);

            string Result = Get(Host + stRequest);
            if (string.IsNullOrEmpty(Result))
            {
                if (ErrCode == 500) // Ошибка сервиса
                    state = 11;
                return;
            }

            string ResultStatus = GetValueFromAnswer(Result, "/Response/Result");
            if (!string.IsNullOrEmpty(ResultStatus) && ResultStatus.ToUpper() == "OK")
            {
                GKID = GetValueFromAnswer(Result, "/Response/Gkid");
                errDesc = GetValueFromAnswer(Result, "/Response/Description");
                errCode = 0;
                state = 0;
            }
            else
            {
                errCode = GetValueFromAnswer(Result, "/Response/ErrCode").ToInt();
                errDesc = string.Format("({0}) {1}", errCode, GetValueFromAnswer(Result, "/Response/Description"));
                errCode = 6;
                state = 12;
            }

            Log("RegPayer: {0} {1}", ErrCode, ErrDesc);

        }

        bool CheckBalance()
        {

            string Result = Get(Host + string.Format("?function=getbalance&PaymExtId={0}", Session));
            if (string.IsNullOrEmpty(Result))
            {
                if (ErrCode == 500) // Ошибка сервиса
                    state = 11;
                return false;
            }

            // Получение статуса запроса

            string sResult = GetValueFromAnswer(Result, "/Response/Result");
            errDesc = GetValueFromAnswer(Result, "/Response/Description");
            
            // Если запрос завершился с ошибкой вернем -1
            if (sResult.ToUpper() != "OK")
            {
                errCode = GetValueFromAnswer(Result, "/Response/ErrCode").ToInt();
                state = 12; // Ошибка на стороне сервиса РАРИДА
                return false;
            }

            // Проверка баланса
            string sBalance = GetValueFromAnswer(Result, "/Response/Data/Balance");
            Balance = sBalance.ToDecimal();

            if (Balance < Amount)
            {
                Log("Баланс точки {0} меньше размера платежа. Сервис приостанавливается", Balance);
                errCode = 12;
                state = 0;
                return false;
            }
            else
            {
                Log("Баланс точки {0}", Balance);
            }

            errCode = 0;
            state = 1;
            return true;
        }


        #region Communications

        /// <summary>
        /// Проверка серверного сертификата
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="policyErrors"></param>
        /// <returns></returns>
        private bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // allow any certificate...
            Log("Серверный сертификат: CN={0} Hash={1} S/n={2}", certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());

            // policyErrors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable;

            if ((policyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                Log("Cert: Chain");
                return false;
            }
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                Log("Cert: Name");
                return false;
            }
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
            {
                Log("Cert: Cert not available");
                return false;
            }

            Log("Сертификат сервера подтверждён");

            return true;
        }

        void InitSecurityProtocol(HttpWebRequest Request)
        {

            // Crypto crypto = new Crypto(CertHash.Replace(" ", ""));
            Crypto crypto = new Crypto(Settings.Rapida.CN);

            // Добавить клиентский 
            if (crypto.Certificate != null)
            {
                if (crypto.HasPrivateKey)
                {
                    Request.ClientCertificates.Add(crypto.Certificate);
                    Log("Подготовлен {2} сертификат: {0} {1}", crypto.Certificate.GetCertHashString(), crypto.Certificate.GetNameInfo(X509NameType.SimpleName, true), Request.ClientCertificates.Count);
                    // Console.WriteLine("Подготовлен {2} сертификат: {0} {1}", certificate.GetCertHashString(), certificate.GetNameInfo(X509NameType.SimpleName, true), Request.ClientCertificates.Count);
                }
                else
                {
                    Log("Клиентский сертификат не имеет приватного ключа");
                    // Console.WriteLine("Клиентский сертификат не имеет приватного ключа");
                }
            }
            else
            {
                Log("Клиентский Сертификат не найден");
                // Console.WriteLine("Клиентский Сертификат не найден");
            }

        }


        string Get(string Endpoint)
        {

            try
            {
                // IPHostEntry Entry = Dns.GetHostEntry(Host);
                // string Endpoint = string.Format("{0}://{1}:{2}/api.php/payment", Proto, Entry.AddressList[0].ToString(), Port);
                // string Endpoint = string.Format("{0}://{1}:{2}/api.php/payment/?{3}", Proto, Host, Port, Query);
                // Console.WriteLine(Endpoint);
                // Log(Endpoint);

                if (Settings.LogLevel.IndexOf("REQ") != -1)
                    Log("GET: {0}", Endpoint);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                ServicePointManager.CheckCertificateRevocationList = false;
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateRemote);
                ServicePointManager.DefaultConnectionLimit = 10;
                ServicePointManager.UseNagleAlgorithm = true;
                // ServicePointManager.CertificatePolicy = new MyCertPolicy();


                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create( Endpoint );

                InitSecurityProtocol(Request);
                // Request.PreAuthenticate = true;
                // Request.Timeout = 300000;
                Request.ProtocolVersion = new Version(1, 1);
                // Request.Credentials = CredentialCache.DefaultNetworkCredentials;
                Request.Credentials = CredentialCache.DefaultCredentials;
                Request.Method = "GET";
                // Request.ContentType = "application/x-www-form-urlencoded";
                Request.Accept = "text/html";
                Request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251");
                Request.KeepAlive = true;
                Request.Timeout = 40000;
                Request.Headers.Add(HttpRequestHeader.ContentEncoding, "identity");
                Request.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity");
                Request.UserAgent = "X-Net Test Client v0.1";

                // CredentialCache cc = new CredentialCache();
                // cc.Add(new Uri("https://apitest.regplat.ru:10443/"), "Negotiate", new NetworkCredential("Администратор", "1"));


                // Request.UseDefaultCredentials = false;
                Request.ContentLength = 0;

                // Log("Получен ответ от сервиса.");
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                Stream ResponseStream = Response.GetResponseStream();
                StreamReader readStream = new StreamReader(ResponseStream, Encoding.GetEncoding(1251));

                string stResponse = readStream.ReadToEnd();
                if (Settings.logLevel.IndexOf("REQ") != -1)
                    Log(" \r\n----------------------------------------------------------\r\n{0}----------------------------------------------------------", stResponse);

                Response.Close();
                readStream.Close();

                return stResponse;
            }
            catch (WebException we)
            {
                // Console.WriteLine("{0} {1} {3}\r\n{2}", we.Status, we.Message, we.StackTrace, we.TargetSite);
                Log("{0} GET: {1} ({2}) {3}", Tid, (int)we.Status, we.Status, we.Message);
                foreach (string key in we.Data.Keys)
                {
                    // Console.WriteLine("{0} = {1}", key, we.Data[key]);
                    Log("{0} = {1}", key, we.Data[key]);
                }

                errCode = (int)we.Status;
                errDesc = we.Message;

            }
            catch (Exception ex)
            {
                Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
                // Console.WriteLine(ex.ToString());
                errCode = 500;
                errDesc = "Внутрення ошибка сервиса HYPERCASSA";
            }

            return "";
        }

        #endregion Communications

        /// <summary>
        /// Извлекает параметр из ответа в виде XPath запроса
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        String GetValueFromAnswer(string answer, string expr)
        {

            if (string.IsNullOrEmpty(answer))
                return "";

            try
            {

                XPathDocument doc = new XPathDocument(new StringReader(answer.ToLower()));
                XPathNavigator nav = doc.CreateNavigator();
                // XPathNodeIterator items = nav.Select(expr);
                XPathNavigator node = nav.SelectSingleNode(expr.ToLower());

                // Log("****************************************************");
                // Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
                // Log("****************************************************");


                // Console.WriteLine("{0}={1}", expr, node.Select(expr.ToLower()).Current.Value);
                return node.Select(expr.ToLower()).Current.Value;

            }
            catch (Exception ex)
            {
                Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
                Log(answer);
            }

            return "";
        }

    }


}
