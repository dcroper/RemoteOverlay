/**
 * RemoteOverlayClient
 * Copyright (C) 2011 David Roper
 * 
 * OverlayServiceProxy.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteOverlayInterfaceLib;
using System.ServiceModel;
using System.Threading;
using System.ServiceModel.Description;
using System.Net;
using System.Net.Sockets;

namespace RemoteOverlayClient
{
    enum OverlayServiceProxyActionType
    {
        AddMessage,
        RemoveMessage
    }

    class OverlayServiceProxyAction
    {
        public OverlayServiceProxyActionType m_type;
        public Object[] m_args;

        public OverlayServiceProxyAction(OverlayServiceProxyActionType type, Object[] args)
        {
            m_type = type;
            m_args = args;
        }
    }

    class OverlayServiceProxy : IOverlayCallback
    {
        private DuplexChannelFactory<IOverlayService> m_pipeFactory;
        private IOverlayService m_serviceProxy = null;
        private InstanceContext m_instanceContext;
        private IOverlayServiceProxyCallback m_callback;
        private Thread m_comThread;
        private CommunicationState m_state;
        private Object m_activity = new Object();
        private uint m_activityCount = 0;
        private Queue<OverlayServiceProxyAction> m_actions = new Queue<OverlayServiceProxyAction>();
        private bool m_performingAction = false;
        private volatile bool m_done = false;

        public OverlayServiceProxy(IOverlayServiceProxyCallback callback)
        {
            m_callback = callback;
            m_instanceContext = new InstanceContext(this);
            m_comThread = new Thread(new ThreadStart(comThreadRun));
            m_comThread.IsBackground = true;
            changeState(CommunicationState.Created);
            activityOccured();
            m_comThread.Start();
        }

        ~OverlayServiceProxy()
        {
            m_done = true;
            m_comThread.Interrupt();
        }

        private void comThreadRun()
        {
            while (!m_done)
            {
                try
                {
                    lock (m_activity)
                    {
                        if (m_activityCount == 0 && m_actions.Count == 0 )
                        {
                            Monitor.Wait(m_activity, 15000);
                        }
                        if (m_activityCount > 0)
                        {
                            m_activityCount--;
                        }
                    }
                    switch (m_state)
                    {
                        case CommunicationState.Created:
                            onCreated();
                            break;
                        case CommunicationState.Opening:
                            onOpening();
                            break;
                        case CommunicationState.Opened:
                            onOpened();
                            break;
                        case CommunicationState.Closing:
                            onClosing();
                            break;
                        case CommunicationState.Closed:
                            onClosed();
                            break;
                        case CommunicationState.Faulted:
                            onFaulted();
                            break;
                    }
                }
                catch (ThreadInterruptedException)
                {
                    onClosing();
                    return;
                }
                catch (ThreadAbortException)
                {
                    onClosing();
                    return;
                }
                catch (Exception e)
                {
                    changeState(CommunicationState.Faulted);
                    activityOccured();
                }
            }
        }

        private void activityOccured()
        {
            lock (m_activity)
            {
                m_activityCount++;
                Monitor.Pulse(m_activity);
            }
        }

        private void onCreated()
        {
            // Use udp probe to get path to WCF service
            string path = null;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Broadcast, OverlayServerMessages.UDP_PORT);
            Socket udpSoc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSoc.ReceiveTimeout = 5000;
            udpSoc.EnableBroadcast = true;
            udpSoc.Bind(localEndPoint);
            byte[] data = Encoding.UTF8.GetBytes(OverlayServerMessages.DiscoveryMessage());
            udpSoc.SendTo(data, ipep);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            EndPoint tmpEP = (EndPoint)remoteEP;
            byte[] recvData = new byte[1024];
            int byteCount = udpSoc.ReceiveFrom(recvData, ref tmpEP);
            udpSoc.Close();
            if (byteCount != 0)
            {
                string recvStr = Encoding.UTF8.GetString(recvData, 0, byteCount);
                path = OverlayServerMessages.getDiscoveryResponsePath(recvStr);
                path = "net.tcp://" + ((IPEndPoint)tmpEP).Address.ToString() + path;
            }
            else
            {
                activityOccured();
                return;
            }

            NetTcpBinding noSecBinding = new NetTcpBinding(SecurityMode.None);
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(IOverlayService)), noSecBinding, new EndpointAddress(path));
            m_pipeFactory = new DuplexChannelFactory<IOverlayService>(m_instanceContext, endpoint);
            m_pipeFactory.Closed += new EventHandler(m_pipeFactory_Closed);
            m_pipeFactory.Faulted += new EventHandler(m_pipeFactory_Faulted);
            changeState(CommunicationState.Opening);
            activityOccured();
        }

        private void onOpening()
        {
            m_pipeFactory.Open(new TimeSpan(0,0,0,5));
            m_serviceProxy = m_pipeFactory.CreateChannel();
            m_serviceProxy.subscribeMessageListUpdate();
            m_callback.initializeMessages(m_serviceProxy.getDisplayMessages().ToArray());
            changeState(CommunicationState.Opened);
        }

        private void onOpened()
        {
            OverlayServiceProxyAction action = null;
            lock (m_actions)
            {
                if (m_actions.Count > 0)
                {
                    action = m_actions.Dequeue();
                }
            }
            if (action != null)
            {
                m_performingAction = true;
                switch (action.m_type)
                {
                    case OverlayServiceProxyActionType.AddMessage:
                        m_serviceProxy.addDisplayMessage(action.m_args[0] as string);
                        break;
                    case OverlayServiceProxyActionType.RemoveMessage:
                        m_serviceProxy.removeDisplayMessage(action.m_args[0] as string);
                        break;
                }
                m_performingAction = false;
            }
            else
            {
                if (!m_serviceProxy.ping())
                {
                    changeState(CommunicationState.Faulted);
                    activityOccured();
                }
            }
        }

        private void onClosing()
        {
            if (m_serviceProxy != null)
            {
                m_serviceProxy.unsubscribeMessageListUpdate();
                m_serviceProxy = null;
            }
            if (m_pipeFactory != null)
            {
                m_pipeFactory.Close();
                m_pipeFactory = null;
            }
            changeState(CommunicationState.Closed);
            activityOccured();
        }

        private void onClosed()
        {

        }

        private void onFaulted()
        {
            m_serviceProxy = null;
            m_pipeFactory = null;
            try
            {
                if (m_performingAction)
                {
                    m_performingAction = false;
                    m_callback.onError("Error: Action could not be performed. Lost connection to server.");
                }
            }
            catch
            {
                // do nothing
            }
            changeState(CommunicationState.Created);
            activityOccured();
        }

        private void m_pipeFactory_Faulted(object sender, EventArgs e)
        {
            DuplexChannelFactory<IOverlayCallback> snd = sender as DuplexChannelFactory<IOverlayCallback>;
            if (m_pipeFactory != null && ReferenceEquals(snd, m_pipeFactory))
            {
                changeState(CommunicationState.Faulted);
                activityOccured();
            }
        }

        private void m_pipeFactory_Closed(object sender, EventArgs e)
        {
            DuplexChannelFactory<IOverlayCallback> snd = sender as DuplexChannelFactory<IOverlayCallback>;
            if (m_pipeFactory != null && ReferenceEquals(snd, m_pipeFactory))
            {
                changeState(CommunicationState.Faulted);
                activityOccured();
            }
        }

        private void assertOpenedState()
        {
            if (m_state != CommunicationState.Opened)
            {
                throw new CommunicationException("Connection is in invalid state: " + m_state.ToString());
            }
        }

        private void changeState(CommunicationState s)
        {
            m_state = s;
            m_callback.onStateChange(m_state);
        }

        #region Public Methods

        public void addDisplayMessage(string message)
        {
            assertOpenedState();
            lock (m_actions)
            {
                m_actions.Enqueue(new OverlayServiceProxyAction(OverlayServiceProxyActionType.AddMessage, new Object[] {message}));
            }
            activityOccured();
        }

        public void removeDisplayMessage(string message)
        {
            assertOpenedState();
            lock (m_actions)
            {
                m_actions.Enqueue(new OverlayServiceProxyAction(OverlayServiceProxyActionType.RemoveMessage, new Object[] {message}));
            }
            activityOccured();
        }

        public CommunicationState getState()
        {
            return m_state;
        }

        #endregion

        #region IOverlayCallback Members

        public void onMessageAdded(string message)
        {
            m_callback.onMessageAdded(message);
        }

        public void onMessageRemoved(string message)
        {
            m_callback.onMessageRemoved(message);
        }

        #endregion
    }
}
