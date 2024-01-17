﻿
using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static RoboClawWF.RoboClawController;

namespace RoboClawWF
{
    public class MacroRunner
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger( typeof( MacroRunner ) );
        public string CurrentMacro;
        public RoboclawClassLib.Roboclaw rc;
        StreamReader fs = null;
        NetworkStream ns = null;
        RoboClawController controller = null;
        UInt16 m_crc;
        public PipeClient pipeClient = null;
        bool socketMode = false;
        private string[] Macro;
        private int currentline = 0;
        private System.Collections.Generic.Dictionary<string, int> label = new System.Collections.Generic.Dictionary<string, int>();

        public MacroRunner( RoboClawController sc, PipeClient pipeClientin, string filename = null )
        {
            rc = sc.rc;
            pipeClient = pipeClientin;
            CurrentMacro = filename;
            controller = sc;
            socketMode = (CurrentMacro == null);
            int currentline = 0;
            if (CurrentMacro != null)
            {
                //Load full macro into memory as array of strings
                Macro = System.IO.File.ReadAllLines( CurrentMacro );
                //Scan macro array for labels, record their line number in Dictionary
                currentline = 0;
                foreach (string line in Macro)
                {
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    if (line1[0].StartsWith( ":" ))
                        label.Add( line1[0].Substring( 1 ).TrimEnd( '\r', '\n', ' ', '\t' ), currentline + 1 );
                    ++currentline;
                }
            }
        }


        private UInt16 crc_update( byte data )
        {
            int i;
            m_crc = (UInt16)(m_crc ^ ((UInt16)data << 8));
            for (i = 0 ; i < 8 ; i++)
            {
                if ((m_crc & 0x8000) != 0)
                    m_crc = (UInt16)((m_crc << 1) ^ 0x1021);
                else
                    m_crc <<= 1;
            }
            return m_crc;
        }

        public async Task<string> readLine()
        {
            //System.Diagnostics.Debugger.Launch();
            string s;
            if (socketMode)
            {
                await pipeClient.receive(); //block until string is received
                s = pipeClient.lastReceive; //retrieve string received
                lock (pipeClient._writerSemaphore)
                {
                    pipeClient.lastReceive = null; //reset lastreceive for next read
                }
            }
            else
            {
                s = currentline >= Macro.Length ? null : Macro[currentline++];
            }
            return s;
        }


        public long MonitorCurrents( long period )
        {
            long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long currentTime = startTime;
            ArrayList argsin = new ArrayList();
            Int16 current1 = 0;
            Int16 current2 = 0;
            int M1cnt = 0, M2cnt = 0;
            argsin.Add( current1 );
            argsin.Add( current2 );
            while (currentTime - startTime < period)
            {
                rc.GetCurrents( ref current1, ref current2 );
                Console.WriteLine( current1.ToString(), " ", current2.ToString() );
                current1 = Math.Min( Math.Max( current1, (short)0 ), (short)100 );
                current2 = Math.Min( Math.Max( current2, (short)0 ), (short)100 );

                controller.SetControlPropertyThreadSafe( controller.parent.progressBar1, "Value", current1 );
                controller.SetControlPropertyThreadSafe( controller.parent.progressBar2, "Value", current2 );
                M1cnt = controller.dcMotor.ReadEncoderPosition( 1 );
                M2cnt = controller.dcMotor.ReadEncoderPosition( 2 );
                controller.SetControlPropertyThreadSafe( controller.parent.textBox2, "Text", current1.ToString() );
                controller.SetControlPropertyThreadSafe( controller.parent.textBox3, "Text", current2.ToString() );
                currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Thread.Yield();
            }
            return period;
        }

        // report progress to conductor, get response from him
        public string ReportProgress( string report, string sender )
        {
            byte[] myReadBuffer = new byte[2];
            string line = "";
            if (ns == null)    //no conductor active, do nothing
                return "";
            string localReport = "sender" + "|" + report + "\n\r";
            ns.Write( System.Text.Encoding.UTF8.GetBytes( localReport ), 0, localReport.Length ); //send report
            //read response from conductor
            while (true)
            {
                int numberOfBytesRead = ns.Read( myReadBuffer, 0, 1 );
                if (numberOfBytesRead > 0)
                {
                    if (myReadBuffer[0] == '\r')
                    {
                        break;
                    }
                    else
                    if (myReadBuffer[0] == 0x03) //EOF
                        return null;
                    else
                        line += myReadBuffer[0];
                }
            }
            return line;
        }

