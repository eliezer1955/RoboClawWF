using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoboClawWF
{
    public class RoboClawController
    {
        public struct CommandStructure
        {
            public byte CmdNumber;
            public string CmdName;
            public string Description;
            public string parameters;
            public string returns;
            Action<char> action;
            public int timeout;
            public int response;
        }
        public bool RunLoop { get; set;}
        private int _receivedItemsCount = 0;                        // Counter of number of plain text items received
        private int _receivedBytesCount = 0;                        // Counter of number of plain text bytes received
        long _beginTime = 0;                                        // Start time, 1st item of sequence received 
        long _endTime = 0;                                          // End time, last item of sequence received 
        public SerialPort _serialPort=null;
        public Dictionary<string, int> CommandNumber = new Dictionary<string, int>();
        public RoboclawClassLib.Roboclaw rc = null; 
        public CommandStructure[] commandStructure = new CommandStructure[] {
            new CommandStructure{CmdNumber=0, CmdName="M1Forward", Description="Set motor1 direction", parameters="c", returns="" },
            new CommandStructure{CmdNumber=1, CmdName="M1Backward", Description="Set motor1 direction", parameters="c",  returns=""},
            new CommandStructure{CmdNumber=4, CmdName="M2Forward", Description="Set motor2 direction", parameters="c",  returns=""},
            new CommandStructure{CmdNumber=5, CmdName="M2Backward", Description="Set motor2 direction", parameters="c",  returns=""},
            new CommandStructure{CmdNumber=16, CmdName="GetM1Encoder", Description="Get motor1 encoder",  parameters="",  returns="lc"},
            new CommandStructure{CmdNumber=17, CmdName="GetM2Encoder", Description="Get motor2 encoder",  parameters="",  returns="lc"},
            new CommandStructure{CmdNumber=18, CmdName="GetM1ISpeed", Description="Get motor1 speed", parameters="",  returns="lc"},
            new CommandStructure{CmdNumber=19, CmdName="GetM2ISpeed", Description="Get motor2 speed",  parameters="",  returns="lc"},
            new CommandStructure{CmdNumber=20, CmdName="ResetEncoders", Description="Reset Encoders", parameters="",  returns=""},
            new CommandStructure{CmdNumber=21, CmdName="GetVersion", Description="Get FW  version",  parameters="",  returns="s"},
            new CommandStructure{CmdNumber=68, CmdName="M1DutyAccel", Description="Set motor1 duty and acceleration",  parameters="il",  returns=""},
            new CommandStructure{CmdNumber=69, CmdName="M2DutyAccel", Description="Set motor2 duty and acceleration",  parameters="il",  returns=""},
            new CommandStructure{CmdNumber=35, CmdName="M1Speed", Description="Drive Motor1 at speed", parameters="l",  returns=""},
            new CommandStructure{CmdNumber=35, CmdName="M2Speed", Description="Drive Motor2 at speed", parameters="l",  returns=""},
            new CommandStructure{CmdNumber=49, CmdName="GetCurrents", Description="Get motor currents", parameters="",  returns="ii"}
    };
        public MacroRunner macroRunner;
        public string CurrentMacro;
        public Form1 parent;
        public Dictionary<string, Action<int>> xlate= new Dictionary<string,Action<int>>();
        public DCmotor dcMotor = null;
        public RoboClawController(string runthis, Form1 parentIn )
        {
            parent = parentIn;
            int i = 0;
            foreach (CommandStructure command in commandStructure)
            {
                CommandNumber.Add(command.CmdName, i++);
            }
            CurrentMacro = runthis;
            //get name of comport associated to roboClaw (as obtained by Listports.py)
            ComPortMap map = new ComPortMap();
            var comport = map.GetComPort( "roboClaw" );
            rc = new RoboclawClassLib.Roboclaw( comport, 115200, 0x80 ); //initialize com port
            rc.Open();
            dcMotor=new DCmotor(rc);

        }



        private delegate void SetControlPropertyThreadSafeDelegate(
                         System.Windows.Forms.Control control,
                         string propertyName,
                         object propertyValue);

        public  void SetControlPropertyThreadSafe(
            Control control,
            string propertyName,
            object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate
                (SetControlPropertyThreadSafe),
                new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    null,
                    control,
                    new object[] { propertyValue });
            }
        }


        async public Task SocketMode( string[] CmdLineArgs )
        {
            PipeClient pipeClient = new PipeClient();
            var mr = new MacroRunner( this, pipeClient, null );
            //Thread macroThread = new Thread( new ThreadStart( mr.RunMacro ) );
            mr.RunMacro();
        }

    }
}