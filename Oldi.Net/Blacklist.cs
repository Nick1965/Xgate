using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Oldi.Net;
using Oldi.Utility;
using System.Net.Http;
using System.Globalization;
using System.Net.Http.Headers;
using System.Threading;
using System.Xml.Linq;
using System.IO;

namespace Oldi.Net.Repost
{
    public class PinResult
    {
        public int TypeId;
        public string PIN;
    }


    #region Gorod
    public class GorodContext : DataContext
    {
        public GorodContext() : base(Oldi.Utility.Settings.GorodConnectionString)
        {
        }

        /// <summary>
        /// Получение PIN-кода
        /// </summary>
        /// <param name="card_number"></param>
        /// <param name="pin_type_id"></param>
        /// <returns></returns>
        [Function(Name = "spc.dbo.grd_card_PIN")]
        public ISingleResult<PinResult> GetCardPIN([Parameter(DbType = "int")] int card_number, [Parameter(DbType = "int")] int pin_type_id)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), card_number, pin_type_id);
            return ((ISingleResult<PinResult>)(result.ReturnValue));
        }

        /// <summary>
        /// Установка состояния платежа
        /// </summary>
        /// <param name="Tid"></param>
        /// <param name="State"></param>
        /// <param name="ResultCode"></param>
        /// <param name="ResultText"></param>
        /// <returns></returns>
        [Function(Name = "Sap2.dbo.Set_State_Payment")]
        [return: Parameter(DbType = "INT")]
        public int SetStatePayment(
            [Parameter(Name = "payment_tid", DbType = "INT")] int Tid,
            [Parameter(Name = "state", DbType = "INT")] int State,
            [Parameter(Name = "result_code", DbType = "INT")] int ResultCode,
            [Parameter(Name = "result_text", DbType = "VARCHAR(255)")] string ResultText)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), Tid, State, ResultCode, ResultText);
            return ((int)(result.ReturnValue));
        }

    }
    #endregion Gorod

    class Payment
    {
        public DateTime DatePay;
        public int Tid;
        public int Template_tid;
        public int Agent_oid;
        public int Point_oid;
        public string ClientAccount;
        public int Card_number;
        public decimal Amount;
        public decimal Summary_amount;
        public int User_id;
    }

    public class User
    {
        public string Login;
        public string Password;
    }

    public class Result
    {
        public int? State;
        public int? CheckId;
        public string SubInnerTid;
    }

    public class Blacklist
    {
        long Tid = 0;
        string Account = "";
        User user = null;
        string Pin = "";

        int? State = 0;
        decimal Amount = 0m;
        decimal AmountAll = 0m;

        // Номер карты для перепроведения
        string RepostCard = "";
        // Имя лог-файла
        string Logfile = "";
        // реестр перепроводок
        string Register = "";
        // Шаблон платёжного сервиса
        string PaymentTemplate = "";

        // SimpleEnpoint
        string Host = "";

        int? Status = 0;
        int? ErrCode = 0;
        string ErrDesc = "";

        Result result = null;

        Payment payment = null;
        GorodContext db = null;

        /// <summary>
        /// Номер чека
        /// </summary>
        string Outtid
        {
            get
            {
                ulong x = (ulong)DateTime.Now.Ticks;
                x = x >> 12;
                string t = x.ToString();
                return t.Length > 10 ? t.Substring(t.Length - 10) : t;
            }
        }

        public Blacklist(long Tid, string Account, decimal Amount, decimal AmountAll)
        {
            this.Tid = Tid;
            this.Account = Account;
            this.Amount = Amount;
            this.AmountAll = AmountAll;
            State = 0;
        }

        /// <summary>
        /// Чтение платежа
        /// </summary>
        public void Run()
        {
            try
            {
                Logfile = Config.AppSettings["Root"] + Config.AppSettings["LogPath"] + Config.AppSettings["BlacklistLog"];
                ReadConfig();

                payment = new Payment();
                user = new User();
                result = new Result();

                Wait12(); // Ожидаем установки статтуса 12
                ReadPayment();
            }
            catch(Exception ex)
            {
                Log(ex.ToString());
            }
        }


        /// <summary>
        /// Ждём статус 12
        /// </summary>
        void Wait12()
        {

            Log($"{Tid} [BLACK] {Account} - Ожидание отмены платежа");

            // Пратёж из BlackList ещё не установлен в Городе в статус 12.
            // Будем ждать!
            int time = 0;
            while (true)
            {
                // Ожидаем 10 сек.
                Thread.Sleep(1000);

                // Проверяем состояние платежа
                GetGorodState();
                Log($"{Tid} [BLACK] {Account} S={AmountAll.AsCF()} New state={State}");

                if (State == 12)
                    break;


                time += 1;
                if (time >= 600)
                {
                    Log($"{Tid} [BLACK] {Account} S={AmountAll.AsCF()} Timeout.");
                    throw new ApplicationException($"{Tid} [BLACK] {Account} S={AmountAll.AsCF()} Timeout.");
                }

            }

        }

        /// <summary>
        /// Читает платёж из БД Город
        /// </summary>
        void ReadPayment()
        {

            using (db = new GorodContext())
            {
                IEnumerable<Payment> Payments = db.ExecuteQuery<Payment>(
                    @"select p.DatePay, p.tid, p.template_tid, p.agent_oid, p.point_oid, d.ClientAccount, p.Card_number, p.Amount, p.Summary_Amount, p.User_id
				            FROM [Gorod].[dbo].[payment] p (NOLOCK)
				            inner join [Gorod].[dbo].[PD4] d (NOLOCK) on p.tid = d.tid 
				            where p.tid = {0}", Tid
                    );

                Payment Item = new Payment();
                Item = Payments.First();

                payment.DatePay = Item.DatePay;
                payment.Tid = Item.Tid;
                payment.Template_tid = Item.Template_tid;
                payment.Agent_oid = Item.Agent_oid;
                payment.Point_oid = Item.Point_oid;
                payment.ClientAccount = Item.ClientAccount;
                payment.Card_number = Item.Card_number;
                payment.Amount = Item.Amount;
                payment.Summary_amount = Item.Summary_amount;
                payment.User_id = Item.User_id;

                // Чтение ПИН-кода
                Pin = db.GetCardPIN(payment.Card_number, 1).First<PinResult>().PIN;

                // Имя пользователя и пароль
                IEnumerable<User> Users = db.ExecuteQuery<User>(
                    @"SELECT [login],[password] FROM [Gorod].[dbo].[user] where user_id = {0}", payment.User_id
                    );

                User u = new User();
                u = Users.First();

                user.Login = u.Login;
                user.Password = u.Password;

                Log($"Tid={Tid} S={payment.Summary_amount.AsCF()} {payment.Amount.AsCF()} Account={payment.ClientAccount} AgentCard={payment.Card_number} PIN={Pin} " +
                    $"Point={payment.Point_oid} Agent={payment.Agent_oid} User={payment.User_id} Nick={user.Login} Password={user.Password}");

                DoRepost(Pin);
            }

        }

        /// <summary>
        /// Перепроведение платежа
        /// </summary>
        void DoRepost(string PIN)
        {

            string request = "";
            if (payment.Card_number.ToString().Substring(0, 1) == "9")
            {
                Host = Config.AppSettings["CardEndpoint"];
                request =
                $"Card_number={payment.Card_number}&PIN={PIN}&template_tid={PaymentTemplate}&ls={RepostCard}"
                + $"&SUMMARY_AMOUNT={payment.Summary_amount.AsCF()}&tid={Outtid}";
            }
            else
            {
                Host = Config.AppSettings["SimpleEndpoint"];
                request =
                    $"Agent_ID={payment.Agent_oid}&Point_ID={payment.Point_oid}&Nick={user.Login}&Password={user.Password}&template_tid={PaymentTemplate}&ls={RepostCard}"
                    + $"&SUMMARY_AMOUNT={payment.Summary_amount.AsCF()}&tid={Outtid}";
            }

            Log(Host + "?" + request);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("IDHTTPSESSIONID", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml") { CharSet = "utf-8" });
            foreach (var key in httpClient.DefaultRequestHeaders)
            {
                string s = "";
                foreach (var k in key.Value)
                    s += $"{k} ";
                Log($"{key.Key} = {s}");
            }

            string response = "";
            Task t = httpClient.GetStringAsync(Host + "?" + request).ContinueWith(
                getTask =>
                {
                    if (getTask.IsCanceled)
                    {
                        Log("Request was canceled");
                    }
                    else if (getTask.IsFaulted)
                    {
                        Log($"Request failed: {getTask.Exception}");
                    }
                    else
                    {
                        response = getTask.Result;
                        // Log($"httpClient received: \r\n{getTask.Result}");
                    }
                });
            t.Wait();

            if (string.IsNullOrEmpty(response))
            {
                return; // Ошибка платёжного сервиса
            }

            Log($"----------------------------------------------------\r\n{response}");


            // Проверка статус и errCode
            Status = XPath.GetInt(response, "/request/result/Status");
            ErrCode = XPath.GetInt(response, "/request/result/errCode");
            Log($"Status={Status} errCode={ErrCode}");
            ErrDesc = XPath.GetString(response, "/request/result/errDesc");
            if (ErrCode != 0)
                Log($"errDesc={ErrDesc}");
            if (Status == 0)
            {
                result.State = XPath.GetInt(response, "/request/result/state");
                result.CheckId = XPath.GetInt(response, "/request/result/out_tid");
                result.SubInnerTid = XPath.GetString(response, "/request/result/tid");
                Log($"Result/state={result.State} checkid={result.CheckId} subinnertid={result.SubInnerTid}");
            }

            // Если создан новый платёж (chrckid > 0) установить статус 10 для исходного платежа
            if (result.CheckId > 0)
            {
                db.SetStatePayment( payment.Tid, 10, 0, $"[BLACK] Платёж перепроведён. Новый платёж {result.CheckId}" );
                // Вписать новый result_text в новый tid
                string ResultText = $"[BLACK] Платёж перепроведён: Tid={payment.Tid} Acc={payment.ClientAccount} Pnt={payment.Point_oid} Agn={payment.Agent_oid} "+ 
                    $"SA={payment.Summary_amount.AsCF()} S={payment.Amount.AsCF()} Tpl={payment.Template_tid}";
                db.ExecuteCommand(
                    @"update [gorod].[dbo].[payment_history] set result_text = {1} 
					    where tid = {0} and old_state is null and new_state = 0 and try_state = 0", result.CheckId, ResultText
                        );

                LogRegister();
            }


        }

        /// <summary>
        /// Получает статус платежа в городе
        /// </summary>
        void GetGorodState()
        {
            using (GorodContext Gorod = new Repost.GorodContext())
            {
                IEnumerable<int?> States = Gorod.ExecuteQuery<int?>("select state from [gorod].[dbo].[payment] where tid = {0}", Tid);
                State = States.First<int?>();
            }
        }
        
        /// <summary>
        /// Запись в реестр
        /// </summary>
        void LogRegister()
        {
            Utility.Log(Oldi.Utility.Settings.LogPath + Register, "DATE={7} Tid={0} SUM={1,12:f2} AMN={2,12:f2} ACC={3,20} CRD={4,7} PNT={5,4} AGN={6,4}",
                payment.Tid, payment.Summary_amount, payment.Amount, payment.ClientAccount, payment.Card_number,
                payment.Point_oid, payment.Agent_oid, payment.DatePay.AsCF().Replace('T', ' '));
        }

        /// <summary>
        /// Чтение параметров из файла конфигурации
        /// </summary>
        void ReadConfig()
        {
            XDocument doc = null;

            // Открывает файл спсиков с разрешением чтения и записи несколькими процессами
            try
            {
                using (FileStream fs = new FileStream(Settings.Root + "oldigw.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    doc = XDocument.Load(fs);

                    foreach (XElement el in doc.Root.Elements())
                    {
                        if (el.Name.LocalName == "Blackrepost")
                        {
                            foreach (XAttribute attr in el.Attributes())
                            {
                                if (attr.Name.LocalName == "Card")
                                    RepostCard = (string)attr.Value;
                                else if (attr.Name.LocalName == "Template")
                                    PaymentTemplate = (string)attr.Value;
                                else if (attr.Name.LocalName == "Register")
                                    Register = (string)attr.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"{Tid} исключение: {ex}");
            }

        }

        void Log(string fmt, params object[] prms)
        {
            if (string.IsNullOrEmpty(Logfile))
                Console.WriteLine(fmt, prms);
            else
                Utility.Log(Logfile, fmt, prms);
        }


    }
}
