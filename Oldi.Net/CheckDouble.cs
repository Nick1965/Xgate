using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using Oldi.Utility;
using System.Data.SqlClient;

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
	public partial class GWRequest
		{

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
				RootLog("{0} [FCHK] {1}/{2} Не задан номер счёта", Tid, Service, Gateway);
				return 0;
				}

			int Doubles = 0;
			using (OldiContext db = new OldiContext(Settings.ConnectionString))
				{
				Doubles = db.CheckDouble(x, Terminal, (int)Tid, Pcdate, Amount, AmountAll);
				}
	
			string SubInnerTid = GetGorodSub();
			if (string.IsNullOrEmpty(SubInnerTid))
				return 0;
			else
				return Doubles;
			
			}
			
		}
	}
