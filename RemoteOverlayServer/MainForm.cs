using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ServiceModel;
using RemoteOverlayInterfaceLib;

namespace RemoteOverlayServer
{
    public partial class MainForm : Form, IOverlayCallback
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        private const int MIN_DISPLAY_TIME_MS = 15000;
        private const int MAX_DISPLAY_TIME_MS = 60000;

        private delegate void AddListItem(string s);
        private delegate void RemoveListItem(string s);

        private OverlayForm m_overlayForm = new OverlayForm();
        private AboutBox m_aboutBox = new AboutBox();
        private bool m_overlayVisible = false;
        private bool m_overlayShown = false;
        private OverlayServer m_server = null;
        private LinkedList<DisplayMessage> m_messages = new LinkedList<DisplayMessage>();
        private LinkedListNode<DisplayMessage> m_currentMessage = null;
        private int m_lastTime = 0;
        private AddListItem m_addMessageDelegate;
        private RemoveListItem m_removeMessageDelegate;

        public MainForm()
        {
            InitializeComponent();
            xPosBox.Value = Properties.Settings.Default.OverlayXPos;
            yPosBox.Value = Properties.Settings.Default.OverlayYPos;
            widthBox.Value = Properties.Settings.Default.OverlayWidth;
            heightBox.Value = Properties.Settings.Default.OverlayHeight;
            fontSizeBox.Value = (decimal)Properties.Settings.Default.OverlayFontSize;
            setOverlayLocation();
            m_overlayForm.setFontSize(Properties.Settings.Default.OverlayFontSize);
            m_overlayForm.Width = Properties.Settings.Default.OverlayWidth;
            m_overlayForm.Height = Properties.Settings.Default.OverlayHeight;
            m_addMessageDelegate = new AddListItem(this.addMessageToList);
            m_removeMessageDelegate = new RemoveListItem(this.removeMessageFromList);
            setOverlayVisible(false);
        }

        private void addMessageToList(string message)
        {
            CurrentMessages.Items.Add(message);
            m_messages.AddLast(new DisplayMessage(message));
            updateOverlay();
        }

        private void removeMessageFromList(string message)
        {
            CurrentMessages.Items.Remove(message);
            LinkedListNode<DisplayMessage> n = m_messages.Find(new DisplayMessage(message));
            if (n != null)
            {
                if (ReferenceEquals(n, m_currentMessage))
                {
                    m_currentMessage = m_currentMessage.Next;
                }
                m_messages.Remove(n);
            }
            updateOverlay();
        }

        private void setOverlayVisible(bool visible)
        {
            if (m_overlayVisible != visible)
            {
                if (visible)
                {
                    if (!m_overlayShown)
                    {
                        m_overlayForm.Show();
                        m_overlayShown = true;
                    }
                    m_overlayForm.Visible = true;
                }
                else
                {
                    m_overlayForm.Visible = false;
                }
                m_overlayVisible = visible;
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (m_server == null || !m_server.isRunning())
            {
                m_server = new OverlayServer();
                m_server.subscribeMessageListUpdate(this);
                m_server.start();
                StartButton.Text = "Stop";
                statusLabel.Text = "Server Started";
                statusLabel.ForeColor = Color.Green;
                overlayTimer.Enabled = true;
            }
            else
            {
                if (m_server != null)
                {
                    m_server.stop();
                    m_server = null;
                }
                StartButton.Text = "Start";
                statusLabel.Text = "Server Stopped";
                statusLabel.ForeColor = Color.Red;
                overlayTimer.Enabled = false;
                setOverlayVisible(false);
                m_messages.Clear();
                m_currentMessage = null;
                CurrentMessages.Items.Clear();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_aboutBox.ShowDialog(this);
        }

        private void setOverlayLocation()
        {
            m_overlayForm.setLocation(new Point((int)xPosBox.Value, (int)yPosBox.Value));
        }

        private void xPosBox_ValueChanged(object sender, EventArgs e)
        {
            setOverlayLocation();
            Properties.Settings.Default.OverlayXPos = (int)(this.xPosBox.Value);
            Properties.Settings.Default.Save();
        }

        private void yPosBox_ValueChanged(object sender, EventArgs e)
        {
            setOverlayLocation();
            Properties.Settings.Default.OverlayYPos = (int)(this.yPosBox.Value);
            Properties.Settings.Default.Save();
        }

        private void widthBox_ValueChanged(object sender, EventArgs e)
        {
            m_overlayForm.Width = (int)(widthBox.Value);
            Properties.Settings.Default.OverlayWidth = (int)(widthBox.Value);
            Properties.Settings.Default.Save();
        }

        private void heightBox_ValueChanged(object sender, EventArgs e)
        {
            m_overlayForm.Height = (int)(heightBox.Value);
            Properties.Settings.Default.OverlayHeight = (int)(heightBox.Value);
            Properties.Settings.Default.Save();
        }

        private void fontSizeBox_ValueChanged(object sender, EventArgs e)
        {
            m_overlayForm.setFontSize((float)(fontSizeBox.Value));
            Properties.Settings.Default.OverlayFontSize = (float)(fontSizeBox.Value);
            Properties.Settings.Default.Save();
        }

        private void cleanup()
        {
            if (m_server != null)
            {
                m_server.stop();
                m_server = null;
            }
            m_overlayForm.Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            cleanup();
        }

        private void updateOverlay()
        {
            if (m_overlayVisible)
            {
                if (m_overlayForm.Handle != GetTopWindow(new IntPtr(0)))
                {
                    BringWindowToTop(m_overlayForm.Handle);
                }
            }

            bool replace = true;
            bool remove = false;
            if (m_currentMessage != null)
            {
                int increment = Environment.TickCount - m_lastTime;
                m_currentMessage.Value.m_totalTimeDisplayed += increment;
                m_currentMessage.Value.m_curTimeDisplayed += increment;
                replace = m_currentMessage.Value.m_curTimeDisplayed > MIN_DISPLAY_TIME_MS;
                remove = m_currentMessage.Value.m_totalTimeDisplayed > MAX_DISPLAY_TIME_MS;
            }
            if (replace || remove)
            {
                if (remove)
                {
                    m_server.removeDisplayMessage(m_currentMessage.Value.m_message);
                }
                if (m_currentMessage == null || ReferenceEquals(m_currentMessage, m_messages.Last))
                {
                    m_currentMessage = m_messages.First;
                }
                else
                {
                    m_currentMessage = m_currentMessage.Next;
                }

                if (m_currentMessage != null)
                {
                    m_currentMessage.Value.m_curTimeDisplayed = 0;
                    m_overlayForm.setText(m_currentMessage.Value.m_message);
                    setOverlayVisible(true);
                }
                else
                {
                    setOverlayVisible(false);
                }
            }

            m_lastTime = Environment.TickCount;
        }

        private void overlayTimer_Tick(object sender, EventArgs e)
        {
            updateOverlay();
        }

        #region IOverlayCallback Members

        public void onMessageAdded(string message)
        {
            this.Invoke(m_addMessageDelegate, message);
        }

        public void onMessageRemoved(string message)
        {
            this.Invoke(m_removeMessageDelegate, message);
        }

        #endregion
    }
}
