using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oldi.Utility
{
    public class Coding
    {

        private static byte IsSpecial(byte b)
        {
            byte[] spc = { /* (byte)'+', */ (byte)':', (byte)'*', (byte)'[', (byte)']', (byte)'+', (byte)'#', (byte)'<', (byte)'>', 
                             (byte)'%', (byte)'\'', (byte)'?', (byte)'=', (byte)'/', (byte)'"' };
            
            for (int i = 0; i < spc.Length; i++)
                if (spc[i] == b)
                    return spc[i];
            
            return 0;
        }

        /// <summary>
        /// Сцепляет 2 строки в виде массива байтов
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        public static byte[] Merge(byte[] array1, byte[] array2)
        {
            byte[] trg = new byte[array1.Length + array2.Length];
            int c = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                trg[c++] = array1[i];
            }
            for (int i = 0; i < array2.Length; i++)
            {
                trg[c++] = array2[i];
            }
            return trg;
        }

        /// <summary>
        /// Возвращает массив в URL-кодировкеж
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string UrlEncoding(string utf)
        {
            // byte[] t = new byte[8192];
            byte spc;
            byte a;
            byte b;
            string t = "";

            byte[] src = Encoding.GetEncoding(1251).GetBytes(utf);
            
            int c = 0;
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] < 0x20 || src[i] > 0x7F || IsSpecial(src[i]) != 0 && src[i] != (byte)'%')
                {
                    t += "%";
                    a = Convert.ToByte((src[i] & 0xF0) / 0x10);
                    b = Convert.ToByte(src[i] & 0x0F);
                    // Console.WriteLine("{0:x} {1:x}", a, b);
                    t += string.Format("{0:x}", a);
                    t += string.Format("{0:x}", b);
                }
                else if (src[i] == (byte)' ')
                    t += "+";
                else
                {
                    t += (char)src[i];
                    // c++;
                }
            }

                return t;
            }


        /// <summary>
        /// Преобразовываем только кирилицу
        /// </summary>
        /// <param name="utf"></param>
        /// <returns></returns>
        public static string UrlEncoding2(string utf)
        {
            // byte[] t = new byte[8192];
            byte spc;
            byte a;
            byte b;
            string t = "";

            byte[] src = Encoding.GetEncoding(1251).GetBytes(utf);
            
            int c = 0;
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] > 0x7F)
                {
                    t += "%";
                    a = Convert.ToByte((src[i] & 0xF0) / 0x10);
                    b = Convert.ToByte(src[i] & 0x0F);
                    // Console.WriteLine("{0:x} {1:x}", a, b);
                    t += string.Format("{0:x}", a);
                    t += string.Format("{0:x}", b);
                }
                else if (src[i] == (byte)' ')
                    t += "%2b";
                else
                {
                    t += (char)src[i];
                    // c++;
                }
            }

            return t; 
        }

        static string utf = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэю";
        static byte[] ansi = new byte[256];

        private static byte IsCyr(char c)
        {
            for (int i = 0; i < utf.Length; i++)
                if (Convert.ToChar(utf.Substring(i, 1)) == c)
                    return ansi[i];
            if (c == 'Ё')
                return 0xC5;
            else if (c == 'ё')
                return 0xE5;

            return 0;
        }
        
        public static string UtfTo1251(string src)
        {
            

            for (int i = 0; i < utf.Length; i++)
                try
                {
                    ansi[i] = Convert.ToByte(i + 0xC0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("i = {0}, ansi = {1} len = {2}", i, i + 0xC0);
                }

            char[] dst = new char[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                char c = Convert.ToChar(src.Substring(i, 1));
                byte b = IsCyr(c);
                if (b == 0)
                    dst[i] = c;
                else
                    dst[i] = Convert.ToChar(b);
            }

            return new string(dst);
        }
    
    }
}
