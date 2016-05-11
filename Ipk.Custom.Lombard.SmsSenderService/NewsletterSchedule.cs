using Ipk.Custom.Lombard.Data.NHibernate;
using Ipk.Custom.Lombard.Data.NHibernate.Base;
using Ipk.Custom.Lombard.Data.NHibernate.Dao;
using Ipk.Custom.Lombard.Model.Models;
using SMPP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipk.Custom.Lombard.SmsSenderService
{
    /// <summary>
    /// Class for sending SMSs by schedule
    /// </summary>
    public class NewsletterSchedule
    {
        private string _smscConfig;

        private ConcurrentQueue<KeyValuePair<string,string>> _smsSendQueue;        
        private volatile Thread _senderThread;
        private object _syncObject = new object();
        private int _backwardScaningDeep;
        private bool _isDebugMode;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="smscConfig">XML config for smpp client</param>
        /// <param name="connectionString">Connection string to database with customers and schedules</param>
        /// <param name="backwardScaningDeep">Depp for scanning in minutes</param>
        /// <param name="isDebugMode">Flag for debug mode</param>
        public NewsletterSchedule(string smscConfig, string connectionString, int backwardScaningDeep, bool isDebugMode)
        {
            _smscConfig = smscConfig;
            _backwardScaningDeep = backwardScaningDeep;
            _isDebugMode = isDebugMode;

            NHibernateManager.Initialize(connectionString);

            _smsSendQueue = new ConcurrentQueue<KeyValuePair<string,string>>();
        }

        /// <summary>
        /// Method for checking schedules and call method for sending SMS
        /// </summary>
        public void CheckAndSend()
        {
            Log.Info("[21] CheckAndSend method begin");

            UnitOfWork unitOfWork = new UnitOfWork();

            HibernateNewsletterDao newsletterDao = new HibernateNewsletterDao(unitOfWork);
            HibernateCustomerDao customerDao = new HibernateCustomerDao(unitOfWork);

            var newsletters = newsletterDao.GetUnlockedList();

            Dictionary<string, string> smsList = new Dictionary<string, string>();

            foreach(var item in newsletters)
            {
                Log.InfoFormat("[21] CheckAndSend method. Check newsletter item \"{0}\".", item.Name);

                if (ScheduleReady(item.Schedule))
                {
                    var list = customerDao.GetCustomerPawnInfoList(item.NewsletterFilterType,
                                            item.NewsletterFilterType == Model.NewsletterFilterType.DaysReturn ? item.FilterDaysReturnCount
                                                : (item.NewsletterFilterType == Model.NewsletterFilterType.DaysGracePeriod ? item.FilterDaysGracePeriodEndCount : 0));

                    var sendingList = CleanCustomerList(list, smsList, item);

                    AddSendingToQueue(sendingList);

                    foreach(var sms in sendingList)
                    {
                        if(!smsList.ContainsKey(sms.Key))
                            smsList.Add(sms.Key, sms.Value);
                    }
                }
            }

            Log.Info("[21] CheckAndSend method end");
        }

        /// <summary>
        /// Method for adding phone-numbers to queue for sending SMS
        /// </summary>
        /// <param name="sendingList"></param>
        private void AddSendingToQueue(Dictionary<string, string> sendingList)
        {
            Log.InfoFormat("[22] AddSendingToQueue method begin. Sending list count: {0}, current sending queue: {1}", sendingList.Count, _smsSendQueue.Count);
            lock (_syncObject)
            {
                foreach (var smsItem in sendingList)
                    _smsSendQueue.Enqueue(smsItem);

                if (_isDebugMode)
                    SendSMSDebug();
                else
                    SendSMS();
            }
            Log.Info("[22] AddSendingToQueue method end");
        }

        private void Handler(Exception ex)
        {
            Log.Error("[23.0] SendSMS Error.", ex);
            throw ex;
        }

        /// <summary>
        /// Method for sending SMS in debug-mode 
        /// </summary>
        private void SendSMSDebug()
        {
            Log.InfoFormat("[23.1] SendSMSDebug method begin. Thread Id: {0}, Sending queue count: {1}", Thread.CurrentThread.ManagedThreadId, _smsSendQueue.Count);
            try
            {
                KeyValuePair<string, string> sms;
                while (_smsSendQueue.TryDequeue(out sms))
                {
                    Log.InfoFormat("[23.1] SendSMSDebug sms.Phone {0}, sms.Value {1}", sms.Key, sms.Value);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Log.Error("[23.1] SendSMSDebug Error.", ex);
                throw;
            }
        }

        /// <summary>
        /// Method for sending SMS 
        /// </summary>
        private void SendSMS()
        {
            Log.InfoFormat("[23.2] SendSMS method begin. Thread Id: {0}, Sending queue count: {1}", Thread.CurrentThread.ManagedThreadId, _smsSendQueue.Count);
            try
            {                
                SmsClient client = new SmsClient(_smscConfig);
                client.OnLog += client_OnLog;    

                KeyValuePair<string, string> sms;
                while (_smsSendQueue.TryDequeue(out sms))
                {
                    client.SendSms(sms.Key, sms.Value);
                }

                client.Disconnect();
            }
            catch (Exception ex)
            {
                Log.Error("[23.2] SendSMS Error.", ex);
                throw;
            }

            Log.Info("[23.2] SendSMS method end");
        }

        private void client_OnLog(LogEventArgs e)
        {
            Log.InfoFormat("Logging operation. {0}", e.Message);
        }

        /// <summary>
        /// Method for preparing SMS text
        /// </summary>
        /// <param name="list">List of customers for making text for SMS</param>
        /// <param name="smsList">List of SMSs created earler</param>
        /// <param name="template">Template for making text for SMS</param>
        /// <returns></returns>
        private Dictionary<string, string> CleanCustomerList(IList<CustomerPawnInfo> list, Dictionary<string, string> smsList, Newsletter template)
        {
            Dictionary<string, string> newsletters = new Dictionary<string, string>();

            Log.InfoFormat("[25] CleanCustomerList method begin");

            foreach(var customerInfo in list)
            {
                string phone;
                if (!ValidatePhone(customerInfo.PhoneMobile1, customerInfo.PhoneMobile2, out phone))
                {
                    Log.InfoFormat("[25] Validate error: Name {0}, PhoneMobile1 {1}, PhoneMobile2 {2}", customerInfo.FullName, customerInfo.PhoneMobile1, customerInfo.PhoneMobile2);
                    continue;
                }

                if (!newsletters.ContainsKey(phone) && !smsList.ContainsKey(phone))
                {
                    var message = template.MessageTemplate.Replace("{ФИО}",string.Format("{0} {1} {2}",customerInfo.FirstName, customerInfo.MiddleName, customerInfo.Surname))
                                            .Replace("{ИО}",string.Format("{0} {1}",customerInfo.FirstName, customerInfo.MiddleName))
                                            .Replace("{ИМЯ}",customerInfo.FirstName)
                                            .Replace("{ДАТАВОЗВРАТА}",customerInfo.DateReturn.ToShortDateString())
                                            .Replace("{ДАТА_ЛП}",customerInfo.DateGracePeriodEnd.ToShortDateString())
                                            .Replace("{БИЛЕТ}",customerInfo.TicketNumber)
                                            .Replace("{ДАТАБИЛЕТ}",customerInfo.DateProcess.ToShortDateString());
                    newsletters.Add(phone, message);
                }
            }

            Log.InfoFormat("[25] CleanCustomerList method end");

            return newsletters;
        }

        /// <summary>
        /// Method for validate Phone-number
        /// </summary>
        /// <param name="phone1"></param>
        /// <param name="phone2"></param>
        /// <param name="validPhone"></param>
        /// <returns></returns>
        public bool ValidatePhone(string phone1, string phone2, out string validPhone)
        {
            validPhone = string.Empty;

            string mostFake1 = "0000000000";
            string mostFake2 = "1111111111";

            string phone = string.IsNullOrWhiteSpace(phone1) ? phone2 : phone1; 

            if(phone != null)
                phone = phone.Replace("(",string.Empty).Replace(")",string.Empty).Replace("-",string.Empty);
            else 
                return false;

             if((phone == mostFake1 || phone == mostFake2) || (phone.Length > 11 || phone.Length < 10))
                 return false;
            
            if(!phone.All(c=>char.IsDigit(c)))
                return false;

            if(phone[0] != '9')
                return false;

            validPhone = "7" + phone;

            return true;
        }

        /// <summary>
        /// Method for checking schedule
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private bool ScheduleReady(Schedule schedule)
        {
            Log.InfoFormat("[26] ScheduleReady method begin. Schedule {0}", schedule.ToString());

            bool result = false;

            var onceTimeStart = schedule.DateStart
                                    .AddHours(schedule.TimeExecute.Hour)
                                        .AddMinutes(schedule.TimeExecute.Minute);
            var todayTimeStart = DateTime.Today
                                    .AddHours(schedule.TimeExecute.Hour)
                                        .AddMinutes(schedule.TimeExecute.Minute);

            switch(schedule.ScheduleType)
            {
                case Model.ScheduleType.Once:
                    if(DateTime.Now.AddMinutes(-_backwardScaningDeep) <= onceTimeStart && onceTimeStart <= DateTime.Now )
                        result = true;
                    break;
                case Model.ScheduleType.EveryDay:
                    if (DateTime.Now > schedule.DateStart && DateTime.Now.AddMinutes(-_backwardScaningDeep) <= todayTimeStart && todayTimeStart <= DateTime.Now)
                        result = true;
                    break;
                case Model.ScheduleType.SpecifiedDays:
                    if (IsTodayScheduleDay(schedule.DaysOfWeek) &&
                            (DateTime.Now > schedule.DateStart && DateTime.Now.AddMinutes(-_backwardScaningDeep) <= todayTimeStart 
                                    && todayTimeStart <= DateTime.Now))
                        result = true;
                    break;
            }

            Log.InfoFormat("[26] ScheduleReady method end. Ready result {0}", result);

            return result;
        }

        private bool IsTodayScheduleDay(byte[] scheduleDays)
        {
            int day = ((int)DateTime.Today.DayOfWeek)-1;
            if (day == -1)
                day = 6;

            return Convert.ToBoolean(scheduleDays[day]);
        }
    }
}
