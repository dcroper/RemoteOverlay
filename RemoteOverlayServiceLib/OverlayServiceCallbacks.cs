using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteOverlayInterfaceLib;
using System.ServiceModel;

namespace RemoteOverlayServiceLib
{
    public interface ICallbackRunner
    {
        void execute(IOverlayCallback callbacks);
    }

    class AddDisplayMessageCallbackRunner : ICallbackRunner
    {
        private string m_message;

        public AddDisplayMessageCallbackRunner(string message)
        {
            m_message = message;
        }

        #region CallbackRunner Members

        public void execute(IOverlayCallback callback)
        {
            callback.onMessageAdded(m_message);
        }

        #endregion
    }

    class RemoveDisplayMessageCallbackRunner : ICallbackRunner
    {
        private string m_message;

        public RemoveDisplayMessageCallbackRunner(string message)
        {
            m_message = message;
        }

        #region CallbackRunner Members

        public void execute(IOverlayCallback callback)
        {
            callback.onMessageRemoved(m_message);
        }

        #endregion
    }

}