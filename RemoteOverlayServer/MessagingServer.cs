using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteOverlayMessagingLib;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace RemoteOverlayServer
{
    public class MessagingServer
    {
        private delegate bool HandlerFunction(byte[] message, int length);
        private TcpListener m_listener = null;
        private Thread m_thread = null;
        private Dictionary<int, RemoteOverlayMessagingLib.Messages.IOverlayMessageHandler> m_handlers;
        private bool m_started = false;

        public MessagingServer()
        {
            m_handlers = new Dictionary<int, RemoteOverlayMessagingLib.Messages.IOverlayMessageHandler>();
        }

        ~MessagingServer()
        {
            stop();
        }

        public void start(int port)
        {
            if (!m_started)
            {
                m_started = true;
                m_listener = new TcpListener(IPAddress.Any, port);
                m_listener.Start();
                m_thread = new Thread(new ParameterizedThreadStart(MessagingServer.run));
                m_thread.Start(this);
            }
        }

        public void stop()
        {
            m_started = false;
            if (m_listener != null)
            {
                m_listener.Stop();
                m_listener = null;
            }
            try
            {
                if (m_thread != null)
                {
                    m_thread.Join();
                    m_thread = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while stopping EventMessagingServer. " + e.ToString());
            }
        }

        public void registerMessageHandler(RemoteOverlayMessagingLib.Messages.IOverlayMessageHandler handler)
        {
            m_handlers.Add(handler.getMessageId(), handler);
        }

        private static void run(object data)
        {
            MessagingServer server = (MessagingServer)(data);
            while (server.m_started)
            {
                try
                {
                    Socket soc = server.m_listener.AcceptSocket();
                    if (soc == null)
                    {
                        continue;
                    }
                    try
                    {
                        byte[] messageHeader = new byte[8];
                        int index = 0;
                        while (index < 8)
                        {
                            int recByteCount = soc.Receive(messageHeader, index, 8 - index, SocketFlags.None);
                            index += recByteCount;
                        }
                        Int32 messageId = new Int32();
                        Int32 length = new Int32();
                        MessageEncoder.decodeMessageHeader(messageHeader, ref messageId, ref length);
                        RemoteOverlayMessagingLib.Messages.IOverlayMessageHandler handler = server.m_handlers[messageId];
                        if (handler != null)
                        {
                            byte[] message = new byte[length];
                            index = 0;
                            while (index < length)
                            {
                                int count = soc.Receive(message, index, length - index, SocketFlags.None);
                                index += count;
                            }

                            HandlerFunction func = new HandlerFunction(handler.handleNetworkMessage);
                            HandlerDoneObject a = new HandlerDoneObject();
                            a.func = func;
                            a.soc = soc;
                            func.BeginInvoke(message, length, MessagingServer.messageHandlerDone, a);
                        }
                        else
                        {
                            soc.Close();
                        }
                    }
                    catch (Exception)
                    {
                        if (soc != null)
                        {
                            soc.Close();
                        }
                    }
                }
                catch (SocketException)
                {
                    // do nothing
                }
            }
        }

        private class HandlerDoneObject
        {
            public Socket soc = null;
            public HandlerFunction func = null;
        }

        private static void messageHandlerDone(IAsyncResult ar)
        {
            HandlerDoneObject a = (HandlerDoneObject)ar.AsyncState;
            bool result = a.func.EndInvoke(ar);
            if (result)
            {
                RemoteOverlayMessagingLib.Messages.ConfirmMessage mes = new RemoteOverlayMessagingLib.Messages.ConfirmMessage();
                byte[] bytes = mes.createNetworkMessage();
                byte[] header = MessageEncoder.createMessageHeader(mes.getMessageId(), bytes.Length);
                a.soc.Send(header, header.Length, SocketFlags.None);
                a.soc.Send(bytes, bytes.Length, SocketFlags.None);
            }
            a.soc.Close();
        }

        public bool isRunning()
        {
            return m_started;
        }
    }
}
