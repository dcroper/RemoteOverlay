using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteOverlayServer
{
    class DisplayMessage
    {
        public readonly string m_message;
        public int m_totalTimeDisplayed;
        public int m_curTimeDisplayed;

        public DisplayMessage(string message)
        {
            m_message = message;
            m_totalTimeDisplayed = 0;
            m_curTimeDisplayed = 0;
        }

        public override bool Equals(object obj)
        {
            try
            {
                DisplayMessage o = obj as DisplayMessage;
                return this.Equals(o);
            }
            catch
            {
                return false;
            }
        }

        public bool Equals(DisplayMessage m)
        {
            return m_message.Equals(m.m_message);
        }

        public override int GetHashCode()
        {
            return m_message.GetHashCode();
        }

        public override string ToString()
        {
            return m_message.ToString();
        }
    }
}
