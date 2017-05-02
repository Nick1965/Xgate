using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.Net;
using System.IO;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using System.Data;
using Oldi.Utility;
using System.Collections.Specialized;

namespace Oldi.Net
{
	public class TaskState
	{
		public ManualResetEvent CancelEvent;
		public TaskState(ManualResetEvent CancelEvent)
		{
			this.CancelEvent = CancelEvent;
		}
	}
	
	

	public partial class GWRequest: IDisposable
    {
        #region Properties

        protected string provider;
        /// <summary>
        /// Шлюз / ПУ
        /// </summary>
        public string Provider { get { return provider; } }

        /// <summary>
        /// Номер транзакции
        /// </summary>
        public long Tid { get { return tid; } }
        protected long tid;

        /// <summary>
        /// Номер телефона
        /// </summary>
        public string Phone { get { return phone; } }
        protected string phone = "";

		// Наименование реквизита № телефона
		public string PhoneParam { get { return phoneParam; }}
		protected string phoneParam = "";

        /// <summary>
        /// Номер лицевого счета
        /// </summary>
        public string Account { get { return account; } }
        protected string account = "";

		// Наименование реквизита № договора (ЛСЧЕТ)
		public string AccountParam { get { return accountParam; } }
		protected string accountParam = "";
		
		/// <summary>
		/// Номер филиала Ростелеком Сибирь
		/// </summary>
		public string Filial { get { return filial; } }
		protected string filial = "";

		// Наименование реквизита филиал
		public string FilialParam { get { return filialParam; } }
		protected string filialParam = "";

		/// <summary>
		/// Номер банковской карты
		/// </summary>
		public string Card { get { return card; } }
		protected string card = "";

		/// <summary>
        /// Номер сессии
        /// </summary>
        public string Session { get { return session; } }
        protected string session = "";

        /// <summary>
        /// Сумма удержанная с клиента
        /// </summary>
        public decimal AmountAll { get { return amountAll; } }
        protected decimal amountAll = decimal.MinusOne;

        /// <summary>
        /// Сумма для ПУ
        /// </summary>
        public decimal Amount { get { return amount; } }
		protected decimal amount = decimal.MinusOne;

        /// <summary>
        /// Номер сервиса
        /// Для МТС: 7 - оплата с комиссией; 8 - оплата без комиссии
        /// </summary>
        public string Service { get { return service; } }
        protected string service = "";

        /// <summary>
        /// Номер шлюза. Опциональный параметр
        /// </summary>
        public string Gateway { get { return gateway; } }
        protected string gateway = "";

        /// <summary>
        /// Номер терминала
        /// </summary>
        public int Terminal { get { return terminal; } }
        protected int terminal = int.MinValue;

		/// <summary>
		/// Номер агента
		/// </summary>
		public int AgentId {get{return agentId;}}
		protected int agentId = int.MinValue;

		/// <summary>
        /// Тип терминала (таблица terminals)
        /// </summary>
        public int TerminalType { get { return terminalType; } }
        protected int terminalType = int.MinValue;

		/// <summary>
		/// Настоящий номер терминала
		/// </summary>
		protected int realTerminalId = int.MinValue;
		public int RealTerminalId { get { return realTerminalId; } }
		
		/// <summary>
        /// Номер транзакции терминала (чек)
        /// </summary>
        public string Transaction { get { return transaction; } }
        public string transaction = "";

        /// <summary>
        /// Время ПЦ
        /// </summary>
        public DateTime Pcdate { get { return pcdate; } }
        protected DateTime pcdate = DateTime.MinValue;

		/// <summary>
		/// Дата/время на ТПП
		/// </summary>
		public DateTime? TerminalDate { get { return terminalDate; } }
		protected DateTime? terminalDate = null;

		/// <summary>
		/// Смещение времени
		/// </summary>
		public int Tz { get { return tz; } }
		protected int tz = Settings.Tz;
		
		/// <summary>
        /// Присоединенный Xml-файл
        /// </summary>
        public String XmlFile { get{ return xmlFile ;}}
        protected string xmlFile = "";

        /// <summary>
        /// Тип запроса
        /// </summary>
        public string RequestType { get { return requestType; } }
        protected string requestType = "";

		/// <summary>
		/// Код и описание ошибки
		/// </summary>
		public int ErrCode { get { return errCode; } set { errCode = value; } }
        public int errCode = int.MinValue;
        public string ErrDesc { get { return errDesc; } set { errDesc = value; } }
        public string errDesc = "";

