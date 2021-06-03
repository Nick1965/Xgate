using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Test
{
	public class RTTest: BaseClass
	{
		const string validNumber = "75554000001";
		const string lostNumber = "75554000002";
		const string delayNumber = "75554000013";
		const string cancelDelayNumber = "75554000016";
		const string badCurNumber = "75554000008";
		const string validAccount = "4000001";
		const string invalidAccount = "4000002";

		const string correctSubNum = "1";
		const string incorrectSubNum = "3";

		int SrcPayId = 0;
		// DateTime PayTime;
		DateTime ReqTime;

		const string HOST = "http://localhost:200/oldigw/";
		const string LOG = "test.log";

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public RTTest()
			: base(HOST, LOG, "1251")
		{
		}
		
		/// <summary>
		/// Выполнение теста
		/// </summary>
		public void Run()
		{
			Log("Тест шлюза ЕСПП - Ростелеком.");
			Console.WriteLine("Тест шлюза ЕСПП - Ростелеком.");
			
			/*
			4.1.1 Запрос по абонентскому номеру при наличии Абонента в АСР
				reqType = checkPaymentParams
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			 */
			Test(Comment: "4.1.1 Запрос по абонентскому номеру при наличии Абонента в АСР", RequestType: "check", Account: validNumber, Amount: 1m);

			/*
			4.1.2 Запрос по абонентскому номеру при отсутствии Абонента в АСР
				reqType = checkPaymentParams
				svcTypeId = 0
				svcNum = svcNum2
				svcSubNum = svcSubNum1
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			Test(Comment: "4.1.2 Запрос по абонентскому номеру при отсутствии Абонента в АСР", RequestType: "check", Account: lostNumber, Amount: 1m);

			/*
			4.1.3 Запрос по абонентскому номеру с указанием субсчета. Субсчет отсутствует в АСР (-12 ERROR_PAYEE_NOT_FOUND)
				reqType = checkPaymentParams
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum2
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			 */
			Test(Comment: "4.1.3 Запрос по абонентскому номеру с указанием субсчета.", RequestType: "check", Account: validNumber, SubNum: incorrectSubNum, Amount: 1m);

			/*
			4.1.4 Запрос при ошибке на стороне ЕСПП (2 ERROR_BAD_AMOUNT) 
				reqType = checkPaymentParams
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				payCurrId = RUB
				payAmount = 3000010
				payPurpose = payPurpose1
			*/
			Test(Comment: "4.1.4 Запрос при ошибке на стороне ЕСПП (2 ERROR_BAD_AMOUNT)", RequestType: "check", Account: validNumber, Amount: 30000.10m);

			/*
			4.1.5 Запрос по лицевому счету при наличии Абонента
				reqType = checkPaymentParams
				svcTypeId = svcTypeId1
				svcNum = svcNum66
				svcSubNum = svcSubNum1
				payCurrId = RUR
				payAmount = 100
				payPurpose = payPurpose1
			*/
			Test(Comment: "4.1.5 Запрос по лицевому счету при наличии Абонента", RequestType: "check", Filial: "RT.DV.25.ACCOUNTTEST", Account: validAccount, Amount: 1m);

			/*
			4.1.6 Запрос по лицевому счету при отсутствии Абонента (-12 ERROR_PAYEE_NOT_FOUND)
				reqType = checkPaymentParams
				svcTypeId = svcTypeId1
				svcNum = svcNum67
				svcSubNum = svcSubNum1
				payCurrId = RUR
				payAmount = 100
				payPurpose = payPurpose1
			*/
			Test(Comment: "4.1.6 Запрос по лицевому счету при отсутствии Абонента (-12 ERROR_PAYEE_NOT_FOUND)", RequestType: "check", Filial: "RT.DV.25.ACCOUNTTEST", Account: invalidAccount, Amount: 1m);

			/*
			4.2.1 Запрос при успешной обработке зачисления по абонентскому номеру на стороне ЕСПП (2 pay_STATUS_acceptED)
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.1 Запрос при успешной обработке зачисления по абонентскому номеру на стороне ЕСПП (2 pay_STATUS_acceptED)", 
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			/*
			4.2.2 апрос при отложеной обработке зачисления по абонентскому номеру на стороне ЕСПП (102 pay_STATUS_acceptING)
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.2 Запрос при отложеной обработке зачисления по абонентскому номеру на стороне ЕСПП (102 pay_STATUS_acceptING)", RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: delayNumber, Amount: 1m, ReqTime: DateTime.Now);

			/*
			4.2.3 Повторный запрос при успешной обработке зачисления на стороне ЕСПП (2 pay_STATUS_acceptED)
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.3 (Шаг - 1) Повторный запрос при успешной обработке зачисления на стороне ЕСПП (2 pay_STATUS_acceptED)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, Amount: 1m, ReqTime: DateTime.Now);
			Test(Comment: "4.2.3 (Шаг - 2) Повторный запрос при успешной обработке зачисления на стороне ЕСПП (2 pay_STATUS_acceptED)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, Amount: 1m, ReqTime: DateTime.Now);


			/*
			4.2.4 Запрос при отсутствии Абонента в АСР
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.4 Запрос при отсутствии Абонента в АСР", RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: lostNumber, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			/*
			4.2.5 Запрос по абонентскому номеру с указанием субсчета. Субсчет отсутствует в АСР
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.5 Запрос по абонентскому номеру с указанием субсчета. Субсчет отсутствует в АСР", RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, SubNum: incorrectSubNum, Amount: 1m, ReqTime: DateTime.Now);

			/*
			4.2.6 Запрос при ошибке на стороне ЕСПП (2 ERROR_BAD_AMOUNT)
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.6 Запрос при ошибке на стороне ЕСПП (2 ERROR_BAD_AMOUNT)", RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, SubNum: correctSubNum, Amount: 30000.1m, ReqTime: DateTime.Now);

			/*
			4.2.7 Запрос при успешной обработке зачисления по лицевому счету (2 pay_STATUS_acceptED)
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.7 Запрос при успешной обработке зачисления по лицевому счету (2 pay_STATUS_acceptED)", 
				RequestType: "payment", Tid: SrcPayId, Filial: "RT.DV.25.ACCOUNTTEST", Account: validAccount, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			/*
			4.2.8 Запрос по лицевому счету при отсутствии лицевого счета в АСР (-12 ERROR_PAYEE_NOT_FOUND)
				reqType = createPayment
				svcTypeId = 0
				svcNum = svcNum1
				svcSubNum = svcSubNum1
				srcPayId = 1342607121674
				payTime = 2012-09-11T16:00:00+06:00
				payCurrId = RUB
				payAmount = 100
				payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.2.8 Запрос по лицевому счету при отсутствии лицевого счета в АСР (-12 ERROR_PAYEE_NOT_FOUND)",
				RequestType: "payment", Tid: SrcPayId, Filial: "RT.DV.25.ACCOUNTTEST", Account: invalidAccount, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			/*
			4.3.1 Запрос на успешную отмену зачисления
			reqType = createPayment
			svcTypeId = 0
			svcNum = svcNum1
			svcSubNum = svcSubNum1
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.3.1 Запрос на успешную отмену зачисления (зачисление)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			// Test(Comment: "4.3.1 Запрос на успешную отмену зачисления (отмена)",
			//	RequestType: "undo", Tid: SrcPayId, ReqTime: DateTime.Now);

			/*
			4.3.2 Запрос на успешную отмену зачисления
			reqType = createPayment
			svcTypeId = 0
			svcNum = svcNum1
			svcSubNum = svcSubNum1
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/

			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			account = "";
			Test(Comment: "4.3.2 Запрос на успешную отмену зачисления (зачисление)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			Test(Comment: "4.3.2 Запрос на успешную отмену зачисления (отмена)",
				RequestType: "undo", Tid: SrcPayId, ReqTime: DateTime.Now);

			Test(Comment: "4.3.2 Запрос на успешную отмену зачисления (отмена2)",
				RequestType: "undo", Tid: SrcPayId, ReqTime: DateTime.Now);

			/*
			4.3.3 Запрос на отложенную отмену
			reqType = createPayment
			svcTypeId = 0
			svcNum = svcNum1
			svcSubNum = svcSubNum1
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/
			
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.3.3 Запрос на отложенную отмену (зачисление)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: "75554000016", SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			Test(Comment: "4.3.2 Запрос на отложенную отмену (отмена)",
				RequestType: "undo", Tid: SrcPayId, ReqTime: DateTime.Now);
			
			/*
			4.4.1 Запрос статуса успешного зачисления на стороне ЕСПП
			reqType = createPayment
			svcTypeId = 0
			svcNum = svcNum1
			svcSubNum = svcSubNum1
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/
			
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			Test(Comment: "4.4.1 Запрос статуса успешного зачисления на стороне ЕСПП (payment)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, SubNum: correctSubNum, Amount: 1m, ReqTime: DateTime.Now);

			Test(Comment: "4.4.1 Запрос статуса успешного зачисления на стороне ЕСПП (status)",
				RequestType: "status", Tid: SrcPayId, ReqTime: DateTime.Now);
			
			/*
			4.4.2 Запрос статуса зачисления в обработке на стороне ЕСПП
			reqType = createPayment
			svcTypeId = 0
			svcNum = svcNum3
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/
			
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			ReqTime = DateTime.Now;
			Test(Comment: "4.4.2 Запрос статуса зачисления в обработке на стороне ЕСПП",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: delayNumber, Amount: 1m, ReqTime: ReqTime);

			Test(Comment: "4.4.2 Запрос статуса зачисления в обработке на стороне ЕСПП (status)",
				RequestType: "status", Tid: SrcPayId, ReqTime: ReqTime);

			/*
			4.4.3 Запрос статуса успешно отмененного зачисления на стороне ЕСПП
			reqType = createPayment/abandon/status
			svcTypeId = 0
			svcNum = svcNum3
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/
			
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			ReqTime = DateTime.Now;
			Test(Comment: "4.4.3 Запрос статуса успешно отмененного зачисления на стороне ЕСПП (create)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: validNumber, Amount: 1m, ReqTime: ReqTime);

			Test(Comment: "4.4.3 Запрос статуса успешно отмененного зачисления на стороне ЕСПП (abandon)",
				RequestType: "undo", Tid: SrcPayId, ReqTime: ReqTime);

			Test(Comment: "4.4.3 Запрос статуса успешно отмененного зачисления на стороне ЕСПП (status)",
				RequestType: "status", Tid: SrcPayId, ReqTime: ReqTime);
			

			/*
			4.4.4 Запрос статуса отмененного зачисления на стороне ЕСПП. Отмена в обработке
			reqType = createPayment/abandon/status
			svcTypeId = 0
			svcNum = svcNum3
			srcPayId = 1342607121674
			payTime = 2012-09-11T16:00:00+06:00
			payCurrId = RUB
			payAmount = 100
			payPurpose = payPurpose1
			*/
			
			SrcPayId = (int)(DateTime.Now.Ticks & 0xFFFF);
			ReqTime = DateTime.Now;
			Test(Comment: "4.4.4 Запрос статуса отмененного зачисления на стороне ЕСПП. Отмена в обработке (create)",
				RequestType: "payment", Tid: SrcPayId, Filial: "0", Account: cancelDelayNumber, Amount: 1m, ReqTime: ReqTime);

			Test(Comment: "4.4.4 Запрос статуса отмененного зачисления на стороне ЕСПП. Отмена в обработке (abandon)",
				RequestType: "undo", Tid: SrcPayId, ReqTime: ReqTime);

			Test(Comment: "4.4.4 Запрос статуса отмененного зачисления на стороне ЕСПП. Отмена в обработке (status)",
				RequestType: "status", Tid: SrcPayId, ReqTime: ReqTime);

			/*
			4.5.1 Запрос при незаданных критериях отбора
			reqType = getPaymentStatus
			*/
			Test(Comment: "4.5.1 Запрос при незаданных критериях отбора", RequestType: "getPaymentsStatus");

			statusType = 1;
			Test(Comment: "4.5.2 Запрос при заданных критериях отбора",
				RequestType: "getPaymentsStatus", Status: statusType);

			DateTime dt;
			DateTime.TryParse("2014-01-17T00:00+07:00", out dt);
			 Test(Comment: "4.5.2 Запрос при заданных критериях отбора", RequestType: "getPaymentsStatus", StartDate: dt);

			DateTime.TryParse("2014-01-20T23:59:59+07:00", out dt);
			 Test(Comment: "4.5.2 Запрос при заданных критериях отбора", RequestType: "getPaymentsStatus", EndDate: dt);

			 // queryFlags = 0;
			
			Test(Comment: "4.6.1 Запрос с полем флаг равным 0 в запросе при наличии абонента", 
				RequestType: "status", Account: validNumber, Filial: "0", Number: "0");
			Test(Comment: "4.6.1 Запрос с полем флаг равным 0 в запросе при наличии абонента",
				RequestType: "status", Account: validNumber, SubNum: correctSubNum, Filial: "0", Number: "0");
			Test(Comment: "4.6.2 Запрос с полем флаг равным 0 в запросе при отсутствии абонента",
				RequestType: "status", Account: lostNumber, Filial: "0", Number: "0");
			Test(Comment: "4.6.2 Запрос с полем флаг равным 0 в запросе при отсутствии абонента",
				RequestType: "status", Account: validNumber, SubNum: incorrectSubNum, Filial: "0", Number: "0");

			Test(Comment: "4.6.3 Запрос с полем флаг равным 1 в запросе при наличии абонента",
				RequestType: "status", Account: validNumber, Filial: "0", Number: "1");
			Test(Comment: "4.6.3 Запрос с полем флаг равным 4 в запросе при наличии абонента",
				RequestType: "status", Account: validNumber, Filial: "0", Number: "4");
			Test(Comment: "4.6.3 Запрос с полем флаг равным 8 в запросе при наличии абонента",
				RequestType: "status", Account: validNumber, Filial: "0", Number: "8");
			
		}

		/// <summary>
		/// Выполнение шага теста
		/// </summary>
		/// <param name="RequestType">Тип запроса</param>
		/// <param name="Filial">Филиал</param>
		/// <param name="Phone">Телефон</param>
		/// <param name="Account">Счёт</param>
		/// <param name="SubNum">Субсчёт</param>
		/// <param name="Comment">Комментарий</param>
		/// <param name="Amount">Сумма платежа</param>
		/// <param name="ReqTime">Время запроса</param>
		void Test(	string RequestType = "",
					int? Tid = null,
					string Filial = "0",
					string Phone = "",
					string Account = "",
					string SubNum = "",
					int? Status = null,
					string Number = "",
					DateTime? StartDate = null,
					DateTime ? EndDate = null,
					string Comment = "",
					Decimal? Amount = null,
					DateTime? ReqTime = null)
		{
			// Создание запроса
			Log("\r\nТест: {0}", Comment);
			Console.WriteLine("\r\nТест: {0}", Comment);

			// Создание запроса
			stRequest = MakeRequest(RequestType : RequestType, 
									Tid: Tid,
									Filial : Filial, 
									Phone : Phone, 
									Account : Account, 
									SubNum : SubNum, 
									Status: Status,
									Number: Number,
									StartDate: StartDate,
									EndDate: EndDate,
									Comment : Comment, 
									Amount : Amount,
									ReqTime : ReqTime);

			Log("Подготовлен запрос к {0}\r\n{1}", HOST, stRequest);
			Console.WriteLine("Подготовлен запрос к {0}\r\n{1}", HOST, stRequest);

			int cnt = 0;

			while (true)
			{
				// Отправка запроса серверу
				while (SendRequest() != 0)
				{
					Console.WriteLine("Заппрос отправлен с кодом {0}", errCode);
					if (errCode != 0) // Ошибка связи со шлюзом
					{
						Log("Ошибка {0} связи с шлюзом.", errCode);
						Thread.Sleep(30000);
					}
					else
					{
						Trace();
						return;
					}
				}

				// Разбор ответа
				ParseAnswer();
				Trace();
				if (errCode == 400 || errCode == 3 || errCode == 6 || errCode == 2 || errCode == 1)
					break;
				if (errCode == 1 && ++cnt == 3)
					break;
				Thread.Sleep(30000);
			}
		}

	}
}
