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
	public partial class Processing: IDisposable
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
			string step = "";

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
					Request.ReportRequest("CHCK - strt");
					step = "CHCK - stop";
					switch (Request.Provider)
					{
						case "cyber":
							Current = new GWCyberRequest(Request);
							break;
						case "rt":
						case "rtm":
							Current = new RTRequest(Request);
							break;
						default:
							// Log(Messages.UnknownProvider, Request.Provider);
							Current.ErrCode = 6;
							Current.ErrDesc = string.Format(Messages.UnknownProvider, Request.Provider);
							Current.State = 12;
							break;
					}
					Current.Check();
					break;

				case "status":
					// Прочитать из БД информацию о запросе
					try
						{
						// Request.ReportRequest("STATUS - начало");
						// step = "STAT - stop";
						if (Request.Provider == "rt" || Request.Provider == "rtm")
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
								// Log(string.Format(Messages.PayNotFound, Current.Tid));
								Current.State = 12;
								Current.errCode = 11;
								Current.errDesc = string.Format(Messages.PayNotFound, Current.Tid);
							}
						}
						// Log(Messages.StatusRequest, Current.Tid, Current.ErrDesc);
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
					try
						{
						Request.GetPaymentInfo();
						Request.ReportRequest("UNDO - strt");
						step = "UNDO - stop";
						if (Request.Provider == "rt" || Request.Provider == "rtm")
							((RTRequest)Request).Undo();
						else
							{
							// Request.Undo();
							Request.ErrCode = 6;
							Request.State = 12;
							Request.ErrDesc = string.Format(Messages.ManualUndo, Request.Provider);
							Request.UpdateState(Current.Tid, state :Request.State, errCode :Request.ErrCode, errDesc :Request.ErrDesc);
							}
						}
					catch (Exception ex)
						{
						Log(Messages.LogError, Request.Tid, ex.Message, ex.StackTrace);
						Request.errDesc = string.Format(Messages.ErrDesc, Request.Tid, ex.Message);
						Request.errCode = 11;
						}

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
							Request.GetTerminalInfo();
							Request.ReportRequest("PAYM - strt");
							step = "PAYM - stop";

							// Поиск дублей

							int Doubles = 0;
							// Если sub_inner_tid содержит 3 '-' возвращает непустую строку
							string SubInnertid = Request.GetGorodSub();

							if (!string.IsNullOrEmpty(SubInnertid) && (Doubles = Request.GetDoubles(SubInnertid)) > 0)
								{
								Log("{0} [DOUB - step] Для sub_inner_tid={1} найдено {2} дублей", Request.Tid, SubInnertid, Doubles);
								Request.State = 12;
								Request.errCode = 6;
								Request.errDesc = string.Format("Найдено {0} подобных платежей в пределах 10 минут. Платёж отменяется.", Doubles);
								Request.UpdateState(Request.Tid, state :Request.State, errCode :Request.ErrCode, errDesc :Request.ErrDesc);
								}
							else
								{
								Log("{0} [DOUB - step] Для sub_inner_tid={1} дублей не найдено", Request.Tid, SubInnertid);
								
								// Для начала определимся с провайдером:
								switch (Request.Provider)
									{
									case "rt":
									case "rtm":
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
                                    case "rapida":
                                        Current = new GWRapidaRequest(Request);
                                        break;
									case "smtp":
										Current = new Oldi.Smtp.Smtp(Request);
										break;
									default:
										// Log(Messages.UnknownProvider, Request.Provider);
										Request.ErrCode = 6;
										Request.State = 12;
										Request.ErrDesc = string.Format(Messages.UnknownProvider, Request.Provider);
										Current.UpdateState(Current.Tid, state :Current.State, errCode :Current.ErrCode, errDesc :Current.ErrDesc);
										break;
									}
								}

							// if (!string.IsNullOrEmpty(SubInnertid))
							//	Log("{0} [DOUB - stop] {1}", Request.Tid, Request.ErrDesc);

							// Если статус равен 0
							// И если возможность есть -- провести его
							if (Current.State == 0)
								Current.Processing(true);
							}
						// Платёж существует - вернём его статус
						else if (Request.Provider == "rt" || Request.Provider == "rtm")
							{
							Current = new RTRequest(Request);
							Current.GetPaymentStatus();
							Current.UpdateState(Current.Tid, state :Current.State, errCode :Current.ErrCode, errDesc :Current.ErrDesc);
							// gw.ReportRequest("status".ToUpper());
							}
						else if (/* Request.State == 12 || */ Request.State == 11)
							{
							Current = Reposting(Request);
							step = "REPT - stop";
							}
						else
							step = "STAT - stop";
							
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
				// case "reposting":
				//	break;

				default:
					Request.errDesc = string.Format(Messages.UnknownRequest, Request.RequestType);
					// Log(Messages.UnknownRequest, m_data.stRequest);
					break;
					// m_data.stResponse = string.Format(Properties.Settings.Default.FailResponse, 6, "Неверный запрос");
					// SendAnswer(m_data);
					// return;
            }


			if (Request.RequestType.ToLower() != "status")
				Current.ReportRequest(step);
			SendAnswer(m_data, Current);
			Interlocked.Decrement(ref GWListener.processes);
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
				if (r.Provider != Settings.Rt.Name && r.Provider != Settings.Rtm.Name && r.Provider != Settings.Rapida.Name) // RT передаёт уже заполненнвй Answer
				{
					if (r.State == 6)
					{
						// stResponse = string.Format(Properties.Settings.Default.Response, 3, gw.ErrDesc, gw.Outtid, gw.Acceptdate, gw.AcceptCode, gw.Account, gw.AddInfo);
						int pos = 0;
						string addInfo = r.AddInfo ?? "";
						if (r.Provider == Settings.Mts.Name)
						{
							// addInfo = string.Format("{0} {1} {2} Limmit={3}", r.Fio, r.Opname, r.Opcode, XConvert.AsAmount(r.Limit));
							addInfo = string.Format("{0} {1} {2}", r.Fio, r.Opname, r.Opcode);
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
						stResponse = string.Format(Properties.Settings.Default.FailResponse, 1,
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
