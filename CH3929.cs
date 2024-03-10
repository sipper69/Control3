using System.Collections.Generic;
using System.IO.Ports;                  // NuGet ->  System.IO.Ports  8.0.0
using System.Linq;
using System.Threading;

namespace Control3
{
    public class CH9329
    {
        // This class is a modified version of the class provided by SmallCodeNote under MIT License
        // https://github.com/SmallCodeNote/CH9329-109KeyClass

        public string PortName;
        public int BaudRate;
        public int LeftStatus = 0;
        private SerialPort serialPort;

        public CH9329(string PortName = "COM8", int BaudRate = 9600)
        {
            this.PortName = PortName;
            this.BaudRate = BaudRate;

            serialPort = new SerialPort(PortName, BaudRate);
            serialPort.Open();

            createMediaKeyTable();
        }

        private Dictionary<mediaKey, byte[]> mediaKeyTable;

        public enum mediaKey
        {
            EJECT,
            CDSTOP,
            PREVTRACK,
            NEXTTRACK,
            PLAYPAUSE,
            MUTE,
            VOLUMEDOWN,
            VOLUMEUP,
        }

        private void createMediaKeyTable()
        {
            mediaKeyTable = new Dictionary<mediaKey, byte[]>();
            mediaKeyTable.Add(mediaKey.EJECT, new byte[] { 0x02, 0x80, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.CDSTOP, new byte[] { 0x02, 0x40, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.PREVTRACK, new byte[] { 0x02, 0x20, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.NEXTTRACK, new byte[] { 0x02, 0x10, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.PLAYPAUSE, new byte[] { 0x02, 0x08, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.MUTE, new byte[] { 0x02, 0x04, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.VOLUMEDOWN, new byte[] { 0x02, 0x02, 0x00, 0x00 });
            mediaKeyTable.Add(mediaKey.VOLUMEUP, new byte[] { 0x02, 0x01, 0x00, 0x00 });
        }

        public enum SpecialKeyCode : byte
        {
            ENTER = 0x28,
            ESCAPE = 0x29,
            BACKSPACE = 0x2A,
            TAB = 0x2B,
            SPACEBAR = 0x2C,
            CAPS_LOCK = 0x39,
            F1 = 0x3A,
            F2 = 0x3B,
            F3 = 0x3C,
            F4 = 0x3D,
            F5 = 0x3E,
            F6 = 0x3F,
            F7 = 0x40,
            F8 = 0x41,
            F9 = 0x42,
            F10 = 0x43,
            F11 = 0x44,
            F12 = 0x45,
            PRINTSCREEN = 0x46,
            SCROLL_LOCK = 0x47,
            PAUSE = 0x48,
            INSERT = 0x49,
            HOME = 0x4A,
            PAGEUP = 0x4B,
            DELETE = 0x4C,
            END = 0x4D,
            PAGEDOWN = 0x4E,
            RIGHTARROW = 0x4F,
            LEFTARROW = 0x50,
            DOWNARROW = 0x51,
            UPARROW = 0x52,
            APPLICATION = 0x65,
            LEFT_CTRL = 0xE0,
            LEFT_SHIFT = 0xE1,
            LEFT_ALT = 0xE2,
            LEFT_WINDOWS = 0xE3,
            RIGHT_CTRL = 0xE4,
            RIGHT_SHIFT = 0xE5,
            RIGHT_ALT = 0xE6,
            RIGHT_WINDOWS = 0xE7,
            CTRL = 0xE4,
            SHIFT = 0xE5,
            ALT = 0xE6,
            WINDOWS = 0xE7,
        }

        public enum MouseButtonCode : byte
        {
            LEFT = 0x01,
            RIGHT = 0x02,
            MIDDLE = 0x04,
        }

        private string sendPacket(byte[] data)
        {
            string resultMessage = "";

            // Use a separate thread for serial port communication
            Thread serialThread = new Thread(() =>
            {
                serialPort.Write(data, 0, data.Length);
                Thread.Sleep(1);

                // Unset isMoving (see GlobalHook_MouseMove in MainWindow)
                // In CH9329 the flag is set to false again after the package is sent to remote. Prevents queue of movement on remote
                App.Flag.isMoving = false;
            });
            serialThread.Start();

            return resultMessage;
        }

        private byte[] createPacketArray(List<int> arrList, bool addCheckSum)
        {
            List<byte> bytePacketList = arrList.ConvertAll(b => (byte)b);
            if (addCheckSum) bytePacketList.Add((byte)(arrList.Sum() & 0xff));
            return bytePacketList.ToArray();
        }

        byte[] charKeyUpPacket = { 0x57, 0xAB, 0x00, 0x02, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c };
        byte[] mediaKeyUpPacket = { 0x57, 0xAB, 0x00, 0x03, 0x04, 0x02, 0x00, 0x00, 0x00, 0x0B };

        public enum KeyGroup : byte
        {
            CharKey = 0x02,
            MediaKey = 0x03,
        }
        public enum CommandCode : byte
        {
            GET_INFO = 0x01,
            SEND_KB_GENERAL_DATA = 0x02,
            SEND_KB_MEDIA_DATA = 0x03,
            SEND_MS_ABS_DATA = 0x04,
            SEND_MS_REL_DATA = 0x05,
            READ_MY_HID_DATA = 0x07,
            GET_PARA_CFG = 0x08,
            GET_USB_STRING = 0x0A,
        }

        public byte CHIP_VERSION;
        public byte CHIP_STATUS;
        public bool NUM_LOCK;
        public bool CAPS_LOCK;
        public bool SCROLL_LOCK;

        public void getInfo()
        {
            byte[] getInfoPacket = { 0x57, 0xAB, 0x00, (byte)CommandCode.GET_INFO, 0x00, 0x03 };
            string resultString = sendPacket(getInfoPacket);

            CHIP_VERSION = (byte)resultString[0];
            CHIP_STATUS = (byte)resultString[1];
            byte flagByte = (byte)resultString[2];
            NUM_LOCK = (flagByte & 0b00000001) > 0;
            CAPS_LOCK = (flagByte & 0b00000010) > 0;
            SCROLL_LOCK = (flagByte & 0b00000100) > 0;
        }

        public void keyDown(KeyGroup keyGroup, byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
            // ========================
            // keyDownPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x02} + LEN{0x08} + DATA{k0, 0x00, k1, k2, k3, k4, k5, k6}
            // CMD = KeyGroup
            // ========================
            List<int> keyDownPacketListInt = new List<int> { 0x57, 0xAB, 0x00, (int)keyGroup, 0x08, k0, 0x00, k1, k2, k3, k4, k5, k6 };

            byte[] keyDownPacket = createPacketArray(keyDownPacketListInt, true);

            sendPacket(keyDownPacket);
        }
        public void keyUpAll()
        {
            keyUpAll(KeyGroup.CharKey);
        }
        public void keyUpAll(KeyGroup keyGroup)
        {
            if (keyGroup == KeyGroup.CharKey) { sendPacket(charKeyUpPacket); }
            else { sendPacket(mediaKeyUpPacket); };
        }
        public void keyDown(SpecialKeyCode specialKeyCode)
        {
            keyDown(KeyGroup.CharKey, (byte)specialKeyCode, 0x00);
        }
        public void charKeyType(byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
            keyDown(KeyGroup.CharKey, k0, k1, k2, k3, k4, k5, k6);
            keyUpAll(KeyGroup.CharKey);
        }

        public void mediaKeyType(mediaKey MediaKey)
        {
            byte[] dat = mediaKeyTable[MediaKey];
            keyDown(KeyGroup.MediaKey, dat[0], dat[1], dat[2], dat[3]);
            keyUpAll(KeyGroup.MediaKey);
        }

        public void mouseMoveRel(int x, int y)
        {
            if (x > 127) { x = 127; }; if (x < -128) { x = -128; }; if (x < 0) { x = 0x100 + x; };
            if (y > 127) { y = 127; }; if (y < -128) { y = -128; }; if (y < 0) { y = 0x100 + y; };

            // ========================
            // mouseMoveRelPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01, 0x00}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseMoveRelPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00 };

            mouseMoveRelPacketListInt[6] = LeftStatus;

            mouseMoveRelPacketListInt.Add((byte)(x));
            mouseMoveRelPacketListInt.Add((byte)(y));
            mouseMoveRelPacketListInt.Add(0x00);

            byte[] mouseMoveRelPacket = createPacketArray(mouseMoveRelPacketListInt, true);
            sendPacket(mouseMoveRelPacket);
        }

        public void mouseButtonDown(MouseButtonCode buttonCode)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00 };
            mouseButtonDownPacketListInt[6] = (int)buttonCode;

            if ((int)buttonCode == 1)
            {
                LeftStatus = 1;
            }

            byte[] mouseButtonDownPacket = createPacketArray(mouseButtonDownPacketListInt, true);
            sendPacket(mouseButtonDownPacket);
        }

        public void mouseButtonUpAll()
        {
            byte[] mouseButtonUpPacket = { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x0D };
            LeftStatus = 0;
            sendPacket(mouseButtonUpPacket);
        }

        public string mouseScroll(int scrollCount)
        {
            // ========================
            // mouseScrollPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================

            List<int> mouseScrollPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00 };
            mouseScrollPacketListInt[9] = scrollCount;
            //mouseScrollPacketListInt.Add(scrollCount);

            byte[] mouseScrollPacket = createPacketArray(mouseScrollPacketListInt, true);
            return sendPacket(mouseScrollPacket);
        }
    }
}
