using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace RemoteOverlayInterfaceLib
{
    public interface IOverlayCallback
    {
        [OperationContract(IsOneWay = true)]
        void onMessageAdded(string message);

        [OperationContract(IsOneWay = true)]
        void onMessageRemoved(string message);
    }
}
