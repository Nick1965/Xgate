using Oldi.Ekt;
using Oldi.Mts;
using Oldi.Net.Cyber;
using Oldi.Utility;
using RT;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Oldi.Net
	{
	public partial class Processing: IDisposable
		{
		GWRequest Reposting(GWRequest Request)
			{
			GWRequest Current = Request;
			DateTime OperDate = DateTime.Now;

			try
				{
				string x = null;
				byte old_state = Request.State;

				x = !string.IsNullOrEmpty(Request.Phone)? Request.Phone: 
								!string.IsNullOrEmpty(Request.Account)? Request.Account: 
								!string.IsNullOrEmpty(Request.Number)? Request.Number: "";

				DateTime? LastAttempt = LastOperDate(Request.Tid);
				int last =  (int)((DateTime.Now.Ticks - LastAttempt.Value.Ticks) / TimeSpan.TicksPerSecond); 
				Log("{0} [REPT - strt] last={1} st={2} Num={3} S={4} A={5} err={6} {7}",
					Request.Tid, last, Request.State, x, XConvert.AsAmount(Request.AmountAll), XConvert.AsAmount(Request.Amount), Request.ErrCode, Request.ErrDesc);
				
				// Если запрос отправлен менее минуты назат - перепровести
				if (last > 60)
					{
					// Т.к. запрос частично затёрт вызовом GetSate() - сделаем новый разбор
					// Разбор входного запроса. 0 - запрос разобран.
					Request.Dispose();
					Request = new GWRequest();
					
					// Параметры заполнены на основе входного запроса
					Request.Parse(m_data.stRequest);
					Request.State = 0;
					Request.errCode = 0;
					Request.errDesc = "Перепроведение платежа";
					Request.UpdatePayment();

					// Для начала определимя с провайдером:
					switch (Request.Provider)
						{
						case "rt":
							Current = new RTClass16(Request);
							break;
						case "ekt":
							Current = new GWEktRequest(Request);
							break;
						case "cyber":
							Current = new GWCyberRequest(Request);
							break;
						case "mts":
							Current = new GWMtsRequest(Request);
							break;
						}
					// Current.Processing(old_state, 1, "Перепроведение платежа");
					// Перепроведение
					Current.Processing(false); 

					x = !string.IsNullOrEmpty(Current.Phone)? Current.Phone: 
								!string.IsNullOrEmpty(Current.Account)? Current.Account: 
								!string.IsNullOrEmpty(Current.Number)? Current.Number: "";
					}
				else
					Log("{0} [REPT - stop] перепроведение не производится", Request.Tid);
				// Log("Tid={0} [REPOST - конец] st={1} Num={2} S={3} A={4} err={5} {6}",
				//	Current.Tid, Current.State, x, XConvert.AsAmount(Current.AmountAll), XConvert.AsAmount(Current.Amount), Current.ErrCode, Current.ErrDesc);
				}
			catch (Exception ex)
				{
				Current.errDesc = string.Format(Messages.InternalPaymentError, ex.Message);
				Log("{0}\r\n{1}", Current.errDesc, ex.StackTrace);
				Current.UpdateState(Current.Tid, state :Current.State, errCode :Current.ErrCode, errDesc :Current.ErrDesc);
				}
			finally
				{
				if (Current != null) Current.SetLock(0);
				}
			return Current;
			}

		DateTime? LastOperDate(long Tid)
			{
			DateTime? last = null;
			using (SqlParamCommand db = new SqlParamCommand(Settings.ConnectionString, "OLDIGW.ver3_GetPayInfo"))
				{
				db.AddParam("Tid", Tid);
				db.Fill();
				DataRow Row = db[0];
				if (Row != null && (int)Row["ErrCode"] == 0)
					last = (DateTime?)Row["LastAttempt"];
				}
			return last;
			}

		}
	}
