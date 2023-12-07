

using System;

namespace CommandMessenger
{
    /// <summary>Class to get a timestamp </summary>
    public static class TimeUtils
    {
        public static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);  // 1 January 1970

        /// <summary> Gets the milliseconds since 1 Jan 1970. </summary>
        /// <value> The milliseconds since 1 Jan 1970. </value>
        public static long Millis { get { return (long)((DateTime.Now.ToUniversalTime() - Jan1St1970).TotalMilliseconds); } }

        /// <summary> Gets the seconds since 1 Jan 1970. </summary>
        /// <value> The seconds since 1 Jan 1970. </value>
        public static long Seconds { get { return (long)((DateTime.Now.ToUniversalTime() - Jan1St1970).TotalSeconds); } }

        // Returns if it has been more than interval (in ms) ago. Used for periodic actions
        public static bool HasExpired(ref long prevTime, long interval)
        {
            var millis = Millis;
            if (millis - prevTime > interval)
            {
                prevTime = millis;
                return true;
            }
            return false;
        }
    }
}