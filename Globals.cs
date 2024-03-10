using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;                    // NuGet -> System.Management 8.0.0
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Control3
{
    public class VideoSourceInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
    // Class GlobalVars for variables to be globally used in this projects classes and windows
    public class Flags
    {
        // The videosource to be used for the remote
        public string selectedSource = null;

        // Is the mouse in motion?  Wait to send the next motion, else you get a queue of movement which continues remote
        // Now it just skips movements when the serialport is not ready yet
        // This global is set at a MouseMove being captured and unnset once the the packet is processed by the CH3929
        public Boolean isMoving = false;

        // True meanse that all the mouse and keyboard actions are processed by the globalhook routines
        // and not send through to the local desktop!!
        public Boolean isRemote = false;

        // Is the remote window started in FullScreen
        public Boolean isFullScreen = false;

        //The Decoration variable is used to send the extra keys which are pressed while typing
        //bit7          bit6        bit5        bit4        bit3            bit2        bit1        bit0
        //Right Windows Right Alt   Right Shift Right Ctrl  Left Windows    Left Alt    Left Shift  Left Ctrl
        public byte Decoration = 0;

        //COM Port for the remote cable
        public string COMPort = null;

        //The Keymap is used to translate the Keycode to the CH9329 codes
        public static Dictionary<byte, byte> KeyMap { get; private set; }
        static Flags()
        {
            KeyMap = new Dictionary<byte, byte>
            {
                { 8, 0x2A },  // Back
                { 9, 0x2B },  // Tab
                { 13, 0x28 },  // Enter
                { 19, 0x48 },  //Pause
                { 20, 0x39 },  //Caps Lock
                { 27, 0x29 },  //Escape
                { 32, 0x2C },  // Space
                { 33, 0x4B },  // PageUp
                { 34, 0x4E },  // Next
                { 35, 0x4D },  // End
                { 36, 0x4A },  // Home
                { 37, 0x50 },  // Left
                { 38, 0x52 },  // Up
                { 39, 0x4F },  // Right
                { 40, 0x51 },  // Down
                { 44, 0x46 },  // PrintScreen
                { 45, 0x49 },  // Insert
                { 46, 0x4C },  // Delete
                { 48, 0x27 },  // 0
                { 49, 0x1E },  // 1
                { 50, 0x1F },  // 2
                { 51, 0x20 },  // 3
                { 52, 0x21 },  // 4
                { 53, 0x22 },  // 5
                { 54, 0x23 },  // 6
                { 55, 0x24 },  // 7
                { 56, 0x25 },  // 8
                { 57, 0x26 },  // 9
                { 65, 0x04 },  // A
                { 66, 0x05 },  // B 
                { 67, 0x06 },  // C
                { 68, 0x07 },  // D
                { 69, 0x08 },  // E
                { 70, 0x09 },  // F
                { 71, 0x0A },  // G
                { 72, 0x0B },  // H
                { 73, 0x0C },  // I
                { 74, 0x0D },  // J
                { 75, 0x0E },  // K
                { 76, 0x0F },  // L
                { 77, 0x10 },  // M
                { 78, 0x11 },  // N
                { 79, 0x12 },  // O
                { 80, 0x13 },  // P
                { 81, 0x14 },  // Q
                { 82, 0x15 },  // R
                { 83, 0x16 },  // S
                { 84, 0x17 },  // T
                { 85, 0x18 },  // U
                { 86, 0x19 },  // V
                { 87, 0x1A },  // W
                { 88, 0x1B },  // X
                { 89, 0x1C },  // Y
                { 90, 0x1D },  // Z
                { 91, 0xE3 },  // LWin
                { 93, 0x65 },  // Applications
                { 96, 0x62 },  // NumPad 0
                { 97, 0x59 },  // NumPad 1
                { 98, 0x5A },  // NumPad 2
                { 99, 0x5B },  // NumPad 3
                { 100, 0x5C },  // NumPad 4
                { 101, 0x5D },  // NumPad 5
                { 102, 0x5E },  // NumPad 6
                { 103, 0x5F },  // NumPad 7
                { 104, 0x60 },  // NumPad 8
                { 105, 0x61 },  // NumPad 9
                { 106, 0x55 },  // NumPad Multiply
                { 107, 0x57 },  // NumPad Add
                { 109, 0x56 },  // NumPad Subtract
                { 110, 0x63 },  // NumPad Decimal
                { 111, 0x54 },  // NumPad Divide
                { 112, 0x3A },  // F1
                { 113, 0x3B },  // F2
                { 114, 0x3C },  // F3
                { 115, 0x3D },  // F4
                { 116, 0x3E },  // F5
                { 117, 0x3F },  // F6
                { 118, 0x40 },  // F7
                { 119, 0x41 },  // F8
                { 120, 0x42 },  // F9
                { 121, 0x43 },  // F10
                { 122, 0x44 },  // F11
                { 123, 0x45 },  // F12
                { 144, 0x53 },  // NumLock
                { 160, 0xE1 },  // LShiftKey
                { 161, 0xE5 },  // RShiftKey
                { 162, 0xE0 },  // LControlKey
                { 163, 0xE4 },  // RControlKey
                { 164, 0xE2 },  // LAlt
                { 165, 0xE6 },  // Ralt
                { 186, 0x33 },  // ;
                { 187, 0x2E },  // =
                { 188, 0x36 },  // ,
                { 189, 0x2D },  // -
                { 190, 0x37 },  // .
                { 191, 0x38 },  // /
                { 192, 0x35 },  // `
                { 219, 0x2F },  // [
                { 220, 0x31 },  // \
                { 221, 0x30 },  // ]
                { 222, 0x34 }  // '
            };
        }
    }
    public static class MouseUtility
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);
        public static void SetMouseCursorAside()
        {
            Screen primaryScreen = Screen.PrimaryScreen;
            int maxX = primaryScreen.Bounds.Width; 
            int maxY = primaryScreen.Bounds.Height/2; 
            SetCursorPos(maxX, maxY);
        }
    }
    public static class SerialPortUtility
    {
        public static Dictionary<string, string> GetSerialPorts()
        {
            Dictionary<string, string> serialPorts = new Dictionary<string, string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                string caption = queryObj["Caption"].ToString();
                string device = queryObj["DeviceID"].ToString();
                int comIndex = caption.IndexOf("(COM");
                if (comIndex >= 0)
                {
                    string portName = caption.Substring(comIndex + 1, caption.IndexOf(")", comIndex) - comIndex - 1);
                    serialPorts.Add(portName, device);
                }
            }
            return serialPorts;
        }
    }
    public class SettingsManager
    {
        public static void SaveSetting(string value)
        {
            string companyName = "Result3";
            string applicationName = "Control3";
            string settingName = "VideoSource";
            RegistryKey key = Registry.CurrentUser.CreateSubKey($"Software\\{companyName}\\{applicationName}");
            key.SetValue(settingName, value);
            key.Close();
        }
        public static string GetSetting()
        {
            string companyName = "Result3";
            string applicationName = "Control3";
            string settingName = "VideoSource";
            RegistryKey readKey = Registry.CurrentUser.OpenSubKey($"Software\\{companyName}\\{applicationName}");
            string readValue = readKey?.GetValue(settingName)?.ToString();
            readKey?.Close();
            return readValue as string;
        }
    }
}
