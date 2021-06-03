using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oldi.Utility;
using System.IO;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Collections.Specialized;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Oldi.Net;
using System.Xml.Linq;

namespace Oldi.Ekt
{
    public class TppRecord
    {
        public string Id;
        public string Area;
        public string City;
        public string CityType;
        public string Kladr;
        public string Address;
        public string Agent;
        public string Inn;
        public string Support;
        public TppRecord(string Id, string Area, string City, string CityType, string Kladr, string Address, string Agent, string Inn, string Support)
        {
            this.Id = Id;
            this.Area = Area;
            this.City = City;
            this.CityType = CityType;
            this.Kladr = Kladr;
            this.Address = Address;
            this.Agent = Agent;
            this.Inn = Inn;
            this.Support = Support;
        }

        public string Deserialize()
        {
            return $"\t<point id=\"{Id}\" area=\"{Area}\" city=\"{City}\" cityType=\"{CityType}\"  kladr=\"{Kladr}\" address=\"{Address}\" agent=\"{Agent}\" inn=\"{Inn}\" support=\"{Support}\" />\r\n";
        }
    }

    public static class Mts
    {

        static string host = "https://217.199.242.228:8182/external2/extended";
        // static string host = Settings.Ekt.Host;

        static string termFolder = "";
        static string logFile = Config.AppSettings["Root"] + Config.AppSettings["LogPath"] + "regtpp.log";
        static string sqlConnection = Config.AppSettings["CoinnectionString"];
        static List<TppRecord> terminals = new List<TppRecord>();
        static string packet = "";

        static string CodePage = Settings.Ekt.Codepage;
		static string ContentType = Settings.Ekt.ContentType;
		static string commonName = Settings.Ekt.Certname;

        static string pointid = Settings.Ekt.Pointid;

        static int errCode = 0;
        static string errDesc = "";

        /// <summary>
        /// Регистрация терминалов МТС
        /// </summary>
        public static void RegTerminals()
        {
            termFolder = Config.AppSettings["Root"] + "tpp\\";
            Console.WriteLine($"Зпись лога в {logFile}");
            ReadTppList();
            SendTppList();
        }

        #region ReadTppList

