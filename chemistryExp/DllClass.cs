using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;   // 用 DllImport 需用此 命名空间
namespace chemistryExp
{
    class DllClass
    {
        //发出“bi”声音的函数
        [DllImport("kernel32.dll")]
        private static extern int Beep(int dwFreq, int dwDuration);
        //查找设备
        [DllImport("ADIO86.dll", EntryPoint = "FindDevice", CharSet = CharSet.Ansi,
           CallingConvention = CallingConvention.Cdecl)]
        public static extern int FindDevice();

        //打开设备
        [DllImport("ADIO86.dll", EntryPoint = "OpenYavDevice", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenYavDevice(UInt16 ID);

        //读取数据
        [DllImport("ADIO86.dll", EntryPoint = "GetYavData", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetYavData(UInt16 TaskID, ref int AddDataBuffer, UInt32 DataSize,
          ref int YavParam, ref int CNTBuffer, ref int IOBuffer);

        //配置设备
        [DllImport("ADIO86.dll", EntryPoint = "SetYavParam", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetYavParam(UInt16 TaskID, Byte CMD, ref Byte SetParam);

        //关闭设备
        [DllImport("ADIO86.dll", EntryPoint = "CloseYavDevice", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseYavDevice(UInt16 TaskID);
    }
}
