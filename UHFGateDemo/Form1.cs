using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gate;
using System.IO.Ports;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UHFGateDemo
{
    public partial class Form1 : Form
    {
        private byte ControllerAdr = 0xff;
        private int fCmdRet = 30;
        private bool IsGetting;         //是否正在执行'C'命令标志,
        private bool IsCloseApp;
        private int PortHandle = -1;
        private byte IRStatus;
        private byte fModel;
        private const byte OK = 0x00;
        private const byte CommandError = 0x08;
        private const byte Mode = 0x09;
        private const byte ErrorMessage = 0x0F;
        private const byte CommunicationErr = 0x30;
        private const byte RetCRCErr = 0x31;
        private const byte RetDataErr = 0x32; //返回数据长度有误
        private const byte CommunicationBusy = 0x33;
        public Form1()
        {
            InitializeComponent();
        }
        private string GetReturnCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case OK:
                    return "success";
                case Mode:
                    return "mode error";
                case CommunicationErr:
                    return "communication error";
                case CommandError:
                    return "command error";
                case ErrorMessage:
                    return "error message";
                case RetCRCErr:
                    return "CRC error";
                case RetDataErr:
                    return "response length error";
                case CommunicationBusy:
                    return "communication busy";
                default:
                    return "";
            }
        }
        private void RefreshFreeRate(byte IRStatus)
        {
            if ((IRStatus & 0x01) == 0)
                StatusBar1.Panels[2].Text = "IR1：Sync";
            else
                StatusBar1.Panels[2].Text = "IR1：Blocked";

            if ((IRStatus & 0x02) == 0)
                StatusBar1.Panels[3].Text = "IR2：Sync";
            else
                StatusBar1.Panels[3].Text = "IR2：Blocked";

            if ((IRStatus & 0x04) == 0)
                StatusBar1.Panels[4].Text = "IR3：Sync";
            else
                StatusBar1.Panels[4].Text = "IR3：Blocked";

            if ((IRStatus & 0x08) == 0)
                StatusBar1.Panels[5].Text = "IR4：Sync";
            else
                StatusBar1.Panels[5].Text = "IR4：Blocked";
        }
        private void AddCmdLog(string CMD, string cmdStr, int cmdRet)
        {
            try
            {
                StatusBar1.Panels[0].Text = "";
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " +
                                            cmdStr + ": " +
                                            GetReturnCodeDesc(cmdRet);
            }
            finally
            {
                ;
            }
        }
        private void EnableForm()
        {
            panel1.Enabled = true;
            grp_Info.Enabled = true;
            grp_mode.Enabled = true;
            btClearControl.Enabled = true;
            groupBox9.Enabled = true;
        }
        private void DisableForm()
        {
            panel1.Enabled = false;
            panel2.Enabled = false;
            panel3.Enabled = false;
            grp_Info.Enabled = false;
            grp_mode.Enabled = false;
            btClearControl.Enabled = false;
            groupBox9.Enabled = false;
        }
        private void btConnet_Click(object sender, EventArgs e)
        {
            int Port = ComboBox_Port.SelectedIndex + 1;
            fCmdRet = Device.OpenComPort(Port, ref ControllerAdr,ref PortHandle);
            if (fCmdRet==0)
            {
                bt_GetDeviceInfo_Click(null,null);
                EnableForm();
                btConnet.Enabled = false;
                btDisconnet.Enabled = true;
                fModel = 0;
                fCmdRet = Device.ModeSwitch(ref ControllerAdr,ref fModel,ref IRStatus,PortHandle);
                if (fCmdRet==0)
                {
                    if (fModel == 0)
                    {
                        panel2.Enabled = true;
                        panel3.Enabled = false;
                        com_mode.SelectedIndex = 0;
                    }
                    else
                    {
                        panel2.Enabled = false;
                        panel3.Enabled = true;
                        com_mode.SelectedIndex = 1;
                    }
                    RefreshFreeRate(IRStatus);
                }
            }
            else
            {
                MessageBox.Show("Communication error");
            }
            AddCmdLog("OpenComPort", "Open COM", fCmdRet);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ComboBox_Port.SelectedIndex = 0;
            for(int i=0;i<256;i++)
            {
                com_IRTime.Items.Add(i.ToString()+"*1s");
                com_TagTime.Items.Add(i.ToString() + "*1s");
                com_delay.Items.Add(i.ToString() + "*100ms");
                com_delay2.Items.Add(i.ToString() + "*100ms");
                com_bzon.Items.Add(i.ToString() + "*100ms");
                com_bzoff.Items.Add(i.ToString() + "*100ms");
                com_bztime.Items.Add(i.ToString());
                com_ledon.Items.Add(i.ToString() + "*100ms");
                com_ledoff.Items.Add(i.ToString() + "*100ms");
                com_ledtime.Items.Add(i.ToString());
            }
            com_IRTime.SelectedIndex = 0;
            com_TagTime.SelectedIndex = 0;
            com_delay.SelectedIndex = 0;
            com_mode.SelectedIndex = 0;
            GetClock.Checked = true;
            com_MQ.SelectedIndex = 4;
            com_MS.SelectedIndex = 0;
            radioButton_band3.Checked = true;
            ComboBox_PowerDbm.SelectedIndex = 30;
            com_Beep.SelectedIndex = 0;
            com_delay2.SelectedIndex = 30;
            com_bzon.SelectedIndex = 1;
            com_bzoff.SelectedIndex = 0;
            com_bztime.SelectedIndex = 1;
            com_ledon.SelectedIndex = 1;
            com_ledoff.SelectedIndex = 0;
            com_ledtime.SelectedIndex = 1;
            DisableForm();
        }

        private void btDisconnet_Click(object sender, EventArgs e)
        {
            fCmdRet = Device.CloseSpecComPort(PortHandle);
            if (fCmdRet==0)
            {
                DisableForm();
                btConnet.Enabled = true;
                btDisconnet.Enabled = false;
            }
            AddCmdLog("CloseSpecComPort", "Close COM", fCmdRet);
        }

        private void bt_GetDeviceInfo_Click(object sender, EventArgs e)
        {
            byte Productcode = 0;
            byte MainVer = 0;
            byte SubVer = 0;
            textBox1.Text = "";
            textBox2.Text = "";
            tb_version.Text = "";
            fCmdRet = Device.GetControllerInfo(ref ControllerAdr, ref Productcode, ref MainVer, ref SubVer, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
            {
                textBox1.Text = Convert.ToString(Productcode, 16).PadLeft(2, '0').ToUpper();
                if (textBox1.Text=="90")
                {
                    tb_version.Text = "RRU-CH-WL";
                }
                if (textBox1.Text == "91")
                {
                    tb_version.Text = "RRU-CH-C16058";
                }
                textBox2.Text =  Convert.ToString(MainVer, 16).PadLeft(2, '0').ToUpper() + "." + Convert.ToString(SubVer, 16).PadLeft(2, '0').ToUpper();
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("I", "Get Information", fCmdRet);
        }

        private void bt_ModeSet_Click(object sender, EventArgs e)
        {
            byte mode = 0;
            if(com_mode.SelectedIndex==0)
            {
                mode = 0x80;
            }
            else
            {
                mode = 0x81;
            }
            fCmdRet = Device.ModeSwitch(ref ControllerAdr, ref mode, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
            {
                if (mode == 0)
                {
                    panel2.Enabled = true;
                    panel3.Enabled = false;
                }
                else
                {
                    panel2.Enabled = false;
                    panel3.Enabled = true;
                }
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("M", "Set", fCmdRet);
        }

        private void bt_ModeGet_Click(object sender, EventArgs e)
        {
            byte mode = 0;
            fCmdRet = Device.ModeSwitch(ref ControllerAdr, ref mode, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
            {
                if (mode == 0)
                {
                    panel2.Enabled = true;
                    panel3.Enabled = false;
                    com_mode.SelectedIndex = 0;
                }
                else
                {
                    panel2.Enabled = false;
                    panel3.Enabled = true;
                    com_mode.SelectedIndex = 1;
                }
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("M", "Get", fCmdRet);
        }

        private void btSetConfig_Click(object sender, EventArgs e)
        {
            byte IREnable=0;
            byte IRTime=0;
            byte TagExistTime=0;
            byte AlarmEn=0;
            byte DelayTime=0;
            byte Pepolemsg=0;
            byte AEn = 1;
            if (check_IR.Checked)
                IREnable = 1;
            else
                IREnable = 0;
            IRTime=(byte)(com_IRTime.SelectedIndex);
            TagExistTime = (byte)com_TagTime.SelectedIndex;
            if (check_Alarm.Checked)
                 AlarmEn = 1;
            else
                 AlarmEn = 0;
             DelayTime = (byte)com_delay.SelectedIndex;
             if (check_Pepole.Checked)
                 Pepolemsg = 1;
             else
                 Pepolemsg = 0;
             fCmdRet = Device.ConfigureController(ref ControllerAdr, IREnable,IRTime,TagExistTime,AlarmEn,DelayTime,Pepolemsg,AEn, ref IRStatus, PortHandle);
             if (fCmdRet==0)
                 RefreshFreeRate(IRStatus);
             AddCmdLog("F", "Set", fCmdRet);

        }

        private void btGetConfig_Click(object sender, EventArgs e)
        {
            byte IREnable = 0;
            byte IRTime = 0;
            byte TagExistTime = 0;
            byte AlarmEn = 0;
            byte DelayTime = 0;
            byte Pepolemsg = 0;
            byte AEn = 1;
            fCmdRet = Device.GetControllerConfig(ref ControllerAdr, ref IREnable, ref IRTime, ref TagExistTime, ref AlarmEn, ref DelayTime, ref Pepolemsg, ref AEn, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
            {
                if (IREnable == 0)
                    check_IR.Checked = false;
                else
                    check_IR.Checked = true;
                com_IRTime.SelectedIndex = IRTime;
                com_TagTime.SelectedIndex = TagExistTime;
                if (AlarmEn == 0)
                    check_Alarm.Checked = false;
                else
                    check_Alarm.Checked = true;
                com_delay.SelectedIndex = DelayTime;
                if (Pepolemsg == 0)
                    check_Pepole.Checked = false;
                else
                    check_Pepole.Checked = true;
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("F", "Get", fCmdRet);
        }

        private void SetControAddr_Click(object sender, EventArgs e)
        {
            byte NewAddr = 0;
            byte Mode = 1;
            if (TextNewConAddr.Text != "")
                NewAddr = Convert.ToByte(TextNewConAddr.Text, 16);
            else
            {
                MessageBox.Show("Input new address", "infor");
                return;
            }
            fCmdRet = Device.SetControllerAddr(ref ControllerAdr, Mode, NewAddr, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
                ControllerAdr = NewAddr;
            AddCmdLog("K", "Set", fCmdRet);
            StatusBar1.Panels[1].Text = " ";
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
        }

        private void ControAddr_Click(object sender, EventArgs e)
        {
            byte NewAddr = 0;
            byte Mode = 0;
            fCmdRet = Device.SetControllerAddr(ref ControllerAdr, Mode, NewAddr, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
                TextBox14.Text = Convert.ToString(ControllerAdr, 16).PadLeft(2, '0');
            AddCmdLog("K", "Set", fCmdRet);
            StatusBar1.Panels[1].Text = " ";
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
        }

        private void bt_GetConnectInfo_Click(object sender, EventArgs e)
        {
            byte ConnectionStatus=0;
            text_ReaderStatue.Text = "";
            fCmdRet = Device.GetControllerReaderConnectionStatus(ref ControllerAdr, ref ConnectionStatus, ref IRStatus, PortHandle);
            if (fCmdRet==0)
            {
                if (ConnectionStatus ==1)
                {
                    text_ReaderStatue.Text = "Connected";
                }
                else
                {
                    text_ReaderStatue.Text = "DisConnect";
                }
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("Z", "Fet", fCmdRet);
        }

        private void ClockCMD_Click(object sender, EventArgs e)
        {
            byte[] setTime = new byte[6];
            byte[] outTime = new byte[6];
            if (SetClock.Checked)
            {
                if (Text_year.Text == "" || Text_month.Text == "" || Text_day.Text == "" || Text_hour.Text == "" || Text_min.Text == "" || Text_sec.Text == "")
                {
                    MessageBox.Show("Input right data", "infor");
                    return;
                }
                setTime[0] = Convert.ToByte(Text_year.Text);
                setTime[1] = Convert.ToByte(Text_month.Text);
                setTime[2] = Convert.ToByte(Text_day.Text);
                setTime[3] = Convert.ToByte(Text_hour.Text);
                setTime[4] = Convert.ToByte(Text_min.Text);
                setTime[5] = Convert.ToByte(Text_sec.Text);
                if (Convert.ToByte(Text_year.Text) < 0 || Convert.ToByte(Text_year.Text) > 99)
                {
                    MessageBox.Show("Input 00-99 data", "infor");
                    return;
                }
                fCmdRet = Device.SetClock(ref ControllerAdr, setTime, ref IRStatus, PortHandle);
                AddCmdLog("@", "Set", fCmdRet);
            }

            if (GetClock.Checked)
            {
                fCmdRet = Device.GetClock(ref ControllerAdr, setTime, ref IRStatus, PortHandle);
                if (fCmdRet == 0)
                {
                    Text_year.Text = Convert.ToString(setTime[0]).PadLeft(2, '0');
                    Text_month.Text = Convert.ToString(setTime[1]).PadLeft(2, '0');
                    Text_day.Text = Convert.ToString(setTime[2]).PadLeft(2, '0');
                    Text_hour.Text = Convert.ToString(setTime[3]).PadLeft(2, '0');
                    Text_min.Text = Convert.ToString(setTime[4]).PadLeft(2, '0');
                    Text_sec.Text = Convert.ToString(setTime[5]).PadLeft(2, '0');
                    AddCmdLog("@", "Query", fCmdRet);
                }
            }

            StatusBar1.Panels[1].Text = " ";
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
        }

        private void btSetIR_Click(object sender, EventArgs e)
        {
            byte model = 0;
            if (rb_r.Checked)
                model = 0x80;
            if (rb_L.Checked)
                model = 0x81;
            fCmdRet = Device.IRDirectionSetting(ref ControllerAdr, ref model, ref IRStatus, PortHandle);
            AddCmdLog("N", "Set", fCmdRet);
            StatusBar1.Panels[1].Text = " ";
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
        }

        private void btGetIR_Click(object sender, EventArgs e)
        {
            byte model = 0;
            fCmdRet = Device.IRDirectionSetting(ref ControllerAdr, ref model, ref IRStatus, PortHandle);
            if (fCmdRet != 0)
            {
                AddCmdLog("B", "Get", fCmdRet);
                return;
            }
            if (model == 0)
            {
                rb_r.Checked = true;
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " + "IR positive";
            }
            if (model == 1)
            {
                rb_L.Checked = true;
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " + "IR reverse";
            }
            RefreshFreeRate(IRStatus);
        }
        #region  16进制字符串到数组之间的相互转换
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }
        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();
        }
        #endregion
        private void btSetParameter_Click(object sender, EventArgs e)
        {
            byte Qvalue=0;
            byte Session=0;
            byte AdrTID=0;
            byte LenTID=0;
            byte MaskMem=0;
            byte[] MaskAdr=new byte[2];
            byte MaskLen=0;
            byte[] MaskData = new byte[100];
            Qvalue = (byte)com_MQ.SelectedIndex;
            Session = (byte)com_MS.SelectedIndex;
            if (Session == 4) Session = 255;
            if((txt_mtidaddr.Text.Length !=2)||(txt_Mtidlen.Text.Length !=2))
                return;
            AdrTID = Convert.ToByte(txt_mtidaddr.Text,16);
            LenTID = Convert.ToByte(txt_Mtidlen.Text,16);
            if (RBM_EPC.Checked) MaskMem = 1;
            if (RBM_TID.Checked) MaskMem = 2;
            if (RBM_USER.Checked) MaskMem = 3;
            MaskAdr = HexStringToByteArray(txt_Maddr.Text);
            MaskLen = Convert.ToByte(txt_Mlen.Text, 16);
            if (txt_Mdata.Text.Length % 2 == 0)
                MaskData = HexStringToByteArray(txt_Mdata.Text);
            fCmdRet = Device.SetReadParameter(ref ControllerAdr, Qvalue, Session, AdrTID, LenTID,MaskMem,MaskAdr,MaskLen,MaskData, ref IRStatus, PortHandle);
            if (fCmdRet==0)
            {
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("q", "Set", fCmdRet);
        }

        private void btGetParameter_Click(object sender, EventArgs e)
        {
            byte Qvalue=0;
            byte Session=0;
            byte AdrTID=0;
            byte LenTID=0;
            byte MaskMem=0;
            byte[] MaskAdr=new byte[2];
            byte MaskLen=0;
            byte[] MaskData = new byte[100];
            fCmdRet = Device.GetReadParameter(ref ControllerAdr, ref  Qvalue, ref Session, ref AdrTID, ref LenTID, ref  MaskMem, MaskAdr, ref MaskLen, MaskData, ref IRStatus, PortHandle);
            if (fCmdRet==0)
            {
                com_MQ.SelectedIndex = Qvalue;
                if (Session == 255)
                    com_MS.SelectedIndex = 4;
                else
                    com_MS.SelectedIndex = Session;
                txt_mtidaddr.Text = Convert.ToString(AdrTID,16).PadLeft(2,'0');
                txt_Mtidlen.Text = Convert.ToString(LenTID, 16).PadLeft(2, '0');
                if (MaskMem == 1) RBM_EPC.Checked = true;
                else if (MaskMem == 2) RBM_TID.Checked = true;
                else if (MaskMem == 3) RBM_USER.Checked = true;
                txt_Mlen.Text = Convert.ToString(MaskLen, 16).PadLeft(2, '0');
                int len = 0;
                if (MaskLen % 8 == 0)
                {
                    len = MaskLen / 8;
                }
                else
                {
                    len = MaskLen / 8+1;
                }
                string temp = "";
                for (int m = 0; m < len; m++)
                {
                    temp = temp+Convert.ToString(MaskData[m], 16).PadLeft(2, '0');
                }
                txt_Mdata.Text = temp;
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("q", "Get", fCmdRet);
        }

        private void bt_GetNum_Click(object sender, EventArgs e)
        {
            byte[] positive=new byte[3];
            byte[] reverse=new byte[3];
            byte[] AlarmNum=new byte[4];
            txt_rnum.Text = "";
            txt_lnum.Text = "";
            txt_Anum.Text = "";
            fCmdRet = Device.StatisticalMsg(ref ControllerAdr, positive, reverse, AlarmNum, ref IRStatus, PortHandle);
            if (fCmdRet==0)
            {
                int num = 0;
                num = positive[2] * 256 * 256 + positive[1] * 256 + positive[0];
                txt_rnum.Text = num.ToString();

                num = reverse[2] * 256 * 256 + reverse[1] * 256 + reverse[0];
                txt_lnum.Text = num.ToString();

                num = AlarmNum[3] * 256 * 256 * 256 + AlarmNum[2] * 256 * 256 + AlarmNum[1] * 256 + AlarmNum[0];
                txt_Anum.Text = num.ToString();
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("q", "Get", fCmdRet);
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button22_Click(object sender, EventArgs e)
        {
            byte[] EASmode = new byte[2];
            if (radioButton_EAS.Checked)
            {
                EASmode[0] = 0;
            }
            else
            {
                EASmode[0] = 1;
                if (check_epc.Checked)
                {
                    EASmode[0] = Convert.ToByte(EASmode[0] | 0x10);
                }
                if (rb_sh.Checked)
                {
                    EASmode[1] = 0;
                }
                if (rb_qg.Checked)
                {
                    EASmode[1] = 1;
                }
                if (rb_kg.Checked)
                {
                    EASmode[1] = 2;
                }
            }
            fCmdRet = Device.SetEASWorkStyle(ref ControllerAdr, EASmode, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
            AddCmdLog("s", "Set", fCmdRet);
        }

        private void bt_GetEAS_Click(object sender, EventArgs e)
        {
            byte[] EASmode = new byte[2];
            fCmdRet = Device.GetEASWorkStyle(ref ControllerAdr, EASmode, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
            {
                if ((EASmode[0] & 0x01) == 0)
                {
                    radioButton_EAS.Checked = true;
                }
                else
                {
                    radioButton_AFI.Checked = true;
                    if ((EASmode[0] & 0x10) == 0)
                    {
                        check_epc.Checked = false;
                    }
                    else
                    {
                        check_epc.Checked = true;
                    }
                    if (EASmode[1] == 0)
                    {
                        rb_sh.Checked = true;
                    }
                    else if (EASmode[1] == 1)
                    {
                        rb_qg.Checked = true;
                    }
                    else if (EASmode[1] == 2)
                    {
                        rb_kg.Checked = true;
                    }
                }

                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("s", "Get", fCmdRet);
        }

        private void btEASStart_Click(object sender, EventArgs e)
        {
            btEASStart.Enabled = false;
            btEASStop.Enabled = true;
            timer_EAS.Enabled = true;
        }

        private void btEASStop_Click(object sender, EventArgs e)
        {
            btEASStart.Enabled = true;
            btEASStop.Enabled = false;
            timer_EAS.Enabled = false;
        }
        private void GetTheEasMessage()
        {
            string InOrOut;
            byte InFlag, MsgLength, MsgType;
            byte[] Msg = new byte[46];
            string year, month, Dates, Hour, minutes, second;
            string Time;
            IsGetting = true;     //进入该过程时将正在执行标志置1.
            MsgLength = 0;
            MsgType = 0;
            InOrOut = "";
            fCmdRet = Device.GetEASMessage(ref ControllerAdr, Msg, ref MsgLength, ref MsgType, ref IRStatus, PortHandle);
            AddCmdLog("Get", "Get EAS mode message", fCmdRet);
            StatusBar1.Panels[1].Text = " ";
            RefreshFreeRate(IRStatus);
            if ((fCmdRet == 0) && (MsgType == 0))
            {
                switch (Msg[0])
                {
                    case 0:
                        InOrOut = "No EAS Alarm";
                        IsGetting = false;   
                        return;
                    case 1:
                        InOrOut = "EAS Alarm";
                        break;
                }

                year = Convert.ToString(Msg[1]).PadLeft(2, '0');
                month = Convert.ToString(Msg[2]).PadLeft(2, '0');
                Dates = Convert.ToString(Msg[3]).PadLeft(2, '0');
                Hour = Convert.ToString(Msg[4]).PadLeft(2, '0');
                minutes = Convert.ToString(Msg[5]).PadLeft(2, '0');
                second = Convert.ToString(Msg[6]).PadLeft(2, '0');
                Time = "20" + year + "-" + month + "-" + Dates + " " + Hour + ":" + minutes + ":" + second;
                ListBox3.Items.Add(Time + "   "+InOrOut);
                if (ListBox3.Items.Count > 0)
                    ListBox3.TopIndex = ListBox3.Items.Count - 1; //垂直滚动条紧挨最底部

            }
            else if ((fCmdRet == 0) && (MsgType == 1))
            {
                InFlag = Convert.ToByte(Msg[0]);
                switch (InFlag)
                {
                    case 0:
                        InOrOut = "Forward";
                        break;
                    case 1:
                        InOrOut = "Reverse";
                        break;
                }
                int num = 0;
                num = Msg[3] * 256 * 256 + Msg[2] * 256 + Msg[1];
                string Rnum = num.ToString();

                num = Msg[6] * 256 * 256 + Msg[5] * 256 + Msg[4];
                string Lnum = num.ToString();

                num = Msg[10] * 256 * 256 * 256 + Msg[9] * 256 * 256 + Msg[8] * 256 + Msg[7];
                string Anum = num.ToString();

                year = Convert.ToString(Msg[11]).PadLeft(2, '0');
                month = Convert.ToString(Msg[12]).PadLeft(2, '0');
                Dates = Convert.ToString(Msg[13]).PadLeft(2, '0');
                Hour = Convert.ToString(Msg[14]).PadLeft(2, '0');
                minutes = Convert.ToString(Msg[15]).PadLeft(2, '0');
                second = Convert.ToString(Msg[16]).PadLeft(2, '0');
                Time = "20" + year + "-" + month + "-" + Dates + " " + Hour + ":" + minutes + ":" + second;
                ListBox4.Items.Add(Time + "   " + InOrOut + "   Statistical:" + Rnum + ":Forward-" + Lnum + ":Reverse-" + Anum + ":Alarm");
                if (ListBox4.Items.Count > 0) 
                    ListBox4.TopIndex = ListBox4.Items.Count - 1;
            }
            else if ((fCmdRet == 0) && (MsgType == 2))
            {
                int num = 0;
                year = Convert.ToString(Msg[1]).PadLeft(2, '0');
                month = Convert.ToString(Msg[2]).PadLeft(2, '0');
                Dates = Convert.ToString(Msg[3]).PadLeft(2, '0');
                Hour = Convert.ToString(Msg[4]).PadLeft(2, '0');
                minutes = Convert.ToString(Msg[5]).PadLeft(2, '0');
                second = Convert.ToString(Msg[6]).PadLeft(2, '0');
                Time = "20" + year + "-" + month + "-" + Dates + " " + Hour + ":" + minutes + ":" + second;

                int len = MsgLength - 7;
                byte[] daw = new byte[len];
                Array.Copy(Msg, 7, daw, 0, len);
                ListBox3.Items.Add(Time + "   EAS Alarm:" + ByteArrayToHexString(daw));
                if (ListBox3.Items.Count > 0)
                    ListBox3.TopIndex = ListBox3.Items.Count - 1;
            }
            IsGetting = false;              //过程执行结束,正在执行标志清零
            Device.Acknowledge(ref ControllerAdr, PortHandle);
           
        }
        private void timer_EAS_Tick(object sender, EventArgs e)
        {
            if (IsGetting) //'是否正在执行() 'C'命令
                return;
            if (IsCloseApp)
                Close();
            GetTheEasMessage();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab!= tabPage3)
            {
                btEASStart.Enabled = true;
                btEASStop.Enabled = false;
                timer_EAS.Enabled = false;
            }
            if (tabControl1.SelectedTab != tabPage2)
            {
                btStart.Enabled = true;
                btStop.Enabled = false;
                timer1.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ListView1_EPC.Items.Clear();
            listBox1.Items.Clear();
            txt_Num.Text = "0";
        }

        private void Button19_Click(object sender, EventArgs e)
        {
            ListBox3.Items.Clear();
            ListBox4.Items.Clear();
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            btStart.Enabled = false;
            btStop.Enabled = true;
            timer1.Enabled = true;
            txt_Num.Text = "0";
            IsGetting = false;
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            btStart.Enabled = true;
            btStop.Enabled = false;
            timer1.Enabled = false;
        }
        public void ChangeSubItem(ListViewItem ListItem, int subItemIndex, string ItemText)
        {
            if (subItemIndex == 1)
            {
                if (ItemText == "")
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    if (ListItem.SubItems[subItemIndex + 2].Text == "")
                    {
                        ListItem.SubItems[subItemIndex + 2].Text = "1";
                    }
                    else
                    {
                        ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 3].Text) + 1);
                    }
                }
                else
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    ListItem.SubItems[subItemIndex + 2].Text = "1";
                }
                else
                {
                    ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 2].Text) + 1);
                    if ((Convert.ToUInt32(ListItem.SubItems[subItemIndex + 2].Text) > 9999))
                        ListItem.SubItems[subItemIndex + 2].Text = "1";
                }

            }
            if (subItemIndex == 2)
            {
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                }
            }
        }
        private void GetNMessage()
        {
            string InOrOut;
            byte InFlag, MsgLength, MsgType;
            byte[] Msg = new byte[300];
            string year, month, Dates, Hour, minutes, second;
            string Time;
            IsGetting = true;     //进入该过程时将正在执行标志置1.
            MsgLength = 0;
            MsgType = 0;
            InOrOut = "";
            fCmdRet = Device.GetChannelMessage(ref ControllerAdr, Msg, ref MsgLength, ref MsgType, ref IRStatus, PortHandle);
            RefreshFreeRate(IRStatus);
            AddCmdLog("Get", "Inventory", fCmdRet);
            if ((fCmdRet == 0) && (MsgType == 0))
            {
                ListViewItem aListItem = new ListViewItem();
                int CardNum = Msg[6];
                if (CardNum == 0) return;
                byte[] daw = new byte[MsgLength-7];//除去前面6个字节的时间和1个字节的长度
                Array.Copy(Msg, 7, daw,0, MsgLength-7);
                string temps = ByteArrayToHexString(daw);
                int m = 0;
                for (int CardIndex = 0; CardIndex < CardNum; CardIndex++)
                {
                    int EPClen = daw[m];
                    string sEPC = temps.Substring(m * 2 + 2, EPClen * 2);
                    m = m + EPClen + 1;
                    if (sEPC.Length != EPClen * 2)
                        return;
                    bool isonlistview = false;
                    for (int i = 0; i < ListView1_EPC.Items.Count; i++)     //判断是否在Listview列表内
                    {
                        if (sEPC == ListView1_EPC.Items[i].SubItems[1].Text)
                        {
                            aListItem = ListView1_EPC.Items[i];
                            ChangeSubItem(aListItem, 1, sEPC);
                            isonlistview = true;
                            break;
                        }
                    }
                    if (!isonlistview)
                    {
                        aListItem = ListView1_EPC.Items.Add((ListView1_EPC.Items.Count + 1).ToString());
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        string s = sEPC;
                        ChangeSubItem(aListItem, 1, s);
                        s = (sEPC.Length / 2).ToString();
                        ChangeSubItem(aListItem, 2, s);
                        ListView1_EPC.EnsureVisible(ListView1_EPC.Items.Count - 1);
                    }
                }
                txt_Num.Text = ListView1_EPC.Items.Count.ToString();
            }
            else if((fCmdRet == 0) && (MsgType == 1))
            {
                InFlag = Convert.ToByte(Msg[0]);
                switch (InFlag)
                {
                    case 0:
                        InOrOut = "Forward";
                        break;
                    case 1:
                        InOrOut = "Reverse";
                        break;
                }
                int num = 0;
                num = Msg[3] * 256 * 256 + Msg[2] * 256 + Msg[1];
                string Rnum = num.ToString();

                num = Msg[6] * 256 * 256 + Msg[5] * 256 + Msg[4];
                string Lnum = num.ToString();

                num = Msg[10] * 256 * 256 * 256 + Msg[9] * 256 * 256 + Msg[8] * 256 + Msg[7];
                string Anum = num.ToString();

                year = Convert.ToString(Msg[11]).PadLeft(2, '0');
                month = Convert.ToString(Msg[12]).PadLeft(2, '0');
                Dates = Convert.ToString(Msg[13]).PadLeft(2, '0');
                Hour = Convert.ToString(Msg[14]).PadLeft(2, '0');
                minutes = Convert.ToString(Msg[15]).PadLeft(2, '0');
                second = Convert.ToString(Msg[16]).PadLeft(2, '0');
                Time = "20" + year + "-" + month + "-" + Dates + " " + Hour + ":" + minutes + ":" + second;
                listBox1.Items.Add(Time + "   " + InOrOut + "   Statistical:" + Rnum + ":Forward-" + Lnum + ":Reverse-" + Anum + ":Alarm");
                if (listBox1.Items.Count > 0)
                    listBox1.TopIndex = listBox1.Items.Count - 1;
            }
            Device.Acknowledge(ref ControllerAdr, PortHandle);
           
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if(IsGetting) return;
            IsGetting = true;
            GetNMessage();
            IsGetting = false;

        }

        private void btClearControl_Click(object sender, EventArgs e)
        {
            fCmdRet = Device.ClearControllerBuffer(ref ControllerAdr, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
            AddCmdLog("Get", "Clear controller buffer", fCmdRet);
        }

        private void radioButton_band2_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 20; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 19;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band3_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 50; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 49;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band4_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 32; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 31;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band5_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dminfre.Items.Clear();
            ComboBox_dmaxfre.Items.Clear();
            for (i = 0; i < 15; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 14;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void btSetWork_Click(object sender, EventArgs e)
        {
            byte Power=0;
            byte MaxFre=0;
            byte MinFre=0;
            byte BeepEn = 0;
            Power = (byte)(ComboBox_PowerDbm.SelectedIndex);
            BeepEn = (byte)(com_Beep.SelectedIndex);
            byte band = 2;
            if (radioButton_band2.Checked)
                band = 1;
            if (radioButton_band3.Checked)
                band = 2;
            if (radioButton_band4.Checked)
                band = 3;
            if (radioButton_band5.Checked)
                band = 4;
            MinFre = Convert.ToByte(((band & 3) << 6) | (ComboBox_dminfre.SelectedIndex & 0x3F));
            MaxFre = Convert.ToByte(((band & 0x0c) << 4) | (ComboBox_dmaxfre.SelectedIndex & 0x3F));
            fCmdRet = Device.SetWorkParameter(ref ControllerAdr, Power, MaxFre, MinFre, BeepEn, ref IRStatus, PortHandle);
            if (fCmdRet==0)
                RefreshFreeRate(IRStatus);
            AddCmdLog("Get", "Set", fCmdRet);
        }

        private void btGetWork_Click(object sender, EventArgs e)
        {
            byte Power = 0;
            byte MaxFre = 0;
            byte MinFre = 0;
            byte BeepEn = 0;
            byte band = 0;
            fCmdRet = Device.GetWorkParameter(ref ControllerAdr, ref Power, ref MaxFre, ref MinFre, ref BeepEn, ref IRStatus, PortHandle);
            if (fCmdRet==0)
            {
                ComboBox_PowerDbm.SelectedIndex = Power;
                com_Beep.SelectedIndex=BeepEn;
                band = Convert.ToByte(((MaxFre & 0xc0) >> 4) | (MinFre >> 6));
                switch (band)
                {
                    case 1:
                        {
                            radioButton_band2.Checked = true;
                        }
                        break;
                    case 2:
                        {
                            radioButton_band3.Checked = true;
                        }
                        break;
                    case 3:
                        {
                            radioButton_band4.Checked = true;
                        }
                        break;
                    case 4:
                        {
                            radioButton_band5.Checked = true;
                        }
                        break;
                }
                ComboBox_dminfre.SelectedIndex = MinFre & 0x3F;
                ComboBox_dmaxfre.SelectedIndex = MaxFre & 0x3F;
                RefreshFreeRate(IRStatus);
            }
            AddCmdLog("Get", "Get", fCmdRet);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void bt_delay_Click(object sender, EventArgs e)
        {
            byte dalay = 0;
            dalay = (byte)com_delay2.SelectedIndex;
            fCmdRet = Device.SetRelay(ref ControllerAdr, dalay, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
            AddCmdLog("Get", "set", fCmdRet);
        }

        private void bt_bzLed_Click(object sender, EventArgs e)
        {
             byte BuzzerOnTime=(byte)com_bzon.SelectedIndex;
             byte BuzzerOffTime=(byte)com_bzoff.SelectedIndex;
             byte BuzzerActTimes=(byte)com_bztime.SelectedIndex;
             byte LEDOnTime=(byte)com_ledon.SelectedIndex;
             byte LEDOffTime=(byte)com_ledoff.SelectedIndex;
             byte LEDFlashTimes = (byte)com_ledtime.SelectedIndex;
             fCmdRet = Device.BuzzerAndLEDControl(ref ControllerAdr, BuzzerOnTime,BuzzerOffTime,BuzzerActTimes,LEDOnTime,LEDOffTime,LEDFlashTimes, ref IRStatus, PortHandle);
             if (fCmdRet == 0)
                 RefreshFreeRate(IRStatus);
             AddCmdLog("Get", "GO", fCmdRet);
        }

        private void bt_ClearMsg_Click(object sender, EventArgs e)
        {
            fCmdRet = Device.ClearStatisticalMsg(ref ControllerAdr, ref IRStatus, PortHandle);
            if (fCmdRet == 0)
                RefreshFreeRate(IRStatus);
            AddCmdLog("Get", "Clear", fCmdRet);
        }

        private void bt_bsearch_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            string str = "";
            Information.IP = "";
            Information.usename = "";
            Information.dsname = "";
            Information.mac = "";
            Information.portnum = "";
            Information.tup = "";
            Information.rm = "";
            Information.cm = "";
            Information.ct = "";
            Information.fc = "";
            Information.dt = "";
            Information.br = "";
            Information.pr = "";
            Information.bb = "";
            Information.rc = "";
            Information.ml = "";
            Information.md = "";
            Information.di = "";
            Information.dp = "";
            Information.gi = "";
            Information.nm = "";
            byte[] data = new byte[1024];
            ListViewItem aListItem = new ListViewItem();
            try
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//初始化一个Scoket实习,采用UDP传输
                IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, 65535);//初始化一个发送广播和指定端口的网络端口实例
                EndPoint ep = (EndPoint)iep;
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);//设置该scoket实例的发送形式
                string request = "X";//初始化需要发送的数据
                byte[] buffer = Encoding.ASCII.GetBytes(request);
                sock.SendTo(buffer, iep);
                for (int i = 0; i < 255; i++)
                {
                    byte[] buffer1 = new byte[1000];
                    sock.ReceiveTimeout = 20;
                    int m_count = sock.ReceiveFrom(buffer1, ref ep);
                    if (m_count > 0)
                    {
                        aListItem = listView1.Items.Add((listView1.Items.Count + 1).ToString());
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        byte[] buffer2 = new byte[m_count];
                        Array.Copy(buffer1, buffer2, m_count);
                        string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                        if (fRecvIDPstring.Substring(0, 1) == "A")
                        {
                            int m = fRecvIDPstring.IndexOf('/');
                            Information.mac = fRecvIDPstring.Substring(1, m - 1);
                            Information.portnum = fRecvIDPstring.Substring(m + 1, 4);
                            m = fRecvIDPstring.IndexOf('*');
                            int n = fRecvIDPstring.Length - m - 8;
                            string IDPstring = fRecvIDPstring.Substring(m + 8, n);
                            if (IDPstring!="")
                            {
                                Information.usename = IDPstring.Substring(0, IDPstring.IndexOf('/'));
                                Information.dsname = IDPstring.Substring(IDPstring.IndexOf('/') + 1, IDPstring.Length - IDPstring.IndexOf('/') - 1);
                            }
                            else
                            {
                                Information.usename = "";
                                Information.dsname = "";
                            }
                            Information.IP = ep.ToString().Substring(0, ep.ToString().IndexOf(':'));
                            if (((Information.usename == "") && (Information.dsname == "")) || (Information.dsname == "/"))
                            {
                                str = "";
                            }
                            else
                            {
                                str = Information.usename + '/' + Information.dsname;
                            }
                            aListItem.SubItems[1].Text = Information.mac;
                            aListItem.SubItems[2].Text = Information.IP;
                            aListItem.SubItems[3].Text = str;
                        }
                    }
                }
                sock.Close();
            }
            catch (System.Exception ex)
            {
                ex.ToString();
                return;
            }
        }

        private void bt_ssearch_Click(object sender, EventArgs e)
        {
            Information.IP = "";
            Information.usename = "";
            Information.dsname = "";
            Information.mac = "";
            Information.portnum = "";
            Information.tup = "";
            Information.rm = "";
            Information.cm = "";
            Information.ct = "";
            Information.fc = "";
            Information.dt = "";
            Information.br = "";
            Information.pr = "";
            Information.bb = "";
            Information.rc = "";
            Information.ml = "";
            Information.md = "";
            Information.di = "";
            Information.dp = "";
            Information.gi = "";
            Information.nm = "";
            locateForm loginform = new locateForm();
            DialogResult result = loginform.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    string IPAddr = loginform.IP1 + "." + loginform.IP2 + "." + loginform.IP3 + "." + loginform.IP4;
                    listView1.Items.Clear();
                    string str = "";
                    byte[] data = new byte[1024];
                    ListViewItem aListItem = new ListViewItem();
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//初始化一个Scoket实习,采用UDP传输

                    IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, 65535);//初始化一个发送广播和指定端口的网络端口实例
                    EndPoint ep = (EndPoint)iep;
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);//设置该scoket实例的发送形式
                    string request = "X";//初始化需要发送的数据
                    byte[] buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    for (int i = 0; i < 3; i++)
                    {
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        iep = new IPEndPoint(IPAddress.Parse(IPAddr), 65535);
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            aListItem = listView1.Items.Add((listView1.Items.Count + 1).ToString());
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('/');
                                Information.mac = fRecvIDPstring.Substring(1, m - 1);
                                Information.portnum = fRecvIDPstring.Substring(m + 1, 4);
                                m = fRecvIDPstring.IndexOf('*');
                                int n = fRecvIDPstring.Length - m - 8;
                                string IDPstring = fRecvIDPstring.Substring(m + 8, n);
                                if (IDPstring != "")
                                {
                                    Information.usename = IDPstring.Substring(0, IDPstring.IndexOf('/'));
                                    Information.dsname = IDPstring.Substring(IDPstring.IndexOf('/') + 1, IDPstring.Length - IDPstring.IndexOf('/') - 1);
                                }
                                else
                                {
                                    Information.usename = "";
                                    Information.dsname = "";
                                }
                                Information.IP = ep.ToString().Substring(0, ep.ToString().IndexOf(':'));
                                if (((Information.usename == "") && (Information.dsname == "")) || (Information.dsname == "/"))
                                {
                                    str = "";
                                }
                                else
                                {
                                    str = Information.usename + '/' + Information.dsname;
                                }
                                if (IPAddr == Information.IP)
                                {
                                    aListItem.SubItems[1].Text = Information.mac;
                                    aListItem.SubItems[2].Text = Information.IP;
                                    aListItem.SubItems[3].Text = str;
                                }
                            }
                            break;
                        }

                    }
                    sock.Close();
                }
                catch (System.Exception ex)
                {
                    ex.ToString();
                }
            }
            loginform.Dispose();
        }

        private void bt_setting_Click(object sender, EventArgs e)
        {
            int i = 0;
            string IPaddr = "";
            try
            {
                if (listView1.SelectedIndices.Count > 0
                    && listView1.SelectedIndices[0] != -1)
                {
                    IPaddr = listView1.SelectedItems[0].SubItems[2].Text;
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//初始化一个Scoket实习,采用UDP传输

                    IPEndPoint iep = new IPEndPoint(IPAddress.Parse(IPaddr), 65535);//初始化一个发送广播和指定端口的网络端口实例
                    EndPoint ep = (EndPoint)iep;
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);//设置该scoket实例的发送形式
                    string request = "X";//初始化需要发送的数据
                    byte[] buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {

                        sock.SendTo(buffer, iep);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('/');
                                Information.mac = fRecvIDPstring.Substring(1, m - 1);
                                Information.portnum = fRecvIDPstring.Substring(m + 1, 4);
                                m = fRecvIDPstring.IndexOf('*');
                                int n = fRecvIDPstring.Length - m - 8;
                                string IDPstring = fRecvIDPstring.Substring(m + 8, n);
                                if (IDPstring != "")
                                {
                                    Information.usename = IDPstring.Substring(0, IDPstring.IndexOf('/'));
                                    Information.dsname = IDPstring.Substring(IDPstring.IndexOf('/') + 1, IDPstring.Length - IDPstring.IndexOf('/') - 1);
                                }
                                else
                                {
                                    Information.usename = "";
                                    Information.dsname = "";
                                }
                                Information.IP = ep.ToString().Substring(0, ep.ToString().IndexOf(':'));
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "W" + Information.mac;//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(100);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "L";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(50);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GON|1";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "1")
                                {
                                    Information.usename = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GDN|2";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "2")
                                {
                                    Information.dsname = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GFE|3";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "3")
                                {
                                    Information.mac = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GIP|4";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "4")
                                {
                                    Information.IP = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GPN|5";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "5")
                                {
                                    Information.portnum = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GTP|6";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "6")
                                {
                                    Information.tup = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GRM|7";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "7")
                                {
                                    Information.rm = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GCM|8";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "8")
                                {
                                    Information.cm = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GCT|9";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 1) == "9")
                                {
                                    Information.ct = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GFC|10";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "10")
                                {
                                    Information.fc = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GDT|11";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "11")
                                {
                                    Information.dt = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GBR|12";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "12")
                                {
                                    Information.br = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GPR|13";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "13")
                                {
                                    Information.pr = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GBB|14";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "14")
                                {
                                    Information.bb = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GRC|15";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "15")
                                {
                                    Information.rc = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GML|16";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "16")
                                {
                                    Information.ml = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GMD|17";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "17")
                                {
                                    Information.md = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GDI|18";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "18")
                                {
                                    Information.di = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GDP|19";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "19")
                                {
                                    Information.dp = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GGI|20";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "20")
                                {
                                    Information.gi = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    i = 0;
                    request = "GNM|21";//初始化需要发送的数据
                    buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            byte[] buffer2 = new byte[m_count];
                            Array.Copy(buffer1, buffer2, m_count);
                            string fRecvIDPstring = Encoding.ASCII.GetString(buffer2);
                            if (fRecvIDPstring.Substring(0, 1) == "A")
                            {
                                int m = fRecvIDPstring.IndexOf('|');
                                if (fRecvIDPstring.Substring(m + 1, 2) == "21")
                                {
                                    Information.nm = fRecvIDPstring.Substring(1, m - 1);
                                }
                                i = 3;
                                break;
                            }
                        }
                        i = i + 1;
                    }

                    if ((Information.nm == "") || (Information.mac == "") || (Information.IP == "") || (Information.portnum == "") ||
                        (Information.br == "") || (Information.bb == "") || (Information.dt == "") || (Information.rm == "") ||
                        (Information.tup == "") || (Information.pr == "") || (Information.fc == "") || (Information.di == "") || (Information.dp == "") || (Information.gi == ""))
                    {
                        MessageBox.Show("Please check device and PC connection status!", "Information");
                        return;
                    }
                    fSetdlg fsetdlg = new fSetdlg();
                    fsetdlg._IP(Information.IP);
                    fsetdlg._bb(Information.bb);
                    fsetdlg._br(Information.br);
                    fsetdlg._di(Information.di);
                    fsetdlg._dp(Information.dp);
                    fsetdlg._dsname(Information.dsname);
                    fsetdlg._dt(Information.dt);
                    fsetdlg._fc(Information.fc);
                    fsetdlg._gi(Information.gi);
                    fsetdlg._MAC(Information.mac);
                    fsetdlg._nm(Information.nm);
                    fsetdlg._portnum(Information.portnum);
                    fsetdlg._pr(Information.pr);
                    fsetdlg._rm(Information.rm);
                    fsetdlg._tup(Information.tup);
                    fsetdlg._usename(Information.usename);
                    DialogResult result = fsetdlg.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        request = "L";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(50);

                        request = "SON" + Information.usename + "|18";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDN" + Information.dsname + "|19";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "STP" + Information.tup + "|20";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SPN" + Information.portnum + "|21";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SRM" + Information.rm + "|22";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SFC" + Information.fc + "|23";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDT" + Information.dt + "|24";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SBR" + Information.br + "|25";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SPR" + Information.pr + "|26";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SBB" + Information.bb + "|27";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SRC" + Information.rc + "|28";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SCM" + Information.cm + "|29";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SCT" + Information.ct + "|30";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SML" + Information.ml + "|31";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SMD" + Information.md + "|32";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDI" + Information.di + "|33";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDP" + Information.dp + "|34";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SGI" + Information.gi + "|35";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SNM" + Information.nm + "|36";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SIP" + Information.IP + "|37";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "E";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(500);

                        iep = new IPEndPoint(IPAddress.Broadcast, 65535);//初始化一个发送广播和指定端口的网络端口实例
                        sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                        request = "W" + Information.mac;//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(100);

                        request = "L";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(50);

                        request = "SON" + Information.usename + "|18";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDN" + Information.dsname + "|19";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "STP" + Information.tup + "|20";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SPN" + Information.portnum + "|21";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SRM" + Information.rm + "|22";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SFC" + Information.fc + "|23";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDT" + Information.dt + "|24";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SBR" + Information.br + "|25";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SPR" + Information.pr + "|26";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SBB" + Information.bb + "|27";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SRC" + Information.rc + "|28";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SCM" + Information.cm + "|29";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SCT" + Information.ct + "|30";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SML" + Information.ml + "|31";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SMD" + Information.md + "|32";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDI" + Information.di + "|33";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SDP" + Information.dp + "|34";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SGI" + Information.gi + "|35";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SNM" + Information.nm + "|36";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "SIP" + Information.IP + "|33";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);

                        request = "E";//初始化需要发送的数据
                        buffer = Encoding.ASCII.GetBytes(request);
                        sock.SendTo(buffer, iep);
                        Thread.Sleep(10);
                        listView1.Items.Clear();
                    }
                    fsetdlg.Dispose();
                    sock.Close();
                }
                else
                {
                    MessageBox.Show("没有选择设备!", "提示");
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void bt_change_Click(object sender, EventArgs e)
        {
            string IPAddr = "";
            ChangeIPdlg changeIPdlg = new ChangeIPdlg();
            DialogResult result = changeIPdlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                IPAddr = changeIPdlg.IP1 + "." + changeIPdlg.IP2 + "." + changeIPdlg.IP3 + "." + changeIPdlg.IP4;
            }
            else
            {
                return;
            }
            try
            {
                if (listView1.SelectedIndices.Count > 0
                    && listView1.SelectedIndices[0] != -1)
                {
                    Information.IP = listView1.SelectedItems[0].SubItems[2].Text;
                    Information.mac = listView1.SelectedItems[0].SubItems[1].Text;
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//初始化一个Scoket实习,采用UDP传输
                    IPEndPoint iep = new IPEndPoint(IPAddress.Parse(Information.IP), 65535);//初始化一个发送广播和指定端口的网络端口实例
                    EndPoint ep = (EndPoint)iep;
                    //   sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);//设置该scoket实例的发送形式
                    string request = "X";//初始化需要发送的数据
                    byte[] buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(100);

                    request = "L";
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(50);

                    request = "SIP" + IPAddr + "|34";
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(10);

                    request = "E|35";
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(200);

                    iep = new IPEndPoint(IPAddress.Broadcast, 65535);//初始化一个发送广播和指定端口的网络端口实例
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    request = "W" + Information.mac;
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(100);

                    request = "L";
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(50);

                    request = "SIP" + IPAddr + "|34";
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    Thread.Sleep(100);

                    request = "E|35";
                    buffer = Encoding.ASCII.GetBytes(request);
                    sock.SendTo(buffer, iep);
                    sock.Close();
                    listView1.Items.Clear();
                }
                else
                {
                  //  MessageBox.Show("没有选择设备!", "提示");
                }

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            changeIPdlg.Dispose();
        }

        private void bt_flash_Click(object sender, EventArgs e)
        {
            int i = 0;
            try
            {
                if (listView1.SelectedIndices.Count > 0
                    && listView1.SelectedIndices[0] != -1)
                {
                    Information.IP = listView1.SelectedItems[0].SubItems[2].Text;
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//初始化一个Scoket实习,采用UDP传输

                    IPEndPoint iep = new IPEndPoint(IPAddress.Parse(Information.IP), 65535);//初始化一个发送广播和指定端口的网络端口实例
                    EndPoint ep = (EndPoint)iep;
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);//设置该scoket实例的发送形式
                    string request = "B";//初始化需要发送的数据
                    byte[] buffer = Encoding.ASCII.GetBytes(request);
                    while (i < 3)
                    {
                        sock.SendTo(buffer, iep);
                        byte[] buffer1 = new byte[1000];
                        sock.ReceiveTimeout = 10;
                        int m_count = sock.ReceiveFrom(buffer1, ref ep);
                        if (m_count > 0)
                        {
                            i = 3;
                            sock.Close();
                            break;
                        }
                        i = i + 1;
                    }
                }
                else
                {
                  //  MessageBox.Show("没有选择设备!", "提示");
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void grp_Info_Enter(object sender, EventArgs e)
        {

        }

        private void rb_rs232_CheckedChanged(object sender, EventArgs e)
        {
            if (rb_rs232.Checked)
            {
                if (PortHandle > 1024)
                    btDisConnectTcp_Click(null, null);
                gpb_rs232.Enabled = true;
                btDisconnet.Enabled = false;
                gpb_tcp.Enabled = false;
            }
        }

        private void rb_tcp_CheckedChanged(object sender, EventArgs e)
        {
            if (rb_tcp.Checked)
            {
                if ((PortHandle > 0) && (PortHandle < 256))
                    btDisconnet_Click(null, null);
                gpb_tcp.Enabled = true;
                btDisConnectTcp.Enabled = false;
                gpb_rs232.Enabled = false;
            }
        }

        private void btDisConnectTcp_Click(object sender, EventArgs e)
        {
            fCmdRet = Device.CloseNetPort(PortHandle);
            if (fCmdRet == 0)
            {
                DisableForm();
                btConnectTcp.Enabled = true;
                btDisConnectTcp.Enabled = false;
            }
            AddCmdLog("CloseNetPort", "Close Net Port", fCmdRet); 
        }

        private void btConnectTcp_Click(object sender, EventArgs e)
        {
           try
           {
               if (tb_Port.Text == "" || text_ipaddr.Text == "")
               {
                   return;
               }
               int Port = Convert.ToInt32(tb_Port.Text, 10);
               string ipAddr = text_ipaddr.Text;
               fCmdRet = Device.OpenNetPort(Port, ipAddr,ref ControllerAdr, ref PortHandle);
               if (fCmdRet == 0)
               {
                   bt_GetDeviceInfo_Click(null, null);
                   EnableForm();
                   btConnectTcp.Enabled = false;
                   btDisConnectTcp.Enabled = true;
                   fModel = 0;
                   fCmdRet = Device.ModeSwitch(ref ControllerAdr, ref fModel, ref IRStatus, PortHandle);
                   if (fCmdRet == 0)
                   {
                       if (fModel == 0)
                       {
                           panel2.Enabled = true;
                           panel3.Enabled = false;
                           com_mode.SelectedIndex = 0;
                       }
                       else
                       {
                           panel2.Enabled = false;
                           panel3.Enabled = true;
                           com_mode.SelectedIndex = 1;
                       }
                       RefreshFreeRate(IRStatus);
                   }
               }
               else
               {
                   MessageBox.Show("Communication");
               }
               AddCmdLog("OpenNetPort", "Open Net Port", fCmdRet);
           }
           catch (System.Exception ex)
           {
           	
           }
        }

        private void com_bztime_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }








    }
}
