﻿using System;
using System.Windows.Forms;


namespace HandshakeEmulator
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HandshakeUi());
        }
    }
}
