using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Oldi.Utility;

namespace Oldi.Net
{
    public class Crypto: IDisposable
    {

        private X509Certificate2 certificate;
        private byte[] publicKeyData;
        private AsymmetricAlgorithm privateKey;
		private bool hasPrivateKey = false;

        /// <summary>
        /// Сертификат X.509 v2
        /// </summary>
		public X509Certificate2 Certificate { get { return certificate; } }
        /// <summary>
        /// True - есть приватный ключ
        /// </summary>
		public bool HasPrivateKey { get { return hasPrivateKey; } }
		/// <summary>
		/// Публичный ключ
		/// </summary>
		public byte[] GetPublicKey { get { return publicKeyData; } }
		/// <summary>
		/// Приватный ключ
		/// </summary>
		public AsymmetricAlgorithm PrivateKey { get { return HasPrivateKey? privateKey: null; } }

		/// <summary>
		/// Конструктор Crypto
		/// </summary>
		/// <param name="CommonName">Общее имя CN</param>
		public Crypto(string CommonName)
        {
            if (string.IsNullOrEmpty(CommonName))
                return;

            certificate = GetCertificate(CommonName);
            if (certificate != null)
            {
				try
				{
					hasPrivateKey = certificate.HasPrivateKey;
					publicKeyData = certificate.GetPublicKey();
					privateKey = certificate.PrivateKey;
				}
				catch (Exception ex)
				{
					string message;
					if (ex.InnerException != null)
					{
						message = string.Format("{0} ({1})", ex.InnerException.Message, ex.InnerException.GetType());
					}
					else
						message = ex.Message;
					Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "Crypto.dll: {0}", message);
					// throw new ApplicationException(string.Format("Crypto.dll: {0}", message));
				}
                // Utility.Log("Сертификат {0} найден в личном хранилище", CommonName);
            }
            else
				Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "Crypto.dll: Сертификат {0} не найден", CommonName);
				// throw new ApplicationException(String.Format("Сертификат {0} не найден в личном хранилище", CommonName));
        }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool disposed = false;

		public virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					if (publicKeyData != null)
					{
						for (int i = 0; i < publicKeyData.Length; i++)
							publicKeyData[i] = 0;
						publicKeyData = null;
					}
					if (privateKey != null)
						privateKey.Clear();
					certificate = null;
				}
				disposed = true;
			}
		}

		/// <summary>
		/// Поиск сертификата
		/// </summary>
		/// <param name="commonName">Common name</param>
		/// <returns>X509Certificate2</returns>
		private X509Certificate2 GetCertificate(string commonName)
        {

			X509Certificate2 cert = null;
			X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

			try
			{
				x509Store.Open(OpenFlags.ReadOnly);
				foreach (X509Certificate2 x509Certificate in x509Store.Certificates)
					// В строке commonName может быть передан, как CN, так и HASH сертификата
					if (commonName == x509Certificate.GetNameInfo(X509NameType.SimpleName, false) || commonName == x509Certificate.GetCertHashString())
					{
						cert = x509Certificate;
						break;
					}
			}
			catch (Exception ex)
			{
				throw new ApplicationException(ex.Message);
			}
			finally
			{
				x509Store.Close();
			}

			return cert;

        } //GetCertificate


        public string EncryptString(string inputString, int dwKeySize, string xmlString)
        {
            // TODO: Add Proper Exception Handlers
            RSACryptoServiceProvider rsaCryptoServiceProvider = new RSACryptoServiceProvider(dwKeySize);
            rsaCryptoServiceProvider.FromXmlString(xmlString);
            int keySize = dwKeySize / 8;
            byte[] bytes = Encoding.UTF32.GetBytes(inputString);
            // The hash function in use by the .NET RSACryptoServiceProvider here is SHA1
            // int maxLength = ( keySize ) - 2 - ( 2 * SHA1.Create().ComputeHash( rawBytes ).Length );
            int maxLength = keySize - 42;
            int dataLength = bytes.Length;
            int iterations = dataLength / maxLength;
            // StringBuilder stringBuilder = new StringBuilder();
			string encryptedString = "";
			for (int i = 0; i <= iterations; i++)
            {
                byte[] tempBytes = new byte[(dataLength - maxLength * i > maxLength) ? maxLength : dataLength - maxLength * i];
                Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0, tempBytes.Length);
                byte[] encryptedBytes = rsaCryptoServiceProvider.Encrypt(tempBytes, true);
                // Be aware the RSACryptoServiceProvider reverses the order of encrypted bytes after encryption and before decryption.
                // If you do not require compatibility with Microsoft Cryptographic API (CAPI) and/or other vendors.
                // Comment out the next line and the corresponding one in the DecryptString function.
                Array.Reverse(encryptedBytes);
                // Why convert to base 64?
                // Because it is the largest power-of-two base printable using only ASCII characters
                // stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
				encryptedString += Convert.ToBase64String(encryptedBytes);
            }
            // return stringBuilder.ToString();
			return encryptedString;
        }

		/// <summary>
		/// Подпись
		/// </summary>
		/// <param name="text">Text: string</param>
		/// <returns>Base64 string</returns>
		public string Sign(string text, int alg = 5, Encoding cp = null)
		{


			if (alg == 1)
			{
				SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
				RSAPKCS1SignatureFormatter enc = new RSAPKCS1SignatureFormatter(privateKey);
				enc.SetHashAlgorithm("SHA1");
				// enc.SetKey(privateKey);
				byte[] hash = sha1.ComputeHash(cp.GetBytes(text));
				byte[] sign = enc.CreateSignature(hash);
				return Convert.ToBase64String(sign);
			}
			else
			{
				MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
				RSAPKCS1SignatureFormatter enc = new RSAPKCS1SignatureFormatter();
				enc.SetHashAlgorithm("MD5");
				enc.SetKey(privateKey);
				if (cp == null) cp = Encoding.Default;
				return Convert.ToBase64String(enc.CreateSignature(md5.ComputeHash(cp.GetBytes(text))));
			}

		} // Sign


        /// <summary>
        /// Вычисление Хэш-функции для заданного алгоритма
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Aalg"></param>
        /// <param name="CP"></param>
        /// <returns></returns>
        public string Hash(string Text, int Alg = 256, Encoding CP = null)
        {
            byte[] buf;
            string str = "";
            if (CP == null)
                CP = UTF8Encoding.UTF8;

            if (Alg == 5)
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                buf = md5.ComputeHash(CP.GetBytes(Text));
            }
            else if (Alg == 1)
            {
                SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
                buf = sha1.ComputeHash(CP.GetBytes(Text));
            }
            else // 256
            {
                SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
                buf = sha256.ComputeHash(CP.GetBytes(Text));
            }

            for (int i = 0; i < buf.Length; i++)
                str += buf[i].ToString("x2");

            return str;
        }

    }
}
