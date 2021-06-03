/*
    CopyRight (C) 1998-2005 CyberPlat.Com. All Rights Reserved.
    e-mail: support@cyberplat.com
*/

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Web;

namespace Oldi.Net.Cyber
{
	class Libpriv
	{
	}

	/// <summary>
	/// Исключения при работе крипто-модуля
	/// </summary>
	public class IPrivException : Exception
	{
		private readonly int m_code;
		public IPrivException(int c)
		{
			m_code = c;
		}
		public override string ToString()
		{
			switch (m_code)
			{
				case -1: return "Ошибка в параметрах"; // "Error in arguments";
				case -2: return "Ошибка выделения памяти"; // "Memory allocation error";
				case -3: return "Неверный формат документа"; //"Invalid document format";
				case -4: return "Чтение документа не закончено"; //"The reading of the document has not been completed";
				case -5: return "Ошибка внутренней структуры документа"; // "Error in the document’s internal structure";
				case -6: return "Неизвестный алгоритм шифрования"; //"Unknown encryption algorithm";
				case -7: return "Длины ключа и подписи не совпадают"; //"The key length and the signature length do not match";
				case -8: return "Неверный пароль приватного ключа"; //"Invalid code phrase to the secret key";
				case -9: return "Неверный тип документа"; //"Invalid document type";
				case -10: return "Error ASCII in encoding of the document";
				case -11: return "Error ASCII in decoding of the document";
				case -12: return "Unknown type of the encryption item";
				case -13: return "The encryption item is not ready";
				case -14: return "The call is not supported by the encryption item";
				case -15: return "Файл не найден"; //"Failed to find the file";
				case -16: return "Ошибка чтения файла"; //"File reading error";
				case -17: return "Ключ не может быть использован"; //"The key cannot be used";
				case -18: return "Ошибка создания подписи"; //"Error in signature creation";
				case -19: return "Публичный ключ с этим серийным номером не найден"; //"A public key with this serial number is not found";
				case -20: return "Подпись и содержимое документа не совпадают"; //"The signature and the document contents do not match";
				case -21: return "Ошибка создания файла"; //"File creation error";
				case -22: return "Ошибка заполнения"; //"Filing error";
				case -23: return "Неверный формат ключа карты"; //"Invalid key card format";
				case -24: return "Ошибка генерации ключей"; //"Keys generation error";
				case -25: return "Ошибка шифрования"; // "Encryption error";
				case -26: return "Ошибка дешифрования"; //"Decryption error";
				case -27: return "Отправитель не определён"; // "The sender is not defined";
				case -101: return "Пустой запрос";
			}
			return "Общая ошибка криптопровайдера"; //"General error";
		}
		public int code
		{
			get { return m_code; }
		}
	};

	public class IPrivKey : IDisposable
	{
		private byte[] pkey;
		private unsafe byte* ppkey;
		private GCHandle handle;
		public unsafe IPrivKey()
		{
			pkey = new byte[64];
			handle = GCHandle.Alloc(pkey, GCHandleType.Pinned);
			ppkey = (byte*)handle.AddrOfPinnedObject();
		}
		~IPrivKey()
		{
			closeKey();
		}
		public string signText(string src)
		{
			return IPriv.signText(src, this);
		}
		public byte[] SignText(string src)
		{
			return IPriv.SignText(src, this);
		}
		public string verifyText(string src)
		{
			return IPriv.verifyText(src, this);
		}
		public unsafe void closeKey()
		{
			if (ppkey != (byte*)IntPtr.Zero)
			{
				IPriv.closeKey(this);
				handle.Free();
				ppkey = (byte*)IntPtr.Zero;
			}
		}

		public byte[] getKey()
		{
			return pkey;
		}

		public unsafe byte* getPKey()
		{
			return ppkey;
		}

		public void Dispose()
		{
			closeKey();
		}
	};

	[SuppressUnmanagedCodeSecurity]
	public static class IPriv
	{
		internal static readonly Encoding cp1251 = Encoding.GetEncoding(1251);
		private const string libname = "libipriv";
		// for internal usage only

		[DllImport(libname)]
		private static extern int Crypt_Initialize();

		[DllImport(libname)]
		private static extern int Crypt_Done();

		[DllImport(libname)]
		private static extern unsafe int Crypt_OpenSecretKeyFromFile(int eng,
			byte* path,
			byte* passwd,
			byte* pkey);

		[DllImport(libname)]
		internal static extern unsafe int Crypt_OpenPublicKeyFromFile(int eng,
			byte* path,
			uint keyserial,
			byte* pkey,
			byte* сakey);

		[DllImport(libname)]
		public static unsafe extern int Crypt_Sign(byte* src,
			int nsrc, sbyte* dst,
			int ndst,
			byte* pkey);

		[DllImport(libname)]
		private static extern unsafe int Crypt_Verify(byte* src,
			int nsrc, sbyte** pdst,
			ref int pndst, byte* pkey);

		[DllImport(libname)]
		private static extern unsafe int Crypt_CloseKey(byte* pkey);

		public static void Initialize()
		{
			Crypt_Initialize();
		}
		public static void Done()
		{
			Crypt_Done();
		}

