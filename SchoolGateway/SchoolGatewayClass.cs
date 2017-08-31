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
        public SchoolGatewayClass(Oldi.Net.GWRequest gw) : base(gw)
        {
            Init();
        }

        public void Init()
        {
            string stRequest = "";
            host = Oldi.Utility.Config.Providers["school"]["host"];
            checkHost = host + Oldi.Utility.Config.Providers["school"]["check"];
            payHost = host + Oldi.Utility.Config.Providers["school"]["pay"];
            Log(checkHost);
            Log(payHost);
        }

        private bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // allow any certificate...
            Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}", certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());

            // policyErrors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable;

            if ((policyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}: Ошибка наследования",
                    certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
                return false;
            }
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}: Имя сертификата не совпадает с именем сервера",
                    certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
                return false;
            }
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
            {
                Log(".\\log\\Certificates.log", "Серверный сертификат: CN={0} Hash={1} S/n={2}: Сертификат недействителен",
                    certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());
                return false;
            }

            Log(".\\log\\Certificates.log", "Серверный сертификат подтверждён: CN={0} Hash={1} S/n={2}",
                certificate.Subject, certificate.GetCertHashString(), certificate.GetSerialNumberString());

            return true;
        }

        /// <summary>
        /// log-file
        /// </summary>
        /// <returns></returns>
        protected override string GetLogName()
        {
            return Oldi.Utility.Config.Providers["school"]["log"];
        }

        new int MakeRequest(int state)
        {
            int date = Oldi.Net.Utility.timeStamp();


            if (State == 0)
                stRequest = host + "?" + string.Format(checkHost, account, Service == "1" ? "cafeteria" : "primary");
            else if (State == 3)
                stRequest = host + "?" + string.Format(payHost, Tid, Account, Amount.AsCF(), date, Service == "1" ? "cafeteria" : "primary");

            RootLog($"{Tid} [School] {Service}|{Account}|{Amount.AsCurrency()}|{Tid}|{date}");

            Log($"Host: {stRequest}");
            // Log($"Md5:  {md5}");

            return 0;
        }

        new void Check()
        {
            MakeRequest(0);
            if (!string.IsNullOrEmpty(stRequest))
            {
                stResponse = Get(stRequest);
                Log(stResponse);
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

        void Pay()
        {
            MakeRequest(3);
            if (!string.IsNullOrEmpty(stRequest))
            {
                stResponse = Get(stRequest);
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
            if (!string.IsNullOrEmpty(Outtid)) sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "Outtid", Outtid);
            if (!string.IsNullOrEmpty(AddInfo)) sb.AppendFormat("\t<{0}>{1}</{0}>\r\n", "AddInfo", AddInfo);

            sb.Append("</Response>\r\n");

            stResponse = sb.ToString();

            Log("Подготовлен ответ:\r\n{0}", Answer);

        }


        public override int DoPay(byte old_state, byte try_state)
        {
            int ret = 0;

            try
            {
                if (old_state == 0)
                {
                    Log("{0} [DoPay] {1} Запрос {2} {3} {4}", Tid, Service, Account, Amount.AsCurrency());

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
                UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, outtid: Outtid, locked: state == 6 ? (byte)0 : (byte)1); // Разблокировать если проведён
            }

            errCode = state == 6 ? 3 : 6;
            MakeAnswer();

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
            StreamReader readStream = new StreamReader(ResponseStream, Encoding.GetEncoding(65001));

            string stResponse = readStream.ReadToEnd();
            Log(" \r\n----------------------------------------------------------\r\n{0}----------------------------------------------------------", stResponse);

            Response.Close();
            readStream.Close();

            errCode = 0;

            return stResponse;

        }


    }
}
