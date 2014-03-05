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
        int errCode;
        string errDesc = "";
		bool disposed = false;
		GWRequest gw = null;
		GWRequest req = null;
		byte[] buffer;
        
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
				{
					if (gw != null)
					{
						gw.Dispose();
						gw = null;
					}
					if (req != null)
					{
						req.Dispose();
						req = null;
					}
					buffer = null;
				}
				
				// Освобождаем ресурсы выходного контекста
				if (m_data.Context != null && m_data.Context.Response != null)
				{
					m_data.Context.Response.Close();
					m_data = null;
				}

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
            req = new GWRequest();
            errCode = req.Parse(m_data.stRequest);
			if (errCode != 0)
            {
                Log("Ошибка разбора {0} {1}", req.errCode, req.errDesc);
				req.State = 12;
				req.ErrCode = 6;
                SendAnswer(m_data, req);
                return;
            }

           
			
			// Запустим цикл выполнения запроса:
            // Check -->
            // Pay -->
            // и если необходимо Status.

			switch (req.RequestType.ToLower())
            {
                case "check":
					// req.ReportRequest();
					switch (req.Provider)
					{
						case "as":
							gw = new Autoshow.Autoshow(req);
							break;
						case "cyber":
							gw = new GWCyberRequest(req);
							break;
						case "rt":
							gw = new RTRequest(req);
							break;
						// case "pcc":
						//	gw = new GWPccRequest(req, Settings.Pcc.Certname);
						//	gw = null;
						//	break;
						default:
							gw = req;
							Log("Неизвестный провайдер {0}", req.Provider);
							req.ErrCode = 6;
							req.ErrDesc = Properties.Resources.MsgURP;
							req.State = 12;
							break;
					}
					gw.Check();
					if (gw.State != 1 && gw.State != 6) gw.ReportRequest();
					SendAnswer(m_data, gw);
					return;

				case "status":
					// Прочитать из БД информацию о запросе
					try
					{
						if (req.Provider == "rt")
						{
							gw = new RTRequest(req);
							Log("{0} Запрос состояния платежа/счёта в ЕСПП", gw.Tid);
							// gw = new RTRequest(req);
							gw.GetPaymentStatus();
							gw.UpdateState(gw.Tid, gw.State, gw.ErrCode, gw.ErrDesc);
							// gw.ReportRequest("status".ToUpper());
						}
						else
						{
							gw = req;
							req.GetState();
							errCode = req.errCode;
							errDesc = req.errDesc;
							if (req.State == 255)
							{
								Log("tid={0} Status: Не найден платёж -- перепроводим", req.Tid);
								req.State = 0;
								req.errCode = 7;
								req.errDesc = string.Format("Tid={0}. Платёж не найден. Перепроведение", req.Tid);
							}
						}
					}
					catch (Exception ex)
					{
						Log("tid={0} Status: {1}", req.Tid, ex.Message);
						req.State = 3;
						req.errCode = 11;
					}
                    break;

				case "getpaymentsstatus":
					try
					{
						gw = new RTRequest(req);
						Log("\r\n{0} Запрос состояния платежей в ЕСПП", gw.Tid);
						gw = new RTRequest(req);
						gw.GetPaymentStatus();
						gw.UpdateState(gw.Tid, gw.State, gw.ErrCode, gw.ErrDesc);
					}
					catch (Exception ex)
					{
						Log("tid={0} Status: {1}", req.Tid, ex.Message);
						req.State = 3;
						req.errCode = 11;
					}
					break;
				
				// Отмена платежа
				case "undo":
					if (req.Provider == "rt")
					{
						gw = new RTRequest(req);
						gw.Undo();
						gw.ReportRequest("UNDO");
					}
					else
					{
						req.State = 12;
						req.errCode = 6;
						req.errDesc = "Платёж отменён оператором";
						req.UpdateState(tid: req.Tid, state: req.State, errCode: req.ErrCode, errDesc: req.ErrDesc);
						// SendAnswer(m_data, req);
						req.ReportRequest("UNDO");
					}
					break;

				case "payment":
					// Проверим наличие платежа и его статус.
					// gw = new GWRequest(req);

					// req.ReportRequest();
					// req.GetState();

					try
					{
						req.GetState();
						// req.ReportRequest();

						if (req.Provider == "rt") // Даже если проведён
						{
							gw = new RTRequest(req);
							gw.Processing(true);
						}
						else if (req.State == 255) // Платёж не существует
						{
							// Для начала определимя с провайдером:
							switch (req.Provider)
							{
								case "as":
									gw = new Autoshow.Autoshow(req);
									break;
								case "ekt":
									gw = new GWEktRequest(req);
									break;
								case "cyber":
									gw = new GWCyberRequest(req);
									break;
								case "pcc":
									// gw = new GWPccRequest(req, Settings.Pcc.Certname);
									gw = null;
									break;
								case "mts":
									gw = new GWMtsRequest(req);
									break;
								case "smtp":
									gw = new Oldi.Smtp.Smtp(req);
									break;
								default:
									gw = req;
									Log("Неизвестный провайдер {0}", req.Provider);
									req.ErrCode = 6;
									req.ErrDesc = Properties.Resources.MsgURP;
									req.State = 12;
									break;
							}
							// Проверка возможности проведения платежа
							// И если возможность есть -- првести его
							gw.Processing(true);
						}
						else if (req.State == 11 /* || req.State == 12*/ ) // Платёж отложен, его надо толкнуть. Только отложен!!! Для перепроведения используйте новый тид!!!
						{
							// Создать новый запрос
							// if (req.Tid != -1 && req.Amount != decimal.MinusOne)
							// {
							// Log("Tid={0} st={1} code={2} {3} {4} {5} - перепроведение", req.Tid, req.State, req.Lastcode, req.Lastdesc, req.Amount, req.Phone);
							// req.ReMakePayment();
							// Для начала определимя с провайдером:
							switch (req.Provider)
							{
								case "ekt":
									gw = new GWEktRequest(req);
									break;
								case "cyber":
									gw = new GWCyberRequest(req);
									break;
								case "pcc":
									// gw = new GWPccRequest(req, Settings.Pcc.Certname);
									gw = null;
									break;
								case "mts":
									gw = new GWMtsRequest(req);
									break;
								default:
									gw = req;
									Log("Неизвестный провайдер {0}", req.Provider);
									req.ErrCode = 6;
									req.ErrDesc = Properties.Resources.MsgURP;
									req.State = 12;
									break;
							}

							// gw.ReportRequest();	
							gw.Processing(0, 1, "Допроведение отложенного/отмененного платежа");
							Log("Tid={0} [Допроведение] st={1} code={2} {3} {4} {5}", gw.Tid, gw.State, gw.ErrCode, gw.ErrDesc, gw.Amount, gw.Phone);
							// }
						}
						else // Найден вернём статус
							gw = req;
					}
					catch (Exception ex)
					{
						Log("[Payment]: Ошибка шлюза {0}\r\n{1}", ex.Message, ex.StackTrace);
						m_data.stResponse = string.Format(Properties.Settings.Default.FailResponse, 11, string.Format("[Payment]: Ошибка шлюза {0}", ex.Message));
						// req.State = 0;
						// req.ErrCode = 11;
						// req.UpdateState(req.Tid, 12, locked: 0);
						SendAnswer(m_data);
						return;
					}
					finally
					{
						if (gw != null) gw.SetLock(0);
					}
                    break;

				default:
					if (gw != null)
						gw.ReportRequest("Неверный запрос");
					else if (req != null)
						req.ReportRequest("Неверный запрос");
					else
						Log("Неверный запрос\r\n{0}", m_data.stRequest);
					m_data.stResponse = string.Format(Properties.Settings.Default.FailResponse, 6, "Неверный запрос");
					SendAnswer(m_data);
					
					return;
            }


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
			if (string.IsNullOrEmpty(r.ErrDesc))
				errDesc = HttpUtility.HtmlEncode(r.ErrDesc);

			try
            {
				if (r.Provider != Settings.Rt.Name)
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
						errCode = r.Times > 50 ? 12 : r.Times > 10 ? 11 : 1;
						errDesc = r.Price != decimal.MinusOne ?
							string.Format("{0} {1} {2}", errDesc, Properties.Resources.MsgWaitPrice, r.Price) :
							errDesc;
						stResponse = string.Format(Properties.Settings.Default.FailResponse, errCode, errDesc);
					}
				}

                // Создаем ответ
				string answer = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n{1}",
					dataHolder.ClientEncoding.WebName, stResponse);

				buffer = dataHolder.ClientEncoding.GetBytes(answer);
                dataHolder.Context.Response.ContentLength64 = buffer.Length;

				if (Settings.LogLevel.IndexOf("OEREQ") != -1)
					Log(Properties.Resources.MsgResponseGW, answer);

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
				buffer = dataHolder.ClientEncoding.GetBytes(answer);
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
