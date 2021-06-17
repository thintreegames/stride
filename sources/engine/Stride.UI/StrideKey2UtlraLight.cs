using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Input;

namespace Stride.UI
{
    public readonly struct StrideKey2UtlraLight
    {
        private static Dictionary<Keys, int> strideKey2VirtualKey = new Dictionary<Keys, int>()
        {
            { Keys.Back, 0x08 },
            { Keys.Tab, 0x09 },
            { Keys.Clear, 0x0C },
            { Keys.Return, 0x0D },

            { Keys.Pause, 0x13 },
            { Keys.CapsLock, 0x14 },

            { Keys.HangulMode, 0x15 },
            { Keys.JunjaMode, 0x17 },
            { Keys.FinalMode, 0x18 },
            { Keys.KanjiMode, 0x19 },

            { Keys.Escape, 0x1B },
            { Keys.ImeConvert, 0x1C },
            { Keys.ImeNonConvert, 0x1D },
            { Keys.ImeModeChange, 0x1F },

            { Keys.Space, 0x20 },
            { Keys.Prior, 0x21 },
            { Keys.Next, 0x22 },
            { Keys.End, 0x23 },
            { Keys.Home, 0x24 },

            { Keys.Left, 0x25 },
            { Keys.Up, 0x26 },
            { Keys.Right, 0x27 },
            { Keys.Down, 0x28 },

            { Keys.Select, 0x29 },
            { Keys.Print, 0x2A },
            { Keys.Execute, 0x2B },
            { Keys.Snapshot, 0x2C },
            { Keys.Insert, 0x2D },
            { Keys.Delete, 0x2E },
            { Keys.Help, 0x2F },

            { Keys.D0, 0x30 },
            { Keys.D1, 0x31 },
            { Keys.D2, 0x32 },
            { Keys.D3, 0x33 },
            { Keys.D4, 0x34 },
            { Keys.D5, 0x35 },
            { Keys.D6, 0x36 },
            { Keys.D7, 0x37 },
            { Keys.D8, 0x38 },
            { Keys.D9, 0x39 },

            { Keys.A, 0x41 },
            { Keys.B, 0x42 },
            { Keys.C, 0x43 },
            { Keys.D, 0x44 },
            { Keys.E, 0x45 },
            { Keys.F, 0x46 },
            { Keys.G, 0x47 },
            { Keys.H, 0x48 },
            { Keys.I, 0x49 },
            { Keys.J, 0x4A },
            { Keys.K, 0x4B },
            { Keys.L, 0x4C },
            { Keys.M, 0x4D },
            { Keys.N, 0x4E },
            { Keys.O, 0x4F },
            { Keys.P, 0x50 },
            { Keys.Q, 0x51 },
            { Keys.R, 0x52 },
            { Keys.S, 0x53 },
            { Keys.T, 0x54 },
            { Keys.U, 0x55 },
            { Keys.V, 0x56 },
            { Keys.W, 0x57 },
            { Keys.X, 0x58 },
            { Keys.Y, 0x59 },
            { Keys.Z, 0x5A },

            { Keys.NumPad0, 0x60 },
            { Keys.NumPad1, 0x61 },
            { Keys.NumPad2, 0x62 },
            { Keys.NumPad3, 0x63 },
            { Keys.NumPad4, 0x64 },
            { Keys.NumPad5, 0x65 },
            { Keys.NumPad6, 0x66 },
            { Keys.NumPad7, 0x67 },
            { Keys.NumPad8, 0x68 },
            { Keys.NumPad9, 0x69 },

            { Keys.OemComma, 0xBC },
            { Keys.OemPeriod, 0xB3 }
        };

        private static Dictionary<Keys, string> strideKey2Text = new Dictionary<Keys, string>()
        {
            { Keys.D0, "0" },
            { Keys.D1, "1" },
            { Keys.D2, "2" },
            { Keys.D3, "3" },
            { Keys.D4, "4" },
            { Keys.D5, "5" },
            { Keys.D6, "6" },
            { Keys.D7, "7" },
            { Keys.D8, "8" },
            { Keys.D9, "9" },

            { Keys.A, "a" },
            { Keys.B, "b" },
            { Keys.C, "c" },
            { Keys.D, "d" },
            { Keys.E, "e" },
            { Keys.F, "f" },
            { Keys.G, "g" },
            { Keys.H, "h" },
            { Keys.I, "i" },
            { Keys.J, "j" },
            { Keys.K, "k" },
            { Keys.L, "l" },
            { Keys.M, "m" },
            { Keys.N, "n" },
            { Keys.O, "o" },
            { Keys.P, "p" },
            { Keys.Q, "q" },
            { Keys.R, "r" },
            { Keys.S, "s" },
            { Keys.T, "t" },
            { Keys.U, "u" },
            { Keys.V, "v" },
            { Keys.W, "w" },
            { Keys.X, "x" },
            { Keys.Y, "y" },
            { Keys.Z, "z" },

            { Keys.NumPad0, "0" },
            { Keys.NumPad1, "1" },
            { Keys.NumPad2, "2" },
            { Keys.NumPad3, "3" },
            { Keys.NumPad4, "4" },
            { Keys.NumPad5, "5" },
            { Keys.NumPad6, "6" },
            { Keys.NumPad7, "7" },
            { Keys.NumPad8, "8" },
            { Keys.NumPad9, "9" },

            { Keys.OemComma, "," },
            { Keys.OemPeriod, "." }
        };

        private static ISet<Keys> strideKeyIsKeypad = new HashSet<Keys>()
        {
            Keys.NumPad0,
            Keys.NumPad1,
            Keys.NumPad2,
            Keys.NumPad3,
            Keys.NumPad4,
            Keys.NumPad5,
            Keys.NumPad6,
            Keys.NumPad7,
            Keys.NumPad8,
            Keys.NumPad9
        };

        private static ISet<Keys> strideKeyIsSystemKey = new HashSet<Keys>();

        public readonly Keys Key;

        public StrideKey2UtlraLight(Keys key)
        {
            Key = key;
        }

        public bool IsSystemKey()
        {
            return strideKeyIsSystemKey.Contains(Key);
        }

        public bool IsKeypad()
        {
            return strideKeyIsKeypad.Contains(Key);
        }

        public int GetVirtualKeyCode()
        {
            strideKey2VirtualKey.TryGetValue(Key, out int virtualKey);
            return virtualKey;
        }

        public string GetText()
        {
            strideKey2Text.TryGetValue(Key, out var text);
            return text;
        }
    }
}
