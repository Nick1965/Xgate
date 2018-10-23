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

            // Разбор входного запроса. 0 - запрос разобран.
            GWRequest Request = new GWRequest();
            GWRequest Current = Request;
            string step = "";
            bool ValidProvider = true;

            try
            {
                if (Request?.Parse(m_data.stRequest) == 0)
                {
                    // Запустим цикл выполнения запроса:
                    // Check -->
                    // Pay -->
                    // и если необходимо Status.

                    // Log($"REQ: Provider=\"{Request.Provider}\" Request=\"{Request.RequestType}\" Service=\"{Request.Service}\"");

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
                        case "boriska":
                            Current = new ServerGate(Request);
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
                        case "xsolla":
                            Current = new GWXsolllaRequest(Request);
                            break;
                        case "school":
                            Current = new SchoolGateway.SchoolGatewayClass (Request);
                            break;
                        // case "smtp":
                        //    Current = new Oldi.Smtp.Smtp(Request);
                        //    break;
                        default:
                            // Log(Messages.UnknownProvider, Request.Provider);
                            if (Request.RequestType.ToLower() != "status")
                            {
                                Current.ErrCode = 6;
                                Current.State = 12;
                                Current.ErrDesc = string.Format(Messages.UnknownProvider, Request.Provider);
                                Log($"Processing.Run(): Unknown provider \"{Request.Provider}\"");
                                Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
                                ValidProvider = false;
                            }
                            break;
                    }


                    if (ValidProvider)
                        switch (Request.RequestType.ToLower())
                        {
                            case "check":
                                Current.ReportRequest("CHCK - strt");
                                step = "CHCK - stop";
                                Current.Check();
                                break;

                            case "status":
                                // Прочитать из БД информацию о запросе
                                // Request.ReportRequest("STATUS - начало");
                                // step = "STAT - stop";
                                if (Request.Provider == "rt" || Request.Provider == "rtm")
                                {
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
                                break;

                            case "getpaymentsstatus":
                                Current = new RTRequest(Request);
                                Log(Messages.StatusRequest, Current.Tid);
                                Current.GetPaymentStatus();
                                Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
                                break;

                            // Отмена платежа
                            // Отменяет платёж на шлюзе, затем в процессинге
                            case "undo":
                                Current.GetPaymentInfo();
                                Current.ReportRequest("UNDO - strt");
                                step = "UNDO - stop";
                                if (Current.Provider == "rt" || Current.Provider == "rtm")
                                    Current.Undo();
                                else
                                {
                                    Current.ErrCode = 6;
                                    Current.State = 12;
                                    Current.ErrDesc = string.Format(Messages.ManualUndo, Request.Provider);
                                    Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
                                }
                                break;

                            // Создание и попытка проведения нового платежа
                            case "payment":
                                // Проверим наличие платежа и его статус.
                                Current.GetState();

                                // Log($"{Current.Tid} [PAYM strt] Status={Current.State}");
                                
                                // Если платёж не существует (state == 255)
                                if (Current.State == 255)
                                {
                                    Current.State = 0;
                                    Current.GetTerminalInfo();
                                    Current.ReportRequest("PAYM - strt");
                                    step = "PAYM - stop";

                                    // Поиск дублей

                                    int Doubles = 0;
                                    // Если sub_inner_tid содержит 3 '-' возвращает непустую строку
                                    // string SubInnertid = Current.GetGorodSub();

                                    // Искать задвоенные платежи по параметрам:
                                    // Point_oid
                                    // Template_tid
                                    // User_id
                                    // Account
                                    // Amount, Commission, Summary_amount
                                    // if (!string.IsNullOrEmpty(SubInnertid) && (Doubles = Current.GetDoubles(SubInnertid)) > 0)
                                    if (Current.GetDoubles() > 0)
                                    {
                                        Log($"{Current.Tid} [Check doubles] Для acc={Current.ID()} найдено {Doubles} дублей");
                                        Current.State = 12;
                                        Current.errCode = 6;
                                        Current.errDesc = $"Найдено {Doubles} подобных платежей в пределах 10 часов. Платёж отменяется.";
                                        Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
                                    }

                                    // if (!string.IsNullOrEmpty(SubInnertid))
                                    //	Log("{0} [DOUB - stop] {1}", Request.Tid, Request.ErrDesc);

                                    // Если статус равен 0
                                    // И если возможность есть -- провести его
                                    if (Current.State == 0)
                                        Current.Processing(true);
                                }
                                // Платёж существует - вернём его статус
                                else if (Current.Provider == "rt" || Current.Provider == "rtm")
                                {
                                    Current.GetPaymentStatus();
                                    Current.UpdateState(Current.Tid, state: Current.State, errCode: Current.ErrCode, errDesc: Current.ErrDesc);
                                    // gw.ReportRequest("status".ToUpper());
                                }
                                else if (/* Request.State == 12 || */ Request.State == 11)
                                {
                                    Current = Reposting(Current);
                                    step = "REPT - stop";
                                }
                                else
                                    step = "STAT - stop";
                                break;

                            // Перепроведение
                            // case "reposting":
                            //	break;

                            default:
                                Current.errDesc = $"Неизвестный запрос {Request.RequestType}";
                                Log($"Неизвестный запрос \"{Request.RequestType}\" в {m_data.stRequest}");
                                break;
                                // m_data.stResponse = string.Format(Properties.Settings.Default.FailResponse, 6, "Неверный запрос");
                                // SendAnswer(m_data);
                                // return;
                        }
                }
                else
                {
                    Log(Messages.ParseError, Current.errCode, Current.errDesc);
                }

            }
            catch (Exception ex)
            {
                Current.errCode = 11;
                Current.errDesc = ex.Message;
                Log(ex.ToString());
            }
            finally
            {
                Current?.SetLock(0);
            }

			if (Current.RequestType.ToLower() != "status")
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
                if (r.Provider != Settings.Rt.Name && r.Provider != Settings.Rtm.Name && r.Provider != Settings.Rapida.Name && r.Gateway != "lyapko") // уже заполненнвй Answer
				{
					if (r.State == 6 || r.State == 0 && r.Provider == "rapida")
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
                    else if (r.State == 1) // 
                    {
                        stResponse = string.Format(Properties.Settings.Default.FailResponse, 2, errDesc);
                    }
                    else
                    {
						stResponse = string.Format(Properties.Settings.Default.FailResponse, 1,
							r.Price > 0M ? string.Format("{0} \"{1} {2}\"", errDesc, Messages.SumWait, XConvert.AsAmount(r.Price)): errDesc);
					}
				}

                if (string.IsNullOrEmpty(stResponse))
                {
                    stResponse = string.Format("<Response><ErrCode>{0}</ErrCode><ErrDesc>{1}</ErrDesc></Response>", r.ErrCode, r.ErrDesc);
                    Log("{0} не получен ответ st={1} err={2} desc={3}", r.Tid, r.State, r.ErrDesc, r.ErrCode);
                }

                if (r.Gateway == "lyapko")
                    Log(stResponse);
                
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
