using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;

namespace Autoshow
{
	public partial class Autoshow : GWRequest
	{
		Encoding Enc;
		string Hash;
		string Password = "qwe123NN";
		string Sign = "Номер билета: {0}. Печать билета на сайте: http://medianebo.ru/";

		public Autoshow()
			: base()
		{
		}

		public Autoshow(GWRequest req)
			: base(req)
		{
			this.account = req.Account;
			this.phone = req.Phone;
			this.amount = req.Amount;
		}

		public override void InitializeComponents()
		{
			base.InitializeComponents();
			host = "http://medianebo.ru/ticket_pay/result.php";
			CodePage = "UTF-8";
			ContentType = "text/html";
			Enc = Encoding.UTF8;
		}

		public override int TimeOut()
		{
			return 120;
		}

		protected override string GetLogName()
		{
			return "log\\As.log";
		}

	}
}
