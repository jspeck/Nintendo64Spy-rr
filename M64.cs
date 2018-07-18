using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NintendoSpy.Readers;

namespace NintendoSpy
{
    class M64
    {
        string fileName = "test.m64";
        BinaryWriter writer;
        int numOfWrites = 0;
        byte[] lastPacket = new byte[4];

        int signature = 0x1A34364D;        // 0x4D36341A
        int version = 3;
        int uid = 1474960799;
        int numberOfFrames = 200000;
        int rerecordCount = 1;
        byte fps = 60;
        byte numberOfControllers = 1;
        short reserve1 = 0;
        int numInputSamples = 100000;
        short movieStartType = 2;
        short reserve2 = 0;
        int controllerFlags = 1;
        int reserve3 = 0;                                     // 160 bytes / 4 = 40
        string internalNameROM = "SUPER MARIO 64";            // 32 byte ascii string
        int CRC32 = 238922318;
        short countryCode = 74;
        int reserve4 = 0;                                     // 56 / 4 = 14
        string videoPlugin = "Jabo's Direct3D8 1.6";          // 64 byte ascii string
        string soundPlugin = "Jabo's DirectSound 1.6";        // 64 byte ascii string
        string inputPlugin = "TAS Input Plugin 0.6";          // 64 byte ascii string
        string rspPlugin = "RSP emulation Plugin";            // 64 byte ascii string
        string author = "minikori deftek";                    // 222-byte utf-8 string
        string movieDescription = "tests";                    // 256-byte utf-8 string

        public M64(string _fileName, IControllerReader reader)
        {
            fileName = _fileName;

            writer = new BinaryWriter(File.Open(fileName, FileMode.Create));

            reader.ControllerStateChanged += reader_ControllerStateChanged;

            createFile();
        }

        private static byte[] ASCIIToByteArray(string str, int length)
        {
            return Encoding.ASCII.GetBytes(str.PadRight(length, '\0'));
        }

        private static byte[] UTF8ToByteArray(string str, int length)
        {
            return Encoding.UTF8.GetBytes(str.PadRight(length, '\0'));
        }

        private void createFile()
        {
            writer.Write(signature);
            writer.Write(version);
            writer.Write(uid);
            writer.Write(numberOfFrames);
            writer.Write(rerecordCount);
            writer.Write(fps);
            writer.Write(numberOfControllers);
            writer.Write(reserve1);
            writer.Write(numInputSamples);
            writer.Write(movieStartType);
            writer.Write(reserve2);
            writer.Write(controllerFlags);

            for (int i = 0; i < 40; i++)
            {
                writer.Write(reserve3);
            }

            writer.Write(ASCIIToByteArray(internalNameROM, 32));
            writer.Write(CRC32);
            writer.Write(countryCode);

            for (int i = 0; i < 14; i++)
            {
                writer.Write(reserve4);
            }

            writer.Write(ASCIIToByteArray(videoPlugin, 64));
            writer.Write(ASCIIToByteArray(soundPlugin, 64));
            writer.Write(ASCIIToByteArray(inputPlugin, 64));
            writer.Write(ASCIIToByteArray(rspPlugin, 64));
            writer.Write(UTF8ToByteArray(author, 222));
            writer.Write(UTF8ToByteArray(movieDescription, 256));
        }

        void reader_ControllerStateChanged(IControllerReader reader, ControllerState newState)
        {
            writePacket(newState.Packet);
        }

        public void writePacket(byte[] packet)
        {
            writer.Flush();
            writer.Write(packet);
            numOfWrites++;
        }

        public void Close()
        {
            writer.Seek(12, SeekOrigin.Begin);      // number of frames?
            //writer.Write((numOfWrites));

            writer.Close();
        }

    }
}
