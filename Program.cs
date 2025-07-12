using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Microsoft.VisualBasic;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        List<uint> Values = new List<uint>();
        uint[] Data = new uint[80];

        if (!File.Exists("Config.txt"))
        {
            Console.WriteLine("setting.txtが見つかりませんでした");
            Console.ReadKey();
            return;
        }
        string[] lines = File.ReadAllLines("Config.txt");

        Console.WriteLine($"[年, 月, 日, 時, 分, 秒, VCount, Timer0, 初期SEED]");

        for (int i = 0; i < 13; i++)
        {
            int index = lines[i].IndexOf("0x");

            string hex = lines[i].Substring(index + 2);
            uint val = uint.Parse(hex, NumberStyles.HexNumber);
            Values.Add(val);
        }

        var Dt = new DateTime(2000, 1, 1, 0, 0, 0);
        int AddHours = 0;
        int AddMinutes = 0;
        int AddSeconds = 1;
        int Count = 1;

        Dt = DateTime.Parse(lines[14].Split('=')[1].Trim());
        AddHours = int.Parse(lines[15].Split('=')[1].Trim());
        AddMinutes = int.Parse(lines[16].Split('=')[1].Trim());
        AddSeconds = int.Parse(lines[17].Split('=')[1].Trim());
        Count = int.Parse(lines[18].Split('=')[1].Trim());

        var increment = new TimeSpan(AddHours, AddMinutes, AddSeconds);

        //Values[0] = Nazo1
        //Values[1] = Nazo2
        //Values[2] = Nazo3
        //Values[3] = Vount
        //Values[4] = Timer0
        //Values[5] = Mac 上位
        //Values[6] = Mac 下位
        //Values[7] = GxFrame
        //Values[8] = Frame
        //Values[9] = Key 入力なし
        //Values[10] = Prm1
        //Values[11] = Prm2
        //Values[12] = Prm3

        for (int n = 0; n < Count; n++)
        {

            byte[] YMDD = new byte[4];

            YMDD[3] = toHex(Dt.Year % 100);
            YMDD[2] = toHex(Dt.Month);
            YMDD[1] = toHex(Dt.Day);
            YMDD[0] = (byte)(Dt.DayOfWeek);

            uint Date = BitConverter.ToUInt32(YMDD, 0);

            byte[] HMSZ = new byte[4];

            HMSZ[3] = toHex(Dt.Hour);
            if (Dt.Hour > 11) HMSZ[3] += 0x40;
            HMSZ[2] = toHex(Dt.Minute);
            HMSZ[1] = toHex(Dt.Second);
            HMSZ[0] = 0;

            uint Time = BitConverter.ToUInt32(HMSZ, 0);

            Data[0] = toLittleEndian(Values[0]);
            Data[1] = toLittleEndian(Values[1]);
            Data[2] = toLittleEndian(Values[2]);
            Data[3] = toLittleEndian(Values[2] + 0x54);
            Data[4] = toLittleEndian(Values[2] + 0x54);
            Data[5] = toLittleEndian((Values[3] << 16) + Values[4]);
            Data[6] = (Values[6]);
            Data[7] = toLittleEndian(((Values[7] ^ Values[8])) ^ toLittleEndian(Values[5]));
            Data[8] = (Date);
            Data[9] = (Time);
            Data[10] = 0;
            Data[11] = 0;
            Data[12] = toLittleEndian(Values[9]);
            Data[13] = (Values[10]);
            Data[14] = (Values[11]);
            Data[15] = (Values[12]);

            //------------------------------------------------------
            for (int t = 16; t < 80; t++)
            {
                var w = Data[t - 3] ^ Data[t - 8] ^ Data[t - 14] ^ Data[t - 16];
                Data[t] = (w << 1) | (w >> 31);
            }

            const uint H0 = 0x67452301;
            const uint H1 = 0xEFCDAB89;
            const uint H2 = 0x98BADCFE;
            const uint H3 = 0x10325476;
            const uint H4 = 0xC3D2E1F0;

            uint A, B, C, D, E;
            A = H0; B = H1; C = H2; D = H3; E = H4;

            for (int t = 0; t < 20; t++)
            {
                var temp = ((A << 5) | (A >> 27)) + ((B & C) | ((~B) & D)) + E + Data[t] + 0x5A827999;
                E = D;
                D = C;
                C = (B << 30) | (B >> 2);
                B = A;
                A = temp;
            }
            for (int t = 20; t < 40; t++)
            {
                var temp = ((A << 5) | (A >> 27)) + (B ^ C ^ D) + E + Data[t] + 0x6ED9EBA1;
                E = D;
                D = C;
                C = (B << 30) | (B >> 2);
                B = A;
                A = temp;
            }
            for (int t = 40; t < 60; t++)
            {
                var temp = ((A << 5) | (A >> 27)) + ((B & C) | (B & D) | (C & D)) + E + Data[t] + 0x8F1BBCDC;
                E = D;
                D = C;
                C = (B << 30) | (B >> 2);
                B = A;
                A = temp;
            }
            for (int t = 60; t < 80; t++)
            {
                var temp = ((A << 5) | (A >> 27)) + (B ^ C ^ D) + E + Data[t] + 0xCA62C1D6;
                E = D;
                D = C;
                C = (B << 30) | (B >> 2);
                B = A;
                A = temp;
            }

            ulong seed = toLittleEndian(H1 + B);
            seed <<= 32;
            seed |= toLittleEndian(H0 + A);
            //------------------------------------------------------

            YMDD[3] = (byte)(Dt.Year % 100);
            YMDD[2] = (byte)Dt.Month;
            YMDD[1] = (byte)Dt.Day;

            HMSZ[3] = (byte)Dt.Hour;
            HMSZ[2] = (byte)Dt.Minute;
            HMSZ[1] = (byte)Dt.Second;

            Console.WriteLine($"{YMDD[3]}, {YMDD[2]}, {YMDD[1]}, {HMSZ[3]}, {HMSZ[2]}, {HMSZ[1]}, 0x{Values[3]:X2}, 0x{Values[4]:X4}, 0x{seed:X16}");

            Dt = Dt.Add(increment);
        }
    }

    static byte toHex(int value)
    {
        return (byte)((value / 10) * 6 + value);
    }

    static uint toLittleEndian(uint values)
    {
        return ((values & 0x000000FF) << 24) |
               ((values & 0x0000FF00) << 8) |
               ((values & 0x00FF0000) >> 8) |
               ((values & 0xFF000000) >> 24);
    }
}
