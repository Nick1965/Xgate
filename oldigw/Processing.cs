using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Oldi.Utility;
using Oldi.Mts;
using Oldi.Net;
using Oldi.Ekt;
using Oldi.Smtp;
using Oldi.Net.Cyber;
using RT;
using Autoshow;
using System.Web;

namespace Oldi.Net
{
    public class Processing: IDisposable
    {
        RequestInfo m_data;
        // int errCode;
        // string errDesc = "";
		bool disposed = false;
		// GWRequest gw = null;
		// GWRequest req = null;
		// byte[] buffer;
        
        public Processing(RequestInfo dataHolder, string logFile)
        {
            m_data = dataHolder;
            m_data.LogFile = logFile;
        }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
					m_data = null;
				disposed = true;
			}
		}

        /// <summary>
        /// Конвейер обработки запроса
        /// </summary>
		public void Run()
        {
        // m_data.stRequest - содержит клиентский запрос
			// HttpListenerRequest request = m_data.Context.Request;

			if (m_data.Context.Request.HttpMethod.ToUpper() == "GET")
			{
				Log("REQ: {0}", m_data.Context.Request.RawUrl);
				foreach (string key in m_data.Context.Request.QueryString.AllKeys)
					Log("{0}: {1}", key, m_data.Context.Request.QueryString[key]);
				m_data.stResponse = "<XML>\r\n\t<Result>OK</Result>\r\n\r\n\t<Remark>OK</Remark>\r\n</XML>";
				SendAnswer(m_data);
				return;
			}
			
			// Разбор входного запроса. 0 - запрос разобран.
            GWRequest Request = new GWRequest();
			GWRequest Current = Request;

			if (Request.Parse(m_data.stRequest) != 0)
            {
				Log(Messages.ParseError, Request.errCode, Request.errDesc);
                SendAnswer(m_data, Request);
                return;
            }

			// Запустим цикл выполнения запроса:
            // Check -->
            // Pay -->
            // и если необходимо Status.

			switch (Request.RequestType.ToLower())
            {
                case "check":
					// req.ReportRequest();
					switch (Request.Provider)
					{
						case "as":
							Current = new Autoshow.Autoshow(Request);
							break;
						case "cyber":
							Current = new GWCyberRequest(Request);
							break;
						case "rt":
							Current = new RTRequest(Request);
							break;
						default:
							Log(Messages.UnknownProvider, Request.Provider);
							Current.ErrCode = 6;
							Current.ErrDesc = string.Format(Messages.UnknownProvider, Request.Provider);
							Current.State = 12;
							break;
					}
					Current.Check();
					break;
					// if (gw.State != 1 && gw.State != 6) gw.ReportRequest();
					// SendAnswer(m_data, gw);
					// return;

				case "status":
					// Прочитать из БД информацию о запросе
					try
					{
						if (Request.Provider == "rt")
						{
							Current = new RTRequest(Request);
							Current.GetPaymentStatus();
							Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
							// gw.ReportRequest("status".ToUpper());
						}
						else
						{
							Current.GetState();
							if (Current.State == 255)
							{
								Log(string.Format(Messages.PayNotFound, Current.Tid));
								Current.State = 0;
								Current.errCode = 7;
								Current.errDesc = string.Format(Messages.PayNotFound, Current.Tid);
							}
						}
						Log(Messages.StatusRequest, Current.Tid, Current.ErrDesc);
					}
					catch (Exception ex)
					{
						Log(Messages.LogError, Request.Tid, ex.Message, ex.StackTrace);
						Request.errDesc = string.Format(Messages.ErrDesc, Request.Tid, ex.Message);
						Request.errCode = 11;
					}
                    break;

				case "getpaymentsstatus":
					try
					{
						Current = new RTRequest(Request);
						Log(Messages.StatusRequest, Current.Tid);
						Current.GetPaymentStatus();
						Current.UpdateState(Current.Tid, state :Current.State, errCode :Current.ErrCode, errDesc :Current.ErrDesc);
					}
					catch (Exception ex)
					{
						Log(Messages.LogError, Request.Tid, ex.Message, ex.StackTrace);
						Request.errDesc = string.Format(Messages.ErrDesc, Request.Tid, ex.Message);
						Request.errCode = 11;
					}
					break;
				
				// Отмена платежа
				// Отменяет платёж на шлюзе, затем в процессинге
				case "undo":
					Log("{0} [UNDO - начало]", Current.Tid);
					if (Request.Provider == "rt")
					{
						Current = new RTRequest(Request);
						Current.Undo();
					}
					else
					{
						Current.State = 12;
						Current.errCode = 6;
						Current.errDesc = Messages.ManualUndo;
					}
					Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
					break;

				// Создание и попытка проведения нового платежа
				case "payment":
					// Проверим наличие платежа и его статус.

					try
					{
						Request.GetState();

						// Если платёж не существует (state == 255)
						if (Request.State == 255)
						{
							Request.State = 0;
							// Для начала определимя с провайдером:
							switch (Request.Provider)
							{
								case "rt":
									Current = new RTRequest(Request);
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
								case "smtp":
									Current = new Oldi.Smtp.Smtp(Request);
									break;
								default:
									Log(Messages.UnknownProvider, Request.Provider);
									Request.ErrCode = 6;
									Request.State = 12;
									Request.ErrDesc = string.Format(Messages.UnknownProvider, Request.Provider);
									Current.UpdateState(Current.Tid, state :Current.State, errCode :Current.ErrCode, errDesc :Current.ErrDesc);
									break;
							}
							// Если статус равен 0
							// И если возможность есть -- првести его
							if (Current.State == 0)
								Current.Processing(true);
						}
						// Платёж существует - вернём его статус
					}
					catch (Exception ex)
					{
						Current.errDesc = string.Format(Messages.InternalPaymentError, ex.Message);
						Log("{0}\r\n{1}", Current.errDesc, ex.StackTrace);
						Current.UpdateState(Current.Tid, state :Current.State, errCode :Current.ErrCode, errDesc :Current.ErrDesc);
						// m_data.stResponse = string.Format(Properties.Settings.Default.FailResponse, 11, errDesc);
						// SendAnswer(m_data);
						// return;
					}
					finally
					{
						if (Current != null) Current.SetLock(0);
					}
                    break;

				// Перепроведение
				case "reposting":
					// Проверим наличие платежа и его статус.
					// gw = new GWRequest(req);

					// req.ReportRequest();
					// req.GetState();

					try
						{
						Request.GetState();

						// Перепроводим только платежи в 11 и 12-м статусе
						if (Request.State == 11 || Request.State == 12)
							{
							string x = null;
							byte old_state = Request.State;

							x = !string.IsNullOrEmpty(Request.Phone)? Request.Phone: 
								!string.IsNullOrEmpty(Request.Account)? Request.Account: 
								!string.IsNullOrEmpty(Request.Number)? Request.Number: "";

							Log("Tid={0} [REPOST - начало] state={1} Num={2} S={3} A={4} err={5} {6}",
								Request.Tid, Request.State, x, XConvert.AsAmount(Request.AmountAll), XConvert.AsAmount(Request.Amount), Request.ErrCode, Request.ErrDesc);

							// Т.к. запрос частично затёрт вызовом GetSate() - сделаем новый разбор
							// Разбор входного запроса. 0 - запрос разобран.
							Request.Dispose();
							Request = new GWRequest();
							// Параметры заполнены на основе входного запроса
							Request.Parse(m_data.stRequest);

							// Для начала определимя с провайдером:
							switch (Request.Provider)
								{
								case "rt":
									Current = new RTRequest(Request);
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
							Current.Processing(old_state, 1, "Перепроведение платежа");

							x = !string.IsNullOrEmpty(Current.Phone)? Current.Phone: 
								!string.IsNullOrEmpty(Current.Account)? Current.Account: 
								!string.IsNullOrEmpty(Current.Number)? Current.Number: "";

							Log("Tid={0} [REPOST - конец] st={1} Num={2} S={3} A={4} err={5} {6}",
								Current.Tid, Current.State, x, XConvert.AsAmount(Current.AmountAll), XConvert.AsAmount(Current.Amount), Current.ErrCode, Current.ErrDesc);
							}
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
					break;

				default:
					Request.errDesc = string.Format(Messages.UnknownRequest, Request.RequestType);
					Log(Messages.UnknownRequest, m_data.stRequest);
					break;
					// m_data.stResponse = string.Format(Properties.Settings.Default.FailResponse, 6, "Неверный запрос");
					// SendAnswer(m_data);
					// return;
            }


			if (Request.RequestType.ToLower() != "status")
				Current.ReportRequest();
			SendAnswer(m_data, Current);
			/*
			if (gw == null)
			{
				req.ReportRequest("cancel");
				SendAnswer(m_data);
			}
			else
			{
				if (gw.RequestType.ToLower() != "status")
					gw.ReportRequest();
				SendAnswer(m_data, gw);
			}
			*/
        }

		/// <summary>
        /// Отпарвляет ответ OE
        /// </summary>
        /// <param name="dataHolder">Контекст запроса OE</param>
        /// <param name="r">Платёж</param>
        private  void SendAnswer(RequestInfo dataHolder, GWRequest r)
        {

			if (r == null)
			{
				SendAnswer(dataHolder);
				return;
			}

			string stResponse = r.Answer;
			string errDesc = !string.IsNullOrEmpty(r.ErrDesc)? HttpUtility.HtmlEncode(r.ErrDesc): "";

			try
            {
				if (r.Provider != Settings.Rt.Name) // RT передаёт уже заполненнвй Answer
				{
					if (r.State == 6)
					{
						// stResponse = string.Format(Properties.Settings.Default.Response, 3, gw.ErrDesc, gw.Outtid, gw.Acceptdate, gw.AcceptCode, gw.Account, gw.AddInfo);
						int pos = 0;
						string addInfo = r.AddInfo ?? "";
						if (r.Provider == Settings.Mts.Name)
						{
							addInfo = string.Format("{0} {1} {2} Limmit={3}", r.Fio, r.Opname, r.Opcode, XConvert.AsAmount(r.Limit));
						}
						else
						{
							if (addInfo.Length > 250)
							{
								pos = addInfo.IndexOf(";");
								if (pos > 0)
									addInfo = addInfo.Substring(pos + 2);
								if (addInfo.Length > 250)
									addInfo = addInfo.Substring(0, 250);
							}
						}
						stResponse = string.Format(Properties.Settings.Default.Response, 3, errDesc,
							r.Outtid, r.Acceptdate, r.AcceptCode, r.Account, addInfo, XConvert.AsAmount(r.Price));
						// errDesc = r.ErrDesc;
					}
					else if (r.State == 12)
					{
						stResponse = string.Format(Properties.Settings.Default.FailResponse, 6, errDesc);
					}
					else if (r.State == 0 && r.ErrCode == 7) // Передача управляющих кодов 7
					{
						stResponse = string.Format(Properties.Settings.Default.FailResponse, r.ErrCode, errDesc);
					}
					else if (r.ErrCode == 11 || r.ErrCode == 12) // Передача управляющих кодов 11, 12
					{
						stResponse = string.Format(Properties.Settings.Default.FailResponse, r.ErrCode, errDesc);
					}
					else if (r.State == 11) // Отложен
					{
						stResponse = string.Format(Properties.Settings.Default.FailResponse, 2, errDesc);
					}
					else
					{
						stResponse = string.Format(Properties.Settings.Default.FailResponse, r.Times > 50 ? 12 : r.Times > 10 ? 11 : 1,
							r.Price > 0M ? string.Format("{0} \"{1} {2}\"", errDesc, Messages.SumWait, XConvert.AsAmount(r.Price)): errDesc);
					}
				}

                // Создаем ответ
				string answer = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n{1}",
					dataHolder.ClientEncoding.WebName, stResponse);

				byte[] buffer = dataHolder.ClientEncoding.GetBytes(answer);
                dataHolder.Context.Response.ContentLength64 = buffer.Length;

				if (Settings.LogLevel.IndexOf("OEREQ") != -1)
					Log(Properties.Resources.MsgResponseGW, stResponse);

				// Utility.Log("tid={0}. Ответ MTS-GATE --> OE\r\n{1}", tid, stResponse);
					System.IO.Stream output = dataHolder.Context.Response.OutputStream;
					output.Write(buffer, 0, buffer.Length);
            }
            catch (WebException we)
            {
                Log("[{0}]: Tid={1}, ({2}){3}", r.RequestType, r.Tid, Convert.ToInt32(we.Status) + 10000, we.Message);
            }
            catch (Exception ex)
            {
                Log("[{0}]: Tid={1}, {2}\r\n{3}", r.RequestType, r.Tid, ex.Message, ex.StackTrace);
            }

        } // makeResponse

		/// <summary>
		/// Отправка ответа без параметров (может его использовать в дальнейшем?
		/// </summary>
		/// <param name="dataHolder"></param>
		private void SendAnswer(RequestInfo dataHolder)
		{

			string stResponse = m_data.stResponse;

			try
			{
				// Создаем ответ
				if (string.IsNullOrEmpty(stResponse))
					stResponse = "<response><error code=\"6\">Нет ответа</error></response>";
				string answer = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n{1}",
					dataHolder.ClientEncoding.WebName, stResponse);
				if (Settings.LogLevel.IndexOf("REQ") != -1)
					Log("Подготовлен ответ: \r\n{0}", answer);
				byte[] buffer = dataHolder.ClientEncoding.GetBytes(answer);
				dataHolder.Context.Response.ContentLength64 = buffer.Length;
				dataHolder.Context.Response.OutputStream.Write(buffer, 0, buffer.Length);
			}
			catch (WebException we)
			{
				Log("({0}){1}", Convert.ToInt32(we.Status) + 10000, we.Message);
			}
			catch (Exception ex)
			{
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				Console.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}

		} // makeResponse
		
		/// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        void Log(string fmt, params object[] _params)
        {
            Utility.Log(m_data.LogFile, fmt, _params);
        }
    
    }
}
