
using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoboClawWF
{
    public class MacroRunner
    {
        public string CurrentMacro;
        public RoboclawClassLib.Roboclaw rc;
        StreamReader fs = null;
        NetworkStream ns = null;
        RoboClawController controller = null;
        UInt16 m_crc;
        public PipeClient pipeClient = null;
        bool socketMode = false;

        public MacroRunner( RoboClawController sc, PipeClient pipeClientin, string filename = null )
        {
            rc = sc.rc;
            pipeClient = pipeClientin;
            CurrentMacro = filename;
            controller = sc;
            socketMode = (CurrentMacro == null);
            if (CurrentMacro != null)
            {
                fs = new StreamReader( CurrentMacro );
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
                s = fs.ReadLine();
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
            argsin.Add( current1 );
            argsin.Add( current2 );
            while (currentTime - startTime < period)
            {
                rc.ReadCmd( rc.m_address, 49, ref argsin );
                Console.WriteLine( current1.ToString(), " ", current2.ToString() );
                controller.SetControlPropertyThreadSafe( controller.parent.progressBar1, "Value", current1 );
                controller.SetControlPropertyThreadSafe( controller.parent.progressBar2, "Value", current2 );
                currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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

        public async void RunMacro()
        {

            //  Read in macro stream

            byte[] b = new byte[1024];
            System.Text.UTF8Encoding temp = new System.Text.UTF8Encoding( true );
            string line;
            byte[] sendBuffer = new byte[1024];
            while (true)
            {
                line = await readLine();

                if (line == null) break;
                if (line.StartsWith( "\0" )) continue;
                if (line.StartsWith( "#" )) continue;
                if (string.IsNullOrEmpty( line )) continue;
                if (string.IsNullOrWhiteSpace( line )) continue;

                Console.WriteLine( "Read line:{0}", line );
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



                fs.Close();
            }


        }

    }
}