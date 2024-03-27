using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoboClawWF
{
    public partial class Form1 : Form
    {
        public string CurrentMacro = "RoboClaw.tst.txt";
        public RoboClawController RoboClawController;
        public string[] CmdLineArgs;
        public Form1( String[] args )
        {
            InitializeComponent();
            CmdLineArgs = args;
            if (args.Length > 0 && args[0] != "Slave")
                CurrentMacro = args[0];
            button2.Text = CurrentMacro;
            RoboClawController = new RoboClawController( CurrentMacro, this );
            if (CmdLineArgs.Length > 0)
                if (CmdLineArgs[0] == "Slave")
                {
                    Thread runner = new Thread( () => RoboClawController.SocketMode( CmdLineArgs ) );
                    runner.Start();
                }
                else
                {
                    MacroRunner macroRunner = new MacroRunner( RoboClawController, null, CurrentMacro );
                    macroRunner.RunMacro();
                }

        }
        // run macro
        private void button1_Click( object sender, EventArgs e )
        {
            Control[] macro = this.Controls.Find( "button2", true );
            string CurrentMacro = macro[0].Text;
            MacroRunner macroRunner = new MacroRunner( RoboClawController, null, CurrentMacro );
            macroRunner.RunMacro();
        }

        // Select macro
        private void button2_Click( object sender, EventArgs e )
        {
            var picker = new OpenFileDialog();
            picker.FileName = CurrentMacro;
            picker.DefaultExt = "txt";
            picker.InitialDirectory = Environment.CurrentDirectory;
            picker.Filter = "txt files (*.txt)|*.txt";
            if (picker.ShowDialog() == DialogResult.OK)
            {
                CurrentMacro = picker.FileName;
                button2.Text = CurrentMacro;

            }
        }

        public void SetStatus( string s )
        {
            button3.Text = s;
        }

        private void progressBar1_Click( object sender, EventArgs e )
        {

        }

        private void progressBar2_Click( object sender, EventArgs e )
        {

        }

        private void label6_Click( object sender, EventArgs e )
        {

        }

        private void textBox2_TextChanged( object sender, EventArgs e )
        {

        }

        private void label8_Click( object sender, EventArgs e )
        {

        }

        private void textBox1_TextChanged( object sender, EventArgs e )
        {

        }

        private void button4_Click( object sender, EventArgs e )
        {
            MacroRunner macroRunner = new MacroRunner( RoboClawController, null, CurrentMacro );
            macroRunner.setPIDs();
        }

        private void button5_Click( object sender, EventArgs e )
        {
            MacroRunner macroRunner = new MacroRunner( RoboClawController, null, CurrentMacro );
            macroRunner.autoTune();
        }

        private void textBox4_TextChanged( object sender, EventArgs e )
        {

        }

        private void textBox5_TextChanged( object sender, EventArgs e )
        {

        }

        private void label9_Click( object sender, EventArgs e )
        {

        }
    }
}
