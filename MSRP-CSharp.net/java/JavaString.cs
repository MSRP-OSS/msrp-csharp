using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace msrp.java
{
    static public class JavaString
    {
        static public string Create(byte[] data, string encoding)
        {
            return Create(data, 0, data.Length, Encoding.GetEncoding(encoding));
        }

        static public string Create(byte[] data, Encoding encoding)
        {
            return Create(data, 0, data.Length, encoding);
        }

        static public string Create(byte[] data, int offset, int length, string encoding)
        {
            return Create(data, offset, length, Encoding.GetEncoding(encoding));
        }

        static public string Create(byte[] data, int offset, int length, Encoding encoding)
        {
            return encoding.GetString(data, offset, length);
        }

        static public byte[] GetBytes(this string String, string encoding)
        {
            return String.GetBytes(Encoding.GetEncoding(encoding));
        }

        static public byte[] GetBytes(this string String, Encoding encoding)
        {
            return encoding.GetBytes(String);
        }
    }
}
