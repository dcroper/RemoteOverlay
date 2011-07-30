/**
 * RemoteOverlayServer
 * Copyright (C) 2011 David Roper
 * 
 * OverlayServer.cs
 */

using System;
using System.Collections.Generic;
using System.ServiceModel;
using RemoteOverlayInterfaceLib;
using RemoteOverlayServiceLib;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using RemoteOverlayServer.Properties;
using System.Text;

namespace RemoteOverlayServer
{
    class OverlayServer : OverlayService
    {
        ServiceHost m_host;
        Thread m_udpThread = null;
        Socket m_udpSoc = null;
        volatile bool m_running = false;

        public OverlayServer()
        {
            m_host = new ServiceHost(this);
        }

        ~OverlayServer()
        {
            stop();
        }

        public void start()
        {
            if (!m_running)
            {
                m_running = true;
                m_udpSoc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_udpThread = new Thread(new ThreadStart(this.udpThreadRun));
                m_udpThread.IsBackground = true;
                m_udpThread.Start();
                m_host.Open();
            }
        }

        public void stop()
        {
            m_running = false;
            if (m_udpSoc != null)
            {
                m_udpSoc.Close();
            }
            if (m_udpThread != null)
            {
                m_udpThread.Interrupt();
                m_udpThread = null;
            }
            m_host.Close();
            lock (m_messageListUpdateCallbacks)
            {
                m_messageListUpdateCallbacks.Clear();
            }
            lock (m_messages)
            {
                m_messages.Clear();
            }
            lock (m_callbackQueue)
            {
                m_callbackQueue.Clear();
            }
        }

        public bool isRunning()
        {
            return m_running;
        }

        public void subscribeMessageListUpdate(IOverlayCallback callback)
        {
            lock (m_messageListUpdateCallbacks)
            {
                m_messageListUpdateCallbacks.Add(callback);
            }
        }

        public void unsubscribeMessageListUpdate(IOverlayCallback callback)
        {
            lock (m_messageListUpdateCallbacks)
            {
                m_messageListUpdateCallbacks.Remove(callback);
            }
        }

        private void udpThreadRun()
        {
            byte[] data = new byte[1024];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, OverlayServerMessages.UDP_PORT);
            m_udpSoc.Bind(localEndPoint);
            while (m_running)
            {
                try
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int bytesRecv = m_udpSoc.ReceiveFrom(data, ref remoteEndPoint);
                    string recvStr = Encoding.UTF8.GetString(data, 0, bytesRecv);
                    if (OverlayServerMessages.isDiscoveryMessage(recvStr))
                    {
                        byte[] retData = Encoding.UTF8.GetBytes(OverlayServerMessages.DiscoveryResponse(":4895/OverlayService"));
                        m_udpSoc.SendTo(retData, remoteEndPoint);
                    }
                }
                catch
                {
                    m_udpSoc.Close();
                    return;
                }
            }
        }
    }
}
