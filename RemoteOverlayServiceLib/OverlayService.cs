/**
 * RemoteOverlayServiceLib
 * Copyright (C) 2011 David Roper
 * 
 * OverlayService.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using RemoteOverlayInterfaceLib;
using System.Threading;

namespace RemoteOverlayServiceLib
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class OverlayService : IOverlayService
    {
        #region Member Variables

        protected HashSet<IOverlayCallback> m_messageListUpdateCallbacks = new HashSet<IOverlayCallback>();
        protected HashSet<string> m_messages = new HashSet<string>();
        protected Queue<ICallbackRunner> m_callbackQueue = new Queue<ICallbackRunner>();
        private Thread m_callbackThread;

        #endregion

        #region C'tor & D'tor

        public OverlayService()
        {
            m_callbackThread = new Thread(new ThreadStart(this.callbackThreadRun));
            m_callbackThread.IsBackground = true;
            m_callbackThread.Start();
        }

        ~OverlayService()
        {
            m_callbackThread.Interrupt();
            m_callbackThread.Abort();
        }

        #endregion

        #region IOverlayService Members

        public void addDisplayMessage(string message)
        {
            bool exists;
            lock (m_messages)
            {
                exists = m_messages.Add(message);
            }
            if (exists)
            {
                lock (m_callbackQueue)
                {
                    m_callbackQueue.Enqueue(new AddDisplayMessageCallbackRunner(message));
                    Monitor.Pulse(m_callbackQueue);
                }
            }
        }

        public void removeDisplayMessage(string message)
        {
            lock (m_messages)
            {
                m_messages.Remove(message);
            }
            lock (m_callbackQueue)
            {
                m_callbackQueue.Enqueue(new RemoveDisplayMessageCallbackRunner(message));
                Monitor.Pulse(m_callbackQueue);
            }
        }

        public HashSet<string> getDisplayMessages()
        {
            HashSet<string> retval;
            lock (m_messages)
            {
                retval = new HashSet<string>(m_messages);
            }
            return retval;
        }

        public void subscribeMessageListUpdate()
        {
            OperationContext.Current.Channel.Closed += new EventHandler(channelClosed);
            OperationContext.Current.Channel.Faulted += new EventHandler(channelFaulted);
            IOverlayCallback callback = OperationContext.Current.GetCallbackChannel<IOverlayCallback>();
            lock (m_messageListUpdateCallbacks)
            {
                m_messageListUpdateCallbacks.Add(callback);
            }
        }

        private void removeMessageListCallback(IOverlayCallback callback)
        {
            if (callback == null)
            {
                return;
            }
            lock (m_messageListUpdateCallbacks)
            {
                m_messageListUpdateCallbacks.Remove(callback);
            }
        }

        public void unsubscribeMessageListUpdate()
        {
            IOverlayCallback callback = OperationContext.Current.GetCallbackChannel<IOverlayCallback>();
            removeMessageListCallback(callback);
        }

        public bool ping()
        {
            return true;
        }

        private void channelFaulted(object sender, EventArgs e)
        {
            IOverlayCallback callback = sender as IOverlayCallback;
            removeMessageListCallback(callback);
        }

        private void channelClosed(object sender, EventArgs e)
        {
            IOverlayCallback callback = sender as IOverlayCallback;
            removeMessageListCallback(callback);
        }

        #endregion

        #region Callback Thread

        private void callbackThreadRun()
        {
            while (true)
            {
                ICallbackRunner runner;
                try
                {
                    runner = null;
                    lock (m_callbackQueue)
                    {
                        Monitor.Wait(m_callbackQueue);
                        runner = m_callbackQueue.Dequeue();
                    }
                    HashSet<IOverlayCallback> callbacks;
                    lock (m_messageListUpdateCallbacks)
                    {
                        callbacks = new HashSet<IOverlayCallback>(m_messageListUpdateCallbacks);
                    }
                    foreach (IOverlayCallback c in callbacks)
                    {
                        try
                        {
                            runner.execute(c);
                        }
                        catch (ThreadInterruptedException)
                        {
                            return;
                        }
                        catch (ThreadAbortException)
                        {
                            return;
                        }
                        catch
                        {
                            removeMessageListCallback(c);
                        }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
            }
        }

        #endregion
    }
}
