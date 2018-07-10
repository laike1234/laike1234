using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Gate
{
     public static class Device
    {
         private const string DLLNAME = @"UHFGate.dll";
         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int AutoOpenComPort(ref int Port,
                                                  ref byte ComAddr,
                                                  ref int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int OpenComPort(int Port,
                                              ref byte ComAddr,
                                              ref int PortHandle);

       
         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int CloseComPort();

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int CloseSpecComPort(int PortHandle);


         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int OpenNetPort(int Port,
                                              string iPaddr,
                                              ref byte ComAddr,
                                              ref int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int CloseNetPort(int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetChannelMessage(ref byte ConAddr,
                                                    byte[] Msg, 
                                                    ref byte MsgLength , 
                                                    ref byte MsgType,
                                                    ref byte IRStatue,
                                                    int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int Acknowledge(ref byte ConAddr,
                                              int PortHandle);


         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetClock(ref byte ConAddr,
                                           byte[] SetTime,
                                           ref byte IRStatue,
                                           int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetClock(ref byte ConAddr,
                                           byte[] CurrentTime,
                                           ref byte IRStatue,
                                           int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int ClearControllerBuffer(ref byte ConAddr,
                                                        ref byte IRStatue,
                                                        int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int ConfigureController(ref byte ConAddr,
                                                      byte IREnable,
                                                      byte IRTime,
                                                      byte TagExistTime,
                                                      byte AlarmEn,
                                                      byte DelayTime,
                                                      byte Pepolemsg,
                                                      byte AEn,
                                                      ref byte IRStatue,
                                                      int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetControllerConfig(ref byte ConAddr,
                                                      ref byte IREnable,
                                                      ref byte IRTime,
                                                      ref byte TagExistTime,
                                                      ref byte AlarmEn,
                                                      ref byte DelayTime,
                                                      ref byte Pepolemsg,
                                                      ref byte AEn,
                                                      ref byte IRStatue,
                                                      int PortHandle);


         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetControllerInfo(ref byte ConAddr, 
                                                    ref byte ProductCode, 
                                                    ref byte MainVer,
                                                    ref byte SubVer,
                                                    ref byte IRStatue,
                                                    int PortHandle);

        

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetControllerReaderConnectionStatus(ref byte ConAddr,
                                                                      ref byte ConnectionStatus,
                                                                      ref byte IRStatue,
                                                                      int PortHandle);

        

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetControllerAddr(ref byte ConAddr,
                                                    byte Flag, 
                                                    byte NewAddr,
                                                    ref byte IRStatue,
                                                    int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int ModeSwitch(ref byte ConAddr,
                                             ref byte Mode,
                                             ref byte IRStatue,
                                             int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int IRDirectionSetting(ref byte ConAddr,
                                                     ref byte Flag,
                                                     ref byte IRStatue,
                                                     int PortHandle);

       

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetEASMessage(ref byte ConAddr,
                                                byte[] Msg, 
                                                ref byte MsgLength ,
                                                ref byte MsgType,
                                                ref byte IRStatue,
                                                int PortHandle);
       

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetEASWorkStyle(ref byte ConAddr,
                                                  byte[] EASMode,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetEASWorkStyle(ref byte ConAddr,
                                                  byte[] EASMode,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int StatisticalMsg(ref byte ConAddr,
                                                  byte[] positive,
                                                  byte[] reverse,
                                                  byte[] AlarmNum,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int ClearStatisticalMsg(ref byte ConAddr,
                                                  ref byte IRStatue,
                                                  int PortHandle);


         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetReadParameter(ref byte ConAddr,
                                                  byte Qvalue,
                                                  byte Session,
                                                  byte AdrTID,
                                                  byte LenTID,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetReadParameter(ref byte ConAddr,
                                                  ref byte Qvalue,
                                                  ref byte Session,
                                                  ref byte AdrTID,
                                                  ref byte LenTID,
                                                  ref byte MaskMem,
                                                  byte[] MaskAdr,
                                                  ref byte MaskLen,
                                                  byte[] MaskData,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetWorkParameter(ref byte ConAddr,
                                                  byte Power,
                                                  byte MaxFre,
                                                  byte MinFre,
                                                  byte BeepEn,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetWorkParameter(ref byte ConAddr,
                                                  ref byte Power,
                                                  ref byte MaxFre,
                                                  ref byte MinFre,
                                                  ref byte BeepEn,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetRelay(ref byte ConAddr,
                                                  byte RelayTime,
                                                  ref byte IRStatue,
                                                  int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int BuzzerAndLEDControl(ref byte ConAddr,
                                                  byte BuzzerOnTime,
                                                  byte BuzzerOffTime,
                                                  byte BuzzerActTimes,
                                                  byte LEDOnTime,
                                                  byte LEDOffTime,
                                                  byte LEDFlashTimes,
                                                  ref byte IRStatue,
                                                  int PortHandle);


    }
}
