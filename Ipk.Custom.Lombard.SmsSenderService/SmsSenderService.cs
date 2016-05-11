using Ipk.Custom.Lombard.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipk.Custom.Lombard.SmsSenderService
{
    public partial class SmsSenderService : ServiceBase
    {
        private Timer _timer;
        public TimeSpan SchedulerTimerInterval = TimeSpan.FromMinutes(5);
        public int _backwardScaningDeep = 5;
        public int _scaningPeriod = 5;
        public bool _isDebugMode = true;

        public const string serviceName = "Ipk.Custom.Lombard.SmsSenderService";
        public const string _smscConfigTemplate = "<?xml version=\"1.0\" encoding=\"utf-8\"?> "+
                                                   "<SMSC xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"> "+
                                                   "   <Description>example</Description> "+
                                                   "   <Host>{0}</Host> "+
                                                   "   <Port>{1}</Port> "+
                                                   "   <SystemId>{2}</SystemId> "+
                                                   "   <Password>{3}</Password> "+
                                                   "   <SystemType>smpp</SystemType> "+
                                                   "   <AddrTon>1</AddrTon> "+
                                                   "   <AddrNpi>1</AddrNpi> "+
                                                   "   <SMSFrom>{4}</SMSFrom> " +
                                                   "   <AddressRange /> "+
                                                   " </SMSC>";
        public string _smscConfig;

        public NewsletterSchedule _newsletterSchedule;

        // Methods
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public SmsSenderService()
        {
            InitializeComponent();

            CanHandlePowerEvent = true;
            CanPauseAndContinue = false;
            CanHandleSessionChangeEvent = false;
            CanShutdown = true;
            CanStop = true;
            
            Log.InfoFormat("[01] Initialize...");
            
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

            try
            {
                var hostValue = ConfigurationManager.AppSettings["Host"];
                var portValue = ConfigurationManager.AppSettings["Port"];

                var systemIdValue = ConfigurationManager.AppSettings["SystemId"];
                var passwordValue = ConfigurationManager.AppSettings["Password"];

                var smsFromValue = ConfigurationManager.AppSettings["SMSFrom"];

                var period = ConfigurationManager.AppSettings["ScaningPeriod"];
                if (!int.TryParse(period, out _scaningPeriod) || (_scaningPeriod <= 0 || _scaningPeriod > 1440))
                    _scaningPeriod = 5;

                var deep = ConfigurationManager.AppSettings["BackwardScaningDeep"];
                if (!int.TryParse(deep, out _backwardScaningDeep) || (_backwardScaningDeep <= 0 || _backwardScaningDeep > 1440))
                    _backwardScaningDeep = 5;

                string debugMode = ConfigurationManager.AppSettings["IsDebugMode"];
                if (!bool.TryParse(debugMode, out _isDebugMode))
                    _isDebugMode = true;
                               
                int port;
                if (hostValue != null && portValue != null
                            && int.TryParse(portValue, out port)
                            && systemIdValue != null
                            && passwordValue != null
                            && smsFromValue != null)
                    _smscConfig = string.Format(_smscConfigTemplate, hostValue, port, systemIdValue, passwordValue, smsFromValue);
            }
            catch(Exception ex)
            {
                Log.Error("[00] Error while initialize service.", ex);
                throw;
            }

            Log.InfoFormat("[01] IsDebugMode {0}", _isDebugMode);

            SchedulerTimerInterval = TimeSpan.FromMinutes(_scaningPeriod);

            _newsletterSchedule = new NewsletterSchedule(_smscConfig, ConfigurationManager.ConnectionStrings["Default"].ConnectionString, _backwardScaningDeep, _isDebugMode);
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            OnUnhandledException((Exception)e.ExceptionObject);
        }

        private static void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            OnUnhandledException(e.Exception);
            e.SetObserved();
        }

        private static void OnUnhandledException(Exception exception) //OnError
        {            
            Log.Error("[10] Unhandled exception", exception);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();            
        }   

        static SmsSenderService()
        {            
        }

        private void InitializeComponent()
        {
            components = new Container();
            ServiceName = serviceName;
        }

        internal void Start(string[] args)
        {
            try
            {
                Log.Info("[11] Start sms service");

                _timer = new Timer(state => OnTimer(), this, Timeout.Infinite, Timeout.Infinite);
                _timer.Change(0, (long)SchedulerTimerInterval.TotalMilliseconds);                
            }
            catch (Exception exception)
            {
                Log.Error("[11] Error sms service", exception);
                throw;
            }
        }

        private object _syncRoot = new object();

        private void OnTimer()
        {
            try
            {
                Log.Info("[12] OnTimer method begin");
                _newsletterSchedule.CheckAndSend();
                Log.Info("[12] OnTimer method end");
            }
            catch (Exception exception)
            {
                Log.Error("[12] Error at OnTimer method. ", exception);
                throw;
            }
        }

        protected override void OnStart(string[] args)
        {
            this.RequestAdditionalTime(30000);
            Start(args);
        }

        protected override void OnStop()
        {
            Log.Info("[13] OnStop method begin");
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();
       
            ExitCode = 0;

            Log.Info("[13] OnStop method end");
        }
        
        protected override void OnShutdown()
        {
            Log.Info("[14] Shutdown service");
            base.OnShutdown();
        }

        protected override void OnPause()
        {
            Log.Info("[15] Pause service");
            base.OnPause();
        }

        protected override void OnContinue()
        {
            Log.Info("[16] Continue service");
            base.OnContinue();
        }
    }
}
