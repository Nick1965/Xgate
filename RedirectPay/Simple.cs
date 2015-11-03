using Oldi.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using System.Collections.Specialized;
using System.Data.Linq;

namespace RedirectPay
	{
	
	public struct CardResult
		{
		decimal ? Balance;
		}
	public struct LoginResult
		{
		public int? Agent_oid;
		public string Agent_ID;
		public int? Point_oid;
		public string Point_ID;
		public string Nick;
		public int? Nick_oid;
		public string FIO;
		CardResult Card;
		}
	public class SimpleResult
		{
		public long? SapSessionId;
		public int? VruSessionId;
		public int? StcSessionId;
		public int Status;
		public int? ErrCode;
		public string ErrDesc;
		public LoginResult Login;
		public SimpleResult(int ErrCode, string ErrDesc = "")
			{
			Status = ErrCode != 0? 1: 0;
			this.ErrCode = ErrCode;
			this.ErrDesc = ErrDesc;
			}
		}

	public class ParameterSession
		{
		public string Agent_ID;
		public int? Agent_oid;
		public string Point_ID;
		public int? Point_oid;
		public string FIO;
		public string Nick;
		public int? User_oid;

		public long? SapSessionId;
		public int? VruSessionId;
		public int? StcSessionId;
		public int? ServiceNumber;
		}

	public class Simple
		{

		#region Utility functions
		void Log(string fmt, params object[] _params)
			{
			Utility.Log(Config.ConnectorLogFile, fmt, _params);
			}
		void Log(string text)
			{
			Utility.Log(Config.ConnectorLogFile, text);
			}
		#endregion

		int rc = 0;
		
		long? SapSessionId;
		int? VruSessionId;
		int? StcSessionId;
		int? ServiceNumber;
		int? TemplateTid;
		short? EmitterRoamingPartnerId;
		int? CallCategoryId;
		string Nick;
		string Password;
		string Hash;
		int? Sertificate;
		string IpAddress;
		string SertCN;
		string ServerSessionId;
		string FIO;
		string Agent_ID;
		string Point_ID;
		int? Agent_oid;
		int? Point_oid;
		int? CardNumber;
		int? User_oid;
		
		int? ErrCode;
		string ErrDesc;

		string Outtid;
		int? Tid;
		int? Mode;

		decimal? Balance = null;

		NameValueCollection Fields;

		ParameterSession OldSession;
		ParameterSession NewSession;

		Gorod db;

		/// <summary>
		/// Регистрация.
		/// </summary>
		/// <returns></returns>
		void CheckLogin()
			{
			SimpleResult Result = new SimpleResult(0);

			int? Agent_oid2 = null;
			int? Agent_p_oid = null;
			int? Point_oid2 = null;
			int? Point_p_oid = null;
			int? User_oid2 = null;
			string LocaleId = null;
			int? ExpireTime = null;
			List<CardInUse> cardInUse;

			if (!string.IsNullOrEmpty(Nick) || !string.IsNullOrEmpty(Agent_ID) || !string.IsNullOrEmpty(Point_ID))
				{

				rc = db.GetCardSAP(SapSessionId, ref Agent_ID, ref Agent_oid, Point_ID, Point_oid, Nick, Password, ref User_oid, ref FIO, ref EmitterRoamingPartnerId,
					ref CardNumber, ref ErrCode, ref ErrDesc);

				if (Agent_oid == null || User_oid == null)
					{
					Result.ErrCode = 50005;
					Result.ErrDesc = "Нет агента или пользователя";
					Finish();
					return ;
					}

				if (SapSessionId != null)
					{

					rc = db.GetSessionParameters(ref SapSessionId, ref ServerSessionId, ref Agent_oid2, ref Agent_p_oid, ref Point_oid2, ref Point_p_oid,
							ref User_oid2, ref VruSessionId, ref LocaleId, ref ExpireTime, 1 /* update_access_date */,
							ref ErrCode, ref ErrDesc);

					if (User_oid2 != User_oid || Agent_oid2 != Agent_oid || Point_oid2 != Point_oid)
						{

						cardInUse = new List<CardInUse>();
						ISingleResult<CardInUse> Cards = db.CardInUseSession(VruSessionId, ref ErrCode, ref ErrDesc);
						foreach (var card in Cards)
							cardInUse.Add(card);

						db.SapEndGorodSession(SapSessionId, null, ref ErrCode, ref ErrDesc);
						int? e = ErrCode;
						string d = ErrDesc;
						db.SapEndSession(SapSessionId, ref e, ref d);

						SapSessionId = null;
						VruSessionId = null;
						StcSessionId = null;

						}

					}
				else
					{
					Finish();
					return;
					}

				}
			else
				{

				Nick = OldSession.Nick;
				FIO = OldSession.FIO;
				Agent_ID = OldSession.Agent_ID;
				Agent_oid = OldSession.Agent_oid;
				Point_ID = OldSession.Point_ID;
				Point_oid = OldSession.Point_oid;

				}

			// Continue:
			// Если SAP-сессия не создана, но Заданы все параметры точки:
			//
			if (SapSessionId == null && !string.IsNullOrEmpty(ServerSessionId) && Point_oid != null && !string.IsNullOrEmpty(Nick))
				{
				// В Password передаём хэш пароля

				rc = db.SapLogin(ServerSessionId, IpAddress, SertCN, Point_oid, Nick, Password, null, ExpireTime, 0, ref SapSessionId, ref User_oid,
					ref ErrCode, ref ErrDesc);

				if (rc != 0 || ErrCode != 0 && ErrCode != null)
					rc = 1;
				else
					rc = db.SapBeginGorodSession(SapSessionId, User_oid, Nick, null, CardNumber, ref VruSessionId, ref StcSessionId,
						ref EmitterRoamingPartnerId, ref CallCategoryId, ref ErrCode, ref ErrDesc);

				}

			Finish();
	
			}

		void Finish()
			{

			if (SapSessionId == null)
				{

				rc = 1;
				VruSessionId = null;
				StcSessionId = null;
				CardNumber = null;
				EmitterRoamingPartnerId = null;

				Finish2();
				return;
				}

			// Продление времени жизни сессии
			db.SapAddSessionParameters(SapSessionId, null, null, null, null, null, ServerSessionId, VruSessionId, null,
				null, ref ErrCode, ref ErrDesc);
				
			int? Agent_p_oid = null;
			int? Point_p_oid = null;
			int? User_oid2 = null;
			string LocaleId = null;
			int? ExpireTime = null;
			rc = db.GetSessionParameters(ref SapSessionId, ref ServerSessionId, ref Agent_oid, ref Agent_p_oid, ref Point_oid, ref Point_p_oid, ref User_oid2, 
				ref VruSessionId, ref LocaleId, ref ExpireTime, 0,
				ref ErrCode, ref ErrDesc);

			if (rc != 0)
				{

				rc = 1;
				SapSessionId = VruSessionId = StcSessionId = null;
				CardNumber = EmitterRoamingPartnerId = null;

				Finish2();
				return;
				}

			var query =
				from sess in db.Sessions
				where sess.vru_session_id == VruSessionId
				select sess;

			if (query.Count() > 0)
				{

				foreach (var sess in query)
					{
					decimal? face_value = null;
					int? card_type_id = null;
					string card_type_name = null;
					int? card_status_id = null;
					string card_status_name = null;
					int? account_id = null;
					string account_name = null;
					DateTime? end_date = null;
					decimal? limit = null;
					string weight_factor = null;
					short? abonent_category_id = null;
					string abonent_category = null;
					short? max_service_available = null;
					int? agent_oid = null;
					string agent_name = null;
					int? local_emitter_oid = null;
					string local_emitter_name = null;

					if (sess.card_number != null  && sess.card_number > 0)
						db.GetCardInfoP(sess.card_number, sess.emitter_roaming_partner_id, ref Balance, ref face_value, ref card_type_id, ref card_type_name,
							ref card_status_id, ref card_status_name, ref account_id, ref account_name, ref end_date, ref limit, ref weight_factor, ref abonent_category_id,
							ref abonent_category, ref max_service_available, ref agent_oid, ref agent_name, ref local_emitter_oid, ref local_emitter_name);
					else
						{
						db.SapEndGorodSession(SapSessionId, null, ref ErrCode, ref ErrDesc);
						int? e1 = null;
						string d1 = null;
						db.SapEndSession(SapSessionId, ref e1, ref d1);
						SapSessionId = VruSessionId = StcSessionId = null;
						rc = 0;
						ErrCode = 55555;
						ErrDesc = "Неожиданное исчезновение карты из сессии!";
						}
					}
				}
			else
				{
				CardNumber = StcSessionId = null;
				EmitterRoamingPartnerId = null;
				db.SapEndSession(SapSessionId, ref ErrCode, ref ErrDesc);
				SapSessionId = VruSessionId = StcSessionId = null;
				rc = 0;
				ErrCode = 55555;
				ErrDesc = "Неожиданное исчезновение сессии из БД ГОРОД!";
				}

			if (rc == 0)
				{
				ErrCode = null;
				ErrDesc = "";
				}
			else if (ErrCode == 0)
				{
				ErrCode = 60006;
				ErrDesc = "Неизвестная ошибка";
				}

			Finish2();

			}

		void Finish2()
			{

			NewSession.ServiceNumber = ServiceNumber;
			NewSession.SapSessionId = SapSessionId;
			NewSession.StcSessionId = StcSessionId;
			NewSession.VruSessionId = VruSessionId;
			if (ErrCode != null && ErrCode != 0)
				Balance = null;
			NewSession.Nick = Nick;
			NewSession.FIO = FIO;
			NewSession.Agent_ID = Agent_ID;
			NewSession.Agent_oid = Agent_oid;
			NewSession.Point_ID = Point_ID;
			NewSession.Point_oid = Point_oid;

			if (ErrCode != 55555 && StcSessionId == null)
				{
				if (CardNumber == null || EmitterRoamingPartnerId == null)
					{
					ErrCode = 50070;
					ErrDesc = "Нет Сессии";
					}
				else
					{
					ErrCode = 60005;
					ErrDesc = "Ошибка авторизации";
					}
				}

			rc = (ErrCode != null && ErrCode != 0)? 1: 0;
	
			}
		
		/// <summary>
		/// Разбор входной строки
		/// </summary>
		/// <param name="QueryString"></param>
		void Parse(String QueryString)
			{
			Fields = new NameValueCollection();
			string[] KeyValues = QueryString.Split(new Char[] { '&' });
			// Разбор пар
			foreach  (string keyValue in KeyValues)
				{
				string[] para = keyValue.Split(new Char[] { '=' });
				switch (para[0])
					{
					// special
					case "Hashcode":
						Hash = para[1];
						break;
					case "template_tid":
						TemplateTid = para[1].ToInt();
						break;
					case "tid":
						Outtid = para[1];
						break;
					case "mode":
						Mode = para[1].ToInt();
						break;
					//auth
					case "Agent_ID":
						Agent_ID = para[1];
						break;
					case "Point_ID":
						Point_ID = para[1];
						break;
					case "Nick":
						Nick = para[1];
						break;
					case "Password":
						Password = para[1];
						break;
					//Fields
					default:
						Fields.Add(para[0], para[1]);
						break;
					}
				}
			}
		
		public SimpleResult Pay(string QueryString)
			{
			SimpleResult Result = new SimpleResult(0, "");
			Gorod db = null;

			try
				{
				db = new Gorod(Config.GorodConnectionString);
				// Проверим доступность БД _common:calc
				db.ExecuteCommand("Select null;");

				OldSession = new ParameterSession();
				NewSession = new ParameterSession();
				
				// Разбор входной строки
				Parse(QueryString);
				CheckLogin();
				if (rc != 0)
					{
					Result.Status = rc;
					Result.ErrCode = 60200;
					Result.ErrDesc = ErrDesc;
					return Result;
					}
				
				return Result;
				}
			catch (Exception ex)
				{
				Log(ex.ToString());
				return new SimpleResult(60200, ex.Message);
				}
			finally
				{
				if (db != null && db.Connection != null)
					{
					db.Connection.Close();
					db.Dispose();
					}
				}

			}
		
		}

	}