        bool SendCommand( string command, int param )
        {
            int enc = 0;
            byte status = 0;
            int speed = 0;
            short curr1 = 0;
            short curr2 = 0;
            byte address = 0x80;
            string version = "";
            byte pwr = 255;


            switch (command)
            {
                case "M1Forward":
                    rc.ST_M1Forward( pwr );
                    break;
                case "M1Backward":
                    rc.ST_M1Backward( pwr );
                    break;
                case "M2Forward":
                    rc.ST_M2Forward( pwr );
                    break;
                case "M2Backward":
                    rc.ST_M1Backward( pwr );
                    break;
                case "GetM1Encoder":
                    int encoder1 = controller.dcMotor.ReadEncoderPosition( 1 );
                    controller.SetControlPropertyThreadSafe( controller.parent.textBox2, "Text", encoder1.ToString() );
                    break;
                case "GetM2Encoder":
                    int encoder2 = controller.dcMotor.ReadEncoderPosition( 2 );
                    controller.SetControlPropertyThreadSafe( controller.parent.textBox3, "Text", encoder2.ToString() );
                    break;
                case "GetM1ISpeed":
                    rc.GetM1ISpeed( ref speed, ref status );
                    break;
                case "GetM2ISpeed":
                    rc.GetM2ISpeed( ref speed, ref status );
                    break;
                case "ResetEncoders":
                    rc.ResetEncoders();
                    break;
                case "GetVersion":
                    controller.dcMotor.GetFirmwareVersion();
                    break;
                case "M1Speed":
                    controller.dcMotor.RunConstSpeed( 1, speed );
                    break;
                case "M2Speed":
                    controller.dcMotor.RunConstSpeed( 1, speed ); ;
                    break;
                case "GetCurrents":
                    rc.GetCurrents( ref curr1, ref curr2 );
                    controller.SetControlPropertyThreadSafe( controller.parent.progressBar1, "Value", curr1 );
                    controller.SetControlPropertyThreadSafe( controller.parent.progressBar2, "Value", curr2 );
                    break;
            }

            return false;
        }
        public async void RunMacro()
        {

            //  Read in macro stream

            byte[] b = new byte[1024];
            System.Text.UTF8Encoding temp = new System.Text.UTF8Encoding( true );
            string line;
            string response = "";
            byte[] sendBuffer = new byte[1024];
            while (true)
            {
                line = await readLine();

                if (line == null) break;
                if (line.StartsWith( "\0" )) continue;
                if (line.StartsWith( "#" )) continue;
                if (line.StartsWith( ":" )) continue;
                if (string.IsNullOrEmpty( line )) continue;
                if (string.IsNullOrWhiteSpace( line )) continue;

                Console.WriteLine( "Read line:{0}", line );
                if (line.StartsWith( "IFRETURNISNOT" )) //conditional execution based on last return
                {
                    string value = "";
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                        value = parsedLine[1]; //isolate target value

                    if (value == response) //last return matches value
                        continue; //do nothing, go to read next command
                    //value is not equal to last response, execute conditional command
                    line = ""; //reassemble rest of conditional command
                    for (int i = 2 ; i < parsedLine.Length ; i++)
                    {
                        line += parsedLine[i];
                        if (i < parsedLine.Length - 1) line += ",";
                    }
                    //coninue execution as if it was non-conditional
                }
                if (line.StartsWith( "IFRETURNIS" )) //conditional execution based on last return
                {
                    string value = "";
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                        value = parsedLine[1]; //isolate target value

                    if (value != response) //last return does not match value
                        continue; //do nothing, go to read next command
                    //value is equal to last response
                    line = ""; //reassemble rest of command
                    for (int i = 2 ; i < parsedLine.Length ; i++)
                    {
                        line += parsedLine[i];
                        if (i < parsedLine.Length - 1) line += ",";
                    }
                    //coninue execution as if it was non-conditional
                }
                if (line.StartsWith( "LOGERROR" )) //write log entry
                {
                    string value = "";
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                        value = parsedLine[1];

                    _logger.Error( value );
                    continue;
                }
                if (line.StartsWith( "GOTO" ))
                {
                    string value = "";
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                        value = parsedLine[1].TrimEnd( '\r', '\n', ' ', '\t' );
                    if (!label.ContainsKey( value ))
                        _logger.Error( "Unknown label " + value );
                    else
                    {

                        currentline = label[value];
                        continue;
                    }

                }
                // "Nested" macro calling
                if (line.StartsWith( "@" ))
                {
                    MacroRunner macroRunner = new MacroRunner( controller, pipeClient, line.Substring( 1 ) );
                    macroRunner.RunMacro();
                    continue;
                }
                // Wait for fixed time
                if (line.StartsWith( "SLEEP" ))
                {
                    int delay = 0;
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                        delay = Int32.Parse( parsedLine[1] );
                    Thread.Sleep( delay );
                    continue;
                }
                // Monitor current for specified time
                if (line.StartsWith( "MONITOR" ))
                {
                    int monitortime = 0;
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                        monitortime = Int32.Parse( parsedLine[1] );
                    MonitorCurrents( monitortime );
                    continue;
                }

                // Wait until status is idle
                if (line.StartsWith( "WAIT" ))
                {
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;
                    if (parsedLine[1] != null)
                    {
                        bool motionDone = false;
                        do
                        {
                            Int32.Parse( parsedLine[1] );

                        } while (!motionDone);

                    }
                    continue;
                }
                // Pop up MessageBox
                if (line.StartsWith( "ALERT" ))
                {
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;

                    if (parsedLine[1] != null)
                    {
                        MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                        DialogResult result;
                        result = MessageBox.Show( parsedLine[1], "Alert!", buttons );
                        response = result.ToString();
                        continue;
                    }
                }
                // Pop up MessageBox
                if (line.StartsWith( "REPORT" ))
                {
                    string[] line1 = line.Split( '#' ); //Disregard comments
                    string[] parsedLine = line1[0].Split( ',' );
                    if (string.IsNullOrWhiteSpace( parsedLine[0] )) //Disregard blanks lines
                        continue;

                    if (parsedLine[1] != null)
                    {
                        var i = line.IndexOf( ',' );
                        if (i > -1)
                        {
                            pipeClient.client.Send( "roboClaw:" + line.Substring( i + 1 ) );
                            continue;
                        }
                    }
                    if (parsedLine[1] != null)
                    {
                        string result = ReportProgress( parsedLine[1], "RoboClaw" );
                        continue;
                    }
                }
                //Actual command
                string[] lin2 = line.Split( '#' ); //kill comments
                if (!string.IsNullOrWhiteSpace( lin2[0] ))
                {
                    string[] lin1 = lin2[0].Split( ',' ); //split parameters
                    Int32 commandNumber = -1;
                    try
                    {
                        commandNumber = controller.CommandNumber[lin1[0]];
                    }
                    catch (Exception e)
                    {
                        // invalid command (not in dictionary)
                        Console.WriteLine( e.Message );
                    }


                    Int32 parametersRequired = controller.commandStructure[commandNumber].parameters.Length;
                    Object[] args = new Object[1];

                    for (Int32 pn = 0 ; pn < parametersRequired ; pn++)
                    {
                        switch (controller.commandStructure[commandNumber].parameters[pn])
                        {
                            case 'i':
                                Int16 pi = Int16.Parse( lin1[pn + 1] );
                                args[pn] = pi;
                                break;
                            case 'l':
                                Int32 pl = Int32.Parse( lin1[pn + 1] );
                                args[pn] = pl;
                                break;
                            case 'b':
                                bool pb = bool.Parse( lin1[pn + 1] );
                                args[pn] = (pb);
                                break;
                            case 's':
                                args[pn] = lin1[pn + 1];
                                break;
                            case 'c':
                                byte pc = (byte)Int32.Parse( lin1[pn + 1] );
                                args[pn] = pc;
                                break;
                            default:
                                break;

                        }

                    }
                    byte actualCommand = controller.commandStructure[commandNumber].CmdNumber;
                    string retstring = controller.commandStructure[commandNumber].returns;
                    if (retstring == "")
                        rc.Write_CRC( rc.m_address, actualCommand, args );
                    else
                    {
                        ArrayList argsin = new ArrayList();
                        Int16[] currents = new short[4]; ;
                        for (Int32 i = 0 ; i < retstring.Length ; i++)
                        {
                            argsin.Add( currents[i] );
                        }
                        rc.ReadCmd( rc.m_address, actualCommand, ref argsin );
                        for (Int32 i = 0 ; i < retstring.Length ; i++)
                        {
                            currents[i] = (short)argsin[i];
                            Console.Write( currents[i].ToString(), " " );

                        }
                        Console.WriteLine( "" );
                    }

                }



            }


            fs.Close();
        }

    }
}