using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using Oldi.Utility;
using System.Data.SqlClient;
using System.Data;

namespace Oldi.Net
	{
	
	public class OldiContext: DataContext
		{
		public OldiContext(string cnn): base(cnn)
			{
			}

		[Function(Name="oldigw.oldigw.Ver3_CheckDouble")]
		public int CheckDouble(
			[Parameter(DbType="nvarchar(255)")] string Target,
			[Parameter(DbType="int")] int TppId,
			[Parameter(DbType="int")] int SourceTid,
			[Parameter(DbType="datetime2(7)")] DateTime Pcdate,
			[Parameter(DbType="decimal(16,2)")] decimal Amount,
			[Parameter(DbType="decimal(16,2)")] decimal AmountAll
			)
			{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
				Target, TppId, SourceTid, Pcdate, Amount, AmountAll);
			return ((int)(result.ReturnValue));
			}

		}

    public class Pay
    {
        public DateTime? DatePay;
        public int? Tid;
        public int? Point_oid;
        public int? Template_tid;
        public int? User_id;
        public decimal? Amount;
        public decimal? Commission;
        public decimal? Summary_amount;
    }

    public partial class GWRequest
		{

		/// <summary>
		/// Возвращает номер транзакции
		/// </summary>
		/// <returns></returns>
		public string GetGorodSub()
			{

			string Sub_inner_tid = "";
			using (OldiContext db = new OldiContext(Settings.GorodConnectionString))
				{
				IEnumerable<string> tids = db.ExecuteQuery<string>(@"
					select [sub_inner_tid] 
						from [gorod].[dbo].payment 
						where tid = {0}
					", Tid);
				// where not sub_inner_tid like 'card-%' and tid = {0}
				try
					{
					Sub_inner_tid = tids.First<string>();
					}
				catch (Exception)
					{
					}
				}

			if (string.IsNullOrEmpty(Sub_inner_tid))
				return "";
			else
				return Sub_inner_tid;
			}

        /// <summary>
        /// Поиск задвоенных платежей
        /// </summary>
        /// <returns></returns>
        public int GetDoubles()
			{
			int Doubles = 0;

            // Искать задвоенные платежи по параметрам:
            // Point_oid
            // Template_tid
            // User_id
            // Account
            // Amount, Commission, Summary_amount

            /*
            select	@Point_oid = p.Point_oid,
                    @Template_tid = p.template_tid,
                    @User_id = p.user_id,
                    @Account = d.clientAccount,
                    @datepay = p.datePay
                from [gorod].[dbo].payment p 
                left join [gorod].[dbo].pd4 d on p.tid = d.tid
                where p.tid = @tid

            select count(p.tid)
                from [gorod].[dbo].payment p 
                left join [gorod].[dbo].pd4 d on p.tid = d.tid
                where	p.tid <> @tid and -- кроме самого себя
                        @Point_oid = p.Point_oid and
                        @Template_tid = p.template_tid and
                        @User_id = p.user_id and
                        @Account = d.clientAccount and 
                        abs(datediff(second, p.datePay, @datePay)) <= 180 
            */

            try
            {
				using (OldiContext db = new OldiContext(Settings.GorodConnectionString))
					{
                    // извлечём информацию о платеже
                    IEnumerable<Pay> Pays =  db.ExecuteQuery<Pay>(@"
                        select	datePay, 
                                Tid, 
                                Point_oid,
                                template_tid,
                                user_id,
                                amount,
                                commission,
                                summary_amount
                            from [gorod].[dbo].payment
                            where tid = {0}
                    ", Tid);
                    Pay current = Pays.First();

                    RootLog($"**** {Tid} [Check doubles] acc={ID()} amount={current.Amount.AsCF()} commissiion={current.Commission.AsCF()} summary={current.Summary_amount.AsCF()} date={current.DatePay.Value.AsCF()} point={current.Point_oid} tpl={current.Template_tid} usr={current.User_id}");

                    Doubles = db.ExecuteCommand(@"
                        select count(p.tid)
                            from [gorod].[dbo].payment p
                            left join [gorod].[dbo].pd4 d on p.tid = d.tid
                            where	p.tid <> {0} and -- кроме самого себя
                                    {1} = p.Point_oid and
                                    {2} = p.template_tid and
                                    {3} = p.user_id and
                                    {4} = p.amount and 
                                    {5} = p.commission and 
                                    {6} = p.summary_amount and 
                                    {7} = d.clientAccount and
                                    abs(datediff(minute, p.datePay, {8})) <= 600 -- 10 часов 
						            ", Tid, current.Point_oid, current.Template_tid, current.User_id, current.Amount, current.Commission, current.Summary_amount, 
                                    ID(), current.DatePay);
					}
					// where not sub_inner_tid like 'card-%' and tid = {0}
				}
			catch (Exception ex)
				{
                RootLog($"{Tid} [Check doubles] acc={ID()}\r\n{ex.ToString()}");
				}

            RootLog($"**** {Tid} [Check doubles] acc={ID()} doubles={Doubles}");
            return Doubles;
			
			}

        public string ID()
        {
            string x = "";

            if (!string.IsNullOrEmpty(Phone))
                x = Phone;
            else if (!string.IsNullOrEmpty(Account) && string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если задан Number, то используется он
                x = Account;
            else if (!string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если только не задан Card
                x = Number;
            else if (!string.IsNullOrEmpty(Card))
                x = Card;

            return x;
        }

        /*
		public int CheckDouble()
			{

			string x = null;

			if (!string.IsNullOrEmpty(Phone))
				x = Phone;
			else if (!string.IsNullOrEmpty(Account) && string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если задан Number, то используется он
				x = Account;
			else if (!string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(Card)) // Если только не задан Card
				x = Number;
			else if (!string.IsNullOrEmpty(Card))
				x = Card;
			else
				{
				RootLog("{0} [DOUB - step] {1}/{2} Не задан номер счёта", Tid, Service, Gateway);
				return 0;
				}

			/*
			int Doubles = 0;
			using (OldiContext db = new OldiContext(Settings.ConnectionString))
				{
				Doubles = db.CheckDouble(x, Terminal, (int)Tid, Pcdate, Amount, AmountAll);
				}
	
			// Проверяем дубли для всех типов терминалов
			// string SubInnerTid = GetGorodSub();
			// if (string.IsNullOrEmpty(SubInnerTid))
			//	return 0;
			// else
				return Doubles;
	
			if (TerminalType == 1)
				return 0;
			return GetDoubles();

			}
			*/

    }
	}
