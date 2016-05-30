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

namespace Oldi.Net
{
    public class GWRapidaRequest : GWRequest
    {
        new const string Host = "http://tepay.rapida.ru/hypertest/";
        const string FunctionGetBalance = "?function=getbalance&PaymExtId={0}";
        const string CertHash = "91 c7 b2 38 a5 99 38 20 f0 5b 84 f9 34 6f a0 4c 03 b3 aa 99";
        readonly string LogFileName = Settings.LogPath + ".\\Rapida.log";
        new decimal Balance = 0;

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

        protected override string GetLogName()
        {
            return LogFileName;
        }

        public override int MakeRequest(int old_state)
        {
            Log("{0} CHEK", Tid);
            // Проверка баланса агента
            Log("{0} [REGP] Начало регистрации", Tid);
            return 0;
        }

        public override int DoPay(byte old_state, byte try_state)
        {

            // Блокировать допроведение записи
            SetLock(1);

            if (old_state == 0)
                if (!CheckBalance())
                {
                    errCode = 12;
                    errDesc = "Ошибка проверки баланса";
                }
                else
                {
                    errCode = 0;
                    state = 1;
                }
            if (old_state > 0)
            {
                errCode = 2;
                state = 11;
                errDesc = "Не реализовано";
            }

            UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, limit: Balance, locked: 0); // Разблокировать если проведён

            return ErrCode;
        }

        bool CheckBalance()
        {

            string Result = Get(Host + string.Format("?function=getbalance&PaymExtId={0}", Tid));
            if (Result == "")
                return false;

            // Проверка баланса
            string sBalance = GetValueFromAnswer(Result, "/Response/Data/Balance");
            if (!decimal.TryParse(sBalance, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out Balance))
                return false;
            if (Balance < Amount)
            {
                Log("Баланс точки {0} меньше размера платежа. Сервис приостанавливается", Balance);
                errCode = 12;
                state = 0;
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

            Crypto crypto = new Crypto(CertHash.Replace(" ", ""));

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

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                ServicePointManager.CheckCertificateRevocationList = false;
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemote);
                ServicePointManager.DefaultConnectionLimit = 10;
                ServicePointManager.UseNagleAlgorithm = true;
                // ServicePointManager.CertificatePolicy = new MyCertPolicy();


                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Endpoint);

                InitSecurityProtocol(Request);
                // Request.PreAuthenticate = true;
                // Request.Timeout = 300000;
                Request.ProtocolVersion = new Version(1, 1);
                // Request.Credentials = CredentialCache.DefaultNetworkCredentials;
                Request.Credentials = CredentialCache.DefaultCredentials;
                Request.Method = "GET";
                Request.ContentType = "application/x-www-form-urlencoded";
                Request.Accept = "text/html";
                // Request.ContentType = "application/x-www-form-urlencoded"; 
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
                // Log(stResponse);

                Response.Close();
                readStream.Close();

                return stResponse;
            }
            catch (WebException we)
            {
                // Console.WriteLine("{0} {1} {3}\r\n{2}", we.Status, we.Message, we.StackTrace, we.TargetSite);
                Log("{0} {1} {3}\r\n{2}", we.Status, we.Message, we.StackTrace, we.TargetSite);
                foreach (string key in we.Data.Keys)
                {
                    Console.WriteLine("{0} = {1}", key, we.Data[key]);
                    Log("{0} = {1}", key, we.Data[key]);
                }

            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                // Console.WriteLine(ex.ToString());
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
            }

            return "";
        }

    }


}
