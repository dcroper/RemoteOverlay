/**
 * RemoteOverlayClient
 * Copyright (C) 2011 David Roper
 * 
 * IOverlayServiceProxyCallback.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace RemoteOverlayClient
{
    interface IOverlayServiceProxyCallback
    {
        void onMessageAdded(string message);

        void onMessageRemoved(string message);

        void initializeMessages(string[] messages);

        void onError(string message);

        void onStateChange(CommunicationState state);
    }
}
