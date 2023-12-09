using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ComPortMap
{
	private Dictionary<string, string> ports;
	public ComPortMap()
	{
		string text = File.ReadAllText( @"C:\\ProgramData\LabScript\\Data\\comports.json" );
		ports = JsonConvert.DeserializeObject<Dictionary<string,string>>( text );
	}

	public string GetComPort( string id )
	{
		string comport = null;
		try
		{
			comport = ports[id];
		}
		catch (Exception e)
		{
			Console.WriteLine( "port for {0} not found!!!", id );
			return null;
		}
		return comport;
	}
}
