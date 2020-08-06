// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-01 오후 7:28:24   
// @PURPOSE     : 로그 남겨주는 클래스
// ===============================

#if UNITY_4 || UNITY_5
#define UNITY
#endif

using Colorful;
using System.Drawing;

namespace CSharpSimpleIOCP.Network.Logger
{

    //출력할 로그레벨
    [System.Flags]
    public enum NetworkLogLevel : int
    {
        None = 0,
        Info = 1 << 0,
        Debug = 1 << 1,
        Error = 1 << 2,
        Warning = 1 << 3,
        FullOption = (Info | Debug | Error | Warning)
    }

    public static class NetworkLogger
    {
        private static INetworkLogger _Logger = null;
        private static object _LogLock = new object();
        private static NetworkLogLevel _PrintableLogLevel = NetworkLogLevel.None;//NetworkLogLevel.FullOption & ~NetworkLogLevel.Error;

        public static void SetLogger(INetworkLogger logger)
        {
            lock (_LogLock)
            {
                _Logger = logger;
            }
        }

        public static void SetPrintEnable(NetworkLogLevel printableLogLevel)
        {
            lock (_LogLock)
            {
                _PrintableLogLevel = printableLogLevel;
            }
        }

        public static void Write(NetworkLogLevel logLevel, string msg, params object[] args)
        {
            WriteLogic(logLevel, msg, args);
        }

        public static void WriteLine(NetworkLogLevel logLevel, string msg, params object[] args)
        {
            WriteLogic(logLevel, msg + System.Environment.NewLine, args);
        }

        public static void Write(NetworkLogLevel logLevel, string format, object arg)
        {
            WriteLogic(logLevel, format, arg);
        }

        public static void WriteLine(NetworkLogLevel logLevel, string format, object arg)
        {
            WriteLogic(logLevel, format + System.Environment.NewLine, arg);
        }

        private static void WriteLogic(NetworkLogLevel logLevel, string msg, params object[] args)
        {
            lock (_LogLock)
            {
                if (_Logger == null)
                {
#if UNITY
                    DefaultWriteInUnity(logLevel, msg, args); //나중에 클라쓸때 구현하자
#else
                    DefaultWriteInConsole(logLevel, msg, args);
#endif
                }
                else
                    _Logger.Write(logLevel, msg, args);
            }
        }



        private static void DefaultWriteInConsole(NetworkLogLevel logLevel, string msg, params object[] args)
        {
            Color originalColor = Console.ForegroundColor;
            Console.ForegroundColor = Color.GhostWhite;

            if (logLevel == NetworkLogLevel.Info && (_PrintableLogLevel & NetworkLogLevel.Info) == NetworkLogLevel.Info)
                Console.Write("[정보] ", Color.LightSeaGreen);
            else if (logLevel == NetworkLogLevel.Debug && (_PrintableLogLevel & NetworkLogLevel.Debug) == NetworkLogLevel.Debug)
                Console.Write("[디버그] ", Color.Gray);
            else if (logLevel == NetworkLogLevel.Error && (_PrintableLogLevel & NetworkLogLevel.Error) == NetworkLogLevel.Error)
                Console.Write("[오류] ", Color.Gray);
            else if (logLevel == NetworkLogLevel.Warning && (_PrintableLogLevel & NetworkLogLevel.Warning) == NetworkLogLevel.Warning)
                Console.Write("[경고] ", Color.Gray);
            else
                return;

            Console.Write(msg, args);
            Console.ForegroundColor = originalColor;
        }
    }
}
