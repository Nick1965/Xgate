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
 * File Name: SMSC.cs
 * 
 * File Authors:
 * 		Balan Name, http://balan.name
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SMPP
{

	public class SMSC
	{
		private string description;
		private string host;
		private int port;
		private string systemId;
		private string password;
		private string systemType;
		private int sequenceNumber;
		private byte addrTon = 0;
		private byte addrNpi = 0;
		private string addressRange = "";

		private Object thisLock = new Object();

		public SMSC()
		{
		}

		public SMSC(string description, string host, int port, string systemId, string password, string systemType, int sequenceNumber)
		{
			this.description = description;
			this.host = host;
			this.port = port;

			this.systemId = Tools.Copy(systemId, 15);
			this.password = Tools.Copy(password, 8);
			this.systemType = Tools.Copy(systemType, 12);

			this.sequenceNumber = sequenceNumber;
		}

		public SMSC(string description, string host, int port, string systemId, string password, string systemType, byte addrTon, byte addrNpi,
			string addressRange, int sequenceNumber)
			: this(description, host, port, systemId, password, systemType, sequenceNumber)
		{
			this.addrTon = addrTon;
			this.addrNpi = addrNpi;
			this.addressRange = addressRange;
		}

		//Description
		public string Description { get { return description; } set { description = value; } }
		//Host
		public string Host { get { return host; } set { host = value; } }
		//Port
		public int Port { get { return port; } set { port = value; } }
		//SystemId
		public string SystemId { get { return systemId; } set { systemId = value; } }
		//Password
		public string Password { get { return password; } set { password = value; } }
		//SystemType
		public string SystemType { get { return systemType; } set { systemType = value; } }
		//AddrTon
		public byte AddrTon { get { return addrTon; } set { addrTon = value; } }
		//AddrNpi
		public byte AddrNpi { get { return addrNpi; } set { addrNpi = value; } }
		//AddressRange
		public string AddressRange
		{
			get { return addressRange; }
			set
			{ addressRange = String.IsNullOrEmpty(value) ? "" : Tools.Copy(value, 40); }
		}
		//SequenceNumber
		public int SequenceNumber
		{
			get
			{
				lock (thisLock)
				{
					if (sequenceNumber == Int32.MaxValue)
						sequenceNumber = 0;
					else
						sequenceNumber++;
					return sequenceNumber;
				}
			}
		}
		//LastSequenceNumber
		public int LastSequenceNumber
		{
			get
			{
				lock (thisLock)
				{
					return sequenceNumber;
				}
			}
		}


	}

	public class SMSCArray
	{
		private ArrayList SMSCAr = new ArrayList();
		private int curSMSC = 0;
		private Object thisLock = new Object();

		public void AddSMSC(SMSC pSMSC)
		{
			lock (thisLock)
			{
				SMSCAr.Add(pSMSC);
			}
		}//AddSMSC

		public void Clear()
		{
			lock (thisLock)
			{
				SMSCAr.Clear();
				curSMSC = 0;
			}
		}//Clear

		public void NextSMSC()
		{
			lock (thisLock)
			{
				curSMSC++;
				if ((curSMSC + 1) > SMSCAr.Count)
					curSMSC = 0;
			}
		}//AddSMSC


		public SMSC currentSMSC
		{
			get
			{
				SMSC mSMSC = null;
				try
				{
					lock (thisLock)
					{

						if (SMSCAr.Count == 0)
							return null;
						if (curSMSC > (SMSCAr.Count - 1))
						{
							curSMSC = 0;
						}
						mSMSC = (SMSC)SMSCAr[curSMSC];
					}
				}
				catch (Exception)
				{
				}
				return mSMSC;
			}
		}//currentSMSC

		public bool HasItems
		{
			get
			{
				lock (thisLock)
				{
					if (SMSCAr.Count > 0)
						return true;
					else
						return false;
				}
			}
		}//HasItems
	}//SMSCArray

}
