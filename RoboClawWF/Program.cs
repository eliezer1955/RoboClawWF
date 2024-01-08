using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoboClawWF
{
    class Program
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(Program));
        static Form1 myform;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            _logger.Info("RoboClaw Diag  is starting...");
            //System.Diagnostics.Debugger.Launch();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            myform = new Form1( args );
            myform.CmdLineArgs = args;
            Application.Run( myform );
        }
    }
}
