using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RemoteOverlayClient
{
    class MessagingClient
    {
        public MessagingClient()
        {
            
        }

        public void sendMessage(RemoteOverlayMessagingLib.Messages.IOverlayMessage message, String host)
        {
            TcpClient tcpConnection = new TcpClient(host, 11253);
            if (tcpConnection.Connected)
            {
                byte[] body = message.createNetworkMessage();
                byte[] header = RemoteOverlayMessagingLib.MessageEncoder.createMessageHeader(message.getMessageId(), body.Length);
                tcpConnection.Client.Send(header, header.Length, SocketFlags.None);
                tcpConnection.Client.Send(body, body.Length, SocketFlags.None);
                tcpConnection.ReceiveTimeout = 20000;
                byte[] recHeader = new byte[8];
                int index = 0;
                while (index < 8)
                {
                    int count = tcpConnection.Client.Receive(recHeader, index, 8 - index, SocketFlags.None);
                    index += count;
                }
                Int32 messageId = new Int32();
                Int32 length = new Int32();
                RemoteOverlayMessagingLib.MessageEncoder.decodeMessageHeader(recHeader, ref messageId, ref length);
                if (messageId == RemoteOverlayMessagingLib.Messages.ConfirmMessage.MessageId)
                {
                    byte[] recBody = new byte[length];
                    index = 0;
                    while (index < length)
                    {
                        int count = tcpConnection.Client.Receive(recBody, index, length - index, SocketFlags.None);
                        index += count;
                    }
                    try
                    {
                        RemoteOverlayMessagingLib.Messages.ConfirmMessage conf = new RemoteOverlayMessagingLib.Messages.ConfirmMessage(recBody, length);
                    }
                    catch (Exception)
                    {
                        // Did not receive confirmation
                        tcpConnection.Close();
                        throw new Exception("Recieved invalid confirmation data");
                    }
                }
                else
                {
                    tcpConnection.Close();
                    throw new Exception("Recieved invalid confirmation message");
                }
            }
            else
            {
                tcpConnection.Close();
                throw new Exception("Could not connect to server");
            }
            tcpConnection.Close();
        }
    }
}