		/// <summary>
		/// Дополнительное сообщение об ошибке
		/// </summary>
		public string ErrMsg { get { return errMsg; } }
		protected string errMsg = "";
		/// <summary>
		/// Код последней ошибки
		/// </summary>
		protected int lastcode = 0;
		/// <summary>
		/// Описание последней ошибки
		/// </summary>
		protected string lastdesc = "";
		/// <summary>
		/// Код последней ошибки
		/// </summary>
		public int Lastcode { get { return lastcode; } }
		/// <summary>
		/// Описание последней ошибки
		/// </summary>
		public string Lastdesc { get { return lastdesc; } }
		/// <summary>
		/// Количество повторов запроса
		/// </summary>
		protected int times = 0;
		/// <summary>
		/// Количество повторов запроса
		/// </summary>
		public int Times { get { return times; } }

        /// <summary>
        /// Дополнительная информация, возвращаемая запросом
        /// Записывается в БД и отправляется в поле <adinfo> ответа
        /// </summary>
        public string AddInfo { get { return addinfo; } }
        protected string addinfo = "";

        /// <summary>
        /// Номер транзакции провайдера
        /// </summary>
        public string Outtid { get { return outtid; } }
        protected string outtid = "";

        /// <summary>
        /// Дата операции
        /// </summary>
        public DateTime Operdate{ get { return operdate; } }
        protected DateTime operdate = DateTime.MinValue;

        /// <summary>
        /// Номер кассового чека
        /// </summary>
        public string CheckNumber { get { return checkNumber; } }
        protected string checkNumber = "";

		// Код валюты (810)
		public string Cur { get { return cur; } }
		protected string cur = "810";

        /// <summary>
        /// Контрольный код
        /// </summary>
        public string ControlCode { get { return controlCode; } }
        protected string controlCode = "";

		/// <summary>
		/// Код акцепта в МТС / авторизации в Кибер
		/// </summary>
		public string AcceptCode { get { return acceptCode; } }
		protected string acceptCode = "";

		/// <summary>
        /// Дата акцептования платежа ПУ
        /// </summary>
        public DateTime Acceptdate { get { return acceptdate; } }
        protected DateTime acceptdate = DateTime.MinValue;

        /// <summary>
        /// Номер АС ВПС
        /// </summary>
        public string Asvps { get { return asvps; } }

        /// <summary>
        /// Код ВПС
        /// </summary>
        public string Vpscode { get { return vpscode; } }

        /// <summary>
        /// Номер договора с ЕСПП
        /// </summary>
        public string Contract { get { return contract; } }

        /// <summary>
        /// Код безопасности
        /// </summary>
        public string Security { get { return security; } }

        /// <summary>
        /// Название сертификата
        /// </summary>
        // public string CommonName { get { return commonName == "" ? vpscode + ";" + security + ";" + asvps : commonName; } }
        public string CommonName { get { return commonName; } }
        protected string commonName = "";

        /// <summary>
        /// Кодовая страница для обмена с провайдером
        /// </summary>
        public string CodePage { get; set; }

        /// <summary>
        /// Код домашнего оператора
        /// </summary>
        public string Opcode { get { return opcode; } }
        protected string opcode = "";

        /// <summary>
        /// Название домашнего оператора
        /// </summary>
        public string Opname { get { return opname; } }
        protected string opname = "";

        /// <summary>
        /// Овердрафт
        /// </summary>
        public decimal Limit { get { return limit; } }
        protected decimal limit = decimal.MinusOne;

        /// <summary>
        /// Дата лимита
        /// </summary>
        public DateTime LimitDate { get { return limitend; } }
        protected DateTime limitend = DateTime.MinValue;

        /// <summary>
        /// Инициалы абонента - FIO
        /// </summary>
        // public string Initials { get { return initials; } }
        // protected string initials = "";

		/// <summary>
        /// Состояние операции
        /// </summary>
		public byte State { get { return state; } set { state = value; } }
        protected byte state = 255;

        /// <summary>
        /// Указывает на блокировку записи другим процессом
        /// </summary>
        public byte Locked { get { return locked; } }
        protected byte locked = 255;

        /// <summary>
        /// Cyber - Номер счета
        /// </summary>
        public string Number { get { return number; } }
        protected string number = "";

        // Наименование организации
        public string Orgname { get { return orgname; } }
        protected string orgname = "";

        // Комментарий
        public string Comment { get { return comment; } }
        protected string comment = "";

        /// <summary>
        /// Номер документа 
        /// </summary>
        public string Docnum { get { return docnum; } }
        protected string docnum = "";

        /// <summary>
        /// Дата документа 
        /// </summary>
        public string Docdate { get { return docdate; } }
        protected string docdate = "";

        /// <summary>
        /// Назначение платежа
        /// </summary>
        public string Purpose { get { return purpose; } }
        protected string purpose = "";

