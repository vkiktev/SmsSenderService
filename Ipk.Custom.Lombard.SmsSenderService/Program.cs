using Ipk.Custom.Lombard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Ipk.Custom.Lombard.SmsSenderService
{
    static class Program
    {
        private static SmsSenderService _service;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Start(args);
        }

        public static void Start(string[] args)
        {
            try
            {
                Log.Info("[01] Starting service Argo SMS");
                PrintInfo();

                _service = new SmsSenderService();

                ServiceBase.Run(_service);
            }
            catch (Exception ex)
            {
                Log.Error("[01] Error starting service", ex);
                throw;
            }
            finally
            {
                ((IDisposable)_service).Dispose();
            }
        }

        private static void PrintInfo()
        {
            // Текущий пользователь.
            var user = WindowsIdentity.GetCurrent();

            Log.InfoFormat("[===============================================]");

            Log.InfoFormat("[01] WindowsIdentity != null: {0}", user != null);

            if (user == null) return;

            Log.Info("[01] WindowsIdentity.Name: " + user.Name);
            Log.Info("[01] WindowsIdentity.AuthenticationType: " + user.AuthenticationType);
            Log.Info("[01] WindowsIdentity.ImpersonationLevel: " + user.ImpersonationLevel.ToString());
            Log.Info("[01] WindowsIdentity.IsAnonymous: " + user.IsAnonymous);
            Log.Info("[01] WindowsIdentity.IsAuthenticated: " + user.IsAuthenticated);
            Log.Info("[01] WindowsIdentity.IsGuest: " + user.IsGuest);
            Log.Info("[01] WindowsIdentity.IsSystem: " + user.IsSystem);
            Log.Info("[01] WindowsIdentity.Owner: " + user.Owner.ToString());
            Log.Info("[01] WindowsIdentity.Token: " + user.Token.ToString());
            Log.Info("[01] WindowsIdentity.User: " + user.User.ToString());

            Log.Info("[01] Environment.UserName: " + Environment.UserName);
            Log.Info("[01] Environment.UserDomainName: " + Environment.UserDomainName);
            Log.Info("[01] Environment.MachineName: " + Environment.MachineName);

            Log.InfoFormat("[===============================================]");
        }

    }
}
