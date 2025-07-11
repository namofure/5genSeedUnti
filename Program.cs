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
        uint[] Data = new uint[16];
        string[] lines = File.ReadAllLines("Config.txt");

        foreach (string line in lines)
        {
            int index = line.IndexOf("0x");

            string hex = line.Substring(index + 2);
            uint val = uint.Parse(hex, NumberStyles.HexNumber);
            Values.Add(val);
        }

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

        var Dt = new DateTime(2000, 01, 01, 0, 0, 0);

        byte[] YMDD = new byte[4];

        YMDD[3] = (byte)(Dt.Year % 100);
        YMDD[2] = (byte)Dt.Month;
        YMDD[1] = (byte)Dt.Day;
        YMDD[0] = (byte)Dt.DayOfWeek;

        uint Date = BitConverter.ToUInt32(YMDD, 0);

        Console.WriteLine($"Year: {Dt.Year}, Month: {Dt.Month}, Day: {Dt.Day}, Dow: {Dt.DayOfWeek}");

        byte[] HMSZ = new byte[4];

        HMSZ[3] = (byte)Dt.Hour;
        HMSZ[2] = (byte)Dt.Minute;
        HMSZ[1] = (byte)Dt.Second;
        HMSZ[0] = 0;

        uint Time = BitConverter.ToUInt32(HMSZ, 0);

        Console.WriteLine($"Hour: {Dt.Hour}, Minute: {Dt.Minute}, Second: {Dt.Second}");


        Data[0] = toLittleEndian(Values[0]);
        Data[1] = toLittleEndian(Values[1]);
        Data[2] = toLittleEndian(Values[2]);
        Data[3] = toLittleEndian(Values[2] + 0x54);
        Data[4] = toLittleEndian(Values[2] + 0x54);
        Data[5] = toLittleEndian((Values[3] << 16) + Values[4]);
        Data[6] = (Values[6]);
        Data[7] = ((toLittleEndian(Values[7] ^ Values[8])) ^ Values[5]);
        Data[8] = (Date);
        Data[9] = (Time);
        Data[10] = 0;
        Data[11] = 0;
        Data[12] = toLittleEndian(Values[9]);
        Data[13] = (Values[10]);
        Data[14] = (Values[11]);
        Data[15] = (Values[12]);

        for (int i = 0; i <= 15; i++)
        {
            Console.WriteLine($"Data[{i}] = 0x{Data[i]:X8}");

        }

        byte[] dataBytes = new byte[64];

        for (int i = 0; i < 16; i++)
        {
            byte[] temp = BitConverter.GetBytes(Data[i]);
            Array.Reverse(temp);
            Buffer.BlockCopy(temp, 0, dataBytes, i * 4, 4);
        }

        for (int i = 0; i < 64; i++)
        {
            Console.Write($"{dataBytes[i]:X2} ");
            if ((i + 1) % 16 == 0) Console.WriteLine();
        }

        SHA1 sha1 = SHA1.Create();

        byte[] hash = sha1.ComputeHash(dataBytes);
        Console.WriteLine("SHA-1 Hash:");
        Console.WriteLine(BitConverter.ToString(hash).Replace("-", "").ToUpper());

    }

    static uint toLittleEndian(uint values)
    {
        return ((values & 0x000000FF) << 24) |
               ((values & 0x0000FF00) << 8) |
               ((values & 0x00FF0000) >> 8) |
               ((values & 0xFF000000) >> 24);
    }
}
