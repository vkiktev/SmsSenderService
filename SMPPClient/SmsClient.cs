/*
 * EasySMPP - SMPP protocol library for fast and easy
 * SMSC(Short Message Service Centre) client development
 * even for non-telecom guys.
 * 
 * Easy to use classes covers all needed functionality
 * for SMS applications developers and Content Providers.
 * 
 * Written for .NET 2.0 in C#
 * 
 * Copyright (C) 2006 Balan Andrei, http://balan.name
 * 
 * Licensed under the terms of the GNU Lesser General Public License:
 * 		http://www.opensource.org/licenses/lgpl-license.php
 * 
 * For further information visit:
 * 		http://easysmpp.sf.net/
 * 
 * 
 * "Support Open Source software. What about a donation today?"
 *
 * 
 * File Name: SmsClient.cs
 * 
 * File Authors:
 * 		Balan Name, http://balan.name
 */
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.Net;

namespace SMPP
{
    public class SmsClient
    {
        private SMPPClient smppClient;
        private int waitForResponse = 30000;
        private SortedList<int, AutoResetEvent> events = new SortedList<int,AutoResetEvent>();
        private SortedList<int, int> statusCodes = new SortedList<int, int>();
        private bool currentSending;

        #region Properties
        
        public int WaitForResponse
        {
            get { return waitForResponse; }
            set { waitForResponse = value; }
        }

        #endregion Properties

        #region Public functions

        public SmsClient(string xmlConfig)
        {
            smppClient = new SMPPClient();
            
            smppClient.OnDeliverSm += new DeliverSmEventHandler(onDeliverSm);
            smppClient.OnSubmitSmResp += new SubmitSmRespEventHandler(onSubmitSmResp);

            smppClient.OnLog += new LogEventHandler(onLog);
            smppClient.LogLevel = LogLevels.LogErrors;

            LoadConfig(xmlConfig);

            smppClient.Connect();
        }

        private void LoadConfig(string xmlConfig)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SMSC));

                using (StringReader sr = new StringReader(xmlConfig))
                {
                    SMSC smsc = (SMSC)serializer.Deserialize(sr);
                    IPHostEntry entry = Dns.GetHostEntry(smsc.Host);
                    smsc.Host = entry.AddressList[0].ToString();
                    smppClient.AddSMSC(smsc);
                }
            }
            catch (Exception ex)
            {
                onLog(new LogEventArgs("Error on loading config: " + ex.Message));
            }
        }

        public void Connect()
        {
            smppClient.Connect();
        }

        public void Disconnect()
        {
            int counter = 0;
            while (currentSending)
            {
                Thread.Sleep(5000);
                if (++counter > 24)
                    break;
            }
            smppClient.Disconnect();
        }

        public bool SendSms(string to, string text)
        {
            bool result = false;
            int tryCount = 1;

            while (!smppClient.CanSend && !smppClient.HasErrors)
            {
                onLog(new LogEventArgs(string.Format("Waiting connection... {0} times", tryCount++)));
                Thread.Sleep(5000);
            }
            
            currentSending = true;

            if(!smppClient.HasErrors)
            {
                AutoResetEvent sentEvent;
                int sequence;
                lock (events)
                {
                    sequence = smppClient.SendSms(to, text);
                    sentEvent = new AutoResetEvent(false);
                    events[sequence] = sentEvent;
                }
                if (sentEvent.WaitOne(waitForResponse, true))
                {
                    lock (events)
                    {
                        events.Remove(sequence);
                    }
                    int statusCode;
                    bool exist;
                    lock (statusCodes)
                    {
                        exist = statusCodes.TryGetValue(sequence, out statusCode);
                    }
                    if (exist)
                    {
                        lock (statusCodes)
                        {
                            statusCodes.Remove(sequence);
                        }
                        if (statusCode == StatusCodes.ESME_ROK)
                            result = true;
                    }
                }
            }
            
            currentSending = false;
            return result;
        }

        #endregion Public functions

        #region Events

        public event NewSmsEventHandler OnNewSms;

        public event LogEventHandler OnLog;

        #endregion Events

        #region Private functions
        private void onDeliverSm(DeliverSmEventArgs args)
        {
            smppClient.sendDeliverSmResp(args.SequenceNumber, StatusCodes.ESME_ROK);
            if (OnNewSms != null)
                OnNewSms(new NewSmsEventArgs(args.From, args.To, args.TextString));
        }
        private void onSubmitSmResp(SubmitSmRespEventArgs args)
        {
            AutoResetEvent sentEvent;
            bool exist;
            lock (events)
            {
                exist = events.TryGetValue(args.Sequence, out sentEvent);
            }
            if (exist)
            {
                lock (statusCodes)
                {
                    statusCodes[args.Sequence] = args.Status;
                }
                sentEvent.Set();
            }
        }
        private void onLog(LogEventArgs args)
        {
            Console.WriteLine(args.Message);
            if (OnLog != null)
                OnLog(args);
        }

        #endregion
    }
}
