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
using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace SchoolGateway
{
    public class SchoolGatewayClass: Oldi.Net.GWRequest
    {

        string balanceHost = "";
        int? benId = null;

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
            balance = null;
            host = Oldi.Utility.Config.Providers["school"]["host"];
            string srv = Service == "1" ? "cafeteria" : "primary";
            balanceHost = host + $"balance?account_id=7{Account}&service={srv}";
            checkHost = host + $"check?account_id=7{Account}&service={srv}";
            payHost = host + $"new?payment_id={Tid}&account_id=7{Account}&sum={Amount.AsCF()}&time={Oldi.Net.Utility.timeStamp()}&service={srv}";
            // Log("CheckHost" + checkHost);
            // Log("PayHost" + payHost);
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

        /*
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
        */


        /// <summary>
        /// Запрос баланса
        /// </summary>
        new void Balance()
        {
            Get(checkHost);
            if (ErrCode == 3)
                balance = stResponse.ToDecimal();
        }

        /// <summary>
        /// Проведение платежа
        /// </summary>
        void Pay()
        {
            // MakeRequest(3);
            if (!string.IsNullOrEmpty(payHost))
                Get(payHost);
        }

        /// <summary>
        /// Создание строки ответа
        /// </summary>
        void MakeAnswer()
        {

            stResponse = $@"<Response>
                            <code>{ErrCode}<code>
                            <desc>{ErrDesc}<desc>
                            <fio>{Fio}</fio>
                            <School>{AddInfo}</School>
                            <food-provider>{Ben}</food-provider>
                            <food-provider-id>{benId}<food-provider-id>
                            <balance>{balance.AsCF()}</balance>
                            </Response>";

            Log($"Подготовлен ответ:\r\n{stResponse}");

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
                    RootLog($"{Tid} [DoPay] Acnt={Account} Usl={Service} Amount={Amount.AsCF()}");

                    ret = 1;
                    Check();
                    // if (ErrCode == 3)
                    //    Balance();
                    if (ErrCode == 1)
                        Pay();
                }
            }
            catch (WebException we)
            {
                // RootLog("{0} - {1} {2}\r\n{3}", Tid, we.Status, we.Message, we.StackTrace);
                // RootLog($"{Tid} {we.Status} - {we.Message}\r\n{we.StackTrace}", Tid, we.Status, we.Message, we.StackTrace);

                // Сообщение об ошибке не логируем
                errCode = 6;
                state = 12;
                errDesc = $"Code: {((HttpWebResponse)we.Response).StatusCode} {((HttpWebResponse)we.Response).StatusDescription}";
            }
            catch (Exception ex)
            {
                // RootLog("{0} - {1}\r\n{2}", Tid, ex.Message, ex.StackTrace);
                RootLog($"{Tid} - {ex.Message}\r\n{ex.StackTrace}");
                errCode = 6;
                errDesc = ex.Message;
                state = 12;
                ret = -1;
            }
            finally
            {
                UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, outtid: Outtid, locked: state == 6 ? (byte)0 : (byte)1); // Разблокировать если проведён
            }

            MakeAnswer();
            return ret;
        }

        public class Rec
        {
            public string SchoolNumber;
            public string FIO;
            // public string Ben;
        }

        public class Rec2
        {
            public string Name;
            public int Id;
        }

        // <summary>
        /// Выполнить запрос Check
        /// </summary>
        public override void DoCheck(bool session = false)
        {
            // Log(stRequest);

            // Создать запрос
            // MakeRequest(0);

            Log($"Поиск ПУ для {account}");

            IDbConnection cnn = new SqlConnection(Settings.ConnectionString);
            var answer = cnn.Query<Rec>("Select SchoolNumber, Name From PU.Studentt where Phone = @p or Phone1 = @p or Phone2 = @p", new { p = Account } );
            if (answer.Count() >= 1)
            {
                fio = answer.First().FIO;
                addinfo = answer.First().SchoolNumber;
                errCode = 1;
                state = 1;
                errDesc = "Абонент найден";

                if (Service == "2") // Cafeteria`
                {
                    var provider = cnn.Query<Rec2>("Select Name, Id From PU.Provider Where School = @School", new { School = addinfo });
                    if (provider.Count() >= 1)
                    {
                        ben = provider.First().Name;
                        benId = provider.First().Id;
                        Log($"Абонент {fio} школа {addinfo} поставщик {Ben} ( {benId} )");
                    }
                    else
                    {
                        errCode = 6;
                        state = 12;
                        errDesc = "Не найден поставщик услуг";
                        Log(errDesc);
                    }
                }
            }
            else
            {
                errCode = 6;
                state = 12;
                errDesc = "Абонент не найден";
                Log(errDesc);
            }

            // Создать ответ
            MakeAnswer();

        }

        /// <summary>
        /// Get - get-запрос к серверу
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <returns></returns>
        void Get(string Endpoint)
        {

            // IPHostEntry Entry = Dns.GetHostEntry(Host);
            // string Endpoint = string.Format("{0}://{1}:{2}/api.php/payment", Proto, Entry.AddressList[0].ToString(), Port);
            // string Endpoint = string.Format("{0}://{1}:{2}/api.php/payment/?{3}", Proto, Host, Port, Query);
            // Console.WriteLine(Endpoint);
            
            Log($"Tid={Tid} Ph={Phone} Acnt={Account} Card={Card} Num={Number} {Endpoint}");

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
            Log($"Ответ сервера:");
            Log($"Code: {Response.StatusCode} {Response.StatusDescription} Ответ: {stResponse}");
            Log($"----------------------------------------------------------");

            Response.Close();
            readStream.Close();

            // Если статус равен 200, вернём код
            if (Response.StatusCode == HttpStatusCode.OK)
            {
                errCode = stResponse.ToInt();
                switch(errCode)
                {
                    case 0:
                        errCode = 3;
                        errDesc = "OK";
                        state = 6;
                        break;
                    case 1:
                        errCode = 3;
                        errDesc = "Платёж с таким идентификатором уже совершён";
                        state = 6;
                        break;
                    case -1:
                        errCode = 6;
                        errDesc = "Неверный идентификатор счёта";
                        state = 12;
                        break;
                    case -2:
                        errCode = 6;
                        errDesc = "Некорркетные или отсутствующие значения параметров";
                        state = 12;
                        break;
                    case -3:
                        errCode = 1;
                        errDesc = "Нефатальная ошибка сервера";
                        state = 3;
                        break;
                }
            }
            else
            {
                errCode = 6;
                errDesc = $"Code: {Response.StatusCode} {Response.StatusDescription}";
                state = 12;
            }

        }


    }
}
