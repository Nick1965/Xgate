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


        int Attempts = 0; // Количество попыток повторных сокдинений при ошибке SSL

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
            // session = src.Session;

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
            try
            {
                host = Settings.Rapida.Host;
                Encoding enc = Encoding.GetEncoding(1251);

                if (Gateway.Equals("regpay", StringComparison.CurrentCultureIgnoreCase))
                {
                    fio = attributes["Fam"] != null ? attributes["Fam"] : ""
                        + " " + attributes["Name"] != null ? attributes["Name"] : ""
                        + " " + attributes["SName"] != null ? attributes["SName"] : "";

                    Fam = HttpUtility.UrlEncode(attributes["Fam"], enc) ?? "";
                    Name = HttpUtility.UrlEncode(attributes["Name"], enc) ?? "";
                    SName = HttpUtility.UrlEncode(attributes["SName"], enc) ?? "";

                    KD = attributes["KD"] ?? "";
                    SD = attributes["SD"] ?? "";
                    ND = attributes["ND"] ?? "";
                    GD = HttpUtility.UrlEncode(attributes["GD"], enc) ?? "";
                    DD = attributes["DD"] ?? "";
                    DR = attributes["DR"] ?? "";
                    MR = HttpUtility.UrlEncode(attributes["MR"], enc) ?? "";
                    CS = HttpUtility.UrlEncode(attributes["CS"], enc) ?? "";
                    AMR = HttpUtility.UrlEncode(attributes["AMR"], enc) ?? "";

                    bik = attributes["BIK"] ?? "";
                    account = HttpUtility.UrlEncode(Account, enc) ?? ""; // Вдруг кто напихает кириллицы в договор
                }
                else
                    // Для платежа по коду требования
                    TemplateTid = attributes["TID"] != null ? attributes["TID"] : "";

                PPID = attributes["PPID"] ?? "";

            }
            catch(Exception ex) {
                RootLog(ex.ToString());
            }

        }
        protected override string GetLogName()
        {
            return Settings.Rapida.Log;
        }

         /// <summary>
        /// Выполняет проверку баланса, вычисляет уникальный номер платежа и регистрирует плательщика и шаблон
        /// </summary>
        public override void Check()
        {

            // Создание уникального номера платежа
            session = Guid.NewGuid().ToString("n").Substring(12);
            errCode = 0;

            if (CheckBalance())
            {
                // Для регистрации
                if (Gateway.Equals("regpay", StringComparison.CurrentCultureIgnoreCase))
                    RegPayeer();
                // Для обоих типов запросов
                if (ErrCode == 0)
                    RegTemplate();
            }
            else
            {
                if (ErrCode == 0) // Будем ждать когда поступят деньги
                {
                    errDesc = "Шлюз временно заблокирован";
                    errCode = 12;
                }
                else
                    errCode = 11;
            }

            if (Tid <= 0)
                MakeAnswer();

        }

        /// <summary>
        /// Проведение платежа
        /// </summary>
        /// <param name="old_state">Текущий статус платежа</param>
        /// <param name="try_state">Требуемый статус платежа</param>
        /// <returns></returns>
        public override int DoPay(byte old_state, byte try_state)
        {

            try
            {
                // Блокировать допроведение записи
                SetLock(1);

                if (old_state == 0)
                {

                    if (string.IsNullOrEmpty(Session))
                        session = Guid.NewGuid().ToString("n").Substring(12);

                    RootLog($"{Tid} [Rapida.DoPay] {Session} Запрос {Gateway} {Fio} {TemplateTid}");

                    Check();
                    if (ErrCode == 0)
                        TemplatePayment();

                }
                else
                    return 0;

                if (ErrCode == 11 || ErrCode == 12)
                    state = old_state;

            }
            catch (Exception ex)
            {
                RootLog(ex.ToString());
                errCode = 2;
                state = 11;
            }
            finally
            {
                UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, opcode: GKID, acceptCode: TemplateTid, limit: Balance, locked: 0); // Разблокировать если проведён
            }


            MakeAnswer();

            return ErrCode;
        }


        /// <summary>
        /// Создание строки ответа
        /// </summary>
        void MakeAnswer()
        {

            stResponse = "<Response>\r\n";

            stResponse += $"\t<UniqID>{Session}</UniqID>\r\n";
            stResponse += $"\t<ErrCode>{ErrCode}</ErrCode>\r\n";
            stResponse += $"\t<ErrDesc>{ErrDesc}</ErrDesc>\r\n";

            if (!string.IsNullOrEmpty(Fam))
                stResponse += $"\t<Fam>{Fam.ToUpper()}</Fam>\r\n";
            if (!string.IsNullOrEmpty(Name))
                stResponse += $"\t<Name>{Name.ToUpper()}</Name>\r\n";
            if (!string.IsNullOrEmpty(SName))
                stResponse += $"\t<SName>{SName.ToUpper()}</SName>\r\n";
            stResponse += $"\t<FIO>{Fio.ToUpper()}</FIO\r\n";
            stResponse += $"\t<GKID>{GKID}</GKID>\r\n";
            stResponse += $"\t<TID>{TemplateTid}</TID>\r\n";
            stResponse += $"\t<Balance>{Balance.AsCF()}</Balance>\r\n";
            stResponse += $"\t<Appendix>{Bank}</Appendix>\r\n";

            stResponse += "</Response>\r\n";

            Log("Подготовлен ответ:\r\n{0}", stResponse);

        }
        
        /// <summary>
        /// Выполнения платежа по шаблону
        /// </summary>
        void TemplatePayment()
        {

            if (ErrCode != 0)
                return;

            int paySum = (int)(Amount * 100M);

            RootLog("{0} [Rapida.TplPay - start] Ext={1} TID={2} Trm={3} Amount={4}", Tid, Session, TemplateTid, PPID, Amount.AsCurrency());
            stRequest = string.Format("?function=payment&PaymExtId={0}&PPID={1}&TID={2}&Amount={3}", Session, PPID, TemplateTid, paySum);

            string Result = Get(Host + stRequest);
            if (string.IsNullOrEmpty(Result))
            {
                // Если ршибка SSL-соединения попробуем ещё раз
                if ((WebExceptionStatus)ErrCode == WebExceptionStatus.SecureChannelFailure && Attempts++ < 5)
                {
                    Wait(20);
                    TemplatePayment();
                    return;
                }

                if (ErrCode != 0) // Ошибка сервиса
                    errCode = 11;
                return;
            }

            string ResultStatus = XPath.GetString(Result, "/Response/Result");
            if (string.IsNullOrEmpty(ResultStatus))
                ResultStatus = XPath.GetString(Result, "/Response/CheckResult");

            if (ResultStatus.ToUpper() == "OK")
            {
                // Баланс агента
                Balance = XPath.GetDec(Result, "/Response/Balance").Value;
                errDesc = "Платёж проведён";
                Bank = $"{XPath.GetString(Result, "/Response/B_Name")}; {XPath.GetString(Result, "/Response/List/par1")}; {XPath.GetString(Result, "/Response/List/par3")}; {XPath.GetString(Result, "/Response/List/par4")}";
                errCode = 3;
                state = 6;
            }
            else
            {
                errCode = XPath.GetInt(Result, "/Response/ErrCode").Value;
                errDesc = $"({errCode}) {XPath.GetString(Result, "/Response/Description")}";
                errCode = 6;
                state = 12;
            }

            RootLog($"{Tid} [Rapida.TplPay - finish] Ext={Session} TID={TemplateTid} Trm={PPID} Balance={Balance.AsCurrency()} err={ErrCode} {ErrDesc}");

        }

        /// <summary>
        /// Регистрация платёжного шаблона
        /// </summary>
        void RegTemplate()
        {

            if (ErrCode != 0)
                return;
            // Если запрос с регистрацией
            if (Gateway.ToLower() == "regpay")
            {
                string Params = $"{Bik};{Account};{Fam} {Name} {SName};{Number}";
                RootLog($"{Tid} [Rapida.RegTpl - start] Ext={Session} Trm={PPID} Ph={Phone} Params=\"{Params}\"");
                stRequest = $"?function=check&PaymExtId={Session}&PPID={PPID}&mPhone={Phone}&RCode=601&Params={Params}";
            }
            // Если запрос по номеру кода
            else
            {
                RootLog($"{Tid} [Rapida.RegTpl - start] Ext={Session} Trm={PPID} TID={TemplateTid}");
                stRequest = $"?function=check&PaymExtId={Session}&PPID={PPID}&TID={TemplateTid}";
            }

            string Result = Get(Host + stRequest);
            if (string.IsNullOrEmpty(Result))
            {
                // Если ршибка SSL-соединения попробуем ещё раз
                if ((WebExceptionStatus)ErrCode == WebExceptionStatus.SecureChannelFailure && Attempts++ < 5)
                {
                    Wait(20);
                    RegTemplate();
                    return;
                }

                if (ErrCode == 500) // Ошибка сервиса
                    state = 11;
                return;
            }

            string ResultStatus = XPath.GetString(Result, "/Response/CheckResult");
            if (ResultStatus.ToUpper() == "OK")
            {
                Fam = XPath.GetString(Result, "/Response/Fam");
                Name = XPath.GetString(Result, "/Response/Name");
                SName = XPath.GetString(Result, "/Response/SName");

                fio = Fam + " " + Name + " " + SName;

                // Код требования
                TemplateTid = XPath.GetString(Result, "/Response/Tid");
                // Банк
                Bank = $"{XPath.GetString(Result, "/Response/B_Name")}; {XPath.GetString(Result, "/Response/List/par1")}; {XPath.GetString(Result, "/Response/List/par3")}; {XPath.GetString(Result, "/Response/List/par4")}";
                // Bank = GetValueFromAnswer(Result, "/Response/B_Name");
                // Номер платежа в системе
                PaymNumb = XPath.GetString(Result, "/Response/PaymNumb");
                // Баланс агента
                Balance = XPath.GetDec(Result, "/Response/Balance").Value;
                errDesc = "Платёж проведён";
                errCode = 0;
                state = 0;
            }
            else
            {
                errCode = XPath.GetInt(Result, "/Response/ErrCode").Value;
                errDesc = $"({ErrCode}) {XPath.GetString(Result, "/Response/Description")}";
                errCode = 6;
                state = 12;
            }

            RootLog($"{Tid} [RegTpl - finish] Ext={Session} TID={TemplateTid} FIO={Fam + " " + Name + " " + SName} Trm={PPID} PayNumb={PaymNumb} Balance={Balance.AsCurrency()} \r\nerr={ErrCode}: {ErrDesc}");

        }

        /// <summary>
        /// Регистрация плательщика
        /// </summary>
        void RegPayeer()
        {

            // Если какого-то параметра нет - возникнет исключение

            if (Gateway.ToLower() != "regpay")
                return;

            RootLog($"{Tid} [Rapida.RegPay - start] Ext={Session} Ph={Phone} Fio={Fam.ToUpper()}||{Name.ToUpper()}||{SName.ToUpper()} Pass={KD}||{SD}||{ND} GD={GD} DD={DD} DR={DR} MR={MR} CS={CS} AMR={AMR}, Trm={PPID}");

            stRequest = $"?function=reg&PaymExtId={Session}&PPID={PPID}&mPhone={Phone}&fam={(Fam != null ? Fam.ToUpper() : "")}&name={(Name != null ? Name.ToUpper(): "")}&sname={(SName != null ? SName.ToUpper() : "")}&KD={KD}&SD={SD}&ND={ND}&GD={GD}&DD={DD}&DR={DR}&MR={MR}&CS={CS}&AMR={AMR}";

            string Result = Get(Host + stRequest);
            if (string.IsNullOrEmpty(Result))
            {
                // Если ршибка SSL-соединения попробуем ещё раз
                if ((WebExceptionStatus)ErrCode == WebExceptionStatus.SecureChannelFailure && Attempts++ < 5)
                {
                    Wait(20);
                    RegPayeer();
                    return;
                }

                if (ErrCode != 0) // Ошибка сервиса
                    errCode = 11;
                return;
            }

            string ResultStatus = XPath.GetString(Result, "/Response/Result");
            if (ResultStatus.ToUpper() == "OK")
            {
                GKID = XPath.GetString(Result, "/Response/Gkid");
                errDesc = XPath.GetString(Result, "/Response/Description");
                errCode = 0;
                state = 0;
            }
            else
            {
                errCode = XPath.GetInt(Result, "/Response/ErrCode").Value;
                errDesc = $"({errCode}) {XPath.GetString(Result, "/Response/Description")}";
                errCode = 6;
                state = 12;
            }

            RootLog($"{Tid} [Rapida.RegPay - finish]  Ext={Session} GkID={GKID}\r\nerr={ErrCode}: {ErrDesc}");

        }

        /// <summary>
        /// Проверка баланса счёта агента
        /// </summary>
        /// <returns></returns>
        bool CheckBalance()
        {

            RootLog($"{Tid} [Rapida.CheckBalance - start] {Session}");

            string Result = Get(Host + $"?function=getbalance&PaymExtId={Session}");

            // Если ршибка SSL-соединения попробуем ещё раз
            if ((WebExceptionStatus)ErrCode == WebExceptionStatus.SecureChannelFailure && Attempts++ < 5)
            {
                Wait(20);
                return CheckBalance();
            }
            
            // Получение статуса запроса
            string sResult = XPath.GetString(Result, "/Response/Result");
            errDesc = XPath.GetString(Result, "/Response/Description");


            // Если запрос завершился с ошибкой вернем -1
            if (sResult.ToUpper() != "OK")
            {
                errCode = XPath.GetInt(Result, "/Response/ErrCode").Value;
                state = 12; // Ошибка на стороне сервиса РАРИДА
                RootLog($"{Tid} [Rapida.CheckBalance - finish] {Session} Result={sResult} {ErrDesc}");
                return false;
            }

            // Проверка баланса
            Balance = XPath.GetDec(Result, "/Response/Data/Balance").Value;

            if (Balance < Amount)
            {
                RootLog($"{Tid} [Rapida.CheckBalance - finish] {Session} Баланс {Balance} меньше размера платежа. Сервис приостанавливается\r\nResult={sResult}: {ErrDesc}");
                errCode = 12;
                errDesc = "Шлюз временно заблокирован";
                state = 0;
                return false;
            }
            else
            {
                RootLog($"{Tid} [Rapida.CheckBalance - finish] {Session} {Service} sResult={sResult} Balance={Balance.AsCF()} Amount={Amount.AsCF()}");
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
            /*
            // allow any certificate...
            // Utility.Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}", certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());

            // policyErrors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable;

            if ((policyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                Utility.Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}: Ошибка наследования", 
                    certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
                return false;
            }
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                Utility.Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}: Имя сертификата не совпадает с именем сервера", 
                    certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
                return false;
            }
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
            {
                Utility.Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}: Сертификат недействителен", 
                    certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
                return false;
            }

            Utility.Log(".\\log\\Certificates.log", "Серверный сертификат подтверждён: CN={0} Hash={1} S/n={2}", 
                certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
            */

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
                    // Log("Подготовлен {2} сертификат: {0} {1}", crypto.Certificate.GetCertHashString(), crypto.Certificate.GetNameInfo(X509NameType.SimpleName, true), Request.ClientCertificates.Count);
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

                Log($"{Tid} GET: {Endpoint}");
                Log($"----------------------------------------------------------");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
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
                Request.Accept = "text/plain";
                Request.Headers.Add(HttpRequestHeader.AcceptCharset, "windows-1251");
                Request.KeepAlive = true;
                Request.Timeout = 60 * 1000;
                Request.Headers.Add(HttpRequestHeader.ContentEncoding, "identity");
                Request.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity");
                Request.UserAgent = Settings.Title;

                // CredentialCache cc = new CredentialCache();
                // cc.Add(new Uri("https://apitest.regplat.ru:10443/"), "Negotiate", new NetworkCredential("Администратор", "1"));


                // Request.UseDefaultCredentials = false;
                Request.ContentLength = 0;

                // Log("Получен ответ от сервиса.");
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                Stream ResponseStream = Response.GetResponseStream();
                StreamReader readStream = new StreamReader(ResponseStream, Encoding.GetEncoding(1251));

                string stResponse = readStream.ReadToEnd();
                Log($" \r\n{stResponse}\r\n----------------------------------------------------------");

                Response.Close();
                readStream.Close();

                errCode = 0;

                return stResponse;
            }
            catch (WebException we)
            {
                Log($"{Tid} {Session} GET: {(int)we.Status} ({we.Status}) {we.Message}\r\n{we.StackTrace}");
                foreach (string key in we.Data.Keys)
                {
                    // Console.WriteLine("{0} = {1}", key, we.Data[key]);
                    // Log("{0} = {1}", key, we.Data[key]);
                }

                errCode = (int)we.Status;
                errDesc = we.Message;

            }
            catch (Exception ex)
            {
                Log("{0} GET: {1}\r\n{2}", Session, ex.Message, ex.StackTrace);
                // Console.WriteLine(ex.ToString());
                errCode = 500;
                errDesc = "Внутрення ошибка сервиса HYPERCASSA";
            }

            return "";
        }

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
            }

            return "";
        }

        #endregion Communications


    }


}
