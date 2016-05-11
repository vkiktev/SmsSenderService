using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Ipk.Custom.Lombard.SmsSenderService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public const string serviceName = "Ipk.Custom.Lombard.SmsSenderService";
        public const string displayName = "ArgoSmsSenderService";
        public const string description = "Сервис отправки SMS собщений АРГО";

        public ProjectInstaller()
        {
            InitializeComponent();
        }
        protected override void OnBeforeInstall(IDictionary savedState)
        {            
            base.OnBeforeInstall(savedState);
        }

        protected override void OnBeforeRollback(IDictionary savedState)
        {
            base.OnBeforeRollback(savedState);
        }
    }
}