        /// <summary>
        /// Чтение списка терминалов
        /// </summary>
        static void ReadTppList()
        {
            try
            {
                Console.WriteLine($"Загрузка списка из {termFolder}");
                OnLog($"Загрузка списка из {termFolder}");

                DirectoryInfo dir = new DirectoryInfo(termFolder);
                foreach (FileInfo f in dir.GetFiles("*.xls"))
                    if (f.Extension.ToLower() == ".xls")
                    {
                        string excelConnectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=Excel 8.0", f.FullName);

                        Console.WriteLine($"Найдена таблица: {f.Name}");
                        OnLog($"Найдена таблица: {f.Name}");

                        using (OleDbConnection cnn = new OleDbConnection(excelConnectionString))
                        {

                            string queryString = "SELECT * FROM [ЛИСТ1$]";

                            using (OleDbCommand cmd = new OleDbCommand(queryString, cnn))
                            {
                                cmd.CommandType = System.Data.CommandType.TableDirect;
                                cnn.Open();
                                OleDbDataReader dr = cmd.ExecuteReader(/*System.Data.CommandBehavior.Default*/);
                                // Пропустить первые 3 строки
                                // int line = 0;

                                while (dr.Read())
                                {
                                    // if (++line < 5) continue;
                                    string modify = dr[0].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string area = dr[13].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string id = dr[1].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string kladr = dr[11].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string cityType = dr[15].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string city = dr[16].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string address = dr[17].ToString().Trim().Replace("\r", "").Replace("\n", "")
                                        + ", " + dr[18].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    string agent = /* dr[5].ToString().Trim().Replace("\r", "").Replace("\n", "")*/ "ОЛДИ-Т, ООО";
                                    string inn = /* dr[6].ToString().Trim().Replace("\r", "").Replace("\n", "") */ "7017044175";
                                    string support = dr[8].ToString().Trim().Replace("\r", "").Replace("\n", "");
                                    if (modify == "1")
                                    {
                                        TppRecord tpp = new Ekt.TppRecord(id, area, city, cityType, kladr, address, agent, inn, support);
                                        terminals.Add(tpp);
                                        OnLog($"Processing: {tpp.Deserialize().Replace("\r\n", "")}");
                                    }
                                }
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                OnLog(ex.ToString());
            }
        }

        #endregion ReadTppList


        /// <summary>
        /// Отправка пакета для регистрации терминалов
        /// </summary>
        static void SendTppList()
        {

            MakeTppPacket();
            // OnLog($"Подготовлен пакет:\r\n{packet}");
            SendRequest();

        }

        /// <summary>
        /// Подготовка пакета регистрации терминалов
        /// </summary>
        /// <returns></returns>
        static void MakeTppPacket()
        {
            int id = (int)(DateTime.Now.Ticks & 0xFFFF);
            packet = $"<request point=\"{pointid}\">\r\n";

            foreach (TppRecord tpp in terminals)
            {
                packet += tpp.Deserialize();
            }

            packet += $"<status id=\"{id}\" />\r\n";
            packet += "</request>";
        }

        #region TCPSection

        static bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            // allow any certificate...
            OnLog($"[SendRequest]: Получен сертификат выданый {certificate.Issuer}");

            return true;
        }

        /// <summary>
        /// Добавление клиентского сертификата
        /// </summary>
        /// <param name="request"></param>
        static bool AddCertificate(HttpWebRequest request)
        {

            // Добавление сертификата
            if (!string.IsNullOrEmpty(commonName))
            {
                using (Crypto cert = new Crypto(commonName))
                {
                    if (cert != null)
                        if (cert.HasPrivateKey)
                            request.ClientCertificates.Add(cert.Certificate);
                        else
                        {
                            errDesc = $"Certificate {cert.Certificate.GetCertHashString()} has no private key";
                            errCode = 2;
                        }
                    else
                    {
                        errDesc = $"Certificate {commonName} not found";
                        errCode = 2;
                    }
                }
            }

            return errCode == 0? true: false;

        }

        /// <summary>
        /// Отправка пакета провайдеру
        /// </summary>
        /// <returns></returns>
        static bool SendRequest()
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            byte[] buf;
            Encoding enc;
            string Signature = "";
            string stRequest = "";
            string stResponse = "";

            // OnLog($"Codepage: {enc.WindowsCodePage.ToString()}");
            enc = Encoding.UTF8;
            string xmlHeader = $"<?xml version=\"1.0\" encoding=\"{enc.WebName}\"?>";
            OnLog(xmlHeader);

            try
            {

                // Использем только протоколы семейства TLS
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.CheckCertificateRevocationList = true;
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemote);
                ServicePointManager.DefaultConnectionLimit = 10;

                request = (HttpWebRequest)WebRequest.Create(host);
                request.ProtocolVersion = HttpVersion.Version11;
                request.Credentials = CredentialCache.DefaultCredentials;
                request.KeepAlive = true;
                request.Timeout = 45000;
                request.Method = "POST";
                request.Accept = "text/html, */*";
                // request.ContentType = "application/x-www-form-urlencoded";
                request.ContentType = "text/html";
                request.Headers.Set("Accept-Encoding", "identity");
                request.AllowAutoRedirect = false;
                request.UserAgent = Settings.Title;
                request.ContentType = ContentType;

                // Добавление сертификата
                if (!AddCertificate(request))
                    return false;

                stRequest = xmlHeader + "\r\n" + packet; //.Replace("\r\n", "").Replace("\t", "");
                OnLog($"Request:\r\n{stRequest}");

                // Подпись в заголовок
                using (Crypto crypto = new Crypto(commonName))
                    Signature = crypto.Sign(stRequest, 1, enc);

                OnLog($"PayLogic-Signature: {Signature}");
                request.Headers.Add("PayLogic-Signature", Signature);
                request.UserAgent = Settings.ClientName;

                OnLog("------------------------------------------------------");
                OnLog($"Host={host}");

                if (request.Method == WebRequestMethods.Http.Post)
                {
                    buf = enc.GetBytes(stRequest);
                    request.ContentLength = buf.Length;
                    OnLog($"ContentLength={request.ContentLength}");
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

                        // Если пустой ответ: 1
                        if (string.IsNullOrEmpty(stResponse))
                        {
                            errCode = 1;
                            errDesc = "Не получен ответ от провайдера";
                        }
                        else // Получен ответ от провайдера
                        {
                            errCode = 0;
                            // Получен ответ от провайдера (на транспортном уровне все ОК.
                            foreach(string key in response.Headers.Keys)
                                OnLog($"{key} = {response.Headers[key]}");
                            OnLog($"-----------------------------------------------------------\r\nПолучен ответ:\r\n{stResponse}");

                            // Разбор ответа
                            XDocument answer = XDocument.Parse(stResponse);
                            IEnumerable<XAttribute> attrs =
                                from el in answer.Root.Elements("result")
                                select el.Attribute("state");
                            string state = "";
                            if (attrs.Count() > 0)
                                state = attrs.First().Value;
                            else
                                state = "1";

                            if (state == "0")
                                errCode = 0;
                            else if (state == "1")
                            {
                                errCode = 1;
                                errDesc = "Ошибка формата";
                            }
                            else if (state == "2")
                            {
                                errCode = 2;
                                errDesc = "Ошибка БД/обработки";
                            }
                            else
                            {
                                errCode = -1;
                                errDesc =  $"Неизвестная ошибка ({state})";
                            }
                        }
                    }
                    else
                    {
                        errCode = 1;
                        errDesc = "Не получен ответ от провайдера";
                    }
            }
            catch (WebException we)
            {
                errCode = 2;
                errDesc = we.Status.ToString();
            }
            catch (Exception ex)
            {
                errCode = 2;
                errDesc = ex.ToString();
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= new RemoteCertificateValidationCallback(ValidateRemote);
                if (request != null && request.ClientCertificates != null)
                    request.ClientCertificates.Clear();
                if (response != null) response.Close();
            }

            if (errCode != 0)
                OnLog($"{errDesc} Err={errCode}");

            return errCode == 0? true: false;

        } // SendRequest

        #endregion TCPSection

        static void OnLog(string text)
        {
            Oldi.Net.Utility.Log(logFile, text);
        }
    }
}
