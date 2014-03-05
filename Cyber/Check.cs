using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using System.Globalization;

namespace Oldi.Net.Cyber
{
	/// <summary>
	///  Реализация метода DoCheck()
	/// </summary>
	public partial class GWCyberRequest : GWRequest
	{

		/// <summary>
		/// Проверяет возможность платежа
		/// </summary>
		/// <param name="OpenSession">false - сессия не открывается</param>
		/// <returns>
		/// 0 - платить по указанным реквизитам можно, Price содержит сумму платежа.
		/// != 0 - платёж невозможен
		/// </returns>
		/*
		public override void DoCheck(bool OpenSession = false)
		{

			// Создаём запрос
			MakeCheckRequest();
			Log("\r\nHost: {0}", CheckHost);
			// Отправляем провайдеру
			SendRequest(CheckHost);
			// Проверяем ответ
			ParseAnswer(stResponse);

			if (ErrCode != 0)
				errDesc = CyberError(ErrCode);

			// Код 7 вернёт требуемую сумму
			if ((errCode == 7 && Price > 0M) || errCode == 0)
				state = 6;
			else
				state = 12;

		}
		*/

		/// <summary>
		/// Создание запроса CHECK
		/// </summary>
		/// <returns></returns>
		public void MakeCheckRequest()
		{
			StringBuilder prm = new StringBuilder();
			string s_text = "";
			
			// Tid'a для операции не существует создадим номер сессии из времени
			string session = "OLDIGW" + string.Format("{0:D14}", DateTime.Now.Ticks).Substring(0, 14);
			if (Amount == decimal.MinusOne)
				amount = 100M;
			if (AmountAll == decimal.MinusOne)
				amountAll = 150M;

			prm.AppendLine("SD", SD);
			prm.AppendLine("AP", AP);
			prm.AppendLine("OP", OP);
			prm.AppendLine("SESSION", session);

			prm.AppendLine("NUMBER", number);
			prm.AppendLine("CARD", card);
			prm.AppendLine("ACCOUNT", account);
			prm.AppendLine("DOCNUM", docnum);
			prm.AppendLine("DOCDATE", docdate);
			prm.AppendLine("FIO", Fio);
			prm.AppendLine("Contact", Contact);
			prm.AppendLine("AMOUNT", Amount);
			prm.AppendLine("AMOUNT_ALL", AmountAll == decimal.MinusOne? 150M: AmountAll);
			prm.AppendLine("REQ_TYPE", "1");

			prm.AppendLine("ACCEPT_KEYS", BankSerial);

			IPriv.SignMessage(prm.ToString(), out stRequest, out s_text, SecretKey, Passwd);

#if __TEST
			TestString1 = s_text;
#endif

			if (Settings.LogLevel.IndexOf("REQ") != -1)
				Log("\r\nПодготовлен запрос:\r\n{0}\r\n", s_text);

			errCode = 0;
			errDesc = null;
		}

	
	}
}
