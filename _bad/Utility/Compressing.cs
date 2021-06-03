using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Oldi.Utility
{
    public enum CompressType
    {
        Deflate,
        Gzip
    }

    public class Compressing : IDisposable
    {
        protected string srcFilename;
        protected string dstFilename;
        protected int bufSize;
        protected int offset;
        protected byte[] buf = null;
        protected int numBytes;
        protected FileStream fsStream = null;
        protected DeflateStream dfStream = null;
        protected GZipStream gzStream = null;
        protected bool disposed = false;
        protected CompressType compressType;
        private byte[] tmp;

        protected ManualResetEvent manualEvent = new ManualResetEvent(false);

        public Compressing(int bufSize, CompressType compressType)
        {
            this.bufSize = bufSize;
            this.compressType = compressType;
        }

        public Compressing(string srcFilename, string dstFilename, int bufSize, CompressType compressType)
        {
            this.srcFilename = srcFilename;
            this.dstFilename = dstFilename;
            this.bufSize = bufSize;
            buf = new byte[bufSize];
            this.compressType = compressType;
        }

        /// <summary>
        /// Сжатие потока
        /// </summary>
        /// <returns>0 - OK; -1 - Fail</returns>
        public virtual int Compress()
        {
            if (File.Exists(srcFilename))
            {
                using (fsStream = File.OpenRead(srcFilename))
                using (FileStream fs = File.OpenWrite(dstFilename))
                {
                    if (compressType == CompressType.Deflate)
                    {
                        using (dfStream = new DeflateStream(fs, CompressionMode.Compress))
                        {

                            IAsyncResult result = fsStream.BeginRead(buf, 0, bufSize, new AsyncCallback(EndReadCallBack), manualEvent);
                            manualEvent.WaitOne();
                            dfStream.Flush();
                            dfStream.Close();
                        }
                    }
                    else
                    {
                        using (gzStream = new GZipStream(fs, CompressionMode.Compress))
                        {

                            IAsyncResult result = fsStream.BeginRead(buf, 0, bufSize, new AsyncCallback(EndReadCallBack), manualEvent);
                            manualEvent.WaitOne();
                            gzStream.Flush();
                            gzStream.Close();
                        }
                    }
                    /*
                    using (dfStream = new DeflateStream(fs, CompressionMode.Compress))
                    {

                        IAsyncResult result = fsStream.BeginRead(buf, 0, bufSize, new AsyncCallback(EndReadCallBack), manualEvent);
                        manualEvent.WaitOne();
                        dfStream.Flush();
                        dfStream.Close();
                        return 0;
                    }
                     */
                }
                return 0;
            }
            return -1;
        }

        #region AsycIO
        /// <summary>
        /// Завершение чтения входного потока
        /// </summary>
        /// <param name="result">result - содержит manualEvent</param>
        protected virtual void EndReadCallBack(IAsyncResult result)
        {
            ManualResetEvent me = (ManualResetEvent)result.AsyncState;

            // Console.WriteLine("Read: Off={0} bytes={1}", offset, numBytes);

            // Читаем блок данных в буфер
            numBytes = fsStream.EndRead(result);
            // Теперь в buf находится numBytes байтов, их надо упаковать
            if (compressType == CompressType.Deflate)
                result = dfStream.BeginWrite(buf, 0, numBytes, new AsyncCallback(EndWriteCallBack), me);
            else
                result = gzStream.BeginWrite(buf, 0, numBytes, new AsyncCallback(EndWriteCallBack), me);
        }

        /// <summary>
        /// Завершение записи выходного потока
        /// </summary>
        /// <param name="result">result - содержит manualEvent</param>
        protected virtual void EndWriteCallBack(IAsyncResult result)
        {
            ManualResetEvent me = (ManualResetEvent)result.AsyncState;
            // Console.WriteLine("Write: Off={0} bytes={1}", offset, numBytes);
            if (compressType == CompressType.Deflate)
                dfStream.EndWrite(result);
            else
                gzStream.EndWrite(result);

            if (numBytes < bufSize)
            {
                // Это последний блок
                me.Set();
            }
            else
            {
                // Читаем дальше
                result = fsStream.BeginRead(buf, 0, bufSize, new AsyncCallback(EndReadCallBack), me);
            }
            offset += bufSize;
        }
        #endregion

        /// <summary>
        /// Упаковка строки в массив
        /// </summary>
        /// <param name="src">Входная строка</param>
        /// <param name="enc">Кодовая траница</param>
        /// <returns>Выходной массив</returns>
        public virtual byte[] Compress(string src, Encoding enc)
        {
            tmp = enc.GetBytes(src);

            using (MemoryStream msSrc = new MemoryStream())
            using (MemoryStream msDst = new MemoryStream())
            {
                // Поместим исходный массив в память
                msSrc.Write(tmp, 0, tmp.Length);
                // Упакуем его в выходной поток
                msSrc.Seek(0, SeekOrigin.Begin);
                if (compressType == CompressType.Deflate)
                {
                    dfStream = new DeflateStream(msDst, CompressionMode.Compress);
                    msSrc.CopyTo(dfStream);
                }
                else
                {
                    gzStream = new GZipStream(msDst, CompressionMode.Compress);
                    msSrc.CopyTo(gzStream);
                }
                // А теперь вернем его в массив
                msDst.Seek(0, SeekOrigin.Begin);
                return msDst.ToArray();
            }
        }

        /// <summary>
        /// Упаковка массива byte[] в строку Base64
        /// </summary>
        /// <param name="src">Входной массив byte[]</param>
        /// <returns>Строка в формате Base64</returns>
        public virtual string CompressToBase64(byte[] src)
        {
            string base64;
            using (MemoryStream msSrc = new MemoryStream())
            using (MemoryStream msDst = new MemoryStream())
            {
                // Поместим исходный массив в память
                msSrc.Write(src, 0, src.Length);
                // Упакуем его в выходной поток
                msSrc.Seek(0, SeekOrigin.Begin);
                if (compressType == CompressType.Deflate)
                {
                    dfStream = new DeflateStream(msDst, CompressionMode.Compress);
                    msSrc.CopyTo(dfStream);
                }
                else
                {
                    gzStream = new GZipStream(msDst, CompressionMode.Compress);
                    msSrc.CopyTo(gzStream);
                }
                // А теперь вернем егов в массив
                msDst.Seek(0, SeekOrigin.Begin);
                base64 = Convert.ToBase64String(msDst.ToArray());
            }
            return base64;
        }

        /// <summary>
        /// Упаковка строки в строку Base64
        /// </summary>
        /// <param name="src"></param>
        /// <param name="enc"></param>
        /// <returns>Упакованная строка в формате Base64</returns>
        public virtual string CompressToBase64(string src, Encoding enc)
        {
            tmp = enc.GetBytes(src);
            return CompressToBase64(tmp);
        }

        /// <summary>
        /// Распаковка потока
        /// </summary>
        /// <returns>0 - OK; -1 - Fail</returns>
        public virtual int Decompress()
        {
            if (File.Exists(srcFilename))
            {
                using (FileStream fs = File.OpenRead(srcFilename))
                using (fsStream = File.OpenWrite(dstFilename))
                {
                    if (compressType == CompressType.Deflate)
                    {
                        using (dfStream = new DeflateStream(fs, CompressionMode.Decompress))
                        {
                            dfStream.CopyTo(fsStream);
                        }
                    }
                    else
                    {
                        using (gzStream = new GZipStream(fs, CompressionMode.Decompress))
                        {
                            gzStream.CopyTo(fsStream);
                        }
                    }
                    fsStream.Flush();
                    fsStream.Close();
                    return 0;
                }
            }
            return -1;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">true -- очистка управляемых объектов</param>
        public virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (fsStream != null)
                        fsStream.Dispose();
                    if (dfStream != null)
                        dfStream.Dispose();
                    
                    // Очистка массива, для быстрой утилизации коллектором
                    if (buf != null)
                    {
                        for (int i = 0; i < bufSize; i++)
                        {
                            buf[i] = 0;
                        }
                        buf = null;
                    }
                    if (tmp != null)
                    {
                        for (int i = 0; i < bufSize; i++)
                        {
                            tmp[i] = 0;
                        }
                        tmp = null;
                    }
                }
                disposed = true;
            }
        }
    }

    /// <summary>
    /// Упаковка методом deflate (не поддерживается архиваторами)
    /// Наследует все методы Compressing
    /// </summary>
    public class DeflateCompressing : Compressing
    {
        public DeflateCompressing(int bufSize = 8192)
            : base(bufSize, CompressType.Deflate)
        {
        }

        public DeflateCompressing(string srcFilename, string dstFilename, int bufSize = 8192)
            : base(srcFilename, dstFilename, bufSize, CompressType.Deflate)
        {
        }
    }

    /// <summary>
    /// Упаковка методом GZip, упаковывает массив в файл архива. Поддерживается архиваторами.
    /// </summary>
    class GzipCompressing : Compressing
    {
        public GzipCompressing(int bufSize = 8192)
            : base(bufSize, CompressType.Gzip)
        {
        }

        public GzipCompressing(string srcFilename, string dstFilename, int bufSize = 8192)
            : base(srcFilename, dstFilename, bufSize, CompressType.Gzip)
        {
        }
    }
}
