/**
 * RemoteOverlayInterfaceLib
 * Copyright (C) 2011 David Roper
 * 
 * IOverlayService.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using RemoteOverlayInterfaceLib;

namespace RemoteOverlayInterfaceLib
{
    [ServiceContract(SessionMode=SessionMode.Required, CallbackContract = typeof(IOverlayCallback))]
    public interface IOverlayService
    {
        [OperationContract]
        void addDisplayMessage(string message);

        [OperationContract]
        void removeDisplayMessage(string message);

        [OperationContract]
        HashSet<string> getDisplayMessages();

        [OperationContract]
        void subscribeMessageListUpdate();

        [OperationContract]
        void unsubscribeMessageListUpdate();

        [OperationContract]
        bool ping();
    }
}
