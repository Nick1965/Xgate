using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Oldi.Net
{
	class GetRequest: GWRequest
	{
		public GetRequest()
		{
			InitializeComponents();
		}

		protected override string GetLogName()
		{
			return "c:\\oldigw\\log\frida.log";
		}

		public override void InitializeComponents()
		{
			CodePage = "1251";
			base.InitializeComponents();
		}

		public void Run(RequestInfo m_data)
		{

			HttpListenerContext m_context = m_data.Context;
	
			foreach (String s in m_context.Request.QueryString.AllKeys)
				Log("{0}: {1}", s, m_context.Request.QueryString[s]);

			/*
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["account".ToLower()]))
				account = m_context.Request.QueryString["account".ToLower()];
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["date".ToLower()]))
				pcdate = Convert.ToDateTime(m_context.Request.QueryString["date".ToLower()]);
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["id".ToLower()]))
				number = m_context.Request.QueryString["id".ToLower()];
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["sum".ToLower()]))
				amount = Convert.ToDecimal(m_context.Request.QueryString["sum".ToLower()]);
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["total_sum".ToLower()]))
				amountAll = Convert.ToDecimal(m_context.Request.QueryString["total_sum".ToLower()]);
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["testMode".ToLower()]))
				testMode = m_context.Request.QueryString["testMode".ToLower()];
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["type".ToLower()]))
				requestType = m_context.Request.QueryString["type".ToLower()];
			if (!string.IsNullOrEmpty(m_context.Request.QueryString["sign".ToLower()]))
				controlCode = m_context.Request.QueryString["sign".ToLower()];
			Log("account={0} date={1} id={2} sum={3} total={4} testMode={5} type={6}\r\n{7}",
				Account, Pcdate, Number, Amount, AmountAll, TestMode, RequestType, ControlCode);
			*/

			SendAnswer(m_data, this);

		}
		// Отправить ответ клиенту
		
		private void SendAnswer(RequestInfo dataHolder, GWRequest gw)
		{

			string stResponse = "<XML><Result>OK</Result></XML>";

			try
			{
				// Создаем ответ
				byte[] buffer = dataHolder.ClientEncoding.GetBytes(dataHolder.ClientEncoding.CodePage == 1251 ?
					Properties.Settings.Default.XmlHeader1251 + "\r\n" + stResponse :
					Properties.Settings.Default.XmlHeader + "\r\n" + stResponse);
				dataHolder.Context.Response.ContentLength64 = buffer.Length;

				// Utility.Log("tid={0}. Ответ MTS-GATE --> OE\r\n{1}", tid, stResponse);
				System.IO.Stream output = dataHolder.Context.Response.OutputStream;
				output.Write(buffer, 0, buffer.Length);
			}
			catch (WebException we)
			{
				Log("[{0}]: Tid={1}, ({2}){3}", gw.RequestType, gw.Tid, Convert.ToInt32(we.Status) + 10000, we.Message);
			}
			catch (Exception ex)
			{
				Log("[{0}]: Tid={1}, {2}\r\n{3}", gw.RequestType, gw.Tid, ex.Message, ex.StackTrace);
			}

		} // makeResponse

	}
}
