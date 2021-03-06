﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using Oldi.Utility;

namespace Oldi.Ekt
{

    /// <summary>
    /// Результат проверки состояния запроса
    /// </summary>
    public class Result
    {
        public int state;
        public int substate;
        public int code;
        public Result()
        {
            state = 0;
            substate = 0;
            code = 0;
        }
    }

    public partial class GWEktRequest : GWRequest
	{
		protected string pointid;
		new Result result;
		string Signature;
		protected Encoding Enc;
        readonly string qiwiGateway = "1418";

		public GWEktRequest()
			: base()
		{
		}

		public GWEktRequest(GWRequest src)
			: base(src)
		{
            // Номер кассы / ПА
            terminal = src.Terminal;
		}

        /// <summary>
        /// Инициализация параметров
        /// </summary>
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
					CodePage = "UTF-8";
					Enc = Encoding.UTF8;
					break;
			}
		}

        public override int TimeOut()
        {
            return int.Parse(Settings.Ekt.Timeout);
        }

        protected override string GetLogName()
        {
            return Settings.Ekt.LogFile;
        }


    }
}
