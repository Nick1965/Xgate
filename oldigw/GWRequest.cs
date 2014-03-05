using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OldiGW
{
    class GWRequest
    {
        /// <summary>
        /// Шлюз / ПУ
        /// </summary>
        public string Provider { get { return provider; } }
        string provider;

        /// <summary>
        /// Номер транзакции
        /// </summary>
        public int Tid { get { return tid; } }
        int tid;

        /// <summary>
        /// Номер телефона
        /// </summary>
        public string Phone { get { return Phone; } }
        string phone;

        /// <summary>
        /// Номер лицевого счета
        /// </summary>
        public string Account { get { return account; } }
        string account;

        /// <summary>
        /// Сумма удержанная с клиента
        /// </summary>
        public decimal Amount { get { return Amount; } }
        decimal amount;

        /// <summary>
        /// Сумма для ПУ
        /// </summary>
        public decimal ToAmount { get { return ToAmount; } }
        decimal toAmount;

        /// <summary>
        /// Номер сервиса
        /// Для МТС: 7 - оплата с комиссией; 8 - оплата без комиссии
        /// </summary>
        public int Service { get { return Service; } }
        int service;

        /// <summary>
        /// Номер терминала
        /// </summary>
        public string Terminal { get { return terminal; } }
        string terminal;

        /// <summary>
        /// Номер транзакции терминала (чек)
        /// </summary>
        public string Transaction { get { return transaction; } }
        string transaction;

        /// <summary>
        /// Время ПЦ
        /// </summary>
        public DateTime Pcdate { get { return Pcdate; } }
        DateTime pcdate;

        /// <summary>
        /// Тип запроса
        /// </summary>
        public string RequestType { get { return requestType; } }
        string requestType;

        public int ErrCode { get; set; }
        public string ErrDesc { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public GWRequest()
        {
        }

        public string asvps;
        public string vpscode;
        public string contract;
        public string security;

        public int Parse(string stResponse)
        {
            try
            {
                XDocument doc = XDocument.Parse(stResponse);

                if (doc.Element("request").HasAttributes)
                {
                    requestType = (string)doc.Element("request").Attribute("type");
                }
                else
                {
                    ErrCode = Properties.Settings.Default.codeLongWait;
                    ErrDesc = string.Format(Properties.Resources.MsgURT, "payment/status/undo/redo");
                    return ErrCode;
                }

                #region Parse
                var body = doc.Element("request")
                              .Elements();

                foreach (XElement el in body)
                    switch (el.Name.LocalName)
                    {
                        case "provider":
                            provider = (string)el.Attribute("name");
                            if (provider.ToLower() == Properties.Settings.Default.providerMts)
                            {
                                asvps = (string)el.Attribute("as-vps");
                                vpscode = (string)el.Attribute("vps-code");
                                contract = (string)el.Attribute("contract");
                                security = (string)el.Attribute("security");
                            }
                            break;
                        case "tid":
                            if (!Int32.TryParse((string)el.Value, out tid))
                            {
                                ErrCode = Properties.Settings.Default.codeLongWait;
                                ErrDesc = string.Format(Properties.Resources.MsgWPV, el.Name.LocalName, (string)el.Value);
                                return ErrCode;
                            }
                            break;
                        case "phone":
                            phone = (string)el.Value;
                            break;
                        case "account":
                            account = (string)el.Value;
                            break;
                        case "amount":
                            if (!decimal.TryParse((string)el.Value, out amount))
                            {
                                ErrCode = Properties.Settings.Default.codeLongWait;
                                ErrDesc = string.Format(Properties.Resources.MsgWPV, el.Name.LocalName, (string)el.Value);
                                return ErrCode;
                            }
                            break;
                        case "to-amount":
                            if (!decimal.TryParse((string)el.Value, out toAmount))
                            {
                                ErrCode = Properties.Settings.Default.codeLongWait;
                                ErrDesc = string.Format(Properties.Resources.MsgWPV, el.Name.LocalName, (string)el.Value);
                                return ErrCode;
                            }
                            break;
                        case "service":
                            if (!Int32.TryParse((string)el.Value, out service))
                            {
                                ErrCode = Properties.Settings.Default.codeLongWait;
                                ErrDesc = string.Format(Properties.Resources.MsgWPV, el.Name.LocalName, (string)el.Value);
                                return ErrCode;
                            }
                            break;
                        case "terminal":
                            terminal = (string)el.Value;
                            break;
                        case "transaction":
                            transaction = (string)el.Value;
                            break;
                        case "pc-date":
                            if (!DateTime.TryParse((string)el.Value, out pcdate))
                            {
                                ErrCode = Properties.Settings.Default.codeLongWait;
                                ErrDesc = string.Format(Properties.Resources.MsgWPV, el.Name.LocalName, (string)el.Value);
                                return ErrCode;
                            }
                            break;
                        default:
                            ErrCode = Properties.Settings.Default.codeLongWait;
                            ErrDesc = string.Format(Properties.Resources.MsgUP, el.Name.LocalName);
                            return ErrCode;
                    }

                #endregion Parse

                ErrCode = Properties.Settings.Default.codeSUCS;
                ErrDesc = Properties.Resources.MsgSUCS;
                // Запрос успешно разобран
                return 0;
            }
            catch (Exception ex)
            {
                ErrCode = Properties.Settings.Default.codeLongWait;
                ErrDesc = ex.Message;
                Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
            }

            return ErrCode;

        }

        /// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        void Log(string fmt, params object[] _params)
        {
            Utility.Log(Settings.OldiGW.LogFile, fmt, _params);
        }
    }
}