		public static unsafe IPrivKey openSecretKey(string path, string passwd)
		{
			IPrivKey k = new IPrivKey();
			byte[] bpath = new byte[path.Length + 1];//zero-terminated string
			byte[] bpasswd = new byte[passwd.Length + 1];//zero-terminated string
			cp1251.GetBytes(path, 0, path.Length, bpath, 0);
			cp1251.GetBytes(passwd, 0, passwd.Length, bpasswd, 0);
			fixed (byte* ppath = bpath)
			fixed (byte* ppasswd = bpasswd)
			{
				int rc = Crypt_OpenSecretKeyFromFile(0, ppath, ppasswd, k.getPKey());
				if (rc != 0)
					throw (new IPrivException(rc));
			}
			return k;
		}

		public static unsafe IPrivKey openPublicKey(string path, uint keyserial)
		{
			IPrivKey k = new IPrivKey();
			byte[] bpath = new byte[path.Length + 1];//zero-terminated string
			cp1251.GetBytes(path, 0, path.Length, bpath, 0);
			fixed (byte* ppath = bpath)
			{
				int rc = Crypt_OpenPublicKeyFromFile(0, ppath, keyserial, k.getPKey(), null);
				if (rc != 0)
					throw (new IPrivException(rc));
			}
			return k;
		}

		public static unsafe byte[] SignText(string src, IPrivKey key)
		{
			const int max_length = 2048;
			sbyte* tmp = stackalloc sbyte[max_length];
			byte[] bsrc = cp1251.GetBytes(src);
			byte[] result = new byte[max_length];

			int rc;
			fixed (byte* psrc = bsrc)
				rc = Crypt_Sign(psrc, bsrc.Length, tmp, max_length, key.getPKey());
			if (rc < 0)
				throw (new IPrivException(rc));
			// return new string(tmp, 0, rc, cp1251);
			int len = 0;
			for (int i = 0; i < max_length; i++)
			{
				len++;
				if (tmp[i] == 0)
				{
					break;
				}
			}
			result = new byte[len];
			for (int i = 0; i < len; i++)
				result[i] = (byte)tmp[i];
			return HttpUtility.UrlEncodeToBytes(result);
		}

		public static unsafe string signText(string src, IPrivKey key)
		{
			const int max_length = 2048;
			sbyte* tmp = stackalloc sbyte[max_length];
			byte[] bsrc = cp1251.GetBytes(src);
			int rc;
			fixed (byte* psrc = bsrc)
				rc = Crypt_Sign(psrc, bsrc.Length, tmp, max_length, key.getPKey());
			if (rc < 0)
				throw (new IPrivException(rc));
			return new string(tmp, 0, rc, cp1251);
		}


		public static unsafe string verifyText(string src, IPrivKey key)
		{
			byte[] srcb = cp1251.GetBytes(src);
			fixed (byte* psrc = srcb)
			{
				sbyte* pdst = (sbyte*)IntPtr.Zero;
				int pndst = 0;
				int rc = Crypt_Verify(psrc, srcb.Length, &pdst, ref pndst, key.getPKey());
				if (rc != 0)
					throw (new IPrivException(rc));
				return new string(pdst, 0, pndst, cp1251);
			}
		}
		public static unsafe void closeKey(IPrivKey key)
		{
			Crypt_CloseKey(key.getPKey());
		}

		/// <summary>
		/// Подписать сообщение в строке в кодировке 1251
		/// </summary>
		/// <param name="src">Исходное сообщение в кодировке UTF-8</param>
		/// <param name="trg">Сообщение в кодировке 1251 упакованное UrlEncode</param>
		/// <param name="s_text">Сообщение с подписью в кодировке UTF-8 (для лога)</param>
		/// <returns>0 - OK; 1 - ошибка криптопровайдера; -1 - системная ошибка</returns>
		public static void SignMessage(string src, out string trg, out string s_text,
			string secret, string passwd)
		{
			trg = "";
			s_text = "";
			IPrivKey SecretKey = null;

			if (string.IsNullOrEmpty(src))
			{
				throw new IPrivException(-101);
			}

			IPriv.Initialize();
			SecretKey = IPriv.openSecretKey(secret, passwd);

			// Подписать сообщение в кодировке 1251
			s_text = SecretKey.signText(src);

			// Закрыть крипто-провайдер
			SecretKey.closeKey();
			IPriv.Done();

			trg = "inputmessage=" + HttpUtility.UrlEncode(s_text, Encoding.GetEncoding(1251));


		}

		/// <summary>
		/// Проверка подписи ответа
		/// </summary>
		/// <param name="src"></param>
		/// <param name="pubkey"></param>
		/// <param name="serial"></param>
		public static void VerifyMessage(string src, string pubkey, string serial)
		{
			IPrivKey PublicKey = null;

			IPriv.Initialize();
			PublicKey = IPriv.openPublicKey(pubkey, Convert.ToUInt32(serial));

			// Проверить подпись
			PublicKey.verifyText(src);

			// Закрыть крипто-провайдер
			PublicKey.closeKey();
			IPriv.Done();

		}


	};
}
