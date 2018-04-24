using Oldi.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SchoolGateway
{
    public class SchoolGatewayClass: Oldi.Net.GWRequest
    {

        public SchoolGatewayClass(): base()
            {
            Init();
            }
        public SchoolGatewayClass(Oldi.Net.GWRequest src) : base(src)
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

            // session = Properties.Settings.Default.SessionPrefix + tid.ToString();
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

            Init();
        }

        public void Init()
        {
            stRequest = "";
            host = Oldi.Utility.Config.Providers["school"]["host"];
            string srv = Service == "1" ? "cafeteria" : "primary";
            checkHost = host + $"check?account_id=7{Account}&service={srv}";
            payHost = host + $"new?payment_id={Tid}&account_id=7{Account}&sum={Amount.AsCF()}&time={Oldi.Net.Utility.timeStamp()}&service={srv}";
            Log("CheckHost" + checkHost);
            Log("PayHost" + payHost);
        }

        private bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        /// <summary>
        /// log-file
        /// </summary>
        /// <returns></returns>
        protected override string GetLogName()
        {
            return Oldi.Utility.Settings.Root + "log\\" + Oldi.Utility.Config.Providers["school"]["log"];
        }

        new int MakeRequest(int state)
        {
            int date = Oldi.Net.Utility.timeStamp();


            if (State == 0)
                stRequest = string.Format(checkHost, account, Service == "1" ? "cafeteria" : "primary");
            else if (State == 3)
                stRequest = string.Format(payHost, Tid, Account, Amount.AsCF(), date, Service == "1" ? "cafeteria" : "primary");

            RootLog($"{Tid} [School] {Service}|{Account}|{Amount.AsCurrency()}|{Tid}|{date}");

            Log($"Host: {stRequest}");
            // Log($"Md5:  {md5}");

            return 0;
        }



        new void Check()
        {
            // MakeRequest(0);
            Log($"{Tid} Check {checkHost}");
            if (!string.IsNullOrEmpty(checkHost))
            {
                stResponse = Get(checkHost);
                errCode = stResponse.ToInt();
                switch(errCode)
                {
                    case -1:
                        errCode = 6;
                        errDesc = "неверный идентификатор счета";
                        state = 12;
                        break;
                    case -2:
                        errCode = 6;
                        errDesc = "некорректные или отсутствующие значения параметров";
                        state = 12;
                        break;
                    case 0:
                        errCode = 0;
                        errDesc = "проверка успешно прошла";
                        state = 3;
                        break;
                }
                // errCode = GetValueFromAnswer(stResponse, "/response/result").ToInt();
                // errCode = stResponse.XPath("/response/result").ToInt();
                // errDesc = GetValueFromAnswer(stResponse, "/response/comment");
                // errDesc = stResponse.XPath("/response/comment");
                // addinfo = GetValueFromAnswer(stResponse, "/response/projectName/ru");
                // addinfo = stResponse.XPath("/response/projectName/ru");
                // outtid = GetValueFromAnswer(stResponse, "/response/order");
                // outtid = stResponse.XPath("/response/order");
            }
        }

        /// <summary>
        /// Запрос баланса
        /// </summary>
        public void Balance()
        {
            // Service "cafeteria" | "primary"
            RootLog($"{Tid} [School] {Service}|{Account}");
            checkHost = host + $"balance?account_id=7{Account}&service={Service}";
            stResponse = Get(checkHost);
            balance = stResponse.ToDecimal();
        }

        void Pay()
        {
            // MakeRequest(3);
            if (!string.IsNullOrEmpty(payHost))
            {
                stResponse = Get(payHost);
                errCode = stResponse.ToInt();
                Log($"{Tid} Pay {payHost}");
                switch (errCode)
                {
                    case -1:
                        errCode = 6;
                        errDesc = "неверный идентификатор счета";
                        state = 12;
                        break;
                    case -2:
                        errCode = 6;
                        errDesc = "некорректные или отсутствующие значения параметров";
                        state = 12;
                        break;
                    case -3:
                        errCode = 1;
                        errDesc = "нефатальная ошибка сервера, возможна повторная попытка передать платеж";
                        state = 3;
                        break;
                    case 0:
                        errCode = 0;
                        errDesc = "операция успешна";
                        state = 6;
                        break;
                    case 1:
                        errCode = 0;
                        errDesc = "платеж с таким идентификатором уже был совершен";
                        state = 6;
                        break;
                }

                // errCode = XPath.GetInt(stResponse, "/response/result").Value;
                // errDesc = XPath.GetString(stResponse, "/response/comment");
                // addinfo = (string.IsNullOrEmpty(addinfo)) ? XPath.GetString(stResponse, "/response/projectName/ru") : addinfo;
                // outtid = (string.IsNullOrEmpty(Outtid)) ? XPath.GetString(stResponse, "/response/order") : Outtid;
            }
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
            // if (!string.IsNullOrEmpty(Outtid)) sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "Outtid", Outtid);
            // if (!string.IsNullOrEmpty(AddInfo)) sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "AddInfo", AddInfo);

            sb.Append("</Response>\r\n");

            stResponse = sb.ToString();

            Log("Подготовлен ответ:\r\n{0}", Answer);

        }


        
        
        /// <summary>
        /// Проверка возможности, либо проведение платежа
        /// </summary>
        /// <param name="old_state"></param>
        /// <param name="try_state"></param>
        /// <returns></returns>
        public override int DoPay(byte old_state, byte try_state)
        {
            int ret = 0;

            try
            {
                if (old_state == 0)
                {
                    RootLog($"{Tid} [DoPay] {Service} Запрос {Account} {Amount.AsCF()}");

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
                       else if (ErrCode == 1)
                        {
                            state = 3;
                            errCode = 1;
                            ret = 0;
                        }
                    }
                }

                errCode = state == 6 ? 3 : 6;
                MakeAnswer();

            }
            catch (WebException we)
            {
                // RootLog("{0} - {1} {2}\r\n{3}", Tid, we.Status, we.Message, we.StackTrace);
                RootLog($"{Tid} {we.Status} - {we.Message}\r\n{we.StackTrace}", Tid, we.Status, we.Message, we.StackTrace);
                errCode = (int)we.Status;
                errDesc = we.Message;
                errCode = 2;
                state = 11;
                ret = -1;
            }
            catch (Exception ex)
            {
                // RootLog("{0} - {1}\r\n{2}", Tid, ex.Message, ex.StackTrace);
                RootLog($"{Tid} - {ex.Message}\r\n{ex.StackTrace}");
                errCode = 2;
                state = 11;
                ret = -1;
            }
            finally
            {
                UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, outtid: Outtid, locked: state == 6 ? (byte)0 : (byte)1); // Разблокировать если проведён
            }

            return ret;
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
            Request.Method = "GET";
            Request.ContentType = "application/x-www-form-urlencoded";
            Request.Accept = "text/html";
            Request.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");
            Request.KeepAlive = true;
            Request.Timeout = 40000;
            Request.Headers.Add(HttpRequestHeader.ContentEncoding, "identity");
            Request.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity");
            Request.UserAgent = "X-Net Test Client v0.1";

            CredentialCache cc = new CredentialCache();
            cc.Add(new Uri(Host), "Basic", new NetworkCredential( Config.Providers["school"]["login"], Config.Providers["school"]["password"] ));
            Request.Credentials = cc;

            // Request.UseDefaultCredentials = false;
            Request.ContentLength = 0;

            // Log("Получен ответ от сервиса.");

            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            Stream ResponseStream = Response.GetResponseStream();
            StreamReader readStream = new StreamReader(ResponseStream, Encoding.GetEncoding(65001));

            string stResponse = readStream.ReadToEnd();
            
            Log($"----------------------------------------------------------");
            Log($"Ответ: {stResponse}");
            Log($"----------------------------------------------------------");

            Response.Close();
            readStream.Close();

            errCode = 0;

            return stResponse;

        }


    }
}
