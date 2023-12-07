using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoboClawWF
{
    internal static class Program
    {
        static Form1 myform;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            //System.Diagnostics.Debugger.Launch();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            myform = new Form1( args );
            myform.CmdLineArgs = args;
            Application.Run( myform );
        }
    }
}