        /// <summary>
        /// ФИО плательщика
        /// </summary>
        public string Fio { get { return fio; } }
        protected string fio = "";
        
        /// <summary>
        /// Адрес плательщика
        /// </summary>
        public string Address { get { return address; } }
        protected string address = "";

        /// <summary>
        /// Согласие с офертой
        /// </summary>
        public byte Agree { get { return agree; } }
        protected byte agree = 255;

        // ИНН
        public string Inn { get { return inn; } }
        protected string inn = "";

        // Контактный телефон
        public string Contact { get { return contact; } }
        protected string contact = "";
        
		/// <summary>
		/// Ответ Кибера: Ожидается сумма PRICE / Рекомендуемый платёж
		/// </summary>
		public decimal Price { get { return price; } }
		protected decimal price = decimal.MinusOne;

		/// <summary>
		/// Остаток на счёте
		/// </summary>
		public decimal? Balance { get { return balance; } }
		protected decimal? balance = null;

        /// <summary>
        /// Имя шаблона
        /// </summary>
        public string TemplateName { get { return templateName; } }
        protected string templateName = "";

        // АС ВПС
        protected string asvps = "";
        // Код ВПС
        protected string vpscode = "";
        // Номер договора
        protected string contract = "";
        // Код безопасности
        protected string security = "";
        // Тип mime
        protected string ContentType = "";
        // Код дилера
        protected string SD = "";
        // Код точки
        protected string AP = "";
        // Код оператора
        protected string OP = "";

        // RESULT - ответ сервера Киберплат
        public int Result { get { return result; } }
        protected int result = int.MinValue;

        public DateTime Dateresult { get { return dateresult; } }
        protected DateTime dateresult = DateTime.MinValue;

		/// <summary>
		/// Адрес хоста ПУ
		/// </summary>
		public string Host { get { return host; } }
		protected string host = "";
		
		/// <summary>
        /// Адрес хоста для запросов Check
        /// </summary>
        public string CheckHost { get { return checkHost; } }
        /// <summary>
        /// Адрес хоста для запросов Payment
        /// </summary>
        public string PayHost { get { return payHost; } }
        /// <summary>
        /// Адрес хоста для запросов Status
        /// </summary>
        public string StatusHost { get { return statusHost; } }

        // Адреса запросов
        protected string checkHost = "";
        protected string payHost = "";
        protected string statusHost = "";

        /// <summary>
        /// Ответ ПУ на запрос
        /// </summary>
        public string Answer { get { return stResponse; } }
        protected string stResponse = "";
        protected string stRequest = "";

		public int Pause { get { return pause; } set { pause = value; } }
		protected int pause = 0;

		public string Bik { get { return bik; } }
		protected string bik = "";

		public string Kpp { get { return kpp; } }
		protected string kpp = "";

		public string PayerInn { get { return payerInn; } }
		protected string payerInn = "";

		public string Ben { get { return ben; } }
		protected string ben = "";

		public int Tax { get { return tax; } }
		protected int tax = int.MinValue;

		public string KBK { get { return kbk; } }
		protected string kbk = "";

		public string OKATO { get { return okato; } }
		protected string okato = "";

		public int PayType { get { return payType; } }
		protected int payType = int.MinValue;

		/// <summary>
		/// Назначение платежа
		/// </summary>
		public string Reason { get { return reason; } }
		protected string reason = "";

		/// <summary>
		/// Номер платёжного инструмента
		/// </summary>
		public string Oid { get { return oid; } }
		protected string oid = "";


		/// <summary>
		/// Дополнительные атрибуты
		/// </summary>
		public AttributesCollection Attributes { get { return attributes; } }
		protected AttributesCollection attributes;

		protected string TechInfo = "";

		/// <summary>
		/// Код рассылки. 0 - ничего не отсылать.
		/// </summary>
		public int DeliveriId { get { return deliveryId; } }
		protected int deliveryId = 0;

		/// <summary>
		/// RT: даты начала и конца выборки
		/// </summary>
		protected DateTime? startDate = null;
		protected DateTime? endDate = null;

		public DateTime? StartDate { get { return startDate; } }
		public DateTime? EndDate { get { return endDate; } }

		protected int? statusType = null;
		public int? StatusType { get { return statusType; } }

        protected decimal amountLimit = 0M;
        /// <summary>
        /// Максимальный размер платежа
        /// </summary>
        public decimal AmountLimit { get { return amountLimit; } }

        protected int amountDelay = 0;
        /// <summary>
        /// Задержка платежа в часах
        /// </summary>
        public int AmountDelay { get { return amountDelay; } }

        protected string notify = "";
        public string Notify { get { return notify; } }

		#endregion


    }


}
