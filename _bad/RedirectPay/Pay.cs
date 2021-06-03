using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using Oldi.Net;
using Oldi.Utility;
using System.Data.Linq;

namespace RedirectPay
	{
	/// <summary>
	/// PayInfo
	/// </summary>
	public class ServerCardPayInfo
		{
		public decimal? CardBalance;
		public string Tid;
		public string CheckId;
		public int? State
			{
			get
				{
				return state;
				}
			set
				{
				state = value;
				string key = string.Format("State{0}", state);
				try
					{
					Description = States.ResourceManager.GetString(key);
					}
				catch (ArgumentNullException)
					{
					Description = "Неизвестное состояние платежа";
					}
				}
			}
		int? state;
		public string Description;
		public override string ToString()
			{
			return string.Format("CardBalance={0}; Tid={1}; CheckId={2}; State={3}; Description={4}",
				CardBalance!= null?CardBalance.Value.ToString("0.00", CultureInfo.InvariantCulture):"", Tid, CheckId, State, Description);
			}
		}

	public class XResponse
		{
		public string errCode;
		public string errDesc;
		public XResponse()
			{
			errCode = "";
			errDesc = "";
			}
		}
	
	public struct ServerCardFields
		{
		[XmlArray("Field")]
		public string[] Field;
		}

	/// <summary>
	/// Контракт данных ServerCrad
	/// </summary>
	[XmlRoot("Result")]
	public class ServerCardResponse:XResponse
		{
		public int Status;
		public ServerCardPayInfo PayInfo;
		[XmlText]
		public string[] Fields;
		public void SetError(int? ErrCode, string ErrDesc)
			{
			errCode = ErrCode.ToString();
			errDesc = ErrDesc;
			}
		}

	public partial class Pay
		{

		#region Definitions

		/// <summary>
		/// Переменные сессии
		/// </summary>
		NameValueCollection Session;

		/// <summary>
		/// ПИН
		/// </summary>
		string PIN;

		/// <summary>
		/// Зашифрованый ПИН
		/// </summary>
		string PINout;

		/// <summary>
		/// Серия
		/// </summary>
		short? Seria;

		/// <summary>
		/// Номер карты
		/// </summary>
		int CardNumber;

		/// <summary>
		/// Тип кодирования карты
		/// </summary>
		int CardEncode = 0;

		int? ErrCode = null;
		string ErrDesc = "";

		int? Template_tid;

		string IpAddress;

		NameValueCollection Params;
		NameValueCollection GorodFields;

		ServerCardResponse Response;

		int Status;

		int? Mode
			{
			get;
			set;
			}
		
		/// <summary>
		/// Имя обрабатываемого поля
		/// </summary>
		string FieldName;

		/// <summary>
		/// Значение обрабатываемого поля
		/// </summary>
		string FieldValue;

		/// <summary>
		/// Номер операции в ПЦ
		/// </summary>
		int? Tid;

		/// <summary>
		/// Номер операции на терминале
		/// </summary>
		string OutTid;

		/// <summary>
		/// Сатаус операции
		/// </summary>
		int? State;

		/// <summary>
		/// Баланс карты
		/// </summary>
		decimal? Balance;
		decimal? NewBalance;
		/// <summary>
		/// Сумма
		/// </summary>
		decimal? Amount;
		decimal? NewAmount;
		/// <summary>
		/// Комиссия
		/// </summary>
		decimal? Commission;
		decimal? NewCommission;

		/// <summary>
		/// Строка пар имён/значений
		/// </summary>
		string Fields;

		/// <summary>
		/// Номер точки
		/// </summary>
		int Point_oid
			{
			get;
			set;
			}

		/// <summary>
		/// Номер агента
		/// </summary>
		int Agent_oid
			{
			get;
			set;
			}

		/// <summary>
		///Хэш пароля
		/// </summary>
		string Hash
			{
			get;
			set;
			}
	
		#endregion Definitions

		public Pay(string Fields, string IpAddress)
			{
			// this.CardNumber = CardNumber;
			// this.Seria = Seria;
			// this.PIN = PIN;
			// this.Template_tid = Template_tid;
			this.IpAddress = IpAddress;
			this.Fields = Fields;
			// Распарсить строку параметров в коллекцию
			Parse(Fields);
			Status = 0;

			Response = new ServerCardResponse();
			Session = new NameValueCollection();
			GorodFields = new NameValueCollection();

			}

		#region Log
		string logFileName = ".\\pay.log";
		void Log(string fmt, params object[] prms)
			{
			Utility.Log(logFileName, fmt, prms);
			}
		#endregion Log

		/// <summary>
		/// Печать параметров
		/// </summary>
		/// <param name="Proc"></param>
		/// <param name="Before"></param>
		void WriteLog(string Proc, bool Before = true)
			{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "CardNumber", CardNumber);
			sb.AppendFormat(", Point={0}", Point_oid);
			sb.AppendFormat(", Agent={0}", Agent_oid);
			sb.AppendFormat(", Seria={0}", Seria);
			sb.AppendFormat(", PIN={0}", PIN);
			sb.AppendFormat(", PINout={0}", PINout);
			sb.AppendFormat(", Template={0}", Template_tid);
			Log(sb.ToString());

			if (Mode != null)
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "Mode", Mode);
			if (!string.IsNullOrEmpty(OutTid))
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "Outtid", OutTid);
			if (Tid != null)
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "Tid", Tid);
			if (State != null)
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "State", State);

			foreach (string name in Session.Keys)
				// if (!string.IsNullOrEmpty(Session[name]))
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, name, Session[name]);

			sb.Clear();
			foreach (string name in Params.Keys)
				if (!string.IsNullOrEmpty(Params[name]))
					sb.AppendFormat("{0}={1};", name, Params[name]);
			if (sb.ToString().Length > 0)
				Log("[{0}] {1} {2}: Parms = \"{3}\"", IpAddress, Before? "до выполнения": "после выполнения", Proc, sb.ToString());

			if (!string.IsNullOrEmpty(FieldName))
				{
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "FiledName", FieldName);
				Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "FieldValue", FieldValue);
				}

			sb.Clear();
			foreach (string name in GorodFields.Keys)
				if (!string.IsNullOrEmpty(GorodFields[name]))
					sb.AppendFormat("{0}={1};", name, GorodFields[name]);
			if (sb.ToString().Length > 0)
				Log("[{0}] {1} {2}: Fields = \"{3}\"", IpAddress, Before? "до выполнения": "после выполнения", Proc, sb.ToString());


			sb.Clear();
			if (Balance != null || NewBalance != null || Amount != null || NewAmount != null || Commission != null || NewCommission != null)
				{
				sb.AppendFormat("[{0}] {1} {2}: ", IpAddress, Before? "до выполнения": "после выполнения", Proc);

				if (Balance != null)
					sb.AppendFormat("Balance = {0}; ", XConvert.AsAmount(Balance));
				if (NewBalance != null)
					sb.AppendFormat("NewBalance = {0}; ", XConvert.AsAmount(NewBalance));

				if (Amount != null)
					sb.AppendFormat("Amount = {0}; ", XConvert.AsAmount(Amount));
				if (NewAmount != null)
					sb.AppendFormat("NewAmount = {0}; ", XConvert.AsAmount(NewAmount));

				if (Commission != null)
					sb.AppendFormat("Commission = {0}; ", XConvert.AsAmount(Commission));
				if (NewBalance != null)
					sb.AppendFormat("NewCommission = {0}; ", XConvert.AsAmount(NewCommission));

				Log(sb.ToString());
				}

			Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "Status", Status);
			Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "ErrCode", ErrCode);
			Log("[{0}] {1} {2}: {3} = {4}", IpAddress, Before? "до выполнения": "после выполнения", Proc, "ErrDesc", ErrDesc);
			}

		/// <summary>
		/// Проведение платежа
		/// </summary>
		/// <returns></returns>
		public ServerCardResponse Process()
			{
			try
				{

				Status = 0;

				Log("\r\n[{0}] pay.xml/{1}", IpAddress, Fields);

				// Шифрование ПИНа
				if (Calc() == 0)
					{

					// Замена ПИНа
					PIN = PINout;

					if (Status == 0)
						CheckLogin();
					if (Status == 0)
						BuildTemplate();
					if (Status == 0)
						MakePay();

					// Контроль и вывод полей
					Read();
					}

				Logout();

				if (ErrCode == null || ErrCode == 0 || ErrCode == 60006 || ErrCode == 50000)
					Response.Status = 0;
				else
					Response.Status = 1;

				Response.SetError(ErrCode, ErrDesc);
				// Response.Fields.Field = new string[GorodFields.Count];
				Response.Fields = new string[GorodFields.Count];
				if (Tid != null)
					{
					Response.PayInfo = new ServerCardPayInfo();
					Response.PayInfo.CardBalance = Balance;
					Response.PayInfo.CheckId = Tid.ToString();
					Response.PayInfo.State = State;
					Response.PayInfo.Tid = OutTid.Substring(OutTid.LastIndexOf('-') + 1);
					}

				int i = 0;
				foreach (string name in GorodFields.Keys)
					{
					// Logging.Log("[{0}] pay.xml: <{1}>{2}</{1}>", IpAddress, name, GorodFields[name]);
					Response.Fields[i++] = string.Format("<{0}>{1}</{0}>", name, GorodFields[name]);
					}

				}
			catch (Exception ex)
				{
				Log(ex.ToString());
				Response.Status = 1;
				if (ErrCode != null && ErrCode == 0)
					ErrCode = 60201;
				if (string.IsNullOrEmpty(ErrDesc))
					ErrDesc = ex.Message;
				Response.SetError(ErrCode, ErrDesc);
				}

			return Response;
			}

		/// <summary>
		/// Закрытие сессии
		/// </summary>
		public void Logout()
			{

			WriteLog("Logout", true);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				db.LogOut(Session["vru_session_id"].ToInt());
				}

			WriteLog("Logout", false);

			}

		/// <summary>
		/// Контроль создания платежа
		/// </summary>
		public void Read()
			{

			WriteLog("Read", true);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				ISingleResult<ReadResult> result = db.Read(Template_tid.ToString(), Session["vru_session_id"].ToInt(), Tid, ref ErrCode, ref ErrDesc);

				if (ErrCode != 0)
					Status = 1;
				else
					Status = 0;
				if (Status == 0)
					foreach (ReadResult para in result)
						{
						GorodFields.Add(para.Name, para.Value);
						}
				}
			
			WriteLog("Read", false);
			}

		/// <summary>
		/// Создание платежа
		/// </summary>
		public void MakePay()
			{
			int? NewServiceNumber = null, NewState = null, NewTid = null;
			string NewOutTid = "";
			// decimal? NewBalance, NewAmount, NewCommission;

			WriteLog("Makepay", true);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				Status = db.MakePay(Template_tid.ToString(),
							Session["vru_session"].ToInt(),
							Session["stc_session_id"].ToInt(),
							Session["service_number"].ToInt(),
							Mode,
							OutTid,
							CardNumber == 1? "card": "redo",
							ref NewServiceNumber, ref NewState, ref NewTid, ref NewOutTid, 
							ref NewBalance, ref NewAmount, ref NewCommission,
							ref ErrCode, ref ErrDesc
							);
				}
	
			Session.Add("new_service_number", NewServiceNumber.ToString());
			Session.Add("new_state", NewState.ToString());
			Session.Add("new_tid", NewTid.ToString());
			Session.Add("new_out_tid", NewOutTid);
			// NewBalance = ToAmount(r.ResultSet.Tables[0].Rows[0]["new_balance"]);
			// NewAmount = ToAmount(r.ResultSet.Tables[0].Rows[0]["new_amount"]);
			// NewCommission = ToAmount(r.ResultSet.Tables[0].Rows[0]["new_commission"]);

			WriteLog("Makepay", false);

			Session["service_number"] = Session["new_service_number"];
			OutTid = Session["new_out_tid"];
			Tid = Session["new_tid"].ToInt();
			State = Session["new_state"].ToInt();
			Balance = NewBalance;
			Amount = NewAmount;
			Commission = NewCommission;

			// Удаление промежуточных параметров сессии
			Session.Remove("new_service_number");
			Session.Remove("new_state");
			Session.Remove("new_tid");
			Session.Remove("new_out_tid");

			NewBalance = null;
			NewAmount = null;
			NewCommission = null;

			}

		/// <summary>
		/// Построение шаблона
		/// </summary>
		public void BuildTemplate()
			{

			WriteLog("BuildTemplate", true);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				Status = db.BuildTemplate(Template_tid.ToString(), Session["vru_session_id"].ToInt(), ref ErrCode, ref ErrDesc);
				}

			foreach (string FName in Params.Keys)
				{
				if (Status != 0)
					break;
				Save(FName, Params[FName]);
				}

			FieldName = "";

			WriteLog("BuildTemplate", false);

			}

		/// <summary>
		/// Сохранение поля
		/// </summary>
		/// <param name="FieldName"></param>
		/// <param name="Value"></param>
		public void Save(string FieldName, string FieldValue)
			{
			this.FieldName = FieldName;
			this.FieldValue = FieldValue;

			WriteLog("Save", true);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				Status = db.Save(Template_tid.Value, Session["vru_session_id"].ToInt(), FieldName, FieldValue, ref ErrCode, ref ErrDesc);
				}
			
			WriteLog("Save", false);
			}

		/// <summary>
		/// Открытие или продление сессии
		/// </summary>
		void CheckLogin()
			{

			WriteLog("CheckLogin", true);

			int? ServiceNumber = null, 
				StcSessionId = null, 
				VruSessionId = null, 
				CallCategoryId = null;

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				int result = db.CheckLogin(null, IpAddress, null, CardNumber, PIN, ref Seria, ref ServiceNumber, ref StcSessionId, ref VruSessionId, ref CallCategoryId,
					ref Balance, ref ErrCode, ref ErrDesc);
				}
			
			if (ErrCode == 0)
				{
				Status = 0;
				Session.Add("service_number", ServiceNumber.ToString());
				Session.Add("stc_session_id", StcSessionId.ToString());
				Session.Add("vru_session_id", VruSessionId.ToString());
				Session.Add("call_category_id", CallCategoryId.ToString());
				}
			else
				{
				Status = 1;
				if (ErrCode != 50070 && ErrCode != 60005)
					{
					CardNumber = 0;
					Seria = null;
					}
				}
	
			WriteLog("CheckLogin", false);

			if (ErrCode == 50091)
				{
				Logout();
				Status = 0;
				ErrCode = 0;
				ErrDesc = "";
				CheckLogin();
				}

			}

		/// <summary>
		/// Разбор входящей строки
		/// </summary>
		/// <param name="Fields"></param>
		void Parse(string Fields)
			{
			Params = new NameValueCollection();

			// Разделим пары Params
			// Log("Parsings {0}", Params);
			string[] para = Fields.Split(new Char[] { '&' });
			// Добавим в коллекцию
			foreach (string p in para)
				{
				string[] x = p.Split(new Char[] { '=' });

				// Logging.Log("{0} = {1}", x[0], x[1]);

				switch (x[0].ToLower())
					{
					case "tid":
						OutTid = x[1];
						break;
					case "template":
						Template_tid =  x[1].ToInt();  //ToInt(x[1]);
						break;
					case "card_number":
						if (!int.TryParse(x[1], out CardNumber))
							CardNumber = -1;
						break;
					case "pin":
						PIN = x[1];
						break;
					case "mode":
						Mode = x[1].ToInt();
						break;
					case "point_id":
						Point_oid = x[1].ToInt();
						break;
					case "agent_id":
						Agent_oid = x[1].ToInt();
						break;
					case "sign":
						continue;
					default:
						Params.Add(x[0], x[1]);
						break;
					}

				}

			}

		/// <summary>
		/// Конвертация в строку 1251
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		string ConvertTo1251(string unicodeString)
			{

			// Create two different encodings.
			Encoding ascii = Encoding.GetEncoding("windows-1251");
			Encoding unicode = Encoding.Unicode;

			// Convert the string into a byte array.
			byte[] unicodeBytes = unicode.GetBytes(unicodeString);

			// Perform the conversion from one encoding to the other.
			byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);

			// Convert the new byte[] into a char[] and then into a string.
			char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
			ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
			string asciiString = HttpUtility.UrlEncode(new string(asciiChars), Encoding.GetEncoding(1251));

			// Log("\r\nConvert: {0}", asciiString);

			return asciiString;
			}

		/// <summary>
		/// Кодирование ПИН-кода
		/// </summary>
		/// <returns></returns>
		public int Calc()
			{

			WriteLog("Calc", true);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{
				// 1 - раскодировать, 0 - закодировать
				db.EncodePin(null, CardNumber, IsNumeric(PIN)? 1: 0, ref PINout);
				}

			WriteLog("Calc", false);

			return 0;
			}

		/// <summary>
		/// Проверяет, является ли строка числом.
		/// </summary>
		/// <param name="Sample"></param>
		/// <returns></returns>
		bool IsNumeric(string Sample)
			{
			foreach (char c in Sample)
				{
				if (c < '0' || c > '9')
					return false;
				}

			return true;
			}
		
		}
	}