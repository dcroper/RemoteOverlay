/**
 * RemoteOverlayClient
 * Copyright (C) 2011 David Roper
 * 
 * Program.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RemoteOverlayClient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
