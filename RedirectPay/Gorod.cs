using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using Oldi.Net;
using System.Globalization;
using System.Data.SqlClient;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Reflection;

namespace RedirectPay
	{
	public class PinResult
		{
		public int TypeId;
		public string PIN;
		}

	public class ReadResult
		{
		public string Name;
		public string Value;
		}
	public class Payment
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
	
	[Table(Name="Payment_history")]
	public class History
		{
		[Column(Name="Tid", DbType="INT")]
		public int Tid;
		[Column(Name="result_text", DbType="VARCHAR(250)")]
		public string ResultText;
		}

	public class CardInUse
		{
		public int? CradNumber;
		public short? EmitterRoamingPartnerId;
		public string SPCName; // Имя БД СПК
		}
	
	
	/// <summary>
	/// Таблица Session
	/// </summary>
	public class Session
		{
		public int vru_session_id;
		public DateTime StartTime;
		public int? EpayEnabled;
		public string pan;
		public string EPay_ID;
		public int? unp;
		public string sessionid;
		public int? inline_number;
		public int? client_oid;
		public string operatorID;
		public int? server_id;
		public int? stc_session_id;
		public int? card_number;
		public short? emitter_roaming_partner_id;
		public int? call_category_id;
		public int? connectKind;
		public DateTime? c_date;
		public int? c_date_control;
		public string notify_proc;
		public int? agent_oid;
		public int? local_emitter_oid;
		public string sub_inner_tid;
		}
	
	public class Gorod:DataContext
		{
		public Gorod(string cnn)
			: base(cnn)
			{
			}

		public History Histories;

		public Table<Session> Sessions;

		[Function(Name="gorod.dbo.get_card_info_info_p")]
		public int GetCardInfoP(
			[Parameter(DbType="INT")] int? number,
			[Parameter(DbType="SMALLINT")] short? emitter_roaming_partner_id,
			[Parameter(DbType="NUMERIC(16,2)")] ref decimal? balance,
			[Parameter(DbType="NUMERIC(16,2)")] ref decimal? face_value,
			[Parameter(DbType="INT")] ref int? card_type_id,
			[Parameter(DbType="VARCHAR(50)")] ref string card_type_name,
			[Parameter(DbType="INT")] ref int? card_status_id,
			[Parameter(DbType="VARCHAR(50)")] ref string card_status_name,
			[Parameter(DbType="INT")] ref int? account_id,
			[Parameter(DbType="VARCHAR(255)")] ref string account_name,
			[Parameter(DbType="DATETIME")] ref DateTime? end_date,
			[Parameter(DbType="NUMERIC(16,2)")] ref decimal? limit,
			[Parameter(DbType="VARCHAR(10)")] ref string weight_factor,
			[Parameter(DbType="SMALLINT")] ref short? abonent_category_id,
			[Parameter(DbType="VARCHAR(50)")] ref string abonent_category,
			[Parameter(DbType="SMALLINT")] ref short? max_service_available,
			[Parameter(DbType="INT")] ref int? agent_oid,
			[Parameter(DbType="VARCHAR(50)")] ref string agent_name,
			[Parameter(DbType="INT")] ref int? local_emitter_oid,
			[Parameter(DbType="VARCHAR(255)")] ref string local_emitter_name
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				number, emitter_roaming_partner_id, balance, face_value, card_type_id, card_type_name, card_status_id, card_status_name, account_id,
				account_name, end_date, limit, weight_factor, abonent_category_id, abonent_category, max_service_available, agent_oid, agent_name,
				local_emitter_oid, local_emitter_name);
			return ((int)(result.ReturnValue));
			}

		[Function(Name="gorod.dbo.sap_addSessionParameters")]
		public int SapAddSessionParameters(
			[Parameter(DbType="BIGINT")] long? SapSessionId,
			[Parameter(DbType="INT")] int? Agent_oid,
			[Parameter(DbType="INT")] int? Agent_p_oid,
			[Parameter(DbType="INT")] int? Point_oid,
			[Parameter(DbType="INT")] int? Point_p_oid,
			[Parameter(DbType="INT")] int? User_oid,
			[Parameter(DbType="VARCHAR(32)")] string ServerSessionId,
			[Parameter(DbType="INT")] int? VruSessionId,
			[Parameter(DbType="VARCHAR(2)")] string LocaleId,
			[Parameter(DbType="INT")] int? ExpireTime,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				SapSessionId, Agent_oid, Agent_p_oid, Point_oid, Point_p_oid, User_oid, ServerSessionId, VruSessionId, LocaleId,
				ExpireTime, ErrCode, ErrDesc);
			return ((int)(result.ReturnValue));
			}

		[Function(Name="gorod.dbo.sap_Login")]
		public int SapLogin(
			[Parameter(DbType="VARCHAR(32)")] string ServerSessionId,
			[Parameter(DbType="VARCHAR(20)")] string IpAddress,
			[Parameter(DbType="VARCHAR(10)")] string SertCN,
			[Parameter(DbType="INT")] int? Point_oid,
			[Parameter(DbType="VARCHAR(32)")] string Nick,
			[Parameter(DbType="VARCHAR(32)")] string Password,
			[Parameter(DbType="VARCHAR(2)")] string LocaleId,
			[Parameter(DbType="INT")] int? ExpireTime,
			[Parameter(DbType="INT")] int? AutoGrdSession,
			[Parameter(DbType="BIGINT")] ref long? SapSessionId,
			[Parameter(DbType="INT")] ref int? User_oid,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				ServerSessionId, IpAddress, SertCN, Point_oid, Nick, Password, LocaleId, ExpireTime, AutoGrdSession,
				SapSessionId, User_oid, ErrCode, ErrDesc);
			return ((int)(result.ReturnValue));
			}

		[Function(Name="gorod.dbo.sap_beginGorodSession")]
		public int SapBeginGorodSession(
			[Parameter(DbType="BIGINT")] long? SapSessionId,
			[Parameter(DbType="INT")] int? User_oid,
			[Parameter(DbType="VARCHAR(32)")] string OperatorId,
			[Parameter(DbType="VARCHAR(2)")] string LocaleId,
			[Parameter(DbType="INT")] int? CardNumber,
			[Parameter(DbType="INT")] ref int? VruSessionId,
			[Parameter(DbType="INT")] ref int ? StcSessionId,
			[Parameter(DbType="SMALLINT")] ref short? EmitterRoamingPartnerId,
			[Parameter(DbType="INT")] ref int? CallCategoryId,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				SapSessionId, LocaleId, ErrCode, ErrDesc);
			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Закрытие сессии в gorod.dbo.session
		/// </summary>
		/// <param name="SapSessionId"></param>
		/// <param name="LocaleId"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="gorod.dbo.sap_endGorodSession")]
		public int SapEndGorodSession(
			[Parameter(DbType="BIGINT")] long? SapSessionId,
			[Parameter(DbType="VARCHAR(2)")] string LocaleId,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				SapSessionId, LocaleId, ErrCode, ErrDesc);
			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Закрытие всех сессиий
		/// </summary>
		/// <param name="SapSessionId"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="gorod.dbo.sap_endSession")]
		public int SapEndSession(
			[Parameter(DbType="BIGINT")] long? SapSessionId,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				SapSessionId, ErrCode, ErrDesc);
			return ((int)(result.ReturnValue));
			}
	
		/// <summary>
		/// Получение PIN-кода
		/// </summary>
		/// <param name="card_number"></param>
		/// <param name="pin_type_id"></param>
		/// <returns></returns>
		[Function(Name="spc.dbo.grd_card_PIN")]
		public ISingleResult<PinResult> GetCardPIN([Parameter(DbType="int")] int card_number, [Parameter(DbType="int")] int pin_type_id)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), card_number, pin_type_id);
			return ((ISingleResult<PinResult>)(result.ReturnValue));
			}

		/// <summary>
		/// Список блокированных карт
		/// </summary>
		/// <param name="VruSessionId"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="spc.dbo.ext_card_is_in_use_session")]
		public ISingleResult<CardInUse> CardInUseSession([Parameter(DbType="int")] int? VruSessionId, [Parameter(DbType="int")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), VruSessionId, ErrCode, ErrDesc);
			return ((ISingleResult<CardInUse>)(result.ReturnValue));
			}

		/// <summary>
		/// Папаметры точки и карты
		/// </summary>
		/// <param name="SapSessionId"></param>
		/// <param name="Agent_ID"></param>
		/// <param name="Agent_oid"></param>
		/// <param name="Point_ID"></param>
		/// <param name="Point_oid"></param>
		/// <param name="Nick"></param>
		/// <param name="Password"></param>
		/// <param name="User_oid"></param>
		/// <param name="FIO"></param>
		/// <param name="EmitterRoamingPartnerId"></param>
		/// <param name="CardNumber"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="gorod.dbo.XML_get_card_sap")]
		public int GetCardSAP(
			[Parameter(DbType="BIGINT")] long? SapSessionId, 
			[Parameter(DbType="VARCHAR(32)")] ref string Agent_ID,
			[Parameter(DbType="INT")] ref int? Agent_oid,
			[Parameter(DbType="VARCHAR(32)")] string Point_ID,
			[Parameter(DbType="INT")] int? Point_oid,
			[Parameter(DbType="VARCHAR(32)")] string Nick,
			[Parameter(DbType="VARCHAR(32)")] string Password, // В этой процедуре не имеет значения
			[Parameter(DbType="INT")] ref int? User_oid,
			[Parameter(DbType="VARCHAR(255)")] ref string FIO,
			[Parameter(DbType="SMALLINT")] ref short? EmitterRoamingPartnerId,
			[Parameter(DbType="INT")] ref int? CardNumber,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), 
				SapSessionId, Agent_ID, Agent_oid, Point_ID, Point_oid, Nick, Password, User_oid, FIO, EmitterRoamingPartnerId, CardNumber,
				ErrCode, ErrDesc) ;
			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Получение параметров сессии
		/// /**
		/// * Access: PRIVATE
		/// *
		///  * Возвращает параметры сессии.
		///  * Сессия ищется по @sap_session_id или @server_session_id.
		///  * Если @sap_session_id не NULL или оба параметра не NULL, то для поиска сессии
		///  * используется только @sap_session_id, иначе для поиска сессии используется
		///  * значение параметра @server_session_id.
		///  * Параметр @update_access_date управляет режимом обновления времени последнего
		///  * обращения к сессии, поле access_date.
		///  * Если @update_access_date = 0, то access_date никогда не меняется.
		///  * Если @update_access_date != 0, то access_date обновляется с учетом защитного
		///  * интервала, т.е. изменение access_date, выполняется только тогда, когда access_date
		///  * отличается от GETDATE больше, чем на 1 мин. Защитный интервал позволяет
		///  * оптимизировать изменение таблицы sap_session при частом чтении из сессии.
		///  * ERRORS:
		///  * 51011 - если оба параметра @sap_session_id и @server_session_id IS NULL
		///  * 51500 - если сессия не существует
		///  * 51501 - если срок действия сессии истек, при этом сессия удаляется из таблицы.
		///  * 51511 - если при обновления access_date в таблице sap_session возникля ошибка
		///  */
		/// </summary>
		/// <param name="SapSessionId"></param>
		/// <param name="ServerSessionId"></param>
		/// <param name="Agent_oid"></param>
		/// <param name="Agent_p_oid"></param>
		/// <param name="Point_oid"></param>
		/// <param name="Point_p_oid"></param>
		/// <param name="UserId"></param>
		/// <param name="VruSessionId"></param>
		/// <param name="LocaleId"></param>
		/// <param name="ExpireTime"></param>
		/// <param name="UpdateAccessDate"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="gorod.dbo.sap_getSessionParameters_Internal")]
		public int GetSessionParameters(
			[Parameter(DbType="BIGINT")] ref long? SapSessionId,
			[Parameter(DbType="VARCHAR(32)")] ref string ServerSessionId,
			[Parameter(DbType="INT")] ref int? Agent_oid,
			[Parameter(DbType="INT")] ref int? Agent_p_oid,
			[Parameter(DbType="INT")] ref int? Point_oid,
			[Parameter(DbType="INT")] ref int? Point_p_oid,
			[Parameter(DbType="INT")] ref int? User_oid,
			[Parameter(DbType="INT")] ref int? VruSessionId,
			[Parameter(DbType="VARCHAR(2)")] ref string LocaleId,
			[Parameter(DbType="INT")] ref int? ExpireTime,
			[Parameter(DbType="INT")] int UpdateAccessDate,
			[Parameter(DbType="INT")] ref int? ErrCode,
			[Parameter(DbType="VARCHAR(255)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				SapSessionId, ServerSessionId, Agent_oid, Agent_p_oid, Point_oid, Point_p_oid, User_oid, VruSessionId, LocaleId, ExpireTime, UpdateAccessDate,
				ErrCode, ErrDesc);
			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Расшифровка/дешифровка ПИНа
		/// </summary>
		/// <param name="card_number"></param>
		/// <param name="pin_type_id"></param>
		/// <returns></returns>
		[Function(Name="Gorod.dbo.EXT_ENCODEPIN")]
		// [return: Parameter(DbType="Int")]
		public void EncodePin(
			[Parameter(Name="emitter_roaming_partner_id", DbType="SMALLINT")] Nullable<short> EmitterRoamingPartnerId,
			[Parameter(Name="card_number", DbType="INT")] int CardNumber,
			[Parameter(Name="card_encode", DbType="INT")] int CardEncode,
			[Parameter(Name="card_pin", DbType="VARCHAR(20)")] ref string CardPin
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), EmitterRoamingPartnerId, CardNumber, CardEncode, CardPin);
			CardPin = ((string)(result.GetParameterValue(1)));

			}

		/// <summary>
		/// Установка состояния платежа
		/// </summary>
		/// <param name="Tid"></param>
		/// <param name="State"></param>
		/// <param name="ResultCode"></param>
		/// <param name="ResultText"></param>
		/// <returns></returns>
		[Function(Name="Sap2.dbo.Set_State_Payment")]
		[return: Parameter(DbType="INT")]
		public int SetStatePayment(
			[Parameter(Name="payment_tid", DbType="INT")] int Tid,
			[Parameter(Name="state", DbType="INT")] int State,
			[Parameter(Name="result_code", DbType="INT")] int ResultCode,
			[Parameter(Name="result_text", DbType="VARCHAR(255)")] string ResultText)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), Tid, State, ResultCode, ResultText);
			return ((int)(result.ReturnValue));
			}

		[Function()]
		public void UpdateResultText([Parameter(Name="Result_text", DbType="VARCHAR(250)")] string ResultText)
			{

			}
		
		/// <summary>
		/// Проверка открытия сессии
		/// </summary>
		/// <param name="card_number"></param>
		/// <param name="pin_type_id"></param>
		/// <returns></returns>
		[Function(Name="SAP2.dbo.ServerCard_CheckLogin")]
		[return: Parameter(DbType="Int")]
		public int CheckLogin(
			[Parameter(Name="certificate", DbType="INT")] int? Certificate,
			[Parameter(Name="ip_address", DbType="VARCHAR(25)")] string IpAddress,
			[Parameter(Name="cert_cn", DbType="VARCHAR(10)")] string CertCN,
			[Parameter(Name="card_number", DbType="INT")] int CardNumber,
			[Parameter(Name="pin", DbType="VARCHAR(20)")] string Pin,
			[Parameter(Name="card_seria", DbType="SMALLINT")] ref short? CardSeria,
			[Parameter(Name="new_service_number", DbType="INT")] ref int? NewServiceNumber,
			[Parameter(Name="new_stc_session_id", DbType="INT")] ref int? NewStcSessionId,
			[Parameter(Name="new_vru_session_id", DbType="INT")] ref int? NewVruSessionId,
			[Parameter(Name="call_category_id", DbType="INT")] ref int? CallCategoryId,
			[Parameter(Name="balance", DbType="DECIMAL(16,2)")] ref decimal? Balance,
			[Parameter(Name="errCode", DbType="INT")] ref int? ErrCode,
			[Parameter(Name="errDesc", DbType="VARCHAR(250)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				Certificate, IpAddress, CertCN, CardNumber, Pin, CardSeria, NewServiceNumber, NewStcSessionId, NewVruSessionId, CallCategoryId,
				Balance, ErrCode, ErrDesc);
			
			CardSeria = ((short)(result.GetParameterValue(5)));
			NewServiceNumber = ((int)(result.GetParameterValue(6)));
			NewStcSessionId = ((int)(result.GetParameterValue(7)));
			NewVruSessionId = ((int)(result.GetParameterValue(8)));
			CallCategoryId = ((int)(result.GetParameterValue(9)));
			Balance = ((decimal)(result.GetParameterValue(10)));
			ErrCode = ((int)(result.GetParameterValue(11)));
			ErrDesc = ((string)(result.GetParameterValue(12)));

			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Построение шаблона платежей
		/// </summary>
		/// <param name="OutTemplateTid"></param>
		/// <param name="VruSessionId"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="SAP2.dbo.ServerCard_BuildTemplate")]
		[return: Parameter(DbType="Int")]
		public int BuildTemplate(
			[Parameter(Name="out_template_tid", DbType="VARCHAR(250)")] string OutTemplateTid,
			[Parameter(Name="vru_session_id", DbType="INT")] int VruSessionId,
			[Parameter(Name="errCode", DbType="INT")] ref int? ErrCode,
			[Parameter(Name="errDesc", DbType="VARCHAR(250)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				OutTemplateTid, VruSessionId, ErrCode, ErrDesc);

			ErrCode = ((int)(result.GetParameterValue(2)));
			ErrDesc = ((string)(result.GetParameterValue(3)));

			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Сохранение поля в шаблоне
		/// </summary>
		/// <param name="TemplateTid"></param>
		/// <param name="VruSessionId"></param>
		/// <param name="ExtentId"></param>
		/// <param name="Value"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="SAP2.dbo.ServerCard_Save")]
		[return: Parameter(DbType="Int")]
		public int Save(
			[Parameter(Name="template_tid", DbType="INT")] int TemplateTid,
			[Parameter(Name="vru_session_id", DbType="INT")] int VruSessionId,
			[Parameter(Name="extent_id", DbType="VARCHAR(64)")] string ExtentId,
			[Parameter(Name="value", DbType="VARCHAR(4000)")] string Value,
			[Parameter(Name="errCode", DbType="INT")] ref int? ErrCode,
			[Parameter(Name="errDesc", DbType="VARCHAR(250)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				TemplateTid, VruSessionId, ExtentId, Value, ErrCode, ErrDesc);

			ErrCode = ((int)(result.GetParameterValue(4)));
			ErrDesc = ((string)(result.GetParameterValue(5)));

			return ((int)(result.ReturnValue));
			}


		/// <summary>
		/// Проведение/создание платежа
		/// </summary>
		/// <param name="OutTemplateTid"></param>
		/// <param name="VruSessionId"></param>
		/// <param name="StcSessionId"></param>
		/// <param name="ServiceNumber"></param>
		/// <param name="Mode"></param>
		/// <param name="OutTid"></param>
		/// <param name="OutTidPrefix"></param>
		/// <param name="NewServiceNumber"></param>
		/// <param name="NewState"></param>
		/// <param name="NewTid"></param>
		/// <param name="NewOutTid"></param>
		/// <param name="NewBalance"></param>
		/// <param name="NewAmount"></param>
		/// <param name="NewCommission"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="SAP2.dbo.ServerCard_MakePay")]
		[return: Parameter(DbType="Int")]
		public int MakePay(
			[Parameter(Name="out_template_tid", DbType="VARCHAR(250)")] string OutTemplateTid,
			[Parameter(Name="vru_session_id", DbType="INT")] int VruSessionId,
			[Parameter(Name="stc_session_id", DbType="INT")] int StcSessionId,
			[Parameter(Name="service_number", DbType="INT")] int? ServiceNumber,
			[Parameter(Name="mode", DbType="INT")] int? Mode,
			[Parameter(Name="out_tid", DbType="VARCHAR(250)")] string OutTid,
			[Parameter(Name="out_tid_prefix", DbType="VARCHAR(10)")] string OutTidPrefix,
			[Parameter(Name="new_service_number", DbType="INT")] ref int? NewServiceNumber,
			[Parameter(Name="new_state", DbType="INT")] ref int? NewState,
			[Parameter(Name="new_tid", DbType="INT")] ref int? NewTid,
			[Parameter(Name="new_out_tid", DbType="VARCHAR(250)")] ref string NewOutTid,
			[Parameter(Name="new_balance", DbType="DECIMAL(16,2)")] ref decimal? NewBalance,
			[Parameter(Name="new_amount", DbType="DECIMAL(16,2)")] ref decimal? NewAmount,
			[Parameter(Name="new_commission", DbType="DECIMAL(16,2)")] ref decimal? NewCommission,
			[Parameter(Name="errCode", DbType="INT")] ref int? ErrCode,
			[Parameter(Name="errDesc", DbType="VARCHAR(250)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
									OutTemplateTid,
									VruSessionId,
									StcSessionId,
									ServiceNumber,
									Mode,
									OutTid,
									OutTidPrefix,

									NewServiceNumber,
									NewState,
									NewTid,
									NewOutTid,
									NewBalance, NewAmount, NewCommission,
									ErrCode, ErrDesc);

			NewServiceNumber = ((int)(result.GetParameterValue(7)));
			NewState = ((int)(result.GetParameterValue(8)));
			NewTid = ((int)(result.GetParameterValue(9)));
			NewOutTid = ((string)(result.GetParameterValue(10)));
			NewBalance = ((decimal)(result.GetParameterValue(11)));
			NewAmount = ((decimal)(result.GetParameterValue(12)));
			NewCommission = ((decimal)(result.GetParameterValue(13)));
			ErrCode = ((int)(result.GetParameterValue(14)));
			ErrDesc = ((string)(result.GetParameterValue(15)));

			return ((int)(result.ReturnValue));
			}

		/// <summary>
		/// Чтение полей созданного платежа
		/// </summary>
		/// <param name="OutTemplateTid"></param>
		/// <param name="VruSessionId"></param>
		/// <param name="Tid"></param>
		/// <param name="ErrCode"></param>
		/// <param name="ErrDesc"></param>
		/// <returns></returns>
		[Function(Name="SAP2.dbo.ServerCard_Read")]
		public ISingleResult<ReadResult> Read(
			[Parameter(Name="out_template_tid", DbType="VARCHAR(250)")] string OutTemplateTid, 
			[Parameter(Name="vru_session_id", DbType="INT")] int VruSessionId, 
			[Parameter(Name="tid", DbType="INT")] int? Tid,
			[Parameter(Name="errCode", DbType="INT")] ref int? ErrCode,
			[Parameter(Name="errDesc", DbType="VARCHAR(250)")] ref string ErrDesc
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), 
				OutTemplateTid, VruSessionId, Tid,
				ErrCode, ErrDesc);
			return ((ISingleResult<ReadResult>)(result.ReturnValue));
			}

		/// <summary>
		/// Закрытие сессии
		/// </summary>
		/// <param name="VruSessionId"></param>
		[Function(Name="SAP2.dbo.ServerCard_LogOut")]
		public void LogOut([Parameter(Name="vru_session_id", DbType="INT")] int VruSessionId)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), VruSessionId);
			}
		}

	}
