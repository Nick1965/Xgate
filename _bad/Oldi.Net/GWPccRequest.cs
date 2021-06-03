using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Web;
using Oldi.Utility;

namespace Oldi.Net
{
    class GWPccRequest: GWRequest
    {
		string find = "http://0.0.0.0:10000/";
		string payCheck = "http://0.0.0.0:10000/";
        // string pay;
        // string payState;

        public GWPccRequest(string commonname = "")
            : base()
        {
            ContentType = "application/x-www-form-urlencoded";
            CodePage = "utf-8";
            commonName = CommonName;
            host = ProvidersSettings.Pcc.Host;
			// find = ProvidersSettings.Pcc.Find;
			// payCheck = ProvidersSettings.Pcc.Check;
			// pay = ProvidersSettings.Pcc.Pay;
			// payState = ProvidersSettings.Pcc.Status;
        }

        public GWPccRequest(GWRequest src, string cn = "")
			: this(cn)
        {
            this.provider = src.Provider;
            this.tid = src.Tid;
            this.phone = src.Phone;
			this.phoneParam = src.PhoneParam;
            this.account = src.Account;         // Номер л/с
			this.accountParam = src.AccountParam;
            this.filial = src.Filial;           // Номер филиала.
			this.filialParam = src.FilialParam;
            this.amount = src.Amount;
            this.amountAll = src.AmountAll;
            this.pcdate = src.Pcdate;
            this.commonName = src.CommonName;
            this.service = src.Service;
            this.terminal = src.Terminal;
            this.transaction = src.Transaction;
            this.templateName = src.TemplateName;
        }

		public override string GetCheckHost()
		{
			//return ProvidersSettings.Pcc.Check;
			return "";
		}
		public override string GetPayHost()
		{
			// return ProvidersSettings.Pcc.Pay;
			return "";
		}
		public override string GetStatusHost()
		{
			//return ProvidersSettings.Pcc.Status;
		return "";

		}

		/// <summary>
		/// Получение баланса счета (телефона). В базе данных ничего не отображается.
		/// Возвращается коды 3 - успех; 6 - невозможность проведения платежа.
		/// </summary>
		/// <returns></returns>
		public override int Find()
		{
			throw new ApplicationException("Метод Find ббыть заменён на Check в GWPccRequest");
			// int ret = MakeFindRequest();
			// return ret;
		}

		/// <summary>
		/// Проверка возможности платежа
		/// </summary>
		public override void Check()
		{
			throw new ApplicationException("Метод Check не реализован в GWPccRequest");
		}

		/// <summary>
		/// Создаем запрос для получения баланса
		/// </summary>
		/// <returns></returns>
		public int MakeFindRequest()
		{
			string req = "&";
			int ret = 0;

			if (!String.IsNullOrEmpty(phone))
			{
				if (!String.IsNullOrEmpty(phoneParam))
					// &№ телефона=3822408633
					req += phoneParam + "=" + phone;
				else
					// &requisite=3822408633
					req += "requisite=" + phone;
				ret = 1;
			}
			// № account, filial, № договора, ЛСЧЕТ
			else if (!string.IsNullOrEmpty(account))
			{
				if (!String.IsNullOrEmpty(accountParam))
				{
					req += accountParam + "=" + account;
					if (!string.IsNullOrEmpty(filial) && !String.IsNullOrEmpty(filialParam))
					{
						req += filialParam + "=" + filial;
						stRequest = find + req;
						ret = 1;
					}
				}
			}
			Log("Find/MakeRequest: ID={0}, Param={1}, Filial={2}, Param={3}", 
				!string.IsNullOrEmpty(phone)? phone: account, 
				!string.IsNullOrEmpty(phoneParam)?phoneParam:!string.IsNullOrEmpty(accountParam) ? accountParam: "<Empty>",
				!string.IsNullOrEmpty(filial) ? filial : "Empty", !string.IsNullOrEmpty(filialParam) ? filialParam: "<Empty>");
			if (ret == 0)
				Log("Построкн запрос Find: {0}", find);
			else
				Log("Ошибка построения запроса Find");
			return ret;
		}
		
        
        /// <summary>
        /// Локальная реализация MakeCheckRequest
        /// </summary>
        /// <returns></returns>
        public new int MakeRequest(int old_state)
        {
            // Сделаем запрос paycheck

            // Получаем дополнительную информацию о терминале
            // GetTerminalInfo(terminal);
            
            // Log("[CHECK/MakeCheckRequest], Tid={0}, Ph/Acc={1}, Amount={2}, AmountAll={3}, recid={4}, point={5}, type={6}",
            //     Tid, !String.IsNullOrEmpty(Phone) ? Phone : Account, XConvert.AsAmount(Amount), XConvert.AsAmount(AmountAll), Service, Terminal, TerminalType);
            StringBuilder sb = new StringBuilder();


            try
            {
                sb.AppendFormat(payCheck, Host, tid, XConvert.AsAmount(amount), service, terminal);
                if (!string.IsNullOrEmpty(phone)) sb.AppendFormat("&{0}={1}", HttpUtility.UrlEncode("№ телефона").ToUpper(), phone);
                if (!string.IsNullOrEmpty(account)) sb.AppendFormat("&{0}={1}", HttpUtility.UrlEncode("№ договора").ToUpper(), account);
                if (!string.IsNullOrEmpty(number)) sb.AppendFormat("&{0}={1}", HttpUtility.UrlEncode("филиал").ToUpper(), number);
                checkHost = sb.ToString();
            }
            catch (Exception ex)
            {
                Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
                return -1;
            }

            stRequest = "";

            Log("[CHECK/MakeCheckRequest] Шаблон запроса сформирован\r\n{0}", checkHost);

            errCode = 6;
            errDesc = "Реализация Check не завершена";
            return 1;
        }

        /// <summary>
        /// Разбор ответа от ПУ
        /// </summary>
        /// <param name="stResponse"></param>
        public override int ParseAnswer(string stResponse)
        {

            try
            {
                XDocument doc = XDocument.Parse(stResponse);

                var answer = doc.Element("Result")
                                   .Elements();

                if (doc.Element("Result").HasAttributes)
                {
                    ErrCode = Convert.ToInt32((string)doc.Element("Result").Attribute("OperationState"));
                    ErrDesc = (string)doc.Element("Result").Value;
                }
                return 0;
            }
            catch (Exception ex)
            {
                ErrCode = 1;
                ErrDesc = "Невозможно разобрать ответ";
                Log(ex.Message);
                Log(ex.StackTrace);
                return -1;
            }
    
            // Log("ParseAnser: ErrCode={0} ErrDesc=\"{1}\"", ErrCode, ErrDesc);

        }

		/// <summary>
		/// LOG-Файл
		/// </summary>
		/// <returns></returns>
		protected override string GetLogName()
		{
			return Settings.OldiGW.LogFile;
		}

    
    }
}
