/*
 * EasySMPP - SMPP protocol library for fast and easy
 * SMSC(Short Message Service Centre) client development
 * even for non-telecom guys.
 * 
 * Easy to use classes covers all needed functionality
 * for SMS applications developers and Content Providers.
 * 
 * Written for .NET 2.0 in C#
 * 
 * Copyright (C) 2006 Balan Andrei, http://balan.name
 * 
 * Licensed under the terms of the GNU Lesser General Public License:
 * 		http://www.opensource.org/licenses/lgpl-license.php
 * 
 * For further information visit:
 * 		http://easysmpp.sf.net/
 * 
 * 
 * "Support Open Source software. What about a donation today?"
 *
 * 
 * File Name: SmsClient.cs
 * 
 * File Authors:
 * 		Balan Name, http://balan.name
 */
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.Globalization;
using System.Net;
using Base;

namespace SMPP
{
	public class SmsClient : BaseClass, IDisposable
	{
		private SMPPClient smppClient;
		private int waitForResponse = 120 * 1000;
		private SortedList<int, AutoResetEvent> events = new SortedList<int, AutoResetEvent>();
		private SortedList<int, int> statusCodes = new SortedList<int, int>();
		#region Properties
		public int WaitForResponse
		{
			get { return waitForResponse; }
			set { waitForResponse = value; }
		}
		#endregion Properties

		#region Public functions

		public override string GetLogFileName()
		{
			return "log\\smppgw.log";
		}

		public SmsClient(string description, string host, int port, string systemId, string password, string systemType, int seqn)
		{
			smppClient = new SMPPClient();

			smppClient.OnDeliverSm += new DeliverSmEventHandler(onDeliverSm);
			smppClient.OnSubmitSmResp += new SubmitSmRespEventHandler(onSubmitSmResp);

			smppClient.OnLog += new LogEventHandler(onLog);
			smppClient.LogLevel = LogLevels.LogErrors;
			// smppClient.LogLevel = LogLevels.LogDebug;

			SMSC smsc = new SMSC(description, host, port, systemId, password, systemType, seqn);
			smppClient.AddSMSC(smsc);
		}
		
		public SmsClient()
		{
			smppClient = new SMPPClient();

			smppClient.OnDeliverSm += new DeliverSmEventHandler(onDeliverSm);
			smppClient.OnSubmitSmResp += new SubmitSmRespEventHandler(onSubmitSmResp);

			smppClient.OnLog += new LogEventHandler(onLog);
			smppClient.LogLevel = LogLevels.LogErrors;
			// smppClient.LogLevel = LogLevels.LogDebug;

			LoadConfig();

			// smppClient.Connect();
		}
		
		public void Connect()
		{
			smppClient.Connect();

			onLog(new LogEventArgs(string.Format("{1} Connection state = {0}", smppClient.ConnectionState,
				DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))));
			int cnt = 0;
			while (smppClient.ConnectionState != ConnectionStates.SMPP_BINDED)
			{
				Thread.Sleep(1000);
				if (cnt++ > 120)
				{
					onLog(new LogEventArgs(string.Format("{0} Connection are timed out", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))));
					break;
				}
			}
			onLog(new LogEventArgs(string.Format("{1} Connection state = {0}", smppClient.ConnectionState,
				DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))));
		}
		public void Disconnect()
		{
			smppClient.Disconnect();
		}

		public virtual void Dispose()
		{
			Disconnect();
			smppClient = null;
			events.Clear();
			events = null;
			statusCodes.Clear();
			statusCodes = null;
		}

		public bool SendSms(string from, string to, string text)
		{
			bool result = false;

			Log("Отправляется: from={0} to={1} {2}", from, to, text);

			if (smppClient.CanSend)
			{
				AutoResetEvent sentEvent;
				int sequence;
				lock (events)
				{
					sequence = smppClient.SendSms(from, to, text);
					sentEvent = new AutoResetEvent(false);
					events[sequence] = sentEvent;
				}
				if (sentEvent.WaitOne(waitForResponse, true))
				{
					lock (events)
					{
						events.Remove(sequence);
					}
					int statusCode;
					bool exist;
					lock (statusCodes)
					{
						exist = statusCodes.TryGetValue(sequence, out statusCode);
					}
					if (exist)
					{
						lock (statusCodes)
						{
							statusCodes.Remove(sequence);
						}
						if (statusCode == StatusCodes.ESME_ROK)
							result = true;
						else
							onLog(new LogEventArgs(string.Format("Error: {0} at sequence {1}", statusCode, sequence)));
					}
				}
			}
			else
				onLog(new LogEventArgs("Error: canSend = false"));

			return result;
		}

		#endregion Public functions

		#region Events

		public event NewSmsEventHandler OnNewSms;

		public event LogEventHandler OnLog;

		#endregion Events

		#region Private functions
		private void onDeliverSm(DeliverSmEventArgs args)
		{
			smppClient.sendDeliverSmResp(args.SequenceNumber, StatusCodes.ESME_ROK);
			if (OnNewSms != null)
				OnNewSms(new NewSmsEventArgs(args.From, args.To, args.TextString));
		}
		private void onSubmitSmResp(SubmitSmRespEventArgs args)
		{
			AutoResetEvent sentEvent;
			bool exist;
			lock (events)
			{
				exist = events.TryGetValue(args.Sequence, out sentEvent);
			}
			if (exist)
			{
				lock (statusCodes)
				{
					statusCodes[args.Sequence] = args.Status;
				}
				sentEvent.Set();
			}
		}
		
		static Object Lock = new Object();

		private void onLog(LogEventArgs args)
		{
			// Console.WriteLine(args.Message);
			lock(Lock)
			{
				Log("{0}", args.Message);
				if (OnLog != null)
					OnLog(args);
			}
		}
		private void LoadConfig()
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(SMSC));

				if (!File.Exists("smsc.cfg"))
				{
					/*
					using (TextWriter writer = new StreamWriter("smsc.cfg"))
					{
						serializer.Serialize(writer, new SMSC("example", "217.23.150.71", 2775, "regplat", "yd2qox45", "test", 1, 1, "+79039531420", 0));
					}
					 */
					onLog(new LogEventArgs("Please edit smsc.cfg and enter your data."));
				}
				using (FileStream fs = new FileStream("smsc.cfg", FileMode.Open))
				{
					SMSC smsc = (SMSC)serializer.Deserialize(fs);
					smppClient.AddSMSC(smsc);

					onLog(new LogEventArgs("Try host " + smsc.Host));
					IPAddress ip;
					if (!IPAddress.TryParse(smsc.Host, out ip))
					{
						IPHostEntry hostinfo = Dns.GetHostEntry(smsc.Host);
						IPAddress[] ips = hostinfo.AddressList;
						smsc.Host = ips[0].ToString();
						string msg = string.Format("IP: {0}", smsc.Host);
					}

					string desc = string.Format("Description: {0}\r\nHost: {1}\r\nPort: {2}", smsc.Description, smsc.Host, smsc.Port);
					onLog(new LogEventArgs(desc));
				}
			}
			catch (Exception ex)
			{
				onLog(new LogEventArgs("Error on loading smsc.cfg : " + ex.Message));
			}

		}

		#endregion


	}
}
