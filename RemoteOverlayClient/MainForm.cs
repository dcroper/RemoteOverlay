/**
 * RemoteOverlayClient
 * Copyright (C) 2011 David Roper
 * 
 * MainForm.cs
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;

namespace RemoteOverlayClient
{
    public partial class MainForm : Form, IOverlayServiceProxyCallback
    {
        private delegate void AddListItem(string s);
        private delegate void RemoveListItem(string s);
        private delegate void InitList(string[] s);
        private delegate void UpdateStatus(CommunicationState s);
        private delegate void DispErrorMessage(string s);

        private AddListItem m_addMessageDelegate;
        private RemoveListItem m_removeMessageDelegate;
        private InitList m_initMessageListDelegate;
        private UpdateStatus m_updateStatusDelegate;
        private DispErrorMessage m_dispErrorMessageDelegate;
        private AboutBox m_aboutBox = new AboutBox();
        private OverlayServiceProxy m_serviceProxy;

        public MainForm()
        {
            InitializeComponent();
            m_addMessageDelegate = new AddListItem(this.addMessage);
            m_removeMessageDelegate = new RemoveListItem(this.removeMessage);
            m_initMessageListDelegate = new InitList(this.initializeMessageList);
            m_updateStatusDelegate = new UpdateStatus(this.updateStatus);
            m_dispErrorMessageDelegate = new DispErrorMessage(this.displayErrorMessage);
        }

        private void displayErrorMessage(string message)
        {
            string m = message.Trim();
            if (m.Length > 0)
            {
                MessageBox.Show(this, m, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void updateStatus(CommunicationState status)
        {
            if (status == CommunicationState.Opened)
            {
                statusLabel.Text = "Connected to Server";
                statusLabel.ForeColor = Color.Green;
            }
            else
            {
                statusLabel.Text = "Disconnected. Looking for Server...";
                statusLabel.ForeColor = Color.Red;
                msgListBox.Items.Clear();
            }
        }

        private void initializeMessageList(string[] messages)
        {
            msgListBox.Items.Clear();
            msgListBox.Items.AddRange(messages);
        }

        private void addMessage(string message)
        {
            msgListBox.Items.Add(message);
        }

        private void removeMessage(string message)
        {
            msgListBox.Items.Remove(message);
        }

        private void sendNumber()
        {
            if (!sendTxtBox.Text.Equals(""))
            {
                try
                {
                    Int32.Parse(sendTxtBox.Text);
                    m_serviceProxy.addDisplayMessage(sendTxtBox.Text);
                    sendTxtBox.Text = "";
                    sendTxtBox.Focus();
                }
                catch (FormatException)
                {
                    MessageBox.Show(this, "Invalid number to send", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    sendTxtBox.Text = "";
                    sendTxtBox.Focus();
                }
                catch (Exception err)
                {
                    MessageBox.Show(this, "Error communicating with server: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    sendTxtBox.Focus();
                }
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            sendNumber();
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            m_aboutBox.ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void sendTxtBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                sendNumber();
            }
        }

        #region IOverlayServiceProxyCallback Members

        public void onMessageAdded(string message)
        {
            this.Invoke(m_addMessageDelegate, message);
        }

        public void onMessageRemoved(string message)
        {
            this.Invoke(m_removeMessageDelegate, message);
        }

        public void initializeMessages(string[] messages)
        {
            this.Invoke(m_initMessageListDelegate, new object[] { messages });
        }

        public void onError(string message)
        {
            this.Invoke(m_dispErrorMessageDelegate, message);
        }

        public void onStateChange(CommunicationState state)
        {
            this.Invoke(m_updateStatusDelegate, state);
        }

        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            m_serviceProxy = new OverlayServiceProxy(this);
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            if (msgListBox.SelectedItem != null)
            {
                try
                {
                    m_serviceProxy.removeDisplayMessage(msgListBox.SelectedItem.ToString());
                    sendTxtBox.Focus();
                }
                catch (Exception err)
                {
                    MessageBox.Show(this, "Error communicating with server: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    sendTxtBox.Focus();
                }
            }
        }
    }
}
