using System;
using log4net;
using log4net.Config;
using System.Security.Principal;

namespace Ipk.Custom.Lombard.SmsSenderService
{
    public static class Log
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(Log));

        public static int InitInputData;
        static int n;
        public static int Number
        {
            get
            {
                n = Math.Abs(n - 1);
                return n + 1;
            }
        }

        static Log()
        {
            XmlConfigurator.Configure();
        }

        public static void Start()
        {
            var user = WindowsIdentity.GetCurrent();

            Debug("WindowsIdentity.Name: " + user.Name);
            Debug("WindowsIdentity.AuthenticationType: " + user.AuthenticationType);
            Debug("WindowsIdentity.ImpersonationLevel: " + user.ImpersonationLevel.ToString());
            Debug("WindowsIdentity.IsAnonymous: " + user.IsAnonymous);
            Debug("WindowsIdentity.IsAuthenticated: " + user.IsAuthenticated);
            Debug("WindowsIdentity.IsGuest: " + user.IsGuest);
            Debug("WindowsIdentity.IsSystem: " + user.IsSystem);
            Debug("WindowsIdentity.Owner: " + user.Owner.ToString());
            Debug("WindowsIdentity.Token: " + user.Token.ToString());
            Debug("WindowsIdentity.User: " + user.User.ToString());

            Debug("Environment.UserName: " + Environment.UserName);
            Debug("Environment.UserDomainName: " + Environment.UserDomainName);
            Debug("Environment.MachineName: " + Environment.MachineName);
        }

        public static void Debug(string info)
        {
            Logger.Debug(info);
        }
        public static void Debug(string format, object arg0)
        {
            Debug(string.Format(format, arg0));
        }
        public static void Debug(string format, object arg0, object arg1)
        {
            Debug(string.Format(format, arg0, arg1));
        }
        public static void Debug(string format, object arg0, object arg1, object arg2)
        {
            Debug(string.Format(format, arg0, arg1, arg2));
        }
        public static void Debug(string format, params object[] args)
        {
            Debug(string.Format(format, args));
        }

        public static void DebugInfo(string format, params object[] args)
        {
            Logger.DebugFormat(format, args);
        }

        public static void Error(string info)
        {
            Console.WriteLine(info);
            Logger.Error(info);
        }
        public static void Error(string format, Exception ex)
        {
            Logger.Error(format, ex);
        }
        public static void Error(string format, object arg0, object arg1)
        {
            Error(string.Format(format, arg0, arg1));
        }
        public static void Error(string format, object arg0, object arg1, object arg2)
        {
            Error(string.Format(format, arg0, arg1, arg2));
        }
        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }

        public static void InfoFormat(string format, params object[] args)
        {
            Logger.InfoFormat(format, args);
        }

        public static void Info(string info)
        {
            Logger.Info(info);
        }
    }
}
