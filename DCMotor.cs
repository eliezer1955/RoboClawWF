using RoboclawClassLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class DCmotor
{
	//RoboClaw Support:
	//------------------------------------------------------------------------------------------------
	Roboclaw roboClaw;
	public string foldername = @"C:\ProgramData\S2_Logs\EncoderLogs\";
	public string filesuffix = @"_" + DateTime.Now.ToString( "yyyy_MM_dd_HHmmss" ) + "_EncoderLog.csv";
	public object DCMotorLock=new object();
	//public string filename = @"C:\LabScript\EncoderLogs\" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + "_EncoderLog.csv";
	int encoder1 = 0;
	int encoder2 = 0;
	int current1 = 0;
	int current2 = 0;
	public DCmotor(Roboclaw rc)
	{  roboClaw = rc;
       roboClaw.ResetEncoders();
    }	

	public string Connect( List<string> ports )
	{
		bool exitloop = false;
		string roboclawport = null;
		foreach (string port in ports)
		{
			//ScriptLog( 0, Severity.Control, "Testing Port: " + port );
			if (!exitloop)
			{
				try
				{
					SetupUsingCOM( port );
					//ScriptLog(Severity.Info, "Setup Worked? " + Setup(port));
					//ScriptLog(Severity.Info, "Firmware reading: " + GetFirmwareVersion());
					if (GetFirmwareVersion().Contains( "USB Roboclaw" ))
					{
						roboclawport = port;
						exitloop = true;
						Cleanup();
					}
					else
					{
						Cleanup();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine( e.ToString() );
				}
			}
		}
		return roboclawport;
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ModuleName"></param>
	/// <returns></returns>
	public bool Setup( string ModuleName )
	{
		//Look up module parameters:
		string myComPort = "";

		//System.Diagnostics.Debugger.Launch();
		//System.Diagnostics.Debugger.Break();
		/*
		if (myDeviceData == null) return false;
		if (myDeviceData.ModuleList.ContainsKey( ModuleName ))
		{
			ModuleSettings value = myDeviceData.ModuleList[ModuleName];

			myComPort = value.ModulePort;
			//MessageBox.Show(myComPort);
		}
		else
			return false;
		*/
		if ((roboClaw == null || !roboClaw.IsOpen()))
		{
			try
			{
				roboClaw = new Roboclaw( myComPort, 115200, 128 ); // Open the interface to the RoboClaw
				roboClaw.Open();
				//roboClaw.Open(myComPort, ref roboClawModel, 128, 38400); // Open the interface to the RoboClaw
				//labelRoboClawModel.Text = roboClawModel; // Display the RoboClaw device model number
				roboClaw.ResetEncoders();
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show( "Com Port Operation Problem: " + ex.Message, "Com Port Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
				return false;
				//throw;
			}
		}
		return false;

	}
	public bool SetupUsingCOM( string myComPort )
	{
		//string roboClawModel = "";
		//roboClaw = new Roboclaw();
		//Connect:
		if ((roboClaw == null || !roboClaw.IsOpen()) 
			//&& !CallerAbortRequested( callerNum ))
			)
		{
			try
			{
				roboClaw = new Roboclaw( myComPort, 115200, 128 ); // Open the interface to the RoboClaw
				roboClaw.Open();
				//roboClaw.Open(myComPort, ref roboClawModel, 128, 38400); // Open the interface to the RoboClaw
				//labelRoboClawModel.Text = roboClawModel; // Display the RoboClaw device model number
				roboClaw.ResetEncoders();
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show( "Com Port Operation Problem: " + ex.Message, "Com Port Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
				return false;
				//throw;
			}
		}
		return false;
	}

	public bool SetupNoCheck( string myComPort )
	{
		roboClaw.Close();
		try
		{
			roboClaw = new Roboclaw( myComPort, 115200, 128 ); // Open the interface to the RoboClaw
			roboClaw.Open();
			//roboClaw.Open(myComPort, ref roboClawModel, 128, 38400); // Open the interface to the RoboClaw
			//labelRoboClawModel.Text = roboClawModel; // Display the RoboClaw device model number
			roboClaw.ResetEncoders();
			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show( "Com Port Operation Problem: " + ex.Message, "Com Port Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
			return false;
			//throw;
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <returns></returns>
	public bool Setup()
	{
		//Connect:
		if (roboClaw == null || !roboClaw.IsOpen())
		{
			try
			{
				roboClaw = new Roboclaw( "COM3", 38400, 128 ); // Open the interface to the RoboClaw
				roboClaw.Open();
				roboClaw.ResetEncoders();
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show( "Com Port Operation Problem: " + ex.Message, "Com Port Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
				return false;
				//throw;
			}
		}
		return true;
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? Yes. Up to several seconds.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="direction"></param>
	/// <param name="SleepTime"></param>
	public void MotorJog( int ch, short direction, int SleepTime )
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen())
			{
				short duty = (short)(20000 * direction);
				uint accel = 500000;
				SetMotorDutyAccel( ch, duty, accel );
				System.Threading.Thread.Sleep( SleepTime );
				if (ch == 1)
					roboClaw.ST_M1Forward( 0 ); // Stop the motor
				else
					roboClaw.ST_M2Forward( 0 ); // Stop the motor
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="pwr"></param>
	public void MotorGoForward( int ch, byte pwr )
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen())
			{
				if (ch == 1)
					roboClaw.ST_M1Forward( pwr ); // Start the motor going forward at power 100
				else
					roboClaw.ST_M2Forward( pwr ); // Start the motor going forward at power 100
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="pwr"></param>
	public void MotorGoReverse( int ch, byte pwr )
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen())
			{
				if (ch == 1)
					roboClaw.ST_M1Backward( pwr ); // Start the motor going forward at power 100
				else
					roboClaw.ST_M2Backward( pwr ); // Start the motor going forward at power 100
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="duty"></param>
	/// <param name="accel"></param>
	public void SetMotorDutyAccel( int ch, short duty, uint accel )
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen())
			{
				if (ch == 1)
					roboClaw.M1DutyAccel( duty, accel );
				else
					roboClaw.M2DutyAccel( duty, accel );
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="RPM"></param>
	/// <param name="direction"></param>
	public void SetMotorRPM( int ch, short RPM, string direction )
	{
		lock (DCMotorLock)
		{
			short directionnum = 1;
			if (roboClaw.IsOpen())
			{
				if (direction == "forward")
					directionnum = 1;
				else
					directionnum = -1;
				uint accel = 500000;
				short duty = (short)(directionnum * ((RPM * 55) + 349));

				if (ch == 1)
					roboClaw.M1DutyAccel( duty, accel );
				else
					roboClaw.M2DutyAccel( duty, accel );
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="RPM"></param>
	public void SetMotorRPM( int ch, short RPM )
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen() 
				//&& !myParentScript.abortRequested)
				)
			{
				uint accel = 500000;
				short duty = (short)((RPM * 55) + 349);

				if (ch == 1)
					roboClaw.M1DutyAccel( duty, accel );
				else
					roboClaw.M2DutyAccel( duty, accel );
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	public void MotorStop( int ch )
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen()) // && (!myParentScript.Bay1Abort || !myParentScript.Bay2Abort))
			{
				if (ch == 1)
					roboClaw.M1DutyAccel( 0, 16000 );
				//roboClaw.ST_M1Forward(0); // Stop the motor
				else
					roboClaw.M2DutyAccel( 0, 16000 );
				//roboClaw.ST_M2Forward(0); // Stop the motor
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	public void MotorStopWaitDone( int ch )
	{
		int dcSpeed = 0;
		byte dcStatus = 0;
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen()) // && (!myParentScript.Bay1Abort || !myParentScript.Bay2Abort))
			{
				if (ch == 1)
				{
					roboClaw.GetM1ISpeed( ref dcSpeed, ref dcStatus );
					if (dcSpeed != 0)
					{
						Thread.Sleep( 250 );
					}
                    roboClaw.ST_M1Forward( 0 ); // Stop the motor
                }
				else
				{
					roboClaw.GetM2ISpeed( ref dcSpeed, ref dcStatus );
					if (dcSpeed != 0)
					{
                        Thread.Sleep( 250 );
                        roboClaw.ST_M2Forward( 0 ); // Stop the motor
                    }
				}
                
                
			}
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	public void Cleanup()
	{
		lock (DCMotorLock)
		{
			if (roboClaw.IsOpen())
			{
				roboClaw.ST_M1Forward( 0 ); // Stop the motor
				roboClaw.ST_M2Forward( 0 ); // Stop the motor
			}
			roboClaw.Close();
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? Yes. Up to several seconds.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="stepnum"></param>
	/// <param name="time"></param>
	/// <param name="RPM"></param>
	/// <param name="bidirectional"></param>
	/// <returns></returns>
	public List<string> Rotate( int callerNum, int ch, int stepnum, int time, short RPM, bool bidirectional )
	{
		lock (DCMotorLock)
		{
			List<string> encoderlist = new List<string>();
			int encpos1;
			int encpos2;
			for (int i = 0 ; i < stepnum 
				//&& (!CallerAbortRequested( callerNum ))
				; i++)
			{
				encpos1 = GetPosition( ch );
				SetMotorRPM( ch, RPM, "forward" );
				Thread.Sleep( time );
				//Sleep( callerNum, time );
				MotorStop( ch );
				encpos2 = GetPosition( ch );
				encoderlist.Add( $",forward, {(encpos2 - encpos1)},\n" );

				if (bidirectional 
					//&& (!CallerAbortRequested( callerNum )))
					)
				{
					SetMotorRPM( ch, RPM, "reverse" );
                    Thread.Sleep( time );
                    //Sleep( callerNum, time );
                    System.Threading.Thread.Sleep( time );
                    MotorStop( ch );
					encpos1 = GetPosition( ch );
					encoderlist.Add( $",reverse, {(encpos2 - encpos1)},\n" );
				}
			}
			return encoderlist;
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? Yes. Up to several seconds.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="stepnum"></param>
	/// <param name="time"></param>
	/// <param name="RPM"></param>
	/// <param name="bidirectional"></param>
	/// <returns></returns>
	public List<string> RotateConstVel( int callerNum, int ch, int stepnum, int time, short RPM, bool bidirectional )
	{
		lock (DCMotorLock)
		{
			List<string> encoderlist = new List<string>();
			int encpos1;
			int encpos2;
			for (int i = 0 ; i < stepnum 
				//&& (!CallerAbortRequested( callerNum ))
				; i++)
			{
				encpos1 = GetPosition( ch );
				RunConstSpeed( ch, RPM * 20 );
                Thread.Sleep( time );
                //Sleep( callerNum, time );
                MotorStop( ch );
				encpos2 = GetPosition( ch );
				encoderlist.Add( $",forward, {(encpos2 - encpos1)},\n" );
				if (bidirectional 
					//&& (!CallerAbortRequested( callerNum )))
					)
				{
					RunConstSpeed( ch, RPM * -20 );
                    Thread.Sleep( time );
                    //Sleep( callerNum, time );
                    System.Threading.Thread.Sleep( time );
                    MotorStop( ch );
					encpos1 = GetPosition( ch );
					encoderlist.Add( $",reverse, {(encpos2 - encpos1)},\n" );
				}
			}
			return encoderlist;
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? Yes. Up to several seconds.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="time"></param>
	/// <param name="RPM"></param>
	/// <param name="bidirectional"></param>
	/// <returns></returns>
	public List<string> RotateOnce( int callerNum, int ch, int time, short RPM, bool bidirectional )
	{
		lock (DCMotorLock)
		{
			List<string> encoderlist = new List<string>();
			int encpos1;
			int encpos2;
			for (int i = 0 ; i < 1 
				//&& (!CallerAbortRequested( callerNum ))
				; i++)
			{
				encpos1 = GetPosition( ch );
				SetMotorRPM( ch, RPM );
                Thread.Sleep( time );
                //Sleep( callerNum, time );
                MotorStop( ch );
				encpos2 = GetPosition( ch );
				encoderlist.Add( $",forward, {(encpos2 - encpos1)},\n" );

				if (bidirectional 
					//&& (!CallerAbortRequested( callerNum )))
					)
				{
					int intRPM = (int)RPM * -1;
					SetMotorRPM( ch, (short)(RPM) );
                    Thread.Sleep( time );
                    //Sleep( callerNum, time );
                    Thread.Sleep(time);
					MotorStop( ch );
					encpos1 = GetPosition( ch );
					encoderlist.Add( $",reverse, {(encpos2 - encpos1)},\n" );
				}
			}
			return encoderlist;
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? Yes. Up to several seconds.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="time"></param>
	/// <param name="RPM"></param>
	/// <param name="bidirectional"></param>
	/// <returns></returns>
	public List<string> RotateOnceConstVel( int callerNum, int ch, int time, short RPM, bool bidirectional )
	{
		lock (DCMotorLock)
		{
			List<string> encoderlist = new List<string>();
			int encpos1;
			int encpos2;
			for (int i = 0 ; i < 1 ; i++)
			{
				encpos1 = GetPosition( ch );
				RunConstSpeed( ch, RPM * 20 );
				Thread.Sleep( time );
				MotorStop( ch );
				//MotorStopWaitDone(ch);
				encpos2 = GetPosition( ch );
				encoderlist.Add( $",forward, {(encpos2 - encpos1)},\n" );
				if (bidirectional 
					//&& (!CallerAbortRequested( callerNum )))
					)
				{
					RunConstSpeed( ch, RPM * -20 );
					Thread.Sleep( time );
					MotorStop( ch );
					MotorStopWaitDone( ch );
					Thread.Sleep( 3000 );
					encpos1 = GetPosition( ch );
					encoderlist.Add( $",reverse, {(encpos2 - encpos1)},\n" );
				}
			}
			return encoderlist;
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <returns></returns>
	public int GetPosition( int ch )
	{
		lock (DCMotorLock)
		{
			int encoder = 999999999;
			byte status = 0;
			try
			{
				if (ch == 1)
				{
					roboClaw.GetM1Encoder( ref encoder1, ref status );

				}

				else
				{
					roboClaw.GetM2Encoder( ref encoder2, ref status );
				}

			}
			catch (Exception e)
			{
                Console.WriteLine( e.ToString() );
            }
			return encoder;
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <param name="speed"></param>
	public void RunConstSpeed( int ch, int speed )
	{
		lock (DCMotorLock)
		{
			if (ch == 1)
				roboClaw.M1Speed( speed );
			else
				roboClaw.M2Speed( speed );
		}
	}

	/// <summary>
	/// DC Motor routine
	/// LRP? No.
	/// </summary>
	/// <param name="ch"></param>
	/// <returns></returns>
	public int ReadEncoderPosition( int ch )
	{
		lock (DCMotorLock)
		{
			int encoder = 0;
			byte status = 0;
			if (ch == 1)
				roboClaw.GetM1Encoder( ref encoder, ref status );
			else
				roboClaw.GetM2Encoder( ref encoder, ref status );

			return encoder;
		}
	}

	/// <summary>
	/// DC Motor -- get firmware version
	/// LRP? No.
	/// </summary>
	/// <returns></returns>
	public string GetFirmwareVersion() //KC: not sure what the address should be
	{
		string version = "";
		byte add = 0x80;
		roboClaw.GetVersion( add, ref version );
		return version;
	}
	//public void CreateEncoderLog()
	//{
	//    System.IO.Directory.CreateDirectory(foldername);
	//    System.IO.File.Create(filename).Close();
	//}

	private void CreateEncoderLog( string scriptname )
	{
		string filename2 = @foldername + scriptname + filesuffix;
		System.IO.Directory.CreateDirectory( foldername );
		System.IO.File.Create( filename2 ).Close();
	}

	private void RecordEncoderData( string data, string scriptname )
	{
		string filename2 = @foldername + scriptname + filesuffix;
		System.IO.File.AppendAllText( filename2, data + "\n" );
	}


}
