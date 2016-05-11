using SMPP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipk.Custom.Lombard.SmsSender.TestConsoleApp
{
    class Program
    {
        public static int _backwardScaningDeep = 5;
        public static int _scaningPeriod = 5;
        public static bool _isDebugMode = true;

        public static string _smscConfigTemplate = "<?xml version=\"1.0\" encoding=\"utf-8\"?> " +
                                                     "<SMSC xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"> " +
                                                     "   <Description>example</Description> " +
                                                     "   <Host>{0}</Host> " +
                                                     "   <Port>{1}</Port> " +
                                                     "   <SystemId>{2}</SystemId> " +
                                                     "   <Password>{3}</Password> " +
                                                     "   <SystemType>smpp</SystemType> " +
                                                     "   <AddrTon>1</AddrTon> " +
                                                     "   <AddrNpi>1</AddrNpi> " +
                                                     "   <AddressRange /> " +
                                                     "   <SMSFrom>{4}</SMSFrom> " +
                                                     " </SMSC>";

        public static string _smscConfig;

        public static List<KeyValuePair<string, string>> _list;
        static void Main(string[] args)
        {
            _list = new List<KeyValuePair<string, string>>();

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
                                    
            _list.Add(new KeyValuePair<string, string>("79000000000", "Тестовое сообщение"));
            _list.Add(new KeyValuePair<string, string>("79000000000", "Тестовое сообщение"));

            Trace.WriteLine(_smscConfig);

            SendSMS();
        }

        private static void SendSMS()
        {
            try
            {
                SmsClient client = new SmsClient(_smscConfig);
                client.OnLog += client_OnLog;

                foreach (var item in _list)
                {
                    client.SendSms(item.Key, item.Value);
                }                

                client.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[23.2] SendSMS Error. {0} {1}", ex.Message, ex.StackTrace);
                throw;
            }

            Console.WriteLine("[23.2] SendSMS method end");
        }

        static void client_OnLog(LogEventArgs e)
        {
            Console.WriteLine("SmsClient LogMessage: {0}", e.Message);
        }
    }
}
