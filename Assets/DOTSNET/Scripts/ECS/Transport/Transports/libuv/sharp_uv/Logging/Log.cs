// unity logger
using System;
using System.Globalization;

namespace NetUV.Core.Logging
{
    public static class Log
    {
        public static bool IsDebugEnabled = false;
        public static bool IsInfoEnabled = true;
        public static bool IsWarnEnabled = true;
        public static bool IsErrorEnabled = true;

        public static void Debug(object obj)
        {
            if (IsDebugEnabled)
            {
                Debug(obj, null);
            }

        }

        public static void Debug(object obj, Exception exception)
        {
            if (IsDebugEnabled)
            {
                UnityEngine.Debug.Log(exception);
            }
        }

        public static void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                DebugFormat(CultureInfo.CurrentCulture, format, args);
            }
        }

        public static void DebugFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!IsDebugEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            UnityEngine.Debug.Log(message);
        }

        public static void Info(object obj)
        {
            if (IsInfoEnabled)
            {
                Info(obj, null);
            }
        }

        public static void Info(object obj, Exception exception)
        {
            if (IsInfoEnabled)
            {
                UnityEngine.Debug.Log(obj + " " + exception);
            }
        }

        public static void InfoFormat(string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                InfoFormat(CultureInfo.CurrentCulture, format, args);
            }
        }

        public static void InfoFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!IsInfoEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            UnityEngine.Debug.Log(message);
        }

        public static void Warn(object obj)
        {
            if (IsWarnEnabled)
            {
                Warn(obj, null);
            }
        }

        public static void Warn(object obj, Exception exception)
        {
            if (IsWarnEnabled)
            {
                UnityEngine.Debug.LogWarning(exception);
            }
        }

        public static void Error(object obj, Exception exception)
        {
            if (IsErrorEnabled)
            {
                UnityEngine.Debug.LogError(exception);
            }
        }
    }
}
