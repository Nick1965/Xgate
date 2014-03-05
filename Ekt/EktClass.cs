using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using Oldi.Utility;

namespace Oldi.Ekt
{
	public partial class GWEktRequest : GWRequest
	{
		string pointid;
		new Result result;
		string Signature;
		Encoding Enc;

		public GWEktRequest()
			: base()
		{
		}

		public GWEktRequest(GWRequest src)
			: base(src)
		{
		}

		public override void InitializeComponents()
		{
			base.InitializeComponents();
			host = Settings.Ekt.Host;
			CodePage = Settings.Ekt.Codepage;
			ContentType = Settings.Ekt.ContentType;
			commonName = Settings.Ekt.Certname;
			pointid = Settings.Ekt.Pointid;
			result = new Result();

			switch (CodePage.ToLower())
			{
				case "1251":
				case "windows-1251":
					Enc = Encoding.GetEncoding(1251);
					break;
				case "866":
				case "cp866":
					Enc = Encoding.GetEncoding(866);
					break;
				case "koi8r":
					Enc = Encoding.GetEncoding(20866);
					break;
				case "utf-8":
				case "65001":
					Enc = Encoding.UTF8;
					break;
				default:
					Enc = Encoding.ASCII;
					break;
			}
		}
	}
}
