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
    public class GWXsolllaRequest: GWRequest
    {

        string Agent = "";
        string AgentKey = "";
        Encoding Enc = Encoding.GetEncoding(1251);

        #region Constructors
        public GWXsolllaRequest(GWRequest src) : base(src)
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

        protected override string GetLogName()
        {
            return Settings.Xsolla.Log;
        }

        public override void InitializeComponents()
        {
            CodePage = Settings.Xsolla.CodePage;
            host = Settings.Xsolla.Host;
            if (Service.ToLower() == "2pay")
            {
                Agent = Settings.Xsolla.Agent; // "12791";
                AgentKey = Settings.Xsolla.AgentKey; // "regplat";
            }
            else
            {
                Agent = Settings.Xsolla.Agent1; // "2581";
                AgentKey = Settings.Xsolla.AgentKey1; // "regplat1";
            }
        }

        #endregion Constructors

        /// <summary>
        /// Добавление HTTP-заголовков при отправке запроса
        /// </summary>
        /// <param name="request"></param>
        /*
        public override void AddHeaders(System.Net.HttpWebRequest request)
        {
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Accept = "application/x-www-form-urlencoded";
            request.Headers.Add("Accept-Charset", "UTF-8");
        }
        */

        new int MakeRequest(int state)
        {
            string md5 = "";
            string date = DateTime.Now.ToString("yyyyMMddHHmmss");
            string V1 = HttpUtility.UrlEncode(Number, Encoding.GetEncoding(1251));
            string Command = state == 0 ? "check" : "pay";

            using (Crypto Crypto = new Crypto(""))
            {
                if (!string.IsNullOrEmpty(Number) || !string.IsNullOrEmpty(Account) && Gateway == "5450")
                {
                    if (string.IsNullOrEmpty(Number) && Gateway == "5450")
                        number = Account;
                    RootLog($"{Tid} [Xsolla md5] {Command}|{Gateway}|{Number}|{Amount.AsCurrency()}|{Tid}|{Agent}|{date}|{AgentKey}");
                    md5 = Crypto.Hash(Command + Gateway + Number + Amount.AsCurrency() + Tid + Agent + date + AgentKey, 5, Encoding.UTF8);
                    stRequest = string.Format($"command={Command}&project={Gateway}&v1={Number}&sum={Amount.AsCurrency()}&id={Tid}&type={Agent}&date={date}&md5={md5}");
                }
                else if (!string.IsNullOrEmpty(Account))
                {
                    RootLog($"{Tid} [Xsolla md5] {Command}|{Account}|{Amount.AsCurrency()}|{Tid}|{Agent}|{date}|{AgentKey}");
                    md5 = Crypto.Hash(Command + Gateway + Account + Amount.AsCurrency() + Tid + Agent + date + AgentKey, 5, Encoding.ASCII);
                    stRequest = string.Format("command={0}&account={1}&sum={2}&id={3}&type={4}&date={5}&md5={6}", Command, Account, Amount.AsCurrency(), Tid, Agent, date, md5);
                }
                else
                {
                    RootLog($"{Tid} Number or Account must be sets!");
                    errDesc = "Number or Account must be sets!";
                    return 1;
                }
            }

            Log($"Host: {Host}?{stRequest}");
            Log($"Md5:  {md5}");

            return 0;
        }

        new void Check()
        {
            MakeRequest(0);
            if (!string.IsNullOrEmpty(stRequest))
            {
                stResponse = Get(Host + "?" + stRequest);
                // errCode = GetValueFromAnswer(stResponse, "/response/result").ToInt();
                errCode = stResponse.XPath("/response/result").ToInt();
                // errDesc = GetValueFromAnswer(stResponse, "/response/comment");
                errDesc = stResponse.XPath("/response/comment");
                // addinfo = GetValueFromAnswer(stResponse, "/response/projectName/ru");
                addinfo = stResponse.XPath("/response/projectName/ru");
                // outtid = GetValueFromAnswer(stResponse, "/response/order");
                outtid = stResponse.XPath("/response/order");
            }
        }

        void Pay()
        {
            MakeRequest(3);
            if (!string.IsNullOrEmpty(stRequest))
            {
                stResponse = Get(Host + "?" + stRequest);
                errCode = XPath.GetInt(stResponse, "/response/result").Value;
                errDesc = XPath.GetString(stResponse, "/response/comment");
                addinfo = (string.IsNullOrEmpty(addinfo))? XPath.GetString(stResponse, "/response/projectName/ru"): addinfo;
                outtid = (string.IsNullOrEmpty(Outtid)) ? XPath.GetString(stResponse, "/response/order"): Outtid;
            }
        }

        public override int DoPay(byte old_state, byte try_state)
        {
            int ret = 0;

            try
            {
                if (old_state == 0)
                {
                    Log("{0} [DoPay] {1} Запрос {2} {3} {4}", Tid, Service, Gateway, Number, Amount.AsCurrency());

                    state = 12;
                    errCode = 6;
                    ret = 1;
                    Check();
                    if (ErrCode == 0)
                    {
                        Pay();
                        if (ErrCode == 0)
                        {
                            state = 6;
                            errCode = 3;
                            ret = 0;
                        }
                    }
                }
            }
            catch (WebException we)
            {
                // RootLog("{0} - {1} {2}\r\n{3}", Tid, we.Status, we.Message, we.StackTrace);
                Log("{0} - {1} {2}\r\n{3}", Tid, we.Status, we.Message, we.StackTrace);
                errCode = (int)we.Status;
                errDesc = we.Message;
                errCode = 2;
                state = 11;
                ret = -1;
            }
            catch (Exception ex)
            {
                // RootLog("{0} - {1}\r\n{2}", Tid, ex.Message, ex.StackTrace);
                Log("{0} - {1}\r\n{2}", Tid, ex.Message, ex.StackTrace);
                errCode = 2;
                state = 11;
                ret = -1;
            }
            finally
            {
                UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, outtid: Outtid, locked: state == 6? (byte)0: (byte)1); // Разблокировать если проведён
            }

            errCode = state == 6 ? 3 : 6;
            MakeAnswer();

            return ret;
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
            if (!string.IsNullOrEmpty(Outtid)) sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "Outtid", Outtid);
            if (!string.IsNullOrEmpty(AddInfo)) sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "AddInfo", AddInfo);

            sb.Append("</Response>\r\n");

            stResponse = sb.ToString();

            Log("Подготовлен ответ:\r\n{0}", Answer);

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
            Utility.Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}", certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());

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

            // IPHostEntry Entry = Dns.GetHostEntry(Host);
            // string Endpoint = string.Format("{0}://{1}:{2}/api.php/payment", Proto, Entry.AddressList[0].ToString(), Port);
            // string Endpoint = string.Format("{0}://{1}:{2}/api.php/payment/?{3}", Proto, Host, Port, Query);
            // Console.WriteLine(Endpoint);
            // Log(Endpoint);

            Log("GET: {0}", Endpoint);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.CheckCertificateRevocationList = false;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateRemote);
            ServicePointManager.DefaultConnectionLimit = 10;
            ServicePointManager.UseNagleAlgorithm = true;
            // ServicePointManager.CertificatePolicy = new MyCertPolicy();


            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Endpoint);

            // InitSecurityProtocol(Request);
            // Request.PreAuthenticate = true;
            // Request.Timeout = 300000;
            Request.ProtocolVersion = new Version(1, 1);
            // Request.Credentials = CredentialCache.DefaultNetworkCredentials;
            Request.Credentials = CredentialCache.DefaultCredentials;
            Request.Method = "GET";
            Request.ContentType = "application/x-www-form-urlencoded";
            Request.Accept = "text/html";
            Request.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");
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
            Log(" \r\n----------------------------------------------------------\r\n{0}----------------------------------------------------------", stResponse);

            Response.Close();
            readStream.Close();

            errCode = 0;

            return stResponse;

        }

        #endregion Communications

    }
}
