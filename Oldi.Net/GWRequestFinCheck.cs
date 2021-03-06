﻿using Oldi.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Oldi.Net
{
    public partial class GWRequest
    {

        /// <summary>
        /// Дневной лимит
        /// </summary>
        decimal DayLimit = 0M;
        /// <summary>
        /// Разовый максимальный платёж
        /// </summary>
        decimal OnePayment = 0M;
        
        /// <summary>
        /// Проверка дневного лимита пользователя сайта
        /// </summary>
        /// <returns></returns>
        public bool DayLimitExceeded(bool New)
        {
            int account = 0;
            decimal pays = 0M;

            try
            {

                // Получить номер лицевого счёта плательщика
                if ((account = GetPayerAccount()) != 0)
                {

                    // RootLog($"{Tid} [DLIM] Проверка дневного лимита.");

                    // Чтение информации о переопределении для Account
                    RedefineForAccount(account);
                    RootLog($"{Tid} [DLIM] Проверка дневного лимита {account} лимит={DayLimit.AsCF()} разовый={OnePayment.AsCF()}");

                    pays = PaysInTime(account);
                    if (pays + Amount > DayLimit)
                    {
                        RootLog($"{Tid} [DLIM] Превышен дневной лимит для {account} Сумма= {(pays + Amount).AsCF()} Лимит= {DayLimit.AsCF()}");

                        state = 0;
                        errCode = 12;
                        errDesc = $"[Фин.кон-ль] превышен дневной лимт. Отложен.";
                        UpdateState(Tid, state: State, errCode: ErrCode, errDesc: ErrDesc, locked: 0);

                        // Ищем переопределение для агента.
                        // GetAgentFromList();
                        // RootLog($"{Tid} [DLIM] Для агента AgentID=\"{AgentId}\" заданы параметры: Limit={AmountLimit.AsCurrency()} Delay={AmountDelay} часов Notify={Notify}");

                        // Сообщать о превышении не будем СМС !!!
                        // Отправить СМС-уведомление, усли список уведомлений не пуст
                        // if (New && State == 0)
                        //    SendNotification(Notify, $"Payer={account} S={AmountAll.AsCF()} превышен дневной лимит.");

                        // RootLog($"{Tid} [DLIM - stop] {Service}/{Gateway} Card={account} Pays={(pays + Amount).AsCF()} Lim={DayLimit.AsCF()} превышен дневной лимт - State={State}");

                        return true;

                    }
                    if (Amount > OnePayment)
                    {
                        RootLog($"{Tid} [DLIM - stop] Превышен размер разового платежа для {account} Платёж= {Amount.AsCF()} Лимит= {OnePayment.AsCF()}");

                        state = 0;
                        errCode = 12;
                        errDesc = $"[Фин.кон-ль] превышен разовый платёж. Отложен.";
                        UpdateState(Tid, state: State, errCode: ErrCode, errDesc: ErrDesc, locked: 0);
                        return true;
                    }
                    // Добавить платёж в Pays
                    if (New && State == 0)
                        AddPay(account);

                    RootLog($"{Tid} [DLIM] Account={account} Конец проверки.");
                }

            }
            catch(Exception ex)
            {
                RootLog($"{Tid} [DLIM] Ph={Phone} Ac={Account} Number={Number}\r\n{ex}");
            }

            return false;

        }

        /// <summary>
        /// Чтение переопределений для Account
        /// </summary>
        /// <returns></returns>
        void RedefineForAccount(int account)
        {

            XElement root = XElement.Load(Settings.Root + @"Lists\Site.xml");
            // XElement root = XElement.Parse(Site);
            IEnumerable<XAttribute> RootAttributes = root.Elements().Attributes();

            // Установка общих значений
            foreach (XAttribute at in RootAttributes)
                switch (at.Name.LocalName)
                {
                    case "DayLimit":
                        DayLimit = at.Value.ToDecimal();
                        break;
                    case "OnePayment":
                        OnePayment = at.Value.ToDecimal();
                        break;
                }

            // Чтение индивидуальных значений для Account
            IEnumerable<XElement> Blocks =
                from el in root.Elements("Blocks").Elements("Block")
                    // where el.Attributes().ToString() == account.ToString()
                select el;

            decimal _dayLimit = 0M;
            decimal _onePayment = 0M;

            foreach (XElement el in Blocks)
            {
                string acnt = "";
                // Переопределение общих значений
                foreach (XAttribute at in el.Attributes())
                    switch (at.Name.LocalName)
                    {
                        case "DayLimit":
                            _dayLimit = at.Value.ToDecimal();
                            break;
                        case "OnePayment":
                            _onePayment = at.Value.ToDecimal();
                            break;
                        case "Account":
                            acnt = at.Value;
                            break;
                    }

                if (acnt == account.ToString())
                {
                    if (_dayLimit > 0M)
                        DayLimit = _dayLimit;
                    if (_onePayment > 0M)
                        OnePayment = _onePayment;
                    break; // Завершим дальнейший перебор
                    // Console.WriteLine($"\t\tName={el.Name.LocalName} account={acnt}");
                }
            }
        }

        /// <summary>
        /// Добыча номера карты плательщика
        /// </summary>
        /// <returns></returns>
        int GetPayerAccount()
        {

            try
            {
                // RootLog($"{Tid} [DLIM] {"SELECT [Card_number] FROM Gorod.dbo].payment where tid ="} {Tid}");

                int? Card = null;

                using (Gorod db = new Gorod(Settings.GorodConnectionString))
                {
                    IEnumerable<int?> Payments = db.ExecuteQuery<int?>("select [Card_number] from [gorod].[dbo].payment where tid = {0}", Tid);
                    Card = Payments.First<int?>();
                }

                return Card >= 900000 && Card <= 999999 ? Card.Value : 0;

/*
                string Account = GetGorodSubLinq();

                RootLog("{0} [DLIM] Найден sub_inner_tid '{1}'", Tid, Account);

                if (!string.IsNullOrEmpty(Account) && Account.Length >= 6 && Account.Substring(0, 6).ToLower() == "card-9")
                {
                    Account = Account.Substring(5, Account.IndexOf('-', 5) - 4);
                    RootLog("{0} [DLIM] Найден account '{1}'", Tid, Account);
                    return int.Parse(Account.Replace("-", ""));
                }
                else
                    return 0;
*/
            }
            catch (Exception ex)
            {
                RootLog($"{Tid} [DLIM - stop] {ex}");
                return 0;
            }

        }

        /// <summary>
        /// Подсчитывает сумму платежей за сутки для счёта
        /// </summary>
        /// <param name="account">Номер счёта</param>
        /// <returns>Сумма всех платежей</returns>
        decimal PaysInTime(int account)
        {
            decimal? balance = 0;
            DateTime start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            DateTime finish = start.AddDays(1);
            using (OldiContext db = new OldiContext(Settings.ConnectionString))
            {
                IEnumerable<decimal?> pays = db.ExecuteQuery<decimal?>("select sum(amount) from oldigw.oldigw.pays where datepay between {0} and {1} and account = {2}", start, finish, account);
                // if (pays != null && pays.Count() > 0)
                balance = pays.First();
            }
            RootLog($"{Tid} [DLIM] Account {account} за день выплачено {balance.AsCF()}");
            return balance == null ? 0M : balance.Value;
        }

        /// <summary>
        /// Добавляет платёж в таблицу Pays
        /// </summary>
        /// <param name="account"></param>
        void AddPay(int account)
        {
            using (OldiContext db = new OldiContext(Settings.ConnectionString))
            {
                db.ExecuteCommand("insert into oldigw.oldigw.pays (tid, datepay, account, amount, balance) values({0}, {1}, {2}, {3}, {4})", Tid, DateTime.Now, account, Amount, 0M);
            }
            RootLog($"{Tid} [DLIM] Account {account} добавлен платёж {Amount.AsCF()}");
        }

        /// <summary>
        /// Финансовы контроль
        /// </summary>
        protected virtual bool FinancialCheck(bool newPay)
        {

            string x = null;

            // Если тип терминала не определён: считаем терминал и включаем финюконтроль
            // if (State == 0 && (tt == 1 || tt == 3)) // Если только новый платёж
            if (State == 0)
            {

                if (!string.IsNullOrEmpty(Phone))
                    x = Phone;
                else if (!string.IsNullOrEmpty(Account) && string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если задан Number, то используется он
                    x = Account;
                else if (!string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если только не задан Card
                    x = Number;
                else if (!string.IsNullOrEmpty(Card))
                    x = Card;
                else
                {
                    RootLog($"{Tid} [FCHK] {Provider} {Service}/{Gateway} Не задан номер счёта");
                    return false;
                }

                // Проверим в чёрном списке (не важно, хоть с сайта)
                if (FindInBlackList(x))
                    return true;

                // Если номер телефона в списке исключаемых завершить финансовый контроль
                if (FindInLists(Settings.Lists, x, 1) == 1) // Найден в белом списке
                {
                    RootLog($"{Tid} [FCHK] Provider={Provider} {Service}/{Gateway} Num=\"{x}\" найден в белом списке, завершение проверки");
                    return false;
                }

                // Получаем тип терминала из БД Город
                // 1 - Терминал
                // 2 - Рабочее место
                // 3 - Сайт
                List<CheckedProvider> ProviderList = LoadProviderList();

                int tt = Terminal == 281 ? 3 : TerminalType;

                string trm = Terminal != int.MinValue ? Terminal.ToString() : "NOREG";

                // Ищем переопределение для агента.
                GetAgentFromList();
                RootLog($"{Tid} [FCHK] Для агента AgentID=\"{AgentId}\" заданы параметры: Limit={AmountLimit.AsCurrency()} Delay={AmountDelay} часов Notify={Notify}");

                foreach (var item in ProviderList)
                {
                    string _provider = string.IsNullOrEmpty(Provider) ? "": Provider.ToLower();
                    string _service = string.IsNullOrEmpty(Service) ? "" : Service.ToLower();
                    string _gateway = string.IsNullOrEmpty(Gateway) ? "" : Gateway.ToLower();

                    if (_provider == item.Name.ToLower() && _service == item.Service.ToLower() && _gateway == item.Gateway.ToLower() && item.TerminalType == tt)
                    {
                        // Переопределяем правило лимитов для провайдера
                        amountLimit = item.Limit > amountLimit? item.Limit: AmountLimit;
                        // check = true;
                        RootLog($"{Tid} [FCHK] Переопределение для {Provider} {Service}/{Gateway} Type={tt}: AmountLimit={AmountLimit.AsCF()}");
                        break;
                    }
                }

                // Строго больше!
                if (AmountAll > AmountLimit && Pcdate.AddHours(AmountDelay) >= DateTime.Now) // Проверка отправки СМС
                {

                    // RootLog($"{Tid} [FCHK] {Provider} {Service}/{Gateway} Trm={Terminal} Limit={AmountLimit.AsCF()} Amount{AmountAll.AsCF()}");
                    // RootLog($"{Tid} [FCHK] {Provider} {Service}/{Gateway} Pcdate+delay={Pcdate.AddHours(AmountDelay).AsCF()} Limit={AmountLimit.AsCF()} Amount{AmountAll.AsCF()}");

                    state = 0;
                    errCode = 11;
                    errDesc = $"[Фин.кон-ль] Отложен до {Pcdate.AddHours(AmountDelay)}";
                    UpdateState(Tid, state: State, errCode: ErrCode, errDesc: ErrDesc, locked: 0);
                    RootLog($"{Tid} [FCHK] {Provider} {Service}/{Gateway} Trm={Terminal} Acc={x} A={Amount.AsCF()} S={AmountAll.AsCF()} Limit={AmountLimit.AsCF()} - Отложен до {Pcdate.AddHours(AmountDelay).AsCF()}");

                    // Отправить СМС-уведомление, усли список уведомлений не пуст
                    // Отправлять СМС только для терминалов
                    if (newPay && !string.IsNullOrEmpty(Notify) && TerminalType == 1)
                        SendNotification(Notify, $"Num={x} S={AmountAll.AsCF()} Trm={Terminal} блок до {Pcdate.AddHours(AmountDelay).AsCF()}");

                    // Не найден  в белом списке - на контроль!
                    return true;
                }
                
                // Если меньше допустимого лимита, не ставить на контроль
                RootLog($"{Tid} [FCHK - stop] {Service}/{Gateway} Num=\"{x}\" сумма платежа {AmountAll.AsCurrency()} меньше общего лимита {AmountLimit.AsCurrency()}, завершение проверки");

            }

            return false;
        }

        class CheckedProvider
        {

            public string Name;         // Имя провайдера
            public string Service;      // услуга
            public string Gateway;      // шлюз
            public decimal Limit;       // предельное значение платежа
            public int TerminalType;    // тип терминала 1, 2 или 3
            public CheckedProvider()
            {
                Name = "";
                Service = "";
                Gateway = "";
                Limit = decimal.MinusOne;
                TerminalType = 1;
            }
        }

        /// <summary>
        /// Загрузка списка провайдеров
        /// </summary>
        List<CheckedProvider> LoadProviderList()
        {

            XDocument doc = XDocument.Load(Settings.Root + "lists\\fincheck.xml");
            // string provider = "";
            amountLimit = 0m;
            amountDelay = 0;

            List<CheckedProvider> CheckedProviders = new List<CheckedProvider>();

            if (doc != null && doc.Element("FinancialCheck").HasElements)
            {
                foreach (XElement el in doc.Root.Elements())
                {
                    string name = el.Name.LocalName;
                    string value = el.Value;
                    Log("FinancialCheck: Section = {0} value = {1}", name, value);

                    switch (el.Name.LocalName)
                    {
                        case "AmountLimit":
                            amountLimit = XConvert.ToDecimal(el.Value.ToString());
                            break;
                        case "AmountDelay":
                            amountDelay = int.Parse(el.Value.ToString());
                            break;
                        case "Providers":
                            IEnumerable<XElement> elements =
                                    from e in el.Elements("Provider")
                                    select e;
                            foreach (XElement e in elements)
                            {
                                // Console.WriteLine(e.Name.LocalName);
                                if (e.Name.LocalName == "Provider")
                                {
                                    // string Name = "";
                                    // string Service = "";
                                    // string Gateway = "";
                                    // decimal Limit = decimal.MinusOne;
                                    // int Delay = 16;
                                    CheckedProvider providerItem = new CheckedProvider();

                                    foreach (var item in e.Attributes())
                                    {
                                        if (item.Name.LocalName == "Name")
                                            providerItem.Name = item.Value;
                                        if (item.Name.LocalName == "Service")
                                            providerItem.Service = item.Value;
                                        if (item.Name.LocalName == "Gateway")
                                            providerItem.Gateway = item.Value;
                                        if (item.Name.LocalName == "Limit")
                                            providerItem.Limit = item.Value.ToDecimal();
                                        if (item.Name.LocalName == "TerminalType")
                                            providerItem.TerminalType = item.Value.ToInt();
                                    }
                                    // Settings.checkedProviders.Add(new ProviderItem(Name, Service, Gateway, Limit));
                                    CheckedProviders.Add(providerItem);
                                    // Log("Заuружен: Name={0} Service={1} Gateway={2} Limit={3} TerminalType={4}");
                                }
                            }

                            break;
                    }
                }
            }
            else
            {
                RootLog($"{Tid} Нет секции FinancialCheck");
            }

            return CheckedProviders;

        }

        bool FindInBlackList(string x)
        {
            RootLog($"{Tid} [FCHK - strt] {Service}/{Gateway} Num=\"{x}\" поиск в чёрном списке");

            // if (TerminalType == 2)
            //	return false;

            foreach (var item in Settings.CheckedProviders)
            {

                // Проверка любой суммы в чёрном списке
                if (item.Name.ToLower() == Provider.ToLower()
                        && item.Service.ToLower() == Service.ToLower()
                        && item.Gateway.ToLower() == Gateway.ToLower()
                        // && AmountAll >= item.Limit -- не проверяем лимит
                        && Pcdate.AddHours(Settings.AmountDelay) >= DateTime.Now)
                {

                    // Если номер телефона в списке исключаемых завершить финансовый контроль
                    if (FindInLists(Settings.Lists, x, 2) == 2) // Найден в чёрном списке
                    {
                        state = 12;
                        errCode = 6;
                        errDesc = string.Format("[BLACK] Отменён системой");
                        UpdateState(Tid, state: State, errCode: ErrCode, errDesc: ErrDesc, locked: 0);
                        RootLog($"{Tid} [BLACK] Num={x} A={Amount.AsCF()} S={AmountAll.AsCF()} - Найден в чёрном списке. Отменён.");

                        // Перепроведение платежа
                        Oldi.Net.Repost.Blacklist blacklist = new Repost.Blacklist(Tid, x, Amount, AmountAll);
                        TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.LongRunning);
                        RootLog($"{Tid} [BLACK] Запускается процесс допроведения");
                        Task blr = factory.StartNew(
                            // Запуск препроведения в другом потоке. Этот  поток завершается.
                            () => blacklist.Run()
                        );

                        RootLog($"{Tid} [BLACK] Передаётся статус в Город");
                        return true;
                    }

                }

            }

            // RootLog("{0} [FCHK - strt] {1}/{2} Num=\"{3}\" в чёрном списке не найден", Tid, Service, Gateway, x);
            return false;
        }

        /// <summary>
        /// Поиск переопределений для агента
        /// </summary>
        /// <param name="Limit"></param>
        /// <param name="Notify"></param>
        /// <returns></returns>
        void GetAgentFromList()
        {
            XDocument doc = null;

            // Открывает файл спсиков с разрешением чтения и записи несколькими процессами
            try
            {
                using (FileStream fs = new FileStream(Settings.Lists, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    doc = XDocument.Load(fs);

                    foreach (XElement el in doc.Root.Elements())
                    {
                        if (el.Name.LocalName == "Agents")
                        {
                            foreach (XElement s in el.Elements())
                                if (s.Name.LocalName == "Agent")
                                {
                                    decimal _amountLimit = decimal.MinusOne;
                                    int _amountDelay = int.MinValue;
                                    string _notify = "";
                                    string _agent = "";

                                    foreach (var item in s.Attributes())
                                    {
                                        if (item.Name.LocalName == "Limit") _amountLimit = item.Value.ToDecimal();
                                        if (item.Name.LocalName == "AmountDelay") _amountDelay = item.Value.ToInt();
                                        if (item.Name.LocalName == "Notify") _notify = item.Value;
                                        if (item.Name.LocalName == "ID") _agent = item.Value;
                                    }

                                    if (_agent == "*" || int.Parse(_agent) == AgentId) // Агент найден
                                    {
                                        // IsDefault = true;
                                        if (_amountLimit > 0M)
                                            amountLimit = _amountLimit;
                                        if (_amountDelay > 0)
                                            amountDelay = _amountDelay;
                                        if (_notify != "")
                                            notify = _notify;
                                        break;
                                    }
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RootLog($"{Tid} {Provider} {Service}/{Gateway} [FCHK] исключение:\r\n{ex}");
            }

        }

        /// <summary>
        /// Открывает чёрно-белый список и ищет в нём номер
        /// </summary>
        /// <param name="Listpath">Путь к списку</param>
        /// <param name="Number">Номер телефона/счёта</param>
        /// <returns>
        /// 0 - не найден; 
        /// 1 - найден в белом списке;
        /// 2 - найден в чёрном списке
        /// </returns>
        int FindInLists(string Listpath, string Number, int ListType)
        {

            XDocument doc = null;

            // Открывает файл спсиков с разрешением чтения и записи несколькими процессами
            try
            {
                using (FileStream fs = new FileStream(Listpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    doc = XDocument.Load(fs);

                    // Выносится в отдельный файл
                    // Settings.excludes.Clear();


                    foreach (XElement el in doc.Root.Elements())
                    {
                        string name = el.Name.LocalName;
                        string value = el.Value;
                        // Console.WriteLine("Section: {0}", name);

                        switch (el.Name.LocalName)
                        {
                            // Белый список
                            case "White":
                                if (ListType == 1)
                                {
                                    if (FindInList(Number, el))
                                        return 1;
                                }
                                break;
                            // Чёрный список
                            case "Black":
                                if (ListType == 2)
                                    if (FindInList(Number, el))
                                        return 2;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RootLog("[FCHK] White/Black lists: {0}", ex.Message);
            }

            return 0; // Номер не найден
        }


        /// <summary>
        /// Поиск номера в чёрном или белом списке
        /// </summary>
        /// <param name="Number">Номер, string</param>
        /// <param name="el">Список, XElement</param>
        /// <returns>true - найден</returns>
        bool FindInList(string Number, XElement el)
        {
            foreach (XElement s in el.Elements())
            {
                switch (s.Name.LocalName)
                {
                    case "Number":
                        string Prefix = "";
                        foreach (var item in s.Attributes())
                        {
                            if (item.Name.LocalName == "Prefix")
                            {
                                Prefix = item.Value.ToString();
                                // Log("{0} [FCHK - found]  {1}", Tid, Prefix);
                                // if (Prefix != "" && Number.Length >= Prefix.Length && Number.Substring(0, Prefix.Length) == Prefix)
                                if (Prefix != "" && Number == Prefix)
                                {
                                    // Номер найден в списке
                                    return true;
                                }
                            }
                        }
                        break;
                }
            }
            return false;
        }



    }
}
