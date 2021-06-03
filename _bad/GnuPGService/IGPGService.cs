using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Xml.Serialization;

namespace GPG
	{
	
	public class GpgResult
		{
		public int ErrCode;
		public string ErrDesc;
		public string Message;
		}

	[ServiceContract(Namespace="http:/regplat.ru/gpg")]
	interface IGPGService
		{
		/// <summary>
		/// Возвращает PGP-подпись
		/// </summary>
		/// <param name="PrivateKeyName"></param>
		/// <param name="PublicKeyName"></param>
		/// <param name="IncludeData"></param>
		/// <param name="Text"></param>
		/// <returns></returns>
		[OperationContract]
		[WebGet(UriTemplate="CreateSign?PrivateKeyName={PrivateKeyName}&PublicKyeName={PublicKeyName}&IncludeData={Flag}&Text={Text}")]
		GpgResult CreateSign(string PrivateKeyName, string PublicKeyName, bool IncludeData, string Text);
		}
		
	}
