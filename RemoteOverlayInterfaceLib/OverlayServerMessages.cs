/**
 * RemoteOverlayInterfaceLib
 * Copyright (C) 2011 David Roper
 * 
 * OverlayServerMessages.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteOverlayInterfaceLib
{
    public static class OverlayServerMessages
    {
        private const string DISCOVERY_MESSAGE = "Are you a Remote Overlay Server?";
        private const string DISCOVERY_RESPONSE = "I am a Remote Overlay Server: ";

        public const int UDP_PORT = 4896;

        public static string DiscoveryMessage()
        {
            return DISCOVERY_MESSAGE;
        }

        public static bool isDiscoveryMessage(string message)
        {
            return message.Equals(DiscoveryMessage());
        }

        public static string DiscoveryResponse(string path)
        {
            return DISCOVERY_RESPONSE + path;
        }

        public static string getDiscoveryResponsePath(string message)
        {
            if (message.Substring(0, DISCOVERY_RESPONSE.Length).Equals(DISCOVERY_RESPONSE))
            {
                return message.Substring(DISCOVERY_RESPONSE.Length);
            }
            return null;
        }
    }
}
