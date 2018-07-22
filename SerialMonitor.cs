using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Windows.Threading;
using System.Text;

namespace NintendoSpy
{
    public delegate void PacketEventHandler (object sender, byte[] packet);

    public class SerialMonitor
    {
        const int BAUD_RATE = 115200;
        const int TIMER_MS = 5;

        byte[] packet = new byte[4];
        byte[] translatedPacket = new byte[32];

        public event PacketEventHandler PacketReceived;
        public event EventHandler Disconnected;

        SerialPort _datPort;
        List <byte> _localBuffer;

        DispatcherTimer _timer;

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }

        public SerialMonitor (string portName)
        {
            _localBuffer = new List <byte> ();
            _datPort = new SerialPort (portName, BAUD_RATE);
        }

        public void Start ()
        {
            if (_timer != null) return;

            _localBuffer.Clear ();
            _datPort.Open ();

            _timer = new DispatcherTimer ();
            _timer.Interval = TimeSpan.FromMilliseconds (TIMER_MS);
            _timer.Tick += tick;
            _timer.Start ();
        }

        public void Stop ()
        {
            if (_datPort != null) {
                try { // If the device has been unplugged, Close will throw an IOException.  This is fine, we'll just keep cleaning up.
                    _datPort.Close ();
                }
                catch (IOException) {}
                _datPort = null;
            }
            if (_timer != null) {
                _timer.Stop ();
                _timer = null;
            }
        }

        void tick (object sender, EventArgs e)
        {
            if (_datPort == null || !_datPort.IsOpen || PacketReceived == null) return;

            // Try to read some data from the COM port and append it to our localBuffer.
            // If there's an IOException then the device has been disconnected.
            try {
                int readCount = _datPort.BytesToRead;
                if (readCount < 4) return;
                byte[] readBuffer = new byte [4];
                _datPort.Read (readBuffer, 0, 4);
                _datPort.DiscardInBuffer ();
                _localBuffer.AddRange (readBuffer);
            }
            catch (IOException) {
                Stop ();
                if (Disconnected != null) Disconnected (this, EventArgs.Empty);
                return;
            }

            packet = _localBuffer.GetRange(0, 4).ToArray();

            // 2 3 0 1
            translatedPacket[0] = (byte)((packet[2] & 128) >> 7);
            translatedPacket[1] = (byte)((packet[2] & 64)  >> 6);
            translatedPacket[2] = (byte)((packet[2] & 32)  >> 5);
            translatedPacket[3] = (byte)((packet[2] & 16)  >> 4);
            translatedPacket[4] = (byte)((packet[2] & 8)   >> 3);
            translatedPacket[5] = (byte)((packet[2] & 4)   >> 2);
            translatedPacket[6] = (byte)((packet[2] & 2)   >> 1);
            translatedPacket[7] = (byte)((packet[2] & 1)   >> 0);

            translatedPacket[8] = 0;
            translatedPacket[9] = 0;
            translatedPacket[10] = (byte)((packet[3] & 32) >> 5);
            translatedPacket[11] = (byte)((packet[3] & 16) >> 4);
            translatedPacket[12] = (byte)((packet[3] & 8)  >> 3);
            translatedPacket[13] = (byte)((packet[3] & 4)  >> 2);
            translatedPacket[14] = (byte)((packet[3] & 2)  >> 1);
            translatedPacket[15] = (byte)((packet[3] & 1)  >> 0);

            translatedPacket[16] = (byte)((packet[0] & 128) >> 7);
            translatedPacket[17] = (byte)((packet[0] & 64)  >> 6);
            translatedPacket[18] = (byte)((packet[0] & 32)  >> 5);
            translatedPacket[19] = (byte)((packet[0] & 16)  >> 4);
            translatedPacket[20] = (byte)((packet[0] & 8)   >> 3);
            translatedPacket[21] = (byte)((packet[0] & 4)   >> 2);
            translatedPacket[22] = (byte)((packet[0] & 2)   >> 1);
            translatedPacket[23] = (byte)((packet[0] & 1)   >> 0);

            translatedPacket[24] = (byte)((packet[1] & 128) >> 7);
            translatedPacket[25] = (byte)((packet[1] & 64)  >> 6);
            translatedPacket[26] = (byte)((packet[1] & 32)  >> 5);
            translatedPacket[27] = (byte)((packet[1] & 16)  >> 4);
            translatedPacket[28] = (byte)((packet[1] & 8)   >> 3);
            translatedPacket[29] = (byte)((packet[1] & 4)   >> 2);
            translatedPacket[30] = (byte)((packet[1] & 2)   >> 1);
            translatedPacket[31] = (byte)((packet[1] & 1)   >> 0);

            PacketReceived(this, translatedPacket);

            // Clear our buffer up until the last split character.
            _localBuffer.RemoveRange(0, 4);
        }
    }
}
