using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PrecisePositionLibrary;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Kalman = PrecisePositionLibrary.Kalman;
using ReportMode = PrecisePositionLibrary.PrecisePosition.ReportMode;
using Point = PrecisePositionLibrary.Point;
using PosititionMode = PrecisePositionLibrary.PrecisePosition.PosititionMode;
using AfewDPos = PrecisePositionLibrary.PrecisePosition.AfewDPos;

using System.Text;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;   //引用MySql

using StackExchange.Redis; //引用redis
using Newtonsoft.Json;

using IronPython.Hosting;//呼叫python(目前已不需要)
using Microsoft.Scripting.Hosting;//呼叫python(目前已不需要)
using System.Diagnostics;//呼叫python(目前已不需要)


namespace PrecisePosition
{
    
    public partial class Form1 : Form
    {
        public static string temp; //計算完之座標，會以temp用字串存入
        public static string temp1; //計算完之座標，會以temp用字串存入
        public static string verification;//四點基站算法驗證，如果出現虛數解，verification存入none
        public static string verification1;//三點基站算法驗證，如果出現虛數解，verification存入none
        public static double[] ans = new double[3];//四基站算法計算完之座標
        public static double[] ans1 = new double[3];//三基站算法計算完之座標
        public static int temp_last_receive_packet; //紀錄上一次收到的封包 
        public static int temp_last_without_move;//紀錄上一次不動的時間
        double compare_distance;//三基站與四基站所算出座標的距離差
        public static double[] final_ans = new double[3];//最後存入mysql內部的XYZ


        //基站座標，之後需寫檢查redis的座標和基站名稱的程式，由網頁存入reids，每次檢查
        //public class port_counter
        //{
        //    public string id;
        //    public double[] position = new double[3];
        //    public double distance;
        //
        //}
        //List<port_counter> myPortList = new List<port_counter>();
        //
        //
        //
        //public static double[] X1 = new double[3];
        //public static double[] X2 = new double[3];
        //public static double[] X3 = new double[3];
        //public static double[] X4 = new double[3];

        double[] X1 = { 0, 0, 0 };
        double[] X2 = { -613, 0, 0 };
        double[] X3 = { -313, 626, 0 };
        double[] X4 = { 0, 0, 160 };


        double[] tempX = new double[3];

        string[] station = { "4475", "4745", "44AF", "460E"};









        public static bool isShowGuidesLine = false;//是否显示辅助线
        public static string config = "net.ini";
        public static float TagR = 5;
        public static float ReferNearR = 20;
        public static int ReceiMaxLen = 2048;
        public static int RealScreenWidth = 0;
        public static int RealScreenHeight = 0;
        public static int ScreenWidth = 1440;//1440
        public static int ScreenHeight = 860;//860
        public static int tick = 0;
        public const string StrThreeStation = "Three station mode";
        public const string StrSingleStation = "Single station mode";
        private static object cls_lock = new object();
        private static GuidRecord MyGuidRecord = null;
        private ReportMode CurReportMode = ReportMode.UnKnown;
        private const Int32 LocaDataLen = 14;
        private const Int32 ReportSeftIDLen = 7;
        private Boolean IsCnn = false;
        private Object obj_lock = new object();
        private Bitmap MapBitMap = null;
        public string StrMapPath = "";
        private System.Windows.Forms.Timer MyTimer = null;
        //刷新地图的线程时间间隔
        private int UpdateInterval = 1000;
        private Thread UpdateMapThread = null;
        private Boolean isUpdate = false;
        private static bool loadreferflag = true;
        private AlarmInfor MyAlarmInfor = null;
        private System.Object Cards_Lock = new System.Object();
        public System.Object Ports_Lock = new System.Object();
        public Dictionary<string, PortInfor> Ports = new Dictionary<string, PortInfor>();
        public ConcurrentDictionary<string, LimitArea> Areas = new ConcurrentDictionary<string, LimitArea>();

        private RegularRegionAlarmReport regularRegionAlarmReport;
        Random mrd = new Random();
        //区域地图讯息
        public Group group = null;
        public class PortInfor
        {
            public byte[] PortID = new byte[2];
            //固件版本号
            public uint ver = 0;
            //上报的时间
            public DateTime ReportTime = new DateTime();
            public int sleep;
        }
        public Dictionary<string, Tagmsg> tgmsgs = new Dictionary<string, Tagmsg>();
        public class Tagmsg
        {
            public byte[] ID = new byte[2];
            public string Name = "";
        }
        public Dictionary<string, Bsmsg> Bsmsgs = new Dictionary<string, Bsmsg>();
        public class Bsmsg:PrecisePositionLibrary.BsInfo
        {
            public string Name = "";
            public PortType porttype = PortType.ThreeMode;
            public double rangeR = 0.0;
        }
        public ConcurrentDictionary<string, PrecisePositionLibrary.BsInfo> InnerPorts = new ConcurrentDictionary<string, PrecisePositionLibrary.BsInfo>();
        public bool isTrace = false;
        public String TraceTagId = "";
        public int PortWidth = 30;
        public int PortHeight = 14;
        public int Str_OffSet = 1;
        //当前是否有参考点可以拖拽
        public bool isDrag = false;
        //是否有移动
        public bool isMove = false;
        //是否开始设置限制区域
        public bool isLimit = false;
        public  LimitArea CurLimitArea = null;
        public bool IsMapDrag = false;
        public int startX = 0, startY = 0;
        //当前可以拖拽的参考点的ID
        public string CanDragPortID = "";
        public bool isStart = false;
        private const int ListMode = 1;
        private const int ImageMode = 2;
        //图片距离与真实距离的比值关系
        public double Img_RealDisRelation = -1;
        public ConcurrentDictionary<string, CardImg> CardImgs = new ConcurrentDictionary<string, CardImg>();
        //用于保存记录
        public ConcurrentQueue<CardImg> tpks = new ConcurrentQueue<CardImg>();
        //保存区域集合
        public ConcurrentDictionary<String, Group> Groups = new ConcurrentDictionary<string, Group>();
        public System.Object AlarmInfors_Lock = new System.Object();
        public List<string> AlarmInfors = new List<string>();
        //0:按照距离排序 1:按照信号品质排序
        public int ListSortMode = 0;
        public Color PriCellColor = Color.Red;
        public List<PrecisePositionLibrary.CustomPacket> ListCustomData = new List<PrecisePositionLibrary.CustomPacket>();
        public object LockListCustomData = new object();

        

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e) {
            LoadResolution();
            ObtainScreenSize();
            ReSetBounds();
            UpLoadPortList();
            LoadParam();
            //保证每次Load指能绑定一个鼠标滚动事件
            Map_panel.MouseWheel -= Map_panel_MouseWheel;    
            Map_panel.MouseWheel += Map_panel_MouseWheel;
            //清理历史记录
            if (Parameter.isClearHistory)
            {
                ClearHistoryRecord();
            }
            //判断当前的触发方式
            if (loadreferflag)
            {
                if (Parameter.ResolutionWidth != RealScreenWidth || Parameter.ResolutionHeight != RealScreenHeight)
                {
                    Parameter.isTwoSizeMode = true;
                }
                else
                {
                    Parameter.isTwoSizeMode = false;
                }
                #region 每次重启应用就开始加载区域讯息
                if (Parameter.isSupportMulArea)
                {
                    LoadMulAreas();
                }
                else
                {
                    LoadLimitAreas();
                    LoadPorts();
                }
                #endregion
                RefreshAreaUI();
                //说明两次分辨率相同，重新设置参考点讯息
                if (Parameter.ResolutionWidth != RealScreenWidth && Parameter.ResolutionHeight != RealScreenHeight)
                {
                    ResetPortPlace(RealScreenWidth,RealScreenHeight);
                    Parameter.ResolutionWidth = RealScreenWidth;
                    Parameter.ResolutionHeight = RealScreenHeight;
                    Ini.SetValue(Ini.ConfigPath, Ini.StrResolution,  Ini.StrSolutionWidth,  Parameter.ResolutionWidth + "");
                    Ini.SetValue(Ini.ConfigPath, Ini.StrResolution, Ini.StrSolutionHeight, Parameter.ResolutionHeight + "");
                }

                
                

                TagPageBox.SelectedIndex = 2;// 直接到監控列表頁面
                Start_Listen_Btn_Click("Start monitoring", new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 2));

                


                //ConnectionMultiplexer redis_outer = ConnectionMultiplexer.Connect("localhost");//大家使用redis的話應該都是本機端
                //IDatabase database = redis_outer.GetDatabase(1);  //連結到指定的資料庫，這樣代表就是db1
                //int length = (int)database.ListLength("set_info");
                //Console.WriteLine(length);
                //
                //for (int i = 0; i< length; i++)
                //{
                //    //port_counter port_counter_data = new port_counter();
                //    //port_counter_data.id = station[i];
                //
                //    string RightPop = (String)database.ListRightPop("set_info");
                //    string[] coord1 = RightPop.Split('"');
                //    string[] coord = coord1[1].Split(',');
                //    Console.WriteLine(coord);
                //    double rst0 = 0.0;
                //    double rst1 = 0.0;
                //    double rst2 = 0.0;
                //    double.TryParse(coord[0], out rst0);
                //    double.TryParse(coord[1], out rst1);
                //    double.TryParse(coord[2], out rst2);
                //    Console.WriteLine(rst0);
                //    Console.WriteLine(rst1);
                //    Console.WriteLine(rst2);
                //
                //    //port_counter_data.position[0] = rst0;
                //    //port_counter_data.position[1] = rst1;
                //    //port_counter_data.position[2] = rst2;
                //    //port_counter_data.distance = -1;
                //    //myPortList.Add(port_counter_data);
                //
                //
                //    if (i==0)
                //    {
                //        double[] X1 = { rst0, rst1, rst2 };
                //    }
                //    if (i == 1)
                //    {
                //        double[] X2 = { rst0, rst1, rst2 };
                //    }
                //    if (i == 2)
                //    {
                //        double[] X3 = { rst0, rst1, rst2 };
                //    }
                //    if (i == 3)
                //    {
                //        double[] X4 = { rst0, rst1, rst2 };
                //    }
                //}

            }
            else
            {
                loadreferflag = true;
            }
        }

        public void RefreshAreaUI()
        {
            if (Parameter.isSupportMulArea)
            {
                MulArealb.Visible = true;
                SelectAreaCB.Visible = true;
                MulArealb.Location = new System.Drawing.Point(13, 82);
                SelectAreaCB.Location = new System.Drawing.Point(11, 100);
                tracecb.Location = new System.Drawing.Point(11, 130);
                tracetagtb.Location = new System.Drawing.Point(11, 154);
                tracetagtb.Visible = true;
                tracecb.Visible = true;
                ListShowCard_groupBox.Location = new System.Drawing.Point(4, 184);
                ListShowCard_groupBox.Height = TagPageBox.Height - ListShowCard_groupBox.Location.Y - 28;
                CardList_panel.Height = ListShowCard_groupBox.Height - CardList_panel.Location.Y - 10;
                SelectAreaCB.Items.Clear();
                List<KeyValuePair<string, Group>> grouplist = Groups.OrderBy(k => k.Key).ToList();
                foreach (KeyValuePair<string, Group> grp in grouplist)
                {
                    if (null == grp.Value)
                    {
                        continue;
                    }
                    if ("".Equals(grp.Value.name))
                    {
                        SelectAreaCB.Items.Add(grp.Key);
                    }
                    else
                    {
                        StringBuilder strbuilder = new StringBuilder();
                        strbuilder.Append(grp.Value.name);
                        strbuilder.Append("(");
                        strbuilder.Append(grp.Key);
                        strbuilder.Append(")");
                        SelectAreaCB.Items.Add(strbuilder.ToString());
                    }
                }
                if (SelectAreaCB.Items.Count > 0)
                {
                    SelectAreaCB.SelectedIndex = 0;
                }
            }
            else
            {
                MulArealb.Visible = false;
                SelectAreaCB.Visible = false;
                tracetagtb.Visible = false;
                tracecb.Visible = false;
                ListShowCard_groupBox.Location = new System.Drawing.Point(4, 73);
                ListShowCard_groupBox.Height = TagPageBox.Height - ListShowCard_groupBox.Location.Y - 30;
                CardList_panel.Height = ListShowCard_groupBox.Height - CardList_panel.Location.Y - 10;
            }
        }
        /// <summary>
        /// 清除历史记录
        /// </summary>
        private void ClearHistoryRecord()
        {
            if (!Directory.Exists(Parameter.RecordDir)) return;
            string[] strrecords = Directory.GetDirectories(Parameter.RecordDir);
            string strdt = "";
            int year = 0, month = 0, day = 0, hour = 0;
            DateTime curdt;
            ArrayList DeleteDirs = new ArrayList();
            foreach (string str in strrecords)
            {
                strdt = str.Substring(str.LastIndexOf("\\") + 1, 8);
                try
                {
                    year = Convert.ToInt32(strdt.Substring(0, 4));
                    month = Convert.ToInt32(strdt.Substring(4, 2));
                    day = Convert.ToInt32(strdt.Substring(6, 2));
                }
                catch (Exception)
                {
                    continue;
                }
                string[] strfiles = Directory.GetFiles(str);
                foreach (string strfile in strfiles)
                {
                    string strhour = strfile.Substring(strfile.LastIndexOf("\\") + 1, 2);
                    try
                    {
                        hour = Convert.ToInt32(strhour);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    curdt = new DateTime(year, month, day, hour, 0, 0);
                    double daynum = Math.Abs((DateTime.Now - curdt).TotalDays);
                    if (daynum >= Parameter.ClearHistoryTime)
                    {
                        DeleteDirs.Add(strfile);
                    }
                }
            }
            foreach (string deletestr in DeleteDirs)
            {
                File.Delete(deletestr);
            }
            DeleteDirs.Clear();
            //清除掉空目录文件
            foreach (string str in strrecords)
            {
                if (Directory.GetFiles(str).Length <= 0)
                {
                    DeleteDirs.Add(str);
                }
            }
            foreach (string deletestr in DeleteDirs)
            {
                Directory.Delete(deletestr, true);
            }
        }
        private DateTime GetsMinDT(string[] strrecords)
        {
            if (null == strrecords)
            {
                return DateTime.MinValue;
            }
            if (strrecords.Length <= 0)
            {
                return DateTime.MinValue;
            }
            int year = 0, month = 0, day = 0;
            string strdt = strrecords[0].Substring(strrecords[0].LastIndexOf("\\") + 1, 8);
            try
            {
                year  = Convert.ToInt32(strdt.Substring(0, 4));
                month = Convert.ToInt32(strdt.Substring(4, 2));
                day   = Convert.ToInt32(strdt.Substring(6, 2));
            }
            catch (Exception)
            {
            }
            DateTime mindt, curdt;
            mindt = new DateTime(year, month, day);
            foreach (string str in strrecords)
            {
                strdt = str.Substring(str.LastIndexOf("\\") + 1, 8);
                try
                {
                    year  = Convert.ToInt32(strdt.Substring(0, 4));
                    month = Convert.ToInt32(strdt.Substring(4, 2));
                    day   = Convert.ToInt32(strdt.Substring(6, 2));
                }
                catch (Exception)
                {
                    return DateTime.MinValue;
                }
                curdt = new DateTime(year, month, day);
                if (DateTime.Compare(curdt, mindt) < 0)
                {
                    mindt = curdt;
                }
            }
            return mindt;
        }
        public void LoadParam()
        {
            string StrBattFlag = Ini.GetValue(Ini.ConfigPath, Ini.BatterySeg, Ini.IsLowBattery);
            if ("False".Equals(StrBattFlag)) {
                Parameter.RecordBatteryLessCard = false;
            } else {
                Parameter.RecordBatteryLessCard = true;
                try {
                    string StrLowBattry = Ini.GetValue(Ini.ConfigPath, Ini.BatterySeg, Ini.LowBattery);
                    if (null != StrLowBattry && !"".Equals(StrLowBattry)) {
                        Parameter.LowBattry = Convert.ToInt32(StrLowBattry);
                    }
                } catch (Exception) {
                    Parameter.RecordBatteryLessCard = false;
                }
            }
            string sIsRegionAlarmOverTime = Ini.GetValue(Ini.ConfigPath, Ini.regionAlarmOver, Ini.isRegionAlarmOverTime);
            if ("False".Equals(sIsRegionAlarmOverTime)) {
                Parameter.isRegionAlarmRateTime = false;
            } else {
                string sRegionAlarmOverTime = Ini.GetValue(Ini.ConfigPath, Ini.regionAlarmOver, Ini.regionAlarmOverTime);
                int regionAlarmOverTime = 0;
                if (null != sRegionAlarmOverTime && !"".Equals(sRegionAlarmOverTime)) {
                    try {
                        Parameter.regionAlarmRateTime = Convert.ToInt32(sRegionAlarmOverTime);
                        Parameter.isRegionAlarmRateTime = true;
                    } catch {
                        Parameter.isRegionAlarmRateTime = false;
                    }
                } else {
                    Parameter.isRegionAlarmRateTime = false;
                } 
            }
            string strlimitarea = Ini.GetValue(Ini.ConfigPath, Ini.AreaAramSeg, Ini.EnableAreaAlarm);
            if ("False".Equals(strlimitarea))
            {
                Parameter.isEnableLimitArea = false;
            }
            else
            {
                Parameter.isEnableLimitArea = true;
            }
            string StrOverTimeNoReceive = Ini.GetValue(Ini.ConfigPath, Ini.OTNoReceiveSeg, Ini.OTNoReveiveKey);
            if ("False".Equals(StrOverTimeNoReceive))
            {
                Parameter.RecordOverTimeNoReceiInfo = false;
            }
            else
            {
                Parameter.RecordOverTimeNoReceiInfo = true;
                string strovertime = "";
                strovertime = Ini.GetValue(Ini.ConfigPath, Ini.OTNoReceiveSeg, Ini.OTNoReceiveWarmTime);
                if (null == strovertime)
                {
                    Parameter.OverNoReceiveWarmTime = 60;
                }
                else
                {
                    try
                    {
                        Parameter.OverNoReceiveWarmTime = Convert.ToUInt16(strovertime);
                    }
                    catch (Exception)
                    {
                        Parameter.OverNoReceiveWarmTime = 60;
                    }
                }
            }
            string StrShowPlacePort = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.ShowReferKey);
            if ("False".Equals(StrShowPlacePort))
            {
                Parameter.ShowPlacePort = false;
            }
            else
            {
                Parameter.ShowPlacePort = true;
            }
            string StrisReferType = Ini.GetValue(Ini.ConfigPath,Ini.ShowSeg,Ini.isEnableReferType);
            if ("True".Equals(StrisReferType))
            {
                Parameter.isEnableReferType = true;
            }
            else
            {
                Parameter.isEnableReferType = false;
            }
            string StrTagShow = Ini.GetValue(Ini.ConfigPath,Ini.TagShowOver,Ini.TagShowOverKey);
            if ("False".Equals(StrTagShow))
            {
                Parameter.TagShowOver = false;
            }
            else
            { 
                Parameter.TagShowOver = true; 
            }
            string StrLongNoExeFlag = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.LongTime_NoExeKey);
            if ("False".Equals(StrLongNoExeFlag))
            {
                Parameter.LongTime_NoExe_ToBlackShow = false;
            }
            else
            {
                Parameter.LongTime_NoExe_ToBlackShow = true;
                string StrLongTimeNoExe = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.NoExeTime);
                try
                {
                    if (null != StrLongTimeNoExe && !"".Equals(StrLongTimeNoExe))
                    {
                        Parameter.OverTime2 = Convert.ToInt32(StrLongTimeNoExe);
                    }
                }
                catch (Exception)
                {
                    Parameter.LongTime_NoExe_ToBlackShow = false;
                }
            }
            string StrOverTimeNoShow = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.TimeOutNoShowKey);
            if ("False".Equals(StrOverTimeNoShow))
            {
                Parameter.NoShow_OverTime_NoRecei = false;
            }
            else
            {
                Parameter.NoShow_OverTime_NoRecei = true;
                string StrOverTime = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.OutTime);
                try
                {
                    if (null != StrOverTime && !"".Equals(StrOverTime))
                    {
                        Parameter.OverTime1 = Convert.ToInt32(StrOverTime);
                    }
                }
                catch (Exception)
                {
                    Parameter.NoShow_OverTime_NoRecei = false;
                }
            }
            //加载刷新时间
            string StrIsRefreshTime = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.ShowRefreshKey);
            if ("False".Equals(StrIsRefreshTime))
            {
                Parameter.isDefineInterval = false;
            }
            else
            {
                Parameter.isDefineInterval = true;
                string strdefineintertime = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.ShowRefreshTime);
                try
                {
                    if (null != strdefineintertime && !"".Equals(strdefineintertime))
                        Parameter.DefineInterval = Convert.ToInt32(strdefineintertime);
                }
                catch (Exception)
                {
                    Parameter.isDefineInterval = false;
                }
            }

            string StrIsClear = Ini.GetValue(Ini.ConfigPath, Ini.ClearSeg, Ini.IsClearKey);
            if ("False".Equals(StrIsClear))
            {
                Parameter.isClearHistory = false;
            }
            else
            {
                string StrClearTime = Ini.GetValue(Ini.ConfigPath, Ini.ClearSeg, Ini.ClearTimeKey);
                if ("".Equals(StrClearTime))
                {
                    Parameter.isClearHistory = false;
                }
                try
                {
                    Parameter.ClearHistoryTime = Convert.ToInt32(StrClearTime);
                }
                catch (Exception)
                {
                    Parameter.isClearHistory = false;
                }
                Parameter.isClearHistory = true;
            }
            string strShow = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.ShowTraceKey);
            if ("False".Equals(strShow))
            {
                Parameter.isShowTrace = false;
            }
            else
            {
                Parameter.isShowTrace = true;
            }
            string strkalman = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.isKalmanKey);
            if ("True".Equals(strkalman))
            {
                Parameter.isKalman = true;
                string StrKalmanMNosieCovar = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.MNosieCovar);
                try
                {
                    Parameter.KalmanMNosieCovar = Convert.ToDouble(StrKalmanMNosieCovar);
                }
                catch (Exception)
                {
                    Parameter.KalmanMNosieCovar = 0.1;
                }
                string StrKalmanProNosieCovar = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.ProNosieCovar);
                try
                {
                    Parameter.KalmanProNosieCovar = Convert.ToDouble(StrKalmanProNosieCovar);
                }
                catch (Exception)
                {
                    Parameter.KalmanProNosieCovar = 0.2;
                }
                string StrKalmanLastStatePre = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.LastStatePre);
                try
                {
                    Parameter.KalmanLastStatePre = Convert.ToDouble(StrKalmanLastStatePre);
                }
                catch (Exception)
                {
                    Parameter.KalmanLastStatePre = 0.5;
                }
            }
            else
            {
                Parameter.isKalman = false;
            }
            //加载参数
            string strmode = Ini.GetValue(Ini.ConfigPath, Ini.StrPositionMode, Ini.StrMode);
            if ("1".Equals(strmode))
            {
                Parameter.positionmode = PosititionMode.Closestdistance;
            }
            else
            {
                Parameter.positionmode = PosititionMode.SigQuality;
            }
            string strmul = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg,Ini.EnableMulAreaMode);
            if ("False".Equals(strmul))
            {
                Parameter.isSupportMulArea = false;
                
            }
            else
            {
                Parameter.isSupportMulArea = true;
            }
            //采用3个基站定位
            string strplace = Ini.GetValue(Ini.ConfigPath,Ini.ShowSeg, Ini.Use3Place);
            if ("True".Equals(strplace))
            {
                Parameter.isUse3Station = true;
            }
            else
            {
                Parameter.isUse3Station = false;
            }

            string str = null;
            str = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.UseTagHeightRange);
            if ("True".Equals(str))
            {
                Parameter.isUseTagHeightRange = true;
            }
            else
            {
                Parameter.isUseTagHeightRange = false;
            }
            str = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.TagHeightRangeLow);
            try
            {
                Parameter.TagHeightRangeLow = Convert.ToInt32(str);
            }
            catch
            {
                Parameter.TagHeightRangeLow = 50;
            }
            str = Ini.GetValue(Ini.ConfigPath, Ini.ShowSeg, Ini.TagHeightRangeHigh);
            try
            {
                Parameter.TagHeightRangeHigh = Convert.ToInt32(str);
            }
            catch
            {
                Parameter.TagHeightRangeLow = 200;
            }

        }
        public void NewPlaceToNormPlace(ref float x, ref float y)
        {
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = L0 * DxfMapParam.scale;
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            { 
                x = (float)(DxfMapParam.PanelCenterX - p1);
                y = (float)(DxfMapParam.PanelCenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            { 
                x = (float)(DxfMapParam.PanelCenterX + p1);
                y = (float)(DxfMapParam.PanelCenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            { 
                x = (float)(DxfMapParam.PanelCenterX - p1);
                y = (float)(DxfMapParam.PanelCenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            { 
                x = (float)(DxfMapParam.PanelCenterX + p1);
                y = (float)(DxfMapParam.PanelCenterY + p0);
            }
        }
        public void Map_panel_MouseWheel(Object obj,MouseEventArgs args)
        {
            //按当前鼠标点放大缩小
            #region
            /*
             * 功能：实现滚动鼠标时按当前鼠标的点进行放大缩小
             * 方法:1、将当前鼠标的坐标转化为scale = 1时的坐标，且当前的坐标保留(后面要使用)。
             *      2、将当前的坐标转化为放大后的坐标。
             *      3、根据鼠标坐标和放大后该点的坐标，来移动地图的中心点位置实现功能。
             * */
            double CurX, CurY,OriginX = 0,OriginY = 0;
            CurX = args.X; CurY = args.Y;
            double p0, p1, d0, d1,l0,l1;
            //计算当前斜边的长度l0
            p0 = Math.Abs(DxfMapParam.CenterX - CurX);
            d0 = Math.Abs(DxfMapParam.CenterY - CurY);
            l0 = Math.Pow(Math.Pow(p0,2)+Math.Pow(d0,2),0.5);
            //计算scale = 1时斜边的长度
            l1 = DxfMapParam.scale * l0;
            p1 = (l1 / l0) * p0;
            d1 = (l1 / l0) * d0;
            //计算scale = 1时鼠标当前的坐标
            if (CurX <= DxfMapParam.CenterX && CurY < DxfMapParam.CenterY)
            {//第一象限
                OriginX = DxfMapParam.CenterX - p1;
                OriginY = DxfMapParam.CenterY - d1;
            }else if(CurX > DxfMapParam.CenterX && CurY <= DxfMapParam.CenterY)
            {//第二象限
                OriginX = DxfMapParam.CenterX + p1;
                OriginY = DxfMapParam.CenterY - d1;
            }
            else if (CurX <= DxfMapParam.CenterX && CurY > DxfMapParam.CenterY)
            {//第三象限
                OriginX = DxfMapParam.CenterX - p1;
                OriginY = DxfMapParam.CenterY + d1;
            }
            else if(CurX > DxfMapParam.CenterX && CurY >= DxfMapParam.CenterY)
            {//第三象限
                OriginX = DxfMapParam.CenterX + p1;
                OriginY = DxfMapParam.CenterY + d1;
            }
            #endregion
            if(args.Delta >= 120)
            {
                if (DxfMapParam.scale > 0.3)
                {
                    DxfMapParam.scale -= 0.3;
                }
            }
            else if(args.Delta <= -120)
            { 
                DxfMapParam.scale += 0.3;
            }
            //挪动地图中心点
            #region
            //计算当前斜边的长度l0
            p0 = Math.Abs(DxfMapParam.CenterX - OriginX);
            d0 = Math.Abs(DxfMapParam.CenterY - OriginY);
            l0 = Math.Pow(Math.Pow(p0, 2) + Math.Pow(d0, 2), 0.5);
            //计算scale时斜边的长度
            l1 = l0/DxfMapParam.scale;
            p1 = (l1 / l0) * p0;
            d1 = (l1 / l0) * d0;
            double ZoomX = 0,ZoomY = 0;
            //计算scale = 1时鼠标当前的坐标
            if (OriginX <= DxfMapParam.CenterX && OriginY < DxfMapParam.CenterY)
            {//第一象限
                ZoomX = DxfMapParam.CenterX - p1;
                ZoomY = DxfMapParam.CenterY - d1;
            }
            else if (OriginX > DxfMapParam.CenterX && OriginY <= DxfMapParam.CenterY)
            {//第二象限
                ZoomX = DxfMapParam.CenterX + p1;
                ZoomY = DxfMapParam.CenterY - d1;
            }
            else if (OriginX <= DxfMapParam.CenterX && OriginY > DxfMapParam.CenterY)
            {//第三象限
                ZoomX = DxfMapParam.CenterX - p1;
                ZoomY = DxfMapParam.CenterY + d1;
            }
            else if (OriginX > DxfMapParam.CenterX && OriginY >= DxfMapParam.CenterY)
            {//第三象限
                ZoomX = DxfMapParam.CenterX + p1;
                ZoomY = DxfMapParam.CenterY + d1;
            }
            //移动中心点
            DxfMapParam.CenterX -= (ZoomX - args.X);
            DxfMapParam.CenterY -= (ZoomY - args.Y);
            #endregion
            LoadDxfMap();
        }
        /// <summary>
        /// 获取屏幕大小
        /// </summary>
        public void ObtainScreenSize()
        {
            Rectangle rec = new Rectangle();
            rec = Screen.GetWorkingArea(this);
            RealScreenWidth = rec.Width;
            //RealScreenWidth = 720;
            RealScreenHeight = rec.Height;
            //RealScreenHeight = 430;
           
        }
        //加载上一次分辨率
        public void LoadResolution()
        {
            string strsoluwidth = Ini.GetValue(Ini.ConfigPath, Ini.StrResolution, Ini.StrSolutionWidth);
            string strsoluheight = Ini.GetValue(Ini.ConfigPath, Ini.StrResolution, Ini.StrSolutionHeight);
            try
            {
                Parameter.ResolutionWidth = Convert.ToInt32(strsoluwidth);
                Parameter.ResolutionHeight = Convert.ToInt32(strsoluheight);
            }
            catch (Exception)
            {
                Parameter.ResolutionWidth = 0;
                Parameter.ResolutionHeight = 0;
            }
        }
        public void SetFormBounds(int width,int height)
        {
            this.SetBounds(0, 0, width, height);
            TagPageBox.SetBounds(5, 5, width - 25, height - 50);
            //参考点、卡片设置页面
            Port_listView.Height = height - 150;
            Card_listView.Height = height - 150;
            //列表
            CardList_listView.Width = TagPageBox.Width - 20 * 2;
            CardList_listView.Height = TagPageBox.Height - CardList_listView.Top - 55;
            //图形界面
            ListShowCard_groupBox.Height = TagPageBox.Height - ListShowCard_groupBox.Location.Y - 30;
            CardList_panel.Height = ListShowCard_groupBox.Height - CardList_panel.Location.Y - 10;
            Map_panel.Height = TagPageBox.Height - Map_panel.Location.Y - 30;
            Map_panel.Width = TagPageBox.Width - Map_panel.Location.X - 10;
            label14.Top = CardList_listView.Height + CardList_listView.Location.Y + 10;
            guidcb.Location = new System.Drawing.Point(TagPageBox.Width - guidcb.Width - 10, TagPageBox.Location.Y + guidcb.Height / 2);
        }
        //重新设置基站位置
        public void ResetPortPlace(int width,int height)
        {
            if (width == Parameter.ResolutionWidth && height == Parameter.ResolutionHeight)
            {//发现宽和高相同，则不需要重新设置基站位置了
                return;
            }
            //记录原来中心点的位置
            double oldcenterx = (double)Map_panel.Width / 2;
            double oldcentery = (double)Map_panel.Height / 2;
            double oldwidth   = (double)Map_panel.Width;
            double oldheight  = (double)Map_panel.Height;
            //重新设置坐标
            SetFormBounds(width, height);
            //记录新坐标
            double newcenterx = (double)Map_panel.Width / 2;
            double newcentery = (double)Map_panel.Height / 2;
            //两个中心点坐标距离
            double ctrd = Math.Pow((Math.Pow(Math.Abs(oldcenterx - newcenterx), 2) + Math.Pow(Math.Abs(oldcentery - newcentery), 2)), 0.5);
            //计算地图的放大比例关系
            double k1 = (double)Map_panel.Width / oldwidth;
            double k2 = (double)Map_panel.Height / oldheight;
            //放大缩小都是以小比例为基准
            //k1为宽度放大的系数，k2为高度放大系数
            double s1 = 0, s2 = 0;
            s1 = (double)Map_panel.Width / Map_panel.Height;
            s2 = (double)oldwidth / oldheight;
            double k = 0;
            if (s1 < s2)
            {
                k = (k1 < k2) ? k2 : k1;
            }
            else
            {
                k = (k1 < k2) ? k1 : k2;
            }
            double pd = 0, cosx = 0, siny = 0;
            //重新设置基站坐标
            List<PrecisePositionLibrary.BsInfo> bss = InnerPorts.Values.ToList<BsInfo>();
            for (int i = 0; i < bss.Count; i++)
            {
                //原来斜边长度
                pd = Math.Pow(Math.Pow(Math.Abs(bss[i].Place.x - oldcenterx), 2) + Math.Pow(Math.Abs(bss[i].Place.y - oldcentery), 2), 0.5);
                if (pd <= 0)
                {
                    continue;
                }
                cosx = (double)(bss[i].Place.x - oldcenterx) / pd;
                siny = (double)(bss[i].Place.y - oldcentery) / pd;
                //放大缩小后的斜边长度
                pd = pd * k;
                //计算移动坐标后
                bss[i].Place.x = newcenterx + pd * cosx;
                bss[i].Place.y = newcentery + pd * siny;
            }
            //计算限制区域坐标信息
            List<LimitArea> limareas = Areas.Values.ToList<LimitArea>();
            for (int i = 0; i < limareas.Count; i++)
            {
                //原来斜边长度
                pd = Math.Pow(Math.Pow(Math.Abs(limareas[i].startpoint.x - oldcenterx), 2) + Math.Pow(Math.Abs(limareas[i].startpoint.y - oldcentery), 2), 0.5);
                if (pd <= 0)
                {
                    continue;
                }
                cosx = (double)(limareas[i].startpoint.x - oldcenterx) / pd;
                siny = (double)(limareas[i].startpoint.y - oldcentery) / pd;
                //放大缩小后的斜边长度
                pd = pd * k;
                //计算移动坐标后开始坐标
                limareas[i].startpoint.x = newcenterx + pd * cosx;
                limareas[i].startpoint.y = newcentery + pd * siny;
                //原来斜边长度
                pd = Math.Pow(Math.Pow(Math.Abs(limareas[i].endpoint.x - oldcenterx), 2) + Math.Pow(Math.Abs(limareas[i].endpoint.y - oldcentery), 2), 0.5);
                if (pd <= 0)
                {
                    continue;
                }
                cosx = (double)(limareas[i].endpoint.x - oldcenterx) / pd;
                siny = (double)(limareas[i].endpoint.y - oldcentery) / pd;
                //放大缩小后的斜边长度
                pd = pd * k;
                //计算移动坐标后开始坐标
                limareas[i].endpoint.x = newcenterx + pd * cosx;
                limareas[i].endpoint.y = newcentery + pd * siny;
            }

            List<Group> grps = Groups.Values.ToList<Group>();
            for (int i = 0; i < grps.Count; i++)
            {
                List<BsInfo> bsinfs = grps[i].groupbss.Values.ToList<BsInfo>();
                for (int j = 0; j < bsinfs.Count; j++)
                {
                    pd = Math.Pow(Math.Pow(Math.Abs(bsinfs[j].Place.x - oldcenterx), 2) + Math.Pow(Math.Abs(bsinfs[j].Place.y - oldcentery), 2), 0.5);
                    if (pd <= 0)
                    {
                        continue;
                    }
                    cosx = (double)(bsinfs[j].Place.x - oldcenterx) / pd;
                    siny = (double)(bsinfs[j].Place.y - oldcentery) / pd;
                    //放大缩小后的斜边长度
                    pd = pd * k;
                    //计算移动坐标后
                    bsinfs[j].Place.x = newcenterx + pd * cosx;
                    bsinfs[j].Place.y = newcentery + pd * siny;
                }
                List<LimitArea> mulliareas = grps[i].grouplimiares.Values.ToList<LimitArea>();
                for (int j = 0; j < mulliareas.Count; j ++)
                {
                    //原来斜边长度
                    pd = Math.Pow(Math.Pow(Math.Abs(mulliareas[j].startpoint.x - oldcenterx), 2) + Math.Pow(Math.Abs(mulliareas[j].startpoint.y - oldcentery), 2), 0.5);
                    if (pd <= 0)
                    {
                        continue;
                    }
                    cosx = (double)(mulliareas[j].startpoint.x - oldcenterx) / pd;
                    siny = (double)(mulliareas[j].startpoint.y - oldcentery) / pd;
                    //放大缩小后的斜边长度
                    pd = pd * k;
                    //计算移动坐标后开始坐标
                    mulliareas[j].startpoint.x = newcenterx + pd * cosx;
                    mulliareas[j].startpoint.y = newcentery + pd * siny;
                    //原来斜边长度
                    pd = Math.Pow(Math.Pow(Math.Abs(mulliareas[j].endpoint.x - oldcenterx), 2) + Math.Pow(Math.Abs(mulliareas[j].endpoint.y - oldcentery), 2), 0.5);
                    if (pd <= 0)
                    {
                        continue;
                    }
                    cosx = (double)(mulliareas[j].endpoint.x - oldcenterx) / pd;
                    siny = (double)(mulliareas[j].endpoint.y - oldcentery) / pd;
                    //放大缩小后的斜边长度
                    pd = pd * k;
                    //计算移动坐标后开始坐标
                    mulliareas[j].endpoint.x = newcenterx + pd * cosx;
                    mulliareas[j].endpoint.y = newcentery + pd * siny;
                }
            }
        }
        public void ReSetBounds()
        {
            //重新确定窗体的大小
            if (Parameter.ResolutionHeight <= 0 || Parameter.ResolutionWidth <= 0)
            {
                Parameter.ResolutionWidth = RealScreenWidth;
                Parameter.ResolutionHeight = RealScreenHeight;
                //保存当前的分辨率
                Ini.SetValue(Ini.ConfigPath, Ini.StrResolution, Ini.StrSolutionWidth, Parameter.ResolutionWidth + "");
                Ini.SetValue(Ini.ConfigPath, Ini.StrResolution, Ini.StrSolutionHeight, Parameter.ResolutionHeight + "");
            }
            SetFormBounds(Parameter.ResolutionWidth, Parameter.ResolutionHeight);
        }
        private void AddPort_Btn_Click(object sender, EventArgs e)
        {
            //向列表中添加新参考地点
            int count = Port_listView.Items.Count;
            string Space_Str = "0000";
            if (count > 0)
            {
                if (Space_Str.Equals(Port_listView.Items[count - 1].Text.ToString()))
                {
                    MessageBox.Show("Item has been added！");
                    return;
                }
            }
            ListViewItem item = new ListViewItem();
            item.Text = "0000"; item.Name = "0000";
            item.SubItems.Add("");
            item.SubItems.Add("");
            item.SubItems.Add("");
            Port_listView.Items.Add(item);
            //右侧列表中显示要增加的向
            PortID_TextBox.Text = "0000";
        }
        private int SelectGroup(string strgroupid)
        {
            string strgroup = "", strid = "";
            int start = 0, end = 0;
            for (int i = 0; i < SelectAreaCB.Items.Count; i++)
            {
                strgroup = SelectAreaCB.Items[i].ToString();
                if (strgroup.Length > 4)
                {
                    start = strgroup.LastIndexOf("(");
                    end = strgroup.LastIndexOf(")");
                    if (start >= 0 && end - start - 1 == 4)
                    {
                        strid = strgroup.Substring(start + 1, end - start - 1);
                        if (strid.Equals(strgroupid))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    if (strgroup.Equals(strgroupid))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case PrecisePositionLibrary.TPPID.WM_TAG_PACK:
                    if ((int)m.WParam == PrecisePositionLibrary.TPPID.WPARAM_TYPE)
                    {
                        PrecisePositionLibrary.PrecisePosition.TagPlace tp = (PrecisePositionLibrary.PrecisePosition.TagPlace)Marshal.PtrToStructure(m.LParam, typeof(PrecisePositionLibrary.PrecisePosition.TagPlace));
                        PrecisePositionLibrary.PrecisePosition.FreeHGLOBAL(m.LParam);
                        string StrTagID = tp.ID[0].ToString("X2") + tp.ID[1].ToString("X2");
                        CardImg copytag = null;
                        CardImg tag = null;
                        Tagmsg tgmsg = null;

                        if (CardImgs.TryGetValue(StrTagID, out tag))
                        {
                            if (null == tag)
                            {
                                return;
                            }
                            #region 获取设置Tag的名称
                            if (tgmsgs.TryGetValue(StrTagID, out tgmsg))
                            {
                                if (null != tgmsg.Name)
                                    tag.Name = tgmsg.Name;
                            }
                            #endregion
                            #region 获取抛上来的基站讯息
                            tag.ReportRouters.Clear();
                            if (tp.Dis1 > 0 && null != tp.ReferID1)
                            {
                                ReportRouterInfor rr1 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID1, 0, rr1.id, 0, 2);
                                rr1.dis = tp.Dis1;
                                rr1.SigQuality = tp.SigQuality1;
                                rr1.ResidualValue = tp.ResidualValue1;
                                tag.ReportRouters.Add(rr1);
                            }
                            if (tp.Dis2 > 0 && null != tp.ReferID2)
                            {
                                ReportRouterInfor rr2 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID2, 0, rr2.id, 0, 2);
                                rr2.dis = tp.Dis2;
                                rr2.SigQuality = tp.SigQuality2;
                                rr2.ResidualValue = tp.ResidualValue2;
                                tag.ReportRouters.Add(rr2);
                            }
                            if (tp.Dis3 > 0 && null != tp.ReferID3)
                            {
                                ReportRouterInfor rr3 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID3, 0, rr3.id, 0, 2);
                                rr3.dis = tp.Dis3;
                                rr3.SigQuality = tp.SigQuality3;
                                rr3.ResidualValue = tp.ResidualValue3;
                                tag.ReportRouters.Add(rr3);
                            
                            }
                            if (tp.Dis4 > 0 && null != tp.ReferID4)
                            {
                                ReportRouterInfor rr4 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID4, 0, rr4.id, 0, 2);
                                rr4.dis = tp.Dis4;
                                rr4.SigQuality = tp.SigQuality4;
                                rr4.ResidualValue = tp.ResidualValue4;
                                tag.ReportRouters.Add(rr4);
                            }
                            if (tp.Dis5 > 0 && null != tp.ReferID5)
                            {
                                ReportRouterInfor rr5 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID5, 0, rr5.id, 0, 2);
                                rr5.dis = tp.Dis5;
                                rr5.SigQuality = tp.SigQuality5;
                                rr5.ResidualValue = tp.ResidualValue5;
                                tag.ReportRouters.Add(rr5);
                            }
                            #endregion
                            #region 计算Tag丢包率
                            if (tp.index-tag.Index>0)
                            {
                                tag.LossPack += (tp.index - tag.Index - 1);
                            }
                            else if (tp.index - tag.Index<0)
                            {
                                tag.LossPack += (Byte.MaxValue - tag.Index + tp.index);
                            }
                            #endregion
                            #region 获取Tag讯息
                            tag.Index = tp.index;
                            tag.No_Exe_Time = tp.NoExeTime;
                            tag.St = tp.SleepTime;
                            tag.LocaType = tp.LocalType;
                            if (tag.LocaType == TPPID.TagWarmLocal && tag.St < 1000)
                            {
                                tag.isShowRed = true;
                                tag.ShowRedTick = Environment.TickCount;
                            }
                            #endregion
                            // 此时的X、Y是Tag的实际坐标
                            if (isStart)
                            {

                                #region 计算真实坐标经过卡尔曼滤波后的坐标
                                if (Parameter.isKalman)
                                {
                                    /* 
                                     * 这里需要我们做出判断上一次定位所在区域和当前的区域是否相同，不相同的话，我们要重新开始滤波
                                     */
                                    if (tp.GroupID[0] == tag.GroupID[0] && tp.GroupID[1] == tag.GroupID[1])
                                    {//两次组别不相同,要重新开始滤波
                                        if (tag.curkalmanX == null || tag.curkalmanY == null || tag.curkalmanZ == null)
                                        {
                                            tag.curkalmanX = new Kalman(tp.X, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                            tag.curkalmanY = new Kalman(tp.Y, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                            tag.curkalmanZ = new Kalman(tp.Z, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                            tag.KalmanPoint.x = tp.X;
                                            tag.KalmanPoint.y = tp.Y;
                                            tag.KalmanPoint.z = tp.Z;
                                        }
                                        else//用卡尔曼滤波算法优化坐标点
                                        {
                                            tag.KalmanPoint.x = tag.curkalmanX.Kalman_Filter(tp.X);
                                            tag.KalmanPoint.y = tag.curkalmanY.Kalman_Filter(tp.Y);
                                            if (tp.Z == Double.MinValue)
                                            {
                                                tag.KalmanPoint.z = tp.Z;
                                            }
                                            else
                                            {
                                                tag.KalmanPoint.z = tag.curkalmanZ.Kalman_Filter(tp.Z);
                                            }
                                        }
                                    }
                                    else 
                                    {
                                        tag.curkalmanX = new Kalman(tp.X, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                        tag.curkalmanY = new Kalman(tp.Y, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                        tag.curkalmanZ = new Kalman(tp.Z, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                        tag.KalmanPoint.x = tp.X;
                                        tag.KalmanPoint.y = tp.Y;
                                        tag.KalmanPoint.z = tp.Z;
                                    }
                                }
                                #endregion
                                #region 获取Tag的前面几次的坐标
                                if (null == tag.CardPoint1)
                                {//判断第一个轨迹点
                                    if (Parameter.isKalman)
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tag.KalmanPoint.x, tag.KalmanPoint.y,tag.KalmanPoint.z);
                                    }
                                    else
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tp.X, tp.Y,tp.Z);
                                    }
                                }
                                else if (null == tag.CardPoint2)
                                {//判断第二个轨迹点
                                    tag.CardPoint2 = new PrecisePositionLibrary.Point(tag.CardPoint1.x, tag.CardPoint1.y, tag.CardPoint1.z);
                                    if (Parameter.isKalman)
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tag.KalmanPoint.x, tag.KalmanPoint.y, tag.KalmanPoint.z);
                                    }
                                    else
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tp.X, tp.Y, tp.Z);
                                    }
                                }
                                else if (null == tag.CardPoint3)
                                {
                                    tag.CardPoint3 = new PrecisePositionLibrary.Point(tag.CardPoint2.x, tag.CardPoint2.y, tag.CardPoint2.z);
                                    tag.CardPoint2 = new Point(tag.CardPoint1.x, tag.CardPoint1.y, tag.CardPoint1.z);
                                    if (Parameter.isKalman)
                                    {
                                        tag.CardPoint1 = new Point(tag.KalmanPoint.x, tag.KalmanPoint.y, tag.KalmanPoint.z);
                                    }
                                    else
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tp.X, tp.Y, tp.Z);
                                    }
                                }
                                else if (null == tag.CardPoint4)
                                {
                                    tag.CardPoint4 = new Point(tag.CardPoint3.x, tag.CardPoint3.y, tag.CardPoint3.z);
                                    tag.CardPoint3 = new Point(tag.CardPoint2.x, tag.CardPoint2.y, tag.CardPoint2.z);
                                    tag.CardPoint2 = new Point(tag.CardPoint1.x, tag.CardPoint1.y, tag.CardPoint1.z);
                                    if (Parameter.isKalman)
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tag.KalmanPoint.x, tag.KalmanPoint.y, tag.KalmanPoint.z);
                                    }
                                    else
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tp.X, tp.Y, tp.Z);
                                    }
                                }
                                else
                                {
                                    tag.CardPoint5 = new Point(tag.CardPoint4.x, tag.CardPoint4.y, tag.CardPoint4.z);
                                    tag.CardPoint4 = new Point(tag.CardPoint3.x, tag.CardPoint3.y, tag.CardPoint3.z);
                                    tag.CardPoint3 = new Point(tag.CardPoint2.x, tag.CardPoint2.y, tag.CardPoint2.z);
                                    tag.CardPoint2 = new Point(tag.CardPoint1.x, tag.CardPoint1.y, tag.CardPoint1.z);
                                    if (Parameter.isKalman)
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tag.KalmanPoint.x, tag.KalmanPoint.y, tag.KalmanPoint.z);
                                    }
                                    else
                                    {
                                        tag.CardPoint1 = new PrecisePositionLibrary.Point(tp.X, tp.Y, tp.Z);
                                    }
                                }
                                #endregion
                            }
                            #region 获取Tag的讯息，包括组别,实际坐标、电量、接收时间、总包数等
                            if(isTrace)
                            {
                                if ((tp.ID[0].ToString("X2") + tp.ID[1].ToString("X2")).Equals(TraceTagId))
                                {
                                    string strid = SelectAreaCB.SelectedItem.ToString();
                                    int start = strid.LastIndexOf("(");
                                    int end = strid.LastIndexOf(")");
                                    if (start >= 0 && end - start - 1 == 4)
                                    {//说明当前选择的组别包含ID
                                        string strgroupid = strid.Substring(start + 1, end - start - 1);
                                        strid = tp.GroupID[0].ToString("X2") + tp.GroupID[1].ToString("X2");
                                        if (!strgroupid.Equals(strid))
                                        {//需要我们切换地图
                                            int index = SelectGroup(strid);
                                            if(index >= 0)
                                            {
                                                SelectAreaCB.SelectedIndex = index;
                                            }
                                        }
                                    }
                                }
                            }
                            System.Buffer.BlockCopy(tp.GroupID, 0, tag.GroupID, 0, 2);
                            tag.CardPoint = new Point(tp.X,tp.Y,tp.Z);
                            tag.Battery = tp.Battery;
                            tag.ReceiTime = DateTime.Now;
                            tag.TotalPack ++;
                            tag.isLowBattery = false;
                            tag.isTimeOutReceiveWarm = false;
                        
                            tag.GsensorX = tp.GsensorX;
                            tag.GsensorY = tp.GsensorY;
                            tag.GsensorZ = tp.GsensorZ;
                            #endregion
                            if (isStart)
                            {
                                #region 是否产生警报讯息（低电量、长时间不移动）
                                if (Parameter.RecordBatteryLessCard)
                                {
                                    if (tag.Battery <= Parameter.LowBattry)
                                    { 
                                        tag.isLowBattery = true; 
                                    }
                                    else
                                    {
                                        if (tag.isLowBatteryWarm)
                                        {
                                            tag.isLowBatteryWarm = false;
                                        }
                                    }
                                }
                                if (tag.No_Exe_Time > Parameter.OverTime2 && Parameter.LongTime_NoExe_ToBlackShow)
                                {
                                    tag.isOverNoMove = true;
                                }
                                else
                                {
                                    tag.isOverNoMove = false;
                                }
                                #endregion
                                #region 动态生成CheckBox
                                if (CurReportMode == ReportMode.ImgMode)
                                {
                                    AddCheckBox_Dynamic(tag);
                                }
                                #endregion 
                                #region
                                //每隔1s刷新一次
                                copytag = (CardImg)CopyObject(tag);
                                //判断是否应该保存当前的记录
                                if (isSaveRecord())
                                {
                                    System.Threading.Tasks.Task.Factory.StartNew(SaveRecord);
                                }
                                if (null != copytag)
                                {
                                    tpks.Enqueue(copytag);
                                }
                                #endregion
                            }
                            return;
                        }
                        //Tag数据包第一次抛上来
                        tag = new CardImg();
                        #region 保存tag的相关参数
                        Array.Copy(tp.ID,0,tag.ID,0,2);
                        if (tgmsgs.TryGetValue(StrTagID, out tgmsg))
                        {
                            if (null != tgmsg.Name)
                            {
                                tag.Name = tgmsg.Name;
                            }
                        }
                        if (tp.Dis1 > 0 && null != tp.ReferID1)
                        {
                            ReportRouterInfor rr1 = new ReportRouterInfor();
                            System.Buffer.BlockCopy(tp.ReferID1, 0, rr1.id, 0, 2);
                            rr1.dis = tp.Dis1;
                            rr1.SigQuality = tp.SigQuality1;
                            rr1.ResidualValue = tp.ResidualValue1;
                            tag.ReportRouters.Add(rr1);
                        }
                        if (tp.Dis2 > 0 && null != tp.ReferID2)
                        {
                            ReportRouterInfor rr2 = new ReportRouterInfor();
                            System.Buffer.BlockCopy(tp.ReferID2, 0, rr2.id, 0, 2);
                            rr2.dis = tp.Dis2;
                            rr2.SigQuality = tp.SigQuality2;
                            rr2.ResidualValue = tp.ResidualValue2;
                            tag.ReportRouters.Add(rr2);
                        }
                        if (tp.Dis3 > 0 && null != tp.ReferID3)
                        {
                            ReportRouterInfor rr3 = new ReportRouterInfor();
                            System.Buffer.BlockCopy(tp.ReferID3, 0, rr3.id, 0, 2);
                            rr3.dis = tp.Dis3;
                            rr3.SigQuality = tp.SigQuality3;
                            rr3.ResidualValue = tp.ResidualValue3;
                            tag.ReportRouters.Add(rr3);
                        }
                        if (tp.Dis4 > 0 && null != tp.ReferID4)
                        {
                            ReportRouterInfor rr4 = new ReportRouterInfor();
                            System.Buffer.BlockCopy(tp.ReferID4, 0, rr4.id, 0, 2);
                            rr4.dis = tp.Dis4;
                            rr4.SigQuality = tp.SigQuality4;
                            rr4.ResidualValue = tp.ResidualValue4;
                            tag.ReportRouters.Add(rr4);
                        }
                        if (tp.Dis5 > 0 && null != tp.ReferID5)
                        {
                            ReportRouterInfor rr5 = new ReportRouterInfor();
                            System.Buffer.BlockCopy(tp.ReferID5, 0, rr5.id, 0, 2);
                            rr5.dis = tp.Dis5;
                            rr5.SigQuality = tp.SigQuality5;
                            rr5.ResidualValue = tp.ResidualValue5;
                            tag.ReportRouters.Add(rr5);
                        }
                        tag.Battery = tp.Battery;
                        tag.isTimeOutReceiveWarm = false;
                        tag.St = tp.SleepTime;
                        tag.Index = tp.index;
                        tag.CardPoint = new PrecisePositionLibrary.Point(tp.X,tp.Y,tp.Z);
                        tag.No_Exe_Time = tp.NoExeTime;
                        tag.ReceiTime = DateTime.Now;

                        tag.GsensorX = tp.GsensorX;
                        tag.GsensorY = tp.GsensorY;
                        tag.GsensorZ = tp.GsensorZ;


                        if (isTrace)
                        {
                            if ((tp.ID[0].ToString("X2") + tp.ID[1].ToString("X2")).Equals(TraceTagId))
                            {
                                string strid = SelectAreaCB.SelectedItem.ToString();
                                int start = strid.LastIndexOf("(");
                                int end = strid.LastIndexOf(")");
                                if (start >= 0 && end - start - 1 == 4)
                                {//说明当前选择的组别包含ID
                                    string strgroupid = strid.Substring(start + 1, end - start - 1);
                                    strid = tp.GroupID[0].ToString("X2") + tp.GroupID[1].ToString("X2");
                                    if (!strgroupid.Equals(strid))
                                    {//需要我们切换地图
                                        int index = SelectGroup(strid);
                                        if (index >= 0)
                                        {
                                            SelectAreaCB.SelectedIndex = index;
                                        }
                                    }
                                }
                            }
                        }
                        System.Buffer.BlockCopy(tp.GroupID, 0,tag.GroupID, 0, 2);
                        tag.TotalPack = 1;
                        tag.LossPack = 0;
                        if (Parameter.RecordBatteryLessCard)
                        {
                            if (tag.Battery <= Parameter.LowBattry)
                            {
                                tag.isLowBattery = true;
                            }
                        }
                        if (tag.No_Exe_Time > Parameter.OverTime2 && Parameter.LongTime_NoExe_ToBlackShow)
                        {
                            tag.isOverNoMove = true;
                        }
                        else
                        {
                            tag.isOverNoMove = false;
                        }
                        #endregion
                        if (isStart)
                        {
                            #region 生成卡尔曼滤波坐标
                            if (Parameter.isKalman)
                            {
                                tag.curkalmanX = new Kalman(tp.X, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                tag.curkalmanY = new Kalman(tp.Y, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                tag.curkalmanZ = new Kalman(tp.Z, Parameter.KalmanMNosieCovar, Parameter.KalmanProNosieCovar, Parameter.KalmanLastStatePre);
                                tag.KalmanPoint.x = tp.X;
                                tag.KalmanPoint.y = tp.Y;
                                tag.KalmanPoint.z = tp.Z;
                            }
                            #endregion
                        }
                        CardImgs.TryAdd(StrTagID,tag);
                        if (isStart)
                        {
                            #region 动态生成CheckBox
                            if (CurReportMode == ReportMode.ImgMode)
                            {
                                AddCheckBox_Dynamic(tag);
                            }
                            #endregion
                            #region 保存当前的记录
                            copytag = (CardImg)CopyObject(tag);
                            
                            if (isSaveRecord())
                            {
                                System.Threading.Tasks.Task.Factory.StartNew(SaveRecord);
                            }
                            if (null != copytag)
                            {
                                tpks.Enqueue(copytag);
                            }
                            #endregion
                        }
                    } else if ((int)m.WParam == PrecisePositionLibrary.TPPID.WREFERSMSG_TYPE){
                        PrecisePositionLibrary.PrecisePosition.TagPlace tp = (PrecisePositionLibrary.PrecisePosition.TagPlace)
                                            Marshal.PtrToStructure(m.LParam, typeof(PrecisePositionLibrary.PrecisePosition.TagPlace));
                        PrecisePositionLibrary.PrecisePosition.FreeHGLOBAL(m.LParam);
                        String tagId = tp.ID[0].ToString("X2") + tp.ID[1].ToString("X2");
                        CardImg cardImg = null;
                        Tagmsg tagMsg = null;
                        if (CardImgs.TryGetValue(tagId, out cardImg))
                        {
                            if (tgmsgs.TryGetValue(tagId, out tagMsg))
                            {
                                if (null != tagMsg.Name)
                                {
                                    cardImg.Name = tagMsg.Name;
                                }
                            }
                            cardImg.ReportRouters.Clear();
                            if (tp.Dis1 > 0 && null != tp.ReferID1)
                            {
                                ReportRouterInfor rr1 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID1, 0, rr1.id, 0, 2);
                                rr1.dis = tp.Dis1;
                                rr1.SigQuality = tp.SigQuality1;
                                rr1.ResidualValue = tp.ResidualValue1;
                                cardImg.ReportRouters.Add(rr1);
                            }
                            if (tp.Dis2 > 0 && null != tp.ReferID2)
                            {
                                ReportRouterInfor rr2 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID2, 0, rr2.id, 0, 2);
                                rr2.dis = tp.Dis2;
                                rr2.SigQuality = tp.SigQuality2;
                                rr2.ResidualValue = tp.ResidualValue2;
                                cardImg.ReportRouters.Add(rr2);
                            }
                            cardImg.Battery = tp.Battery;
                            cardImg.isTimeOutReceiveWarm = false;
                            cardImg.St = tp.SleepTime;
                            cardImg.Index = tp.index;
                            cardImg.GroupID = tp.GroupID;
                            cardImg.LocaType = tp.LocalType;
                            if (cardImg.LocaType == TPPID.TagWarmLocal && cardImg.St < 1000)
                            {
                                cardImg.isShowRed = true;
                                cardImg.ShowRedTick = Environment.TickCount;
                            }
                            cardImg.No_Exe_Time = tp.NoExeTime;
                            cardImg.ReceiTime = DateTime.Now;
                        }
                        else
                        {
                            cardImg = new CardImg();
                            System.Array.Copy(tp.ID, 0, cardImg.ID, 0, 2);
                            Array.Copy(tp.ID, 0, tp.ID, 0, 2);
                            if (tgmsgs.TryGetValue(tagId, out tagMsg))
                            {
                                if (null != tagMsg.Name)
                                {
                                    cardImg.Name = tagMsg.Name;
                                }
                            }
                            if (tp.Dis1 > 0 && null != tp.ReferID1)
                            {
                                ReportRouterInfor rr1 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID1, 0, rr1.id, 0, 2);
                                rr1.dis = tp.Dis1;
                                rr1.SigQuality = tp.SigQuality1;
                                rr1.ResidualValue = tp.ResidualValue1;
                                cardImg.ReportRouters.Add(rr1);
                            }
                            if (tp.Dis2 > 0 && null != tp.ReferID2)
                            {
                                ReportRouterInfor rr2 = new ReportRouterInfor();
                                System.Buffer.BlockCopy(tp.ReferID2, 0, rr2.id, 0, 2);
                                rr2.dis = tp.Dis2;
                                rr2.SigQuality = tp.SigQuality2;
                                rr2.ResidualValue = tp.ResidualValue2;
                                cardImg.ReportRouters.Add(rr2);
                            }
                            cardImg.Battery = tp.Battery;
                            cardImg.isTimeOutReceiveWarm = false;
                            cardImg.St = tp.SleepTime;
                            cardImg.GroupID = tp.GroupID;
                            cardImg.Index = tp.index;
                            cardImg.CardPoint = new PrecisePositionLibrary.Point(tp.X, tp.Y, tp.Z);
                            cardImg.No_Exe_Time = tp.NoExeTime;
                            if (cardImg.LocaType == TPPID.TagWarmLocal && cardImg.St < 1000)
                            {
                                cardImg.isShowRed = true;
                                cardImg.ShowRedTick = Environment.TickCount;
                            }
                            cardImg.ReceiTime = DateTime.Now;
                            CardImgs.TryAdd(tagId, cardImg);
                            if (CurReportMode == ReportMode.ImgMode)
                            {
                                AddCheckBox_Dynamic(cardImg);
                            }
                        }
                    } else if((int)m.WParam == PrecisePositionLibrary.TPPID.WBASIC_TYPE) {
                        PrecisePositionLibrary.PrecisePosition.BasicReport br = (PrecisePositionLibrary.PrecisePosition.BasicReport)Marshal.PtrToStructure(m.LParam, typeof(PrecisePositionLibrary.PrecisePosition.BasicReport));
                        PrecisePositionLibrary.PrecisePosition.FreeHGLOBAL(m.LParam);
                        PortInfor pt = null;
                        string StrPortID = br.ID[0].ToString("X2") + br.ID[1].ToString("X2");
                        if (Ports.TryGetValue(StrPortID, out pt))
                        {
                            pt.ReportTime = DateTime.Now;
                            pt.ver = br.Version;
                            pt.sleep = br.SleepTime;
                            break;
                        }
                        pt = new PortInfor();
                        System.Buffer.BlockCopy(br.ID, 0, pt.PortID, 0, 2);
                        pt.ReportTime = DateTime.Now;
                        pt.ver = br.Version;
                        //pt.sleep = br.SleepTime;
                        pt.sleep = 30;
                        //Console.WriteLine("pt.sleep");
                        //Console.WriteLine(pt.sleep);
                        Ports.Add(StrPortID, pt);
                    }
                    break;
                case PrecisePositionLibrary.TPPID.WM_VERSION_ERR:
                    byte[] Id = new byte[2];
                    int IntId = (int)m.LParam;
                    Id[1] = (byte)IntId;
                    Id[0] = (byte)(IntId >> 8);
                    if ((int)m.WParam == PrecisePositionLibrary.TPPID.WBIGVERSION)
                    {
                        Alarminfor_textBox.Visible = true;
                        StringBuilder strbuilder = new StringBuilder("Warning message: The large version of the ");
                        strbuilder.Append(Id[0].ToString("X2") + Id[1].ToString("X2"));
                        strbuilder.Append(" base station is different!");
                        if(isStart && !IsCnn)
                        {
                            AlarmInfors.Add(strbuilder.ToString());
                            Alarminfor_textBox.Text = strbuilder.ToString();
                        }
                        else if (!isStart && IsCnn)
                        {
                            WarmTxt.Visible = true;
                            WarmTxt.Text = strbuilder.ToString();
                        }
                    }
                    else if ((int)m.WParam == PrecisePositionLibrary.TPPID.WSMALLVERSION)
                    {
                        if (isStart && !IsCnn)
                        {

                        }
                        else if (!isStart && IsCnn)
                        {
                         
                        }
                    }
                    break;
                case PrecisePositionLibrary.TPPID.WM_DEVICE_DIS:
                    if ((int)m.WParam == PrecisePositionLibrary.TPPID.WBASIC_TYPE)
                    {//基站类型
                       PrecisePositionLibrary.PrecisePosition.BasicReport obj = (PrecisePositionLibrary.PrecisePosition.BasicReport)Marshal.PtrToStructure(m.LParam, typeof(PrecisePositionLibrary.PrecisePosition.BasicReport));
                       PrecisePositionLibrary.PrecisePosition.FreeHGLOBAL(m.LParam);
                    }
                    else if ((int)m.WParam == PrecisePositionLibrary.TPPID.WTAG_TYPE)
                    {//Tag类型
                        PrecisePositionLibrary.PrecisePosition.TagPlace obj = (PrecisePositionLibrary.PrecisePosition.TagPlace)Marshal.PtrToStructure(m.LParam, typeof(PrecisePositionLibrary.PrecisePosition.TagPlace));
                        PrecisePositionLibrary.PrecisePosition.FreeHGLOBAL(m.LParam);
                    }
                    break;
                //自定义数据上报
                case PrecisePositionLibrary.TPPID.WM_CUSTOM_PACK:
                    if ((int)m.WParam == PrecisePositionLibrary.TPPID.WCUSTOM_TYPE)
                    {
                        PrecisePositionLibrary.PrecisePosition.CustomPacketReport cus = (PrecisePositionLibrary.PrecisePosition.CustomPacketReport)Marshal.PtrToStructure(m.LParam, typeof(PrecisePositionLibrary.PrecisePosition.CustomPacketReport));
                        PrecisePositionLibrary.PrecisePosition.FreeHGLOBAL(m.LParam);                        
                        lock (LockListCustomData)
                        {
                            bool isNeedNotDeal = false;
                            if (ListCustomData.Count > 0)
                            {
                                //几个基站都上报同一封包，只处理一个
                                if (ListCustomData[ListCustomData.Count - 1].AnchorId[0] == cus.AnchorId[0] && ListCustomData[ListCustomData.Count - 1].AnchorId[1] == cus.AnchorId[1] &&
                                   ListCustomData[ListCustomData.Count - 1].TagId[0] == cus.TagId[0] && ListCustomData[ListCustomData.Count - 1].TagId[1] == cus.TagId[1] &&
                                   ListCustomData[ListCustomData.Count - 1].Data[0] == cus.Data[0] && ListCustomData[ListCustomData.Count - 1].Data[1] == cus.Data[1] &&
                                   ListCustomData[ListCustomData.Count - 1].Data[2] == cus.Data[2] && ListCustomData[ListCustomData.Count - 1].Data[3] == cus.Data[3] &&
                                   ListCustomData[ListCustomData.Count - 1].Data[4] == cus.Data[4] && ListCustomData[ListCustomData.Count - 1].Data[5] == cus.Data[5] &&
                                   ListCustomData[ListCustomData.Count - 1].SerialNum == cus.SerialNum &&
                                   Environment.TickCount - ListCustomData[ListCustomData.Count - 1].TickCount < 1000)
                                {
                                    isNeedNotDeal = true;
                                }

                            }
                            if (isNeedNotDeal == false)
                            {
                                CustomPacket customData = new CustomPacket();
                                customData.AnchorId[0] = cus.AnchorId[0];
                                customData.AnchorId[1] = cus.AnchorId[1];
                                customData.TagId[0] = cus.TagId[0];
                                customData.TagId[1] = cus.TagId[1];
                                customData.SerialNum = cus.SerialNum;
                                customData.Data[0] = cus.Data[0];
                                customData.Data[1] = cus.Data[1];
                                customData.Data[2] = cus.Data[2];
                                customData.Data[3] = cus.Data[3];
                                customData.Data[4] = cus.Data[4];
                                customData.Data[5] = cus.Data[5];
                                customData.TickCount = cus.TickCount;
                                customData.ReportTime = DateTime.Now.ToString("yy-MM-dd") + " " + DateTime.Now.ToLongTimeString().ToString();
                                ListCustomData.Add(customData);
                            }
                        }
                    }
                    break;
                default:
                    base.WndProc(ref m);
                break;
            }
            
        }
        private bool isSaveRecord()
        {
            //获得当前的年月日
            DateTime CurDT = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            CardImg lastpk = null;
            if (tpks.TryPeek(out lastpk))
            {//获取上一次的数据包的年月日
                DateTime lastDT = new DateTime(lastpk.ReceiTime.Year, lastpk.ReceiTime.Month, lastpk.ReceiTime.Day, lastpk.ReceiTime.Hour, 0, 0);
                if ((CurDT - lastDT).TotalHours >= 1)
                {//发现这一次上报（年月日）的数据包比上一次多了一个小时，开始保存记录
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 深度拷贝对象
        /// </summary>
        /// <param name="obj"></param>
        public static Object CopyObject(Object obj)
        {
            Object CloneObj = null;
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                ms.Seek(0,SeekOrigin.Begin);
                CloneObj = bf.Deserialize(ms);
            }
            catch (Exception)
            {}
            finally 
            {
                if (null != ms) ms.Close();
            }
            return CloneObj;
        }
        /// <summary>
        /// 保存单条记录
        /// </summary>
        /// <param name="tg"></param>
        private void SaveSingleRecord(object tg)
        {
            CardImg tag = tg as CardImg;
            if (null==tag)return;
            //判断保存记录的Record文件是否存在，若不存在的话就创建该文件夹
            if (!Directory.Exists(Parameter.RecordDir)) Directory.CreateDirectory(Parameter.RecordDir);
            //生成该包记录的日期文件名称
            string strdt = tag.ReceiTime.Year.ToString().PadLeft(4,'0') + 
                           tag.ReceiTime.Month.ToString().PadLeft(2,'0') + 
                           tag.ReceiTime.Day.ToString().PadLeft(2,'0');
            //判断日期文件名称的文件夹是否存在
            if (!Directory.Exists(Parameter.RecordDir+"\\"+strdt))Directory.CreateDirectory(Parameter.RecordDir+"\\"+strdt);
            //生成该包的小时文件
            string strhour = tag.ReceiTime.Hour + ".dat";
            //判断小时文件是否存在
            List<CardImg> tags = null;
            if (File.Exists(Parameter.RecordDir + "\\" + strdt + "\\" + strhour))
            {//文件存在
                object obj = null;
                DeserializeObject(out obj,Parameter.RecordDir+"\\"+strdt +"\\"+strhour);
                tags = obj as List<CardImg>;
                if (null != tags) tags.Add(tag);
                else return;
            }
            else
            {//不存在
                tags = new List<CardImg>();
                tags.Add(tag);
            }
            SeralizeObject(tags,Parameter.RecordDir + "\\" + strdt + "\\" + strhour);
        }
        /// <summary>
        /// 每隔一个小时保存一次数据记录
        /// </summary>
        private void SaveRecord()
        {
            //判断保存记录的文件夹是否存在，不存在的话就创建该文件夹
            if (!Directory.Exists(Parameter.RecordDir))
            {
                Directory.CreateDirectory(Parameter.RecordDir);
            }
            //获取文件夹中的所有目录（年+月+天）
            CardImg lastpk = null;
            if (!tpks.TryPeek(out lastpk))
            {
                return;
            }
            //判断最后一包时间的文件夹是否存在
            string StrDT = lastpk.ReceiTime.Year.ToString().PadLeft(4, '0') + lastpk.ReceiTime.Month.ToString().PadLeft(2, '0') + lastpk.ReceiTime.Day.ToString().PadLeft(2, '0');
            string StrHour = lastpk.ReceiTime.Hour.ToString().PadLeft(2, '0');
            List<CardImg> Oldtags = null;
            object obj = null;
            if (!Directory.Exists(Parameter.RecordDir + "\\" + StrDT))
            {//目录中不存在“年+月+天”的文件夹
                Directory.CreateDirectory(Parameter.RecordDir + "\\" + StrDT); 
                Oldtags = new List<CardImg>();
            }
            else
            {
                if (File.Exists(Parameter.RecordDir + "\\" + StrDT + "\\" + StrHour + ".dat"))
                {
                    DeserializeObject(out obj,Parameter.RecordDir + "\\" + StrDT + "\\" + StrHour + ".dat");
                }
            }
            if (null != obj)
            {
                Oldtags = obj as List<CardImg>;
            }
            if (null == Oldtags)
            {
                Oldtags = new List<CardImg>();
            }
            CardImg tk = null;
            while (tpks.Count > 0)
            {
                tpks.TryDequeue(out tk);
                if (null == tk)
                {
                    continue;
                }
                string StrDTH = tk.ReceiTime.Year.ToString().PadLeft(4, '0') + tk.ReceiTime.Month.ToString().PadLeft(2, '0') + tk.ReceiTime.Day.ToString().PadLeft(2, '0') + tk.ReceiTime.Hour.ToString().PadLeft(2, '0');
                if (StrDTH.Equals(StrDT + StrHour))
                {
                    Oldtags.Add(tk);
                }
            }
            SeralizeObject(Oldtags, Parameter.RecordDir + "\\" + StrDT + "\\" + StrHour + ".dat");
        }
        /// <summary>
        /// 删除节点列表中选中项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dele_Port_Btn_Click(object sender, EventArgs e)
        {
            //删除列表中选中项
            int count = Port_listView.SelectedItems.Count;
            if (count == 0)
            {
                MessageBox.Show("Please select items to delete！");
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Port_listView.Items.Remove(Port_listView.SelectedItems[i]);
            }
            Save_Btn_Click(null, null);
        }
        /// <summary>
        /// 更新右侧节点到左侧中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateLeftPort_Btn_Click(object sender, EventArgs e)
        {
            //将右侧的内容
            string StrID = PortID_TextBox.Text.Trim().ToUpper();
            string StrName = PortName_TextBox.Text.Trim();
            int ReferType = refertypeCb.SelectedIndex;
            string StrR = sgpintRTb.Text;
            double curR = -1;
            if (StrID == null)
            {
                MessageBox.Show("Reference point ID can not be empty！");
                return;
            }
            if (StrID.Equals(""))
            {
                MessageBox.Show("Reference point ID can not be empty！");
                return;
            }
            if (StrID.Length != 4)
            {
                MessageBox.Show("Reference point format is wrong!Such as::0A01");
                return;
            }
            if (StrID.Equals("0000"))
            {
                MessageBox.Show("Reference point ID not for 0000！");
                return;
            }
            if (ReferType == 1)
            {//说明当前设置的基站是单点模式
                if ("".Equals(StrR))
                {// 
                    MessageBox.Show("When single point mode, the base station scope cannot be empty！");
                    return;
                }
                try 
                {
                    curR = Convert.ToDouble(StrR);
                }catch(Exception)
                {
                    MessageBox.Show("When single point mode, base station scope format is wrong！");
                    return;
                }
                if (curR < 0 || curR > 1000)
                {
                    MessageBox.Show("When single point mode, the base station scope is greater than 0 and less than 1000cm");
                    return;
                }
            }
            int count = Port_listView.Items.Count;
            if (count <= 0)
            {
                MessageBox.Show("Please first click add reference point！");
                return;
            }
            byte[] PortID = new byte[2];
            try
            {
                PortID[0] = Convert.ToByte(StrID.Substring(0, 2), 16);
                PortID[1] = Convert.ToByte(StrID.Substring(2, 2), 16);
            }
            catch (Exception)
            {
                MessageBox.Show("Reference point format is wrong！");
                return;
            }
            bool isExit = false;
            //判断是否存在修改后项
            for (int k = 0; k < count; k++)
            {
                if (StrID.Equals(Port_listView.Items[k].Text.ToString()))
                {
                    isExit = true;
                }
            }
            string Space_Str = "0000";
            int i = 0;
            for (i = 0; i < count; i++)
            {
                if (Space_Str.Equals(Port_listView.Items[i].Text.ToString()))
                {
                    if (!isExit)
                    {
                        Port_listView.Items[i].Text = StrID;
                        Port_listView.Items[i].SubItems[1].Text = StrName;
                        if (ReferType == 1)
                       { 
                            Port_listView.Items[i].SubItems[2].Text = "1("+curR+"CM)";
                        }
                        else
                        {
                            Port_listView.Items[i].SubItems[2].Text = "3";
                        }
                    }
                    else
                    {
                        MessageBox.Show("Sorry, already exists" + StrID + "items in a list！");
                    }
                    break;
                }
            }
            if (i >= count)
            {//没有找到添加项，去修改选中的项
                int seleCount = Port_listView.SelectedItems.Count;
                if (seleCount <= 0)
                {
                    MessageBox.Show("Please first click add reference point！");
                    return;
                }
                if (!isExit)
                {
                    Port_listView.SelectedItems[0].Text = StrID;
                    Port_listView.SelectedItems[0].SubItems[1].Text = StrName;
                    if (ReferType == 1)
                    {
                        Port_listView.SelectedItems[0].SubItems[2].Text = "1(" + curR + "CM)";
                    }
                    else
                    {
                        Port_listView.SelectedItems[0].SubItems[2].Text = "3";
                    }
                }
                else
                {
                    Port_listView.SelectedItems[0].SubItems[1].Text = StrName;
                    if (ReferType == 1)
                    {
                        Port_listView.SelectedItems[0].SubItems[2].Text = "1(" + curR + "CM)";
                    }
                    else
                    {
                        Port_listView.SelectedItems[0].SubItems[2].Text = "3";
                    }
                }
            }
            Save_Btn_Click(null, null);
        }
        private void Port_listView_Click(object sender, EventArgs e)
        {

            int index,end;
            int count = Port_listView.SelectedItems.Count;
            if (count <= 0)
            {
                MessageBox.Show("Please select items to modify！");
                return;
            }
            PortID_TextBox.Text = Port_listView.SelectedItems[0].Text;
            PortName_TextBox.Text = Port_listView.SelectedItems[0].SubItems[1].Text;
            
            String strtype = Port_listView.SelectedItems[0].SubItems[2].Text;

            if ("3".Equals(strtype))
            {
                refertypeCb.SelectedIndex = 0;
            }
            else
            {
                refertypeCb.SelectedIndex = 1;
                index = strtype.IndexOf("(");
                end = strtype.IndexOf("CM");
                try
                {
                    sgpintRTb.Text = strtype.Substring(index + 1, end - index - 1);
                }catch(Exception)
                {
                    sgpintRTb.Text = "500";
                }
            }
        }
        private void Save_Btn_Click(object sender, EventArgs e)
        {
            //将列表保存到.Ini配置文件中
            string strinfor = "";
            int index = -1,end = -1;
            int count = Port_listView.Items.Count;
            if (Ini.Clear(Ini.PortPath))
            {
                for (int i = 0; i < count; i++)
                {
                    Ini.SetValue(Ini.PortPath, Port_listView.Items[i].Text.ToString(), Ini.Name, Port_listView.Items[i].SubItems[1].Text.ToString());
                    strinfor = Port_listView.Items[i].SubItems[2].Text.ToString();
                    index = strinfor.IndexOf("(");
                    if (index < 0)
                    {//说明是3点定位
                        Ini.SetValue(Ini.PortPath, Port_listView.Items[i].Text.ToString(), Ini.Type,"3");
                    }
                    else
                    {
                        end = strinfor.IndexOf("CM");
                        Ini.SetValue(Ini.PortPath, Port_listView.Items[i].Text.ToString(), Ini.Type,"1");
                        try
                        {
                            Ini.SetValue(Ini.PortPath, Port_listView.Items[i].Text.ToString(), Ini.Range, strinfor.Substring(index + 1, end - index - 1));
                        }catch(Exception)
                        {
                            Ini.SetValue(Ini.PortPath, Port_listView.Items[i].Text.ToString(), Ini.Range,"500");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please save the list！");
            }
        }
        /// <summary>
        ///  加载参考点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReUpdatePort_Btn_Click(object sender, EventArgs e)
        {
            UpLoadPortList();
        }
        /// <summary>
        /// 加载限制区域
        /// </summary>
        public void LoadLimitAreas()
        {
            List<string> lists = null;
            LimitArea marea = null;
            byte[] ID = null;
            string StrStartX, StrStartY, StrEndX, StrEndY;
            if (Ini.GetAllSegment(Ini.SaveLimitsAreaPath, out lists))
            {
                for (int i = 0; i < lists.Count; i++)
                {
                    if (lists[i] != null && !"".Equals(lists[i]))
                    {
                        marea = new LimitArea();
                        try
                        {
                            ID = new byte[2];
                            ID[0] = Convert.ToByte(lists[i].Substring(0, 2), 16);
                            ID[1] = Convert.ToByte(lists[i].Substring(2, 2), 16);
                        }
                        catch (Exception)
                        {

                        }
                        marea.ID = ID;
                        marea.Name = Ini.GetValue(Ini.SaveLimitsAreaPath, lists[i], Ini.Name);

                        StrStartX = Ini.GetValue(Ini.SaveLimitsAreaPath, lists[i], Ini.LimitStartX);
                        StrStartY = Ini.GetValue(Ini.SaveLimitsAreaPath, lists[i], Ini.LimitStartY);

                        StrEndX = Ini.GetValue(Ini.SaveLimitsAreaPath, lists[i], Ini.LimitEndX);
                        StrEndY = Ini.GetValue(Ini.SaveLimitsAreaPath, lists[i], Ini.LimitEndY);

                        try
                        {
                            marea.startpoint.x = Convert.ToDouble(StrStartX);
                            marea.startpoint.y = Convert.ToDouble(StrStartY);
                            marea.endpoint.x = Convert.ToDouble(StrEndX);
                            marea.endpoint.y = Convert.ToDouble(StrEndY);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        Areas.TryAdd(ID[0].ToString("X2") + ID[1].ToString("X2"), marea);
                    }
                }
            }
        }
        /// <summary>
        /// 加载参考点讯息
        /// </summary>
        public void LoadPorts()
        {
            List<string> lists = null;
            PrecisePositionLibrary.BsInfo port = null;
            byte[] ID = null;
            string StrX, StrY, StrZ;
            if (Ini.GetAllSegment(Ini.SavePortsPath, out lists))
            {
                for (int i = 0; i < lists.Count; i++)
                {
                    if (lists[i] != null)
                    {
                        if (!lists[i].Equals(""))
                        {
                            port = new PrecisePositionLibrary.BsInfo();
                            try
                            {
                                ID = new byte[2];
                                ID[0] = Convert.ToByte(lists[i].Substring(0, 2), 16);
                                ID[1] = Convert.ToByte(lists[i].Substring(2, 2), 16);
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                            port.ID = ID;
                            StrX = Ini.GetValue(Ini.SavePortsPath, lists[i], Ini.Loca_X);
                            StrY = Ini.GetValue(Ini.SavePortsPath, lists[i], Ini.Loca_Y);
                            StrZ = Ini.GetValue(Ini.SavePortsPath, lists[i], Ini.Loca_Z);
                            if (StrX != null && StrY != null && StrZ != null && !"".Equals(StrX) && !"".Equals(StrY) && !"".Equals(StrZ))
                            {
                                try
                                {
                                    port.Place = new PrecisePositionLibrary.Point();
                                    port.Place.x = Convert.ToDouble(StrX);
                                    port.Place.y = Convert.ToDouble(StrY);
                                    port.Place.z = Convert.ToDouble(StrZ);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                InnerPorts.TryAdd(lists[i], port);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 刷新选择多个区域时的地图讯息
        /// </summary>
        public void RefreshMulAreaMap()
        { 
            if(Parameter.isSupportMulArea)
            {
                //获取当前获取的区域讯息
                String stritem = SelectAreaCB.Text;
                int index = stritem.LastIndexOf("(");
                if (index >= 0)
                {
                    stritem = stritem.Substring(index + 1, stritem.Length - index - 1);
                }
                //此时stritem是区域的ID
                Group group = null;
                if (!Groups.TryGetValue(stritem, out group))
                {
                    return;
                }
                
            }
        }
        /// <summary>
        /// 加载多个区域讯息
        /// </summary>
        public void LoadMulAreas()
        {
            List<String> strs = null;
            List<String> keyslist = null;
            // 加载所有的区域ID讯息
            if (!Ini.GetAllSegment(Ini.SaveMulAreaPath, out strs))
            {
                return;
            }
            Groups.Clear();
            string name = "", path = "", stractualwidth = "", stractualheight = "", strscale = "";
            byte[] id = null;
            float actualwidth = 0.0f, actualheight = 0.0f, scale = 0.0f;
            for (int i = 0; i < strs.Count; i++)
            {
                //获取ID
                if (null == strs[i] || "".Equals(strs[i]))
                {
                    continue;
                }
                if ("0000".Equals(strs[i]))
                {
                    continue;
                }
                if (strs[i].Length != 4)
                {
                    continue;
                }
                id = new byte[2];
                try
                {
                    id[0] = Convert.ToByte(strs[i].Substring(0, 2), 16);
                    id[1] = Convert.ToByte(strs[i].Substring(2, 2), 16);
                }
                catch (Exception)
                {
                    continue;
                }
                //获取名称
                name = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], Ini.Name);
                //获取地图路径
                path = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], Ini.MapKey_Path);
                //获取真实的宽度和高度
                stractualwidth = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], Ini.ActualWidth);
                stractualheight = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], Ini.ActualHeight);
                strscale = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], Ini.RealScale);
                if (null == stractualwidth || null == stractualheight)
                {
                    continue;
                }
                try
                {
                    actualwidth = Convert.ToSingle(stractualwidth);
                    actualheight = Convert.ToSingle(stractualheight);
                }
                catch (Exception)
                {
                    continue;
                }

                try
                {
                    scale = Convert.ToSingle(strscale);
                }catch(Exception)
                {
                    continue;
                }

                //获取所有基站讯息
                if (!Ini.GetAllKey(Ini.SaveMulAreaPath, strs[i], out keyslist))
                {
                    continue;
                }
                Group group = new Group();
                System.Buffer.BlockCopy(id, 0, group.id, 0, 2);
                group.name = name;
                group.grouppath = path;
                group.actualwidth = actualwidth;
                group.actualheight = actualheight;
                group.scale = scale;
                int index = 0;
                BsInfo bs = null;
                LimitArea liarea = null;
                String strbsid = "", strbsX = "", strbsY = "", strbsZ = "", strbsgroupid = "";
                String strareaid = "", strname = "", strstartx = "", strstarty = "", strendx = "", strendy = "";
                for (int j = 0; j < keyslist.Count; j++)
                {
                    if (null == keyslist[j] || "".Equals(keyslist[j]))
                    {
                        continue;
                    }
                    if (Ini.Name.Equals(keyslist[j]) || Ini.MapKey_Path.Equals(keyslist[j]))
                    {
                        continue;
                    }

                    if (keyslist[j].IndexOf(Ini.BsID_) >= 0)
                    { //说明是基站ID
                        strbsid = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strbsid || "".Equals(strbsid))
                        {//ID都获取失败了，舍弃当前的基站
                            bs = null;
                            continue;
                        }
                        if (strbsid.Length != 4)
                        {
                            bs = null;
                            continue;
                        }
                        if ("0000".Equals(strbsid))
                        {
                            bs = null;
                            continue;
                        }
                        byte[] bsid = new byte[2];
                        try
                        {
                            bsid[0] = Convert.ToByte(strbsid.Substring(0, 2), 16);
                            bsid[1] = Convert.ToByte(strbsid.Substring(2, 2), 16);
                        }
                        catch (Exception)
                        {
                            bs = null;
                            continue;
                        }
                        int tt = keyslist[j].IndexOf("_");
                        try
                        {
                            index = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        bs = new BsInfo();
                        System.Buffer.BlockCopy(bsid, 0, bs.ID, 0, 2);
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.BsX_) >= 0)
                    {//说明是基站的X坐标
                        if (null == bs)
                        {
                            continue;
                        }
                        strbsX = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strbsX || "".Equals(strbsX))
                        {
                            bs = null;
                            continue;
                        }
                        float x = 0f;
                        try
                        {
                            x = Convert.ToSingle(strbsX);
                        }
                        catch (Exception)
                        {
                            bs = null;
                            continue;
                        }

                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            bs.Place.x = x;
                        }
                        else
                        {
                            bs = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.BsY_) >= 0)
                    {//说明是基站的Y坐标
                        if (null == bs)
                        {
                            continue;
                        }
                        strbsY = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strbsY || "".Equals(strbsY))
                        {
                            bs = null;
                            continue;
                        }
                        float y = 0f;
                        try
                        {
                            y = Convert.ToSingle(strbsY);
                        }
                        catch (Exception)
                        {
                            bs = null;
                            continue;
                        }

                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            bs.Place.y = y;
                        }
                        else
                        {
                            bs = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.BsZ_) >= 0)
                    {//说明是基站的Z坐标
                        if (null == bs)
                        {
                            continue;
                        }
                        strbsZ = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strbsZ || "".Equals(strbsZ))
                        {
                            bs = null;
                            continue;
                        }
                        float z = 0f;
                        try
                        {
                            z = Convert.ToSingle(strbsZ);
                        }
                        catch (Exception)
                        {
                            bs = null;
                            continue;
                        }

                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            bs.Place.z = z;
                            
                        }
                        else
                        {
                            bs = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.BsGroupID_) >= 0)
                    {
                        if (null == bs)
                        {
                            continue;
                        }
                        strbsgroupid = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strbsgroupid || "".Equals(strbsgroupid))
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        if (strbsgroupid.Length != 4)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        byte[] groupid = new byte[2];
                        try 
                        { 
                            groupid[0] = Convert.ToByte(strbsgroupid.Substring(0,2),16);
                            groupid[1] = Convert.ToByte(strbsgroupid.Substring(2,2),16);
                        }catch(Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }

                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                        }
                        if (ke == index)
                        {
                            System.Buffer.BlockCopy(groupid, 0, bs.GroupID, 0, 2);
                            //将bs添加到集合中
                            group.groupbss.Add(bs.ID[0].ToString("X2") + bs.ID[1].ToString("X2"), bs);
                        }
                        else
                        {
                            bs = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.LimitAreaID_) >= 0)
                    {//说明存在基站
                        strareaid = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strareaid || "".Equals(strareaid))
                        {
                            liarea = null;
                            continue;
                        }
                        if (strareaid.Length != 4)
                        {
                            liarea = null;
                            continue;
                        }
                        id = new byte[2];
                        try
                        {
                            id[0] = Convert.ToByte(strareaid.Substring(0,2),16);
                            id[1] = Convert.ToByte(strareaid.Substring(2,2),16);
                        }catch(Exception)
                        {
                            liarea = null;
                            continue;
                        }

                        int tt = keyslist[j].IndexOf("_");
                        try
                        {
                            index = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        liarea = new LimitArea();
                        System.Buffer.BlockCopy(id, 0, liarea.ID, 0, 2);
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.LimitAreaName_) >= 0)
                    {
                        if (null == liarea)
                        {
                            continue;
                        }
                        strname = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strname)
                        {
                            continue;
                        }
                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            bs = null;
                            index = -1;
                            continue;
                        }
                        if (index == ke)
                        {
                            liarea.Name = strname;
                        }
                        else
                        {
                            liarea = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.LimitAreaStartX_) >= 0)
                    {
                        if (null == liarea)
                        {
                            continue;
                        }
                        strstartx = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strstartx || "".Equals(strstartx))
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        float x = 0.0f;
                        try
                        {
                            x = Convert.ToSingle(strstartx);
                        }catch(Exception)
                        {
                            index = -1;
                            liarea = null;
                            continue;
                        }
                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            liarea.startpoint.x = x;
                        }
                        else
                        {
                            liarea = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.LimitAreaStartY_) >= 0)
                    {
                        if (null == liarea)
                        {
                            continue;
                        }
                        strstarty = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strstarty || "".Equals(strstarty))
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        float y = 0.0f;
                        try
                        {
                            y = Convert.ToSingle(strstarty);
                        }
                        catch (Exception)
                        {
                            index = -1;
                            liarea = null;
                            continue;
                        }
                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            liarea.startpoint.y = y;
                        }
                        else
                        {
                            liarea = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.LimitAreaEndX_) >= 0)
                    {
                        if (null == liarea)
                        {
                            continue;
                        }
                        strendx = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strendx || "".Equals(strendx))
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        float x = 0.0f;
                        try
                        {
                            x = Convert.ToSingle(strendx);
                        }
                        catch (Exception)
                        {
                            index = -1;
                            liarea = null;
                            continue;
                        }
                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            liarea.endpoint.x = x;
                        }
                        else
                        {
                            liarea = null;
                            index = -1;
                        }
                        continue;
                    }
                    else if (keyslist[j].IndexOf(Ini.LimitAreaEndY_) >= 0)
                    {
                        if (null == liarea)
                        {
                            continue;
                        }
                        strendy = Ini.GetValue(Ini.SaveMulAreaPath, strs[i], keyslist[j]);
                        if (null == strendy || "".Equals(strendy))
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        float y = 0.0f;
                        try
                        {
                            y = Convert.ToSingle(strendy);
                        }
                        catch (Exception)
                        {
                            index = -1;
                            liarea = null;
                            continue;
                        }
                        int tt = keyslist[j].IndexOf("_");
                        int ke = -1;
                        try
                        {
                            ke = Convert.ToInt32(keyslist[j].Substring(tt + 1, keyslist[j].Length - tt - 1));
                        }
                        catch (Exception)
                        {
                            liarea = null;
                            index = -1;
                            continue;
                        }
                        if (ke == index)
                        {
                            liarea.endpoint.y = y;
                            group.grouplimiares.Add(liarea.ID[0].ToString("X2") + liarea.ID[1].ToString("X2"), liarea);
                        }
                        else
                        {
                            liarea = null;
                            index = -1;
                        }
                        continue;
                    }
                    bs = null;
                }
                Groups.TryAdd(group.id[0].ToString("X2") + group.id[1].ToString("X2"), group);
            }
        }
        /// <summary>
        ///  将参考点讯息加载到参考点列表中
        /// </summary>
        public void UpLoadPortList()
        {
            List<string> lists = null;
            String StrValue = "", StrType = "", StrRange = "";
            if (!Ini.GetAllSegment(Ini.PortPath, out lists))
            {
                return;
            }
            int index = 0;
            Port_listView.Items.Clear();
            for (int i = 0; i < lists.Count; i++)
            {
                if (lists[i] == null)
                {
                    return;
                }
                ListViewItem item = new ListViewItem();
                item.Text = lists[i];
                item.Name = lists[i];
                StrValue = Ini.GetValue(Ini.PortPath, lists[i], Ini.Name);
                if (StrValue != null && !"".Equals(StrValue))
                {
                    item.SubItems.Add(StrValue);
                }
                else
                {
                    item.SubItems.Add("");
                }
                //获取基站类型
                StrType = Ini.GetValue(Ini.PortPath, lists[i], Ini.Type);
                if (null != StrType && !"".Equals(StrType))
                {
                    if ("3".Equals(StrType))
                    {
                        item.SubItems.Add(StrType);
                    }
                    else
                    {
                        StrRange = Ini.GetValue(Ini.PortPath, lists[i], Ini.Range);
                        item.SubItems.Add(StrType + "(" + StrRange + "CM)");
                    }
                }
                else
                {
                    item.SubItems.Add("3");
                }
                Port_listView.Items.Add(item);
                index++;
            }
        }
        /// <summary>
        /// 添加卡片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCard_Btn_Click(object sender, EventArgs e)
        {
            //向列表中添加新卡片
            int count = Card_listView.Items.Count;
            string Space_Str = "0000";
            if (count > 0)
            {
                if (Space_Str.Equals(Card_listView.Items[count - 1].Text.ToString()))
                {
                    MessageBox.Show("Item has been added！");
                    return;
                }
            }
            ListViewItem item = new ListViewItem();
            item.Text = "0000"; item.Name = "0000";
            item.SubItems.Add(""); item.SubItems.Add("");
            Card_listView.Items.Add(item);
            //右侧列表中显示要增加的向
            CardID_TextBox.Text = "0000";
        }
        /// <summary>
        /// 删除卡片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dele_Card_Btn_Click(object sender, EventArgs e)
        {
            //删除列表中选中项
            int count = Card_listView.SelectedItems.Count;
            if (count == 0)
            {
                MessageBox.Show("Please select items to delete！");
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Card_listView.Items.Remove(Card_listView.SelectedItems[i]);
            }
            SaveCard_Btn_Click(null, null);
        }
        /// <summary>
        /// 卡片更新到列表中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateLeftCard_Btn_Click(object sender, EventArgs e)
        {
            String StrID = CardID_TextBox.Text.Trim().ToUpper();
            String StrName = CardName_TextBox.Text.Trim();
            String StrHeight = CardHeightTB.Text.Trim();

            if ("".Equals(StrHeight))
            {
                MessageBox.Show("The height of the card cannot be empty!");
                return;
            }
            double height = 0;
            try
            {
                height = Convert.ToDouble(StrHeight);
            }
            catch (Exception)
            {
                MessageBox.Show("The format of the card is wrong!");
                return;
            }

            if (StrID == null)
            {
                MessageBox.Show("ID card can not be empty！");
                return;
            }
            if (StrID.Equals(""))
            {
                MessageBox.Show("ID card can not be empty！");
                return;
            }
            if (StrID.Length != 4)
            {
                MessageBox.Show("ID card format is wrong!Such as：0A01");
                return;
            }
            if (StrID.Equals("0000"))
            {
                MessageBox.Show("ID CARDS for 0000！");
                return;
            }
            int count = Card_listView.Items.Count;
            if (count <= 0)
            {
                MessageBox.Show("Please click on add card first！");
                return;
            }
            Byte[] CardId = new Byte[2];
            try
            {
                CardId[0] = Convert.ToByte(StrID.Substring(0, 2), 16);
                CardId[1] = Convert.ToByte(StrID.Substring(2, 2), 16);
            }
            catch (Exception)
            {
                MessageBox.Show("ID card format is wrong！");
                return;
            }
            bool isExit = false;
            //判断是否存在修改后项
            for (int k = 0; k < count; k++)
            {
                if (StrID.Equals(Card_listView.Items[k].Text.ToString()))
                {
                    isExit = true;
                }
            }
            string Space_Str = "0000";
            int i = 0;
            for (i = 0; i < count; i++)
            {
                if (Space_Str.Equals(Card_listView.Items[i].Text.ToString()))
                {
                    if (!isExit)
                    {
                        Card_listView.Items[i].Text = StrID; Card_listView.Items[i].Name = StrID;
                        Card_listView.Items[i].SubItems[1].Text = StrName;
                        Card_listView.Items[i].SubItems[2].Text = StrHeight;
                    }
                    else
                    {
                        MessageBox.Show("Sorry，already exists" + StrID + "items in a list！");
                    }
                    break;
                }
            }
            if (i >= count)
            {//没有找到添加项，去修改选中的项
                int seleCount = Card_listView.SelectedItems.Count;
                if (seleCount <= 0)
                {
                    MessageBox.Show("Please click on add card first！");
                    return;
                }
                if (!isExit)
                {
                    Card_listView.SelectedItems[0].Text = StrID; Card_listView.Items[0].Name = StrID;
                    Card_listView.SelectedItems[0].SubItems[1].Text = StrName;
                    Card_listView.SelectedItems[0].SubItems[2].Text = StrHeight;
                }
                else
                {
                    Card_listView.SelectedItems[0].SubItems[1].Text = StrName;
                    Card_listView.SelectedItems[0].SubItems[2].Text = StrHeight;
                }
            }
            SaveCard_Btn_Click(null, null);
        }
        private void Card_listView_Click(object sender, EventArgs e)
        {
            //点击列表中的某项时，更新右侧
            int count = Card_listView.SelectedItems.Count;
            if (count <= 0)
            {MessageBox.Show("Please select items to modify！");return;}
            CardID_TextBox.Text = Card_listView.SelectedItems[0].Text;
            CardName_TextBox.Text = Card_listView.SelectedItems[0].SubItems[1].Text;
            CardHeightTB.Text = Card_listView.SelectedItems[0].SubItems[2].Text;
        }
        private void SaveCard_Btn_Click(object sender, EventArgs e){
            //将列表保存到.Ini配置文件中
            int count = Card_listView.Items.Count;
            if (Ini.Clear(Ini.CardPath))
            {
                for (int i = 0; i < count; i ++)
                {
                    Ini.SetValue(Ini.CardPath, Card_listView.Items[i].Text, Ini.Name, Card_listView.Items[i].SubItems[1].Text.ToString());
                    Ini.SetValue(Ini.CardPath, Card_listView.Items[i].Text, Ini.Height, Card_listView.Items[i].SubItems[2].Text.ToString());
                }
            }
            else
            {
                MessageBox.Show("Save the list fail！");
            }
        }
        private void ReUpdateCard_Btn_Click(object sender, EventArgs e)
        {
            UploadCardList();
        }
        private void UploadCardList() 
        {
            //将配置文件中的项加载到列表中
            List<string> lists = null;
            String StrValue = "";
            if (!Ini.GetAllSegment(Ini.CardPath, out lists))
            {
                return;
            }
            Card_listView.Items.Clear();
            int index = 0;
            for (int i = 0; i < lists.Count; i++)
            {
                if (null == lists[i])
                {
                    return;
                }
                ListViewItem item = new ListViewItem();
                item.Text = lists[i]; item.Name = lists[i];
                StrValue = Ini.GetValue(Ini.CardPath, lists[i], Ini.Name);
                if (null != StrValue && !"".Equals(StrValue))
                {
                    item.SubItems.Add(StrValue);
                }
                else
                {
                    item.SubItems.Add("");
                }
                StrValue = Ini.GetValue(Ini.CardPath, lists[i], Ini.Height);
                if (null != StrValue && !"".Equals(StrValue))
                {
                    item.SubItems.Add(StrValue);
                }
                else
                {
                    item.SubItems.Add("");
                }
                Card_listView.Items.Add(item);
                index++;
            }
        }
        public void TagPageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (TagPageBox.SelectedIndex)
            {
                case 0:
                    //基站类型
                    refertypeCb.SelectedIndex = 0;
                    UpLoadPortList();
                    break;
                case 1:
                    UploadCardList();
                    break;
                case 2:
                    //初始化讯息
                    SigOrderCb.SelectedIndex = ListSortMode;
                    CardList_listView.ListViewItemSorter = new ListViewCompareor();
                    ListIp_comboBox.Items.Clear();
                    String StrName = Dns.GetHostName();
                    IPAddress[] ips = Dns.GetHostAddresses(StrName);
                    foreach (IPAddress ip in ips)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            ListIp_comboBox.Items.Add(ip.ToString());
                    }
                    string StrIP = Ini.GetValue(Ini.ConfigPath, Ini.NetSeg, Ini.NetKey_IP);
                    string StrPort = Ini.GetValue(Ini.ConfigPath, Ini.NetSeg, Ini.NetKey_Port);
                    
                    
                    if (StrIP == null || StrPort == null)
                    {
                        ListIp_comboBox.SelectedIndex = 0;
                        return;
                    }
                    if (StrIP.Equals("") || StrPort.Equals(""))
                    {
                        ListIp_comboBox.SelectedIndex = 0;
                        return;
                    }
                    int Port = 0;
                    try
                    {
                        Port = Convert.ToInt32(StrPort);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    if (Port > 65535 || Port < 1025)
                    {
                        return;
                    }
                    int i = 0;
                    for (i = 0; i < ListIp_comboBox.Items.Count; i++)
                    {
                        if (ListIp_comboBox.Items[i].ToString().Equals(StrIP))
                        {
                            ListIp_comboBox.SelectedIndex = i;
                        }
                    }
                    if (i >= ListIp_comboBox.Items.Count)
                    {
                        ListIp_comboBox.SelectedIndex = 0;
                    }
                    Port_textBox.Text = StrPort;
                    break;
                case 3:
                    Bsmsgs.Clear();
                    List<string> lists = null;
                    if (Ini.GetAllSegment(Ini.PortPath,out lists))
                    {   
                        for (int j = 0; j < lists.Count; j++)
                        {
                            if (null == lists[j])
                            {
                                break;
                            }
                            if (lists[j].Length != 4)
                            {
                                continue;
                            }
                            Bsmsg bsmsg = new Bsmsg();
                            try
                            {
                                bsmsg.ID[0] = Convert.ToByte(lists[j].Substring(0, 2), 16);
                                bsmsg.ID[1] = Convert.ToByte(lists[j].Substring(2, 2), 16);
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                            string strname = Ini.GetValue(Ini.PortPath, lists[j], Ini.Name);
                            if (null != strname && !"".Equals(strname))
                            {
                                bsmsg.Name = strname;
                            }
                            string strheight = Ini.GetValue(Ini.PortPath, lists[j], Ini.Height);
                            try
                            {
                                bsmsg.Place.z = Convert.ToDouble(strheight);
                            }
                            catch (Exception)
                            {
                                bsmsg.Place.z = -1;
                            }
                            string strtype = Ini.GetValue(Ini.PortPath, lists[j], Ini.Type);
                            if ("1".Equals(strtype))
                            {
                                bsmsg.porttype = PortType.SingleMode;
                                string strrange = Ini.GetValue(Ini.PortPath, lists[j], Ini.Range);
                                try
                                {
                                    bsmsg.rangeR = Convert.ToDouble(strrange);
                                }
                                catch (Exception)
                                {
                                    bsmsg.rangeR = 500;
                                }
                            }
                            else
                            {
                                bsmsg.porttype = PortType.ThreeMode;
                            }
                            Bsmsgs.Add(lists[j], bsmsg);
                        }
                    }
                    //手动向里面添加控件
                    ScreenWidth = Map_panel.Width;
                    ScreenHeight = Map_panel.Height;
                    string StrRealWidth = Ini.GetValue(Ini.ConfigPath, Ini.MapSeg, Ini.RealWidth);
                    string StrRealHeight = Ini.GetValue(Ini.ConfigPath, Ini.MapSeg, Ini.RealHeight);
                    if (null != StrRealWidth && null != StrRealHeight)
                    {
                        try
                        {
                            Parameter.RealWidth = Convert.ToDouble(StrRealWidth);
                            Parameter.RealHeight = Convert.ToDouble(StrRealHeight);
                        }
                        catch (Exception)
                        {
                            Parameter.RealWidth = Parameter.RealHeight = -1;
                        }
                    }
                    AllCard_checkBox.Checked = true;

                    if(!Parameter.isSupportMulArea)
                    {
                        StrMapPath = Ini.GetValue(Ini.ConfigPath, Ini.MapSeg, Ini.MapKey_Path);
                    }

                    DxfMapParam.scale = 1;
                    DxfMapParam.CenterX = DxfMapParam.PanelCenterX = (double)Map_panel.Width / 2;
                    DxfMapParam.CenterY = DxfMapParam.PanelCenterY = (double)Map_panel.Height / 2;
                    if (Parameter.isSupportMulArea)
                    {
                        if (Parameter.RealWidth != 0 && Parameter.RealWidth != 0)
                        {
                            //计算当前图片与面板的比例
                            double wscale = (double)Map_panel.Width / Parameter.RealWidth;
                            double hscale = (double)Map_panel.Height / Parameter.RealHeight;
                            //面板距离与实际距离的比值知道
                            Img_RealDisRelation = wscale > hscale ? hscale : wscale;
                        }
                    }
                    LoadDxfMap();
                    break;
                default:
                    break;
            }
        }
        public void DrawScale(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            Graphics g = Graphics.FromImage(bitmap);
            Pen LinePen = Pens.Black;
            Brush StrBrush = Brushes.Black;
            for (int i = 0; i < width / 20;i++)
            {
                if (i != 0)
                {
                    g.DrawLine(LinePen, i * 20, 0, i * 20, 10);
                    g.DrawString(i.ToString(), DefaultFont, StrBrush, i * 20 - 5,10);
                }
            }
            for (int i = 0; i < height / 20;i++)
            {
                if (i != 0)
                {
                    g.DrawLine(LinePen, 0, i * 20, 10, i * 20);
                    g.DrawString(i.ToString(), DefaultFont, StrBrush, 10, i * 20);
                }
            }
        }
        public static void DrawMap(Bitmap DrawPad, string pathStr)
        {
            int width = DrawPad.Width;
            int height = DrawPad.Height;
            Bitmap map = null;
            try
            {

                map = new Bitmap(pathStr, false);

            }catch(Exception)
            {
                return;
            }
            Graphics Draw_g = Graphics.FromImage(DrawPad);

            Draw_g.Clear(Color.White);

            if ((height / map.Height) * map.Width > width)
            {
                map = new Bitmap(map, width, (width * map.Height / map.Width));
            }
            else 
            {
                map = new Bitmap(map, (height * map.Width / map.Height), height);
            }
        }
        //动态添加卡片
        public void AddCheckBox_Dynamic(CardImg CurTag)
        {
            //Tagmsg tgmsg = null;
            string StrTagID = CurTag.ID[0].ToString("X2") + CurTag.ID[1].ToString("X2");
            if (!CardList_panel.Controls.ContainsKey(StrTagID))
            {
                CheckBox chb = new CheckBox();
                chb.Name = StrTagID;
                if (null != CurTag.Name && !"".Equals(CurTag.Name))
                {
                    StringBuilder strbuilder = new StringBuilder(CurTag.Name);
                    strbuilder.Append("(");
                    strbuilder.Append(StrTagID);
                    strbuilder.Append(")");
                    chb.Text = strbuilder.ToString();
                }
                else
                {
                    chb.Text = StrTagID;
                }
                chb.Checked = true;
                CardList_panel.Controls.Add(chb);
                chb.Left = 5;
                chb.Top = (CardList_panel.Controls.Count - 1) * (chb.Height) + CardList_panel.AutoScrollPosition.Y;
                chb.CheckedChanged += ChangeCheck;
            }
            else
            {
                //包含后看名称是否需要修改
                Control[] contrls = CardList_panel.Controls.Find(StrTagID,false);
                string StrInfor = "";
                if (null != CurTag.Name && !"".Equals(CurTag.Name))
                {
                    StringBuilder strbuilder = new StringBuilder(CurTag.Name);
                    strbuilder.Append("(");
                    strbuilder.Append(StrTagID);
                    strbuilder.Append(")");
                    StrInfor = strbuilder.ToString();
                }
                else
                {
                    StrInfor = StrTagID;
                }
                if (!StrInfor.Equals(contrls[0].Text))
                {
                    contrls[0].Text = StrInfor;
                }
            }
            StringBuilder varstr = new StringBuilder("Select monitor display card (Total Number:");
            varstr.Append(CardImgs.Count);
            varstr.Append(")");
            ListShowCard_groupBox.Text = varstr.ToString();
        }
        private void ChangeCheck(object sender, EventArgs e)
        {
            CheckBox chb = (CheckBox)sender;
            String StrName = chb.Name.ToString();
            if (StrName != null)
            {
                if (!"".Equals(StrName))
                {
                    foreach (KeyValuePair<string, CardImg> card in CardImgs)
                    {
                        if (card.Key.Equals(StrName))
                        {
                            card.Value.isShowTag = chb.Checked;
                        }
                    }
                }
            }
            if (!chb.Checked)
            {
                //没有勾选
                if (AllCard_checkBox.Checked)
                {
                    AllCard_checkBox.Checked = false;
                }
            }
            else
            {
                //勾选
                int count = CardList_panel.Controls.Count;
                int i;
                for (i = 0; i < count; i++)
                {
                    if (!((CheckBox)(CardList_panel.Controls[i])).Checked)
                    {
                        break;
                    }
                }
                if (i >= count)
                {
                    AllCard_checkBox.Checked = true;
                }
            }
        }
        private void AllCard_checkBox_Click(object sender, EventArgs e)
        {
                int count = CardList_panel.Controls.Count;
                if (AllCard_checkBox.Checked)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!((CheckBox)(CardList_panel.Controls[i])).Checked)
                        {
                            ((CheckBox)(CardList_panel.Controls[i])).Checked = true;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (((CheckBox)(CardList_panel.Controls[i])).Checked)
                        {
                            ((CheckBox)(CardList_panel.Controls[i])).Checked = false;
                        }
                    }
                }
        }
        private void Set_button_Click(object sender, EventArgs e)
        {
            SetForm MySetForm = new SetForm(this);
            MySetForm.ShowDialog();
            
        }

        private void Start_Listen_Btn_Click(object sender, EventArgs e)
        {
            //Console.WriteLine(sender.ToString());
            //Console.WriteLine(e.ToString());
            if (IsCnn)
            {
                PrecisePositionLibrary.PrecisePosition.Stop();
                CurReportMode = ReportMode.UnKnown;
                MyTimer.Stop();
                MyTimer = null;
                Port_textBox.Enabled = true;
                ListIp_comboBox.Enabled = true;
                Start_Listen_Btn.Text = "Start monitoring";
                IsCnn = false;
                SigOrderCb.Enabled = true;

            }
            else 
            {
                //断开连接
                string StrIP = ListIp_comboBox.Text;
                string StrPort = Port_textBox.Text;
                int Port = 0;
                if (isStart)
                {
                    MessageBox.Show("Sorry, please close the graphic display!");
                    return;
                }

                try
                {
                    //Port = Convert.ToInt32(StrPort);
                    Port = 1025;
                }
                catch (Exception)
                {
                    MessageBox.Show("Sorry, the port format is wrongggg!");
                    return;
                }
                if (Port <= 1024 || Port > 65535)
                {
                    MessageBox.Show("The port range is 1024 to 65535！");
                    return;
                }
                IPAddress ip = null;
                if (!IPAddress.TryParse(StrIP, out ip))
                {
                    MessageBox.Show("IP format is wrong!");
                    return;
                }
                WarmTxt.Text = ""; WarmTxt.Visible = false;
                PrecisePositionLibrary.PrecisePosition.InitNet(ip, Port);
                CurReportMode = ReportMode.ListMode;
                try
                {

                    PrecisePositionLibrary.PrecisePosition.Start(this.Handle, CurReportMode, PosititionMode.UnKnown, (Parameter.isUse3Station ? AfewDPos.Pos2Dim : AfewDPos.Pos3Dim), Parameter.isUseTagHeightRange, Parameter.TagHeightRangeLow, Parameter.TagHeightRangeHigh);
                }
                catch (ArgumentNullException e1)
                {
                    Console.WriteLine(e1.ToString());
                    MessageBox.Show(e1.ToString());
                    return;
                }
                catch (System.Net.Sockets.SocketException e2)
                {
                    Console.WriteLine(e2.ToString());
                    MessageBox.Show(e2.ToString());
                    return;
                }
                CardList_listView.Items.Clear();
                lock(Ports_Lock)
                {
                    Ports.Clear();
                }
                CardImgs.Clear();
                Port_textBox.Enabled = false;
                ListIp_comboBox.Enabled = false;
                Start_Listen_Btn.Text = "Disconnect the monitoring";
                IsCnn = true;

                SigOrderCb.Enabled = false;
                //开启一个定时器
                if (MyTimer == null)
                {
                    MyTimer = new System.Windows.Forms.Timer();
                }
                MyTimer.Interval = 1000;//每隔2s执行一次程序
                MyTimer.Tick += MyTimer_Tick;
                MyTimer.Start();

                CardList_listView.Items.Clear();
                CardImgs.Clear();

            }
        }
        //按距离排序
        private void BubbleSortPortFun(SortPort[] CurSortPort)
        {
            SortPort TempPort = new SortPort();
            for (int i = 0; i < CurSortPort.Length-1; i++)
            {
                for (int j = i; j < CurSortPort.Length;j++ )
                {
                    if (ListSortMode == 1)
                    {
                        if (CurSortPort[i].SigQuality > CurSortPort[j].SigQuality)
                        {//交换位置
                            System.Buffer.BlockCopy(CurSortPort[i].ID, 0, TempPort.ID, 0, 2);
                            TempPort.Distanse = CurSortPort[i].Distanse;
                            TempPort.isOptimal = CurSortPort[i].isOptimal;
                            TempPort.Name = CurSortPort[i].Name;
                            System.Buffer.BlockCopy(CurSortPort[j].ID, 0, CurSortPort[i].ID, 0, 2);
                            CurSortPort[i].Distanse = CurSortPort[j].Distanse;
                            CurSortPort[i].isOptimal = CurSortPort[j].isOptimal;
                            CurSortPort[i].Name = CurSortPort[j].Name;
                            System.Buffer.BlockCopy(TempPort.ID, 0, CurSortPort[j].ID, 0, 2);
                            CurSortPort[j].Distanse = TempPort.Distanse;
                            CurSortPort[j].isOptimal = TempPort.isOptimal;
                            CurSortPort[j].Name = TempPort.Name;
                        }
                    }
                    else
                    {
                        if (CurSortPort[i].Distanse > CurSortPort[j].Distanse)
                        {//交换位置
                            System.Buffer.BlockCopy(CurSortPort[i].ID, 0, TempPort.ID, 0, 2);
                            TempPort.Distanse = CurSortPort[i].Distanse;
                            TempPort.isOptimal = CurSortPort[i].isOptimal;
                            TempPort.Name = CurSortPort[i].Name;

                            System.Buffer.BlockCopy(CurSortPort[j].ID, 0, CurSortPort[i].ID, 0, 2);
                            CurSortPort[i].Distanse = CurSortPort[j].Distanse;
                            CurSortPort[i].isOptimal = CurSortPort[j].isOptimal;
                            CurSortPort[i].Name = CurSortPort[j].Name;

                            System.Buffer.BlockCopy(TempPort.ID, 0, CurSortPort[j].ID, 0, 2);
                            CurSortPort[j].Distanse = TempPort.Distanse;
                            CurSortPort[j].isOptimal = TempPort.isOptimal;
                            CurSortPort[j].Name = TempPort.Name;
                        }
                    }
                }
            }
        }

        //呼叫python核心程式碼
        public static void RunPythonScript(string sArgName, string args = "", params string[] teps)
        {
            Process p = new Process();
            string path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + sArgName;// 獲得python檔案的絕對路徑（將檔案放在c#的debug資料夾中可以這樣操作）
            path = "C:/Users/User/Downloads/" + sArgName;//(因為我沒放debug下，所以直接寫的絕對路徑,替換掉上面的路徑了)
            p.StartInfo.FileName = @"C:\Users\User\AppData\Local\Programs\Python\Python37\python.exe";//沒有配環境變數的話，可以像我這樣寫python.exe的絕對路徑。如果配了，直接寫"python.exe"即可
            string sArguments = path;
            foreach (string sigstr in teps)
            {
                sArguments += " " + sigstr;//傳遞引數
            }

            sArguments += " " + args;

            p.StartInfo.Arguments = sArguments;

            p.StartInfo.UseShellExecute = false;

            p.StartInfo.RedirectStandardOutput = true;

            p.StartInfo.RedirectStandardInput = true;

            p.StartInfo.RedirectStandardError = true;

            p.StartInfo.CreateNoWindow = true;

            p.Start();
            p.BeginOutputReadLine();
            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            //Console.ReadLine();
            p.WaitForExit();
        }
        //輸出列印的資訊
        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                temp = (e.Data + Environment.NewLine);
                //AppendText(e.Data + Environment.NewLine);
            }
        }
        //public delegate void AppendTextCallback(string text);
        //public static void AppendText(string text)
        //{
        //    Console.WriteLine(text);     //此處在控制檯輸出.py檔案print的結果
        //}

        //計算行列式函數：利用遞歸和行列式的數學計算式計算。時間複雜度為O(n三次方)，性能較低。
        double Hanglieshi(int N, double[,] xishu)
        {
            double Mo = 0;
            if (N == 0) return 0;
            else if (N == 1) return xishu[0, 0];
            else if (N == 2) return xishu[0, 0] * xishu[1, 1] - xishu[0, 1] * xishu[1, 0];
            else
            {

                for (int i = 0; i < N; i++)
                {
                    double[,] NewXishu = new double[N - 1, N - 1];
                    for (int j = 0; j < N - 1; j++)
                    {
                        int mark = 0;
                        for (int k = 0; k < N - 1; k++)
                        {

                            if (k == i) { NewXishu[j, k] = xishu[j + 1, mark + 1]; mark++; }
                            else NewXishu[j, k] = xishu[j + 1, mark];
                            //Console.WriteLine("k的值為：{0}\tmark的值為:{1}\t數組的值為:{2}",k,mark,NewXishu[j,k]);
                            mark++;
                        }
                    }
                    //Console.WriteLine("這是第{0}次迴圈",i+1);
                    if (i % 2 == 0)
                        Mo += xishu[0, i] * Hanglieshi(N - 1, NewXishu);
                    else
                        Mo -= xishu[0, i] * Hanglieshi(N - 1, NewXishu);
                }
                return Mo;
            }
        }
        /*創建新的數組讓方程結果值代替列值，時間複雜度為O（n）主要問題在空間複雜度上，傳
        參時，需要把原數組複製，所以要O（n三次方）。註意：正常函數傳參是按值傳參，函數內形參不
        改變函數外部實參的值。但是數組比較特殊，會被更改。 */
        double Rexishu(int lieshu, double[,] xishu, double[] Zhi, int Size)
        {
            Console.WriteLine();

            for (int i = 0; i < Size; i++)
            {
                xishu[i, lieshu] = Zhi[i];
            }
            double resulti = Hanglieshi(Size, xishu);
            return resulti;
        }

        static void Swap<T>(ref T a, ref T b)
        {
            T t = a;
            a = b;
            b = t;
        }

        

        //triposition3為三基站算法
        void triposition3(double xa, double ya, double za, double d1, string an, double xb, double yb, double zb, double d2, string bn, double xc, double yc, double zc, double d3, string cn, double xd, double yd, double zd, double d4, string dn)
        {
            if(d4==-1&&d3==-1)
            {
                verification1 = "none";
            }
            else
            {
                double da = 0, db = 0, dc = 0, dd = 0;
                //da = d1;
                //db = d2;
                //dc = d3;
                double[] ddd = { d1, d2, d3, d4 };
                string[] tt = { an, bn, cn, dn};
                
                double[] coef = new double[12];
                
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (tt[i] == station[j] && j == 0)
                        {
                            da = ddd[i];
                        }
                
                        if (tt[i] == station[j] && j == 1)
                        {
                            db = ddd[i];
                        }
                
                        if (tt[i] == station[j] && j == 2)
                        {
                            dc = ddd[i];
                        }
                
                        if (tt[i] == station[j] && j == 3)
                        {
                            dd = ddd[i];
                        }
                    }
                }

                double U = Math.Pow((Math.Pow((xa - xb), 2) + Math.Pow((ya - yb), 2) + Math.Pow((za - zb), 2)), 0.5);
                double V = Math.Pow((Math.Pow((xb - xc), 2) + Math.Pow((yb - yc), 2) + Math.Pow((zb - zc), 2)), 0.5);
                double W = Math.Pow((Math.Pow((xc - xa), 2) + Math.Pow((yc - ya), 2) + Math.Pow((zc - za), 2)), 0.5);
                double u = dc;
                double v = da;
                double w = db;

                double x = (U - v + w) * (v - w + U);
                double y = (V - w + u) * (w - u + V);
                double z = (W - u + v) * (u - v + W);
                double X = (w - U + v) * (U + v + w);
                double Y = (u - V + w) * (V + w + u);
                double Z = (v - W + u) * (W + u + v);
                double a = Math.Pow((x * Y * Z),0.5);
                double b = Math.Pow((y * Z * X),0.5);
                double c = Math.Pow((z * X * Y),0.5);
                double d = Math.Pow((x * y * z),0.5);

                double B = Math.Pow(((-a + b + c + d) * (a - b + c + d) * (a + b - c + d) * (a + b + c - d)),0.5);
                double C = (192 * u * v * w);
                double volume = B / C;
                double p = (U + V + W) / 2;
                double area = Math.Pow((p * (p - U) * (p - V) * (p - W)), 0.5);
                double height = (3 * volume) / area;
                double co1 = Math.Pow((Math.Pow(da,2) - Math.Pow(height,2)),0.5);
                double co2 = Math.Pow((Math.Pow(db,2) - Math.Pow(height,2)),0.5);
                double co3 = Math.Pow((Math.Pow(dc,2) - Math.Pow(height,2)),0.5);


                double f1 = xa - xc;
                double f2 = ya - yc;
                double f3 = Math.Pow(xa, 2) - Math.Pow(xc, 2) + Math.Pow(ya, 2) - Math.Pow(yc, 2) + Math.Pow(co3, 2) - Math.Pow(co1, 2);
                double f4 = xb - xc;
                double f5 = yb - yc;
                double f6 = Math.Pow(xb, 2) - Math.Pow(xc, 2) + Math.Pow(yb, 2) - Math.Pow(yc, 2) + Math.Pow(co3, 2) - Math.Pow(co2, 2);

                double xxx = (f2 * f6 - f5 * f3) / (2 * f2 * f4 - 2 * f1 * f5);
                double yyy = (f1 * f6 - f4 * f3) / (2 * f1 * f5 - 2 * f2 * f4);


                ans1[0] = xxx;
                ans1[1] = yyy;
                ans1[2] = height;


                string[] tempans1 = new string[7];
                tempans1[0] = "[";
                tempans1[1] = ans1[0].ToString();
                tempans1[2] = ",";
                tempans1[3] = ans1[1].ToString();
                tempans1[4] = ",";
                tempans1[5] = ans1[2].ToString();
                tempans1[6] = "]";

                temp1 = String.Concat(tempans1);
                //Console.WriteLine(temp1);


                if (ans1[0].ToString() == "非數值" || ans1[1].ToString() == "非數值" || ans1[2].ToString() == "非數值")
                {
                    verification1 = "none";
                }
                else
                {
                    verification1 = "have";
                }
                
            }
           
        }

        //triposition1為四基站算法(三平面計算)
        void triposition1(double xa, double ya, double za, double d1, string a, double xb, double yb, double zb, double d2, string b, double xc, double yc, double zc, double d3, string c, double xd, double yd, double zd, double d4, string d)
        {
            if(d4 == -1)
            {
                verification = "none";
                triposition3(xa, ya, za, d1, a, xb, yb, zb, d2, b, xc, yc, zc, d3, c, xd, yd, zd, d4, d);
            }
            else
            {
                double da = 0, db = 0, dc = 0, dd = 0;
                //da = d1;
                //db = d2;
                //dc = d3;
                //dd = d4;
                double[] ddd = { d1, d2, d3, d4 };
                string[] tt = { a, b, c, d };
                
                double[] coef = new double[12];
                
                
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (tt[i] == station[j] && j == 0)
                        {
                            da = ddd[i];
                        }
                
                        if (tt[i] == station[j] && j == 1)
                        {
                            db = ddd[i];
                        }
                
                        if (tt[i] == station[j] && j == 2)
                        {
                            dc = ddd[i];
                        }
                
                        if (tt[i] == station[j] && j == 3)
                        {
                            dd = ddd[i];
                        }
                    }
                }
        
                double AB = ((Math.Pow(xb - xa, 2)) + (Math.Pow(yb - ya, 2)) + (Math.Pow(zb - zc, 2)));
                AB = Math.Pow(AB, 0.5);
                double cos_OAB = (Math.Pow(da, 2) + Math.Pow(AB, 2) - Math.Pow(db, 2)) / (2 * da * AB);
                double cos_OBA = (Math.Pow(db, 2) + Math.Pow(AB, 2) - Math.Pow(da, 2)) / (2 * db * AB);
                double AM = cos_OAB * da;
                double gamma_AM = AM / AB;
                double xm = ((1 - gamma_AM) * xa) + (gamma_AM * xb);
                double ym = ((1 - gamma_AM) * ya) + (gamma_AM * yb);
                double zm = ((1 - gamma_AM) * za) + (gamma_AM * zb);
        
                coef[0] = (xb - xa);
                coef[1] = (yb - ya);
                coef[2] = (zb - za);
                coef[3] = ((xb - xa) * xm) + ((yb - ya) * ym) + ((zb - za) * zm);
        
        
        
                double BC = ((Math.Pow(xc - xb, 2)) + (Math.Pow(yc - yb, 2)) + (Math.Pow(zc - zb, 2)));
                BC = Math.Pow(BC, 0.5);
                double cos_OBC = (Math.Pow(db, 2) + Math.Pow(BC, 2) - Math.Pow(dc, 2)) / (2 * db * BC);
                double cos_OCB = (Math.Pow(dc, 2) + Math.Pow(BC, 2) - Math.Pow(db, 2)) / (2 * dc * BC);
        
        
                double BN = cos_OBC * db;
                double gamma_BN = BN / BC;
        
                double xn = ((1 - gamma_BN) * xb) + (gamma_BN * xc);
                double yn = ((1 - gamma_BN) * yb) + (gamma_BN * yc);
                double zn = ((1 - gamma_BN) * zb) + (gamma_BN * zc);
        
                coef[4] = (xc - xb);
                coef[5] = (yc - yb);
                coef[6] = (zc - zb);
                coef[7] = ((xc - xb) * xn) + ((yc - yb) * yn) + ((zc - zb) * zn);
        
        
                double AD = ((Math.Pow(xd - xa, 2)) + (Math.Pow(yd - ya, 2)) + (Math.Pow(zd - za, 2)));
                AD = Math.Pow(AD, 0.5);
        
                double cos_OAD = (Math.Pow(da, 2) + Math.Pow(AD, 2) - Math.Pow(dd, 2)) / (2 * da * AD);
                double cos_ODA = (Math.Pow(dd, 2) + Math.Pow(AD, 2) - Math.Pow(da, 2)) / (2 * dd * AD);
        
                double AQ = cos_OAD * da;
                double gamma_AQ = AQ / AD;
        
                double xq = ((1 - gamma_AQ) * xa) + (gamma_AQ * xd);
                double yq = ((1 - gamma_AQ) * ya) + (gamma_AQ * yd);
                double zq = ((1 - gamma_AQ) * za) + (gamma_AQ * zd);
        
                coef[8] = (xd - xa);
                coef[9] = (yd - ya);
                coef[10] = (zd - za);
                coef[11] = ((xd - xa) * xq) + ((yd - ya) * yq) + ((zd - za) * zq);
        
        
                int n = 3;
        
                //依次輸入每行方程的繫數和結果
                double[,] Xishu = new double[n, n];
                double[] zhi = new double[n];
                double[] EachLineResult = new double[n];
        
                Xishu[0, 0] = coef[0];
                Xishu[0, 1] = coef[1];
                Xishu[0, 2] = coef[2];
                zhi[0] = coef[3];
        
                Xishu[1, 0] = coef[4];
                Xishu[1, 1] = coef[5];
                Xishu[1, 2] = coef[6];
                zhi[1] = coef[7];
        
                Xishu[2, 0] = coef[8];
                Xishu[2, 1] = coef[9];
                Xishu[2, 2] = coef[10];
                zhi[2] = coef[11];
        
        
                //計算行列式的值和用結果值代替繫數的行列式的值
                double result = Hanglieshi(n, Xishu);
                //測試用句1： Console.WriteLine("計算出行列式的結果為：{0}", result);
                if (result == 0)
                {
                    verification = "none";
                    triposition3(xa, ya, za, d1, a, xb, yb, zb, d2, b, xc, yc, zc, d3, c, xd, yd, zd, d4, d);
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        double[,] TempXishu = new double[n, n];
                        for (int ii = 0; ii < n; ii++)
                        {
                            for (int jj = 0; jj < n; jj++)
                            {
                                TempXishu[ii, jj] = Xishu[ii, jj];
                            }
                        }
                        EachLineResult[i] = Rexishu(i, TempXishu, zhi, n);
                        //測試用句2： Console.WriteLine("第{0}個結果行列式的值為:{1}",i+1,EachLineResult[i]);
                    }
        
        
                    for (int i = 0; i < n; i++)
                    {
                        ans[i] = EachLineResult[i] / result;
                    }
        
                    string[] tempans = new string[7];
                    tempans[0] = "[";
                    tempans[1] = ans[0].ToString();
                    tempans[2] = ",";
                    tempans[3] = ans[1].ToString();
                    tempans[4] = ",";
                    tempans[5] = ans[2].ToString();
                    tempans[6] = "]";
        
                    temp = String.Concat(tempans);
                    //Console.WriteLine(temp);
                    verification = "have";
        
                    triposition3(xa, ya, za, d1, a, xb, yb, zb, d2, b, xc, yc, zc, d3, c, xd, yd, zd, d4, d);
        
                    
                }
            }
            
        
        }


        //triposition1為四基站算法(四球計算)
        //void triposition2(double xa, double ya, double za, double d1, string a, double xb, double yb, double zb, double d2, string b, double xc, double yc, double zc, double d3, string c, double xd, double yd, double zd, double d4, string d)
        //{
        //    if (d4 == -1)
        //    {
        //        verification = "none";
        //        triposition3(xa, ya, za, d1, a, xb, yb, zb, d2, b, xc, yc, zc, d3, c, xd, yd, zd, d4, d);
        //    }
        //    else
        //    {
        //
        //        
        //        double[] coef = new double[12];                
        //
        //        double lunda1 = ((Math.Pow(d2, 2)) - (Math.Pow(d1, 2)) - (Math.Pow(xb, 2)) + (Math.Pow(xa, 2)) - (Math.Pow(yb, 2)) + (Math.Pow(ya, 2)) - (Math.Pow(zb, 2)) + (Math.Pow(za, 2)));
        //        double lunda2 = ((Math.Pow(d3, 2)) - (Math.Pow(d1, 2)) - (Math.Pow(xc, 2)) + (Math.Pow(xa, 2)) - (Math.Pow(yc, 2)) + (Math.Pow(ya, 2)) - (Math.Pow(zc, 2)) + (Math.Pow(za, 2)));
        //        double lunda3 = ((Math.Pow(d4, 2)) - (Math.Pow(d1, 2)) - (Math.Pow(xd, 2)) + (Math.Pow(xa, 2)) - (Math.Pow(yd, 2)) + (Math.Pow(ya, 2)) - (Math.Pow(zd, 2)) + (Math.Pow(za, 2)));
        //
        //
        //        coef[0] = 2 * (xa - xb);
        //        coef[1] = 2 * (ya - yb);
        //        coef[2] = 2 * (za - zb);
        //        coef[3] = lunda1;
        //
        //        coef[4] = 2 * (xa - xc);
        //        coef[5] = 2 * (ya - yc);
        //        coef[6] = 2 * (za - zc);
        //        coef[7] = lunda2;
        //
        //        coef[8] = 2 * (xa - xd);
        //        coef[9] = 2 * (ya - yd);
        //        coef[10] = 2 * (za - zd);
        //        coef[11] = lunda3;
        //
        //        int n = 3;
        //
        //        //依次輸入每行方程的繫數和結果
        //        double[,] Xishu = new double[n, n];
        //        double[] zhi = new double[n];
        //        double[] EachLineResult = new double[n];
        //
        //        Xishu[0, 0] = coef[0];
        //        Xishu[0, 1] = coef[1];
        //        Xishu[0, 2] = coef[2];
        //        zhi[0] = coef[3];
        //
        //        Xishu[1, 0] = coef[4];
        //        Xishu[1, 1] = coef[5];
        //        Xishu[1, 2] = coef[6];
        //        zhi[1] = coef[7];
        //
        //        Xishu[2, 0] = coef[8];
        //        Xishu[2, 1] = coef[9];
        //        Xishu[2, 2] = coef[10];
        //        zhi[2] = coef[11];
        //
        //
        //        //計算行列式的值和用結果值代替繫數的行列式的值
        //        double result = Hanglieshi(n, Xishu);
        //        //測試用句1： Console.WriteLine("計算出行列式的結果為：{0}", result);
        //        if (result == 0)
        //        {
        //            verification = "none";
        //            triposition3(xa, ya, za, d1, a, xb, yb, zb, d2, b, xc, yc, zc, d3, c, xd, yd, zd, d4, d);
        //        }
        //        else
        //        {
        //            for (int i = 0; i < n; i++)
        //            {
        //                double[,] TempXishu = new double[n, n];
        //                for (int ii = 0; ii < n; ii++)
        //                {
        //                    for (int jj = 0; jj < n; jj++)
        //                    {
        //                        TempXishu[ii, jj] = Xishu[ii, jj];
        //                    }
        //                }
        //                EachLineResult[i] = Rexishu(i, TempXishu, zhi, n);
        //                //測試用句2： Console.WriteLine("第{0}個結果行列式的值為:{1}",i+1,EachLineResult[i]);
        //            }
        //
        //
        //            for (int i = 0; i < n; i++)
        //            {
        //                ans[i] = EachLineResult[i] / result;
        //            }
        //
        //            string[] tempans = new string[7];
        //            tempans[0] = "[";
        //            tempans[1] = ans[0].ToString();
        //            tempans[2] = ",";
        //            tempans[3] = ans[1].ToString();
        //            tempans[4] = ",";
        //            tempans[5] = ans[2].ToString();
        //            tempans[6] = "]";
        //
        //            temp = String.Concat(tempans);
        //            //Console.WriteLine(temp);
        //            verification = "have";
        //
        //            triposition3(xa, ya, za, d1, a, xb, yb, zb, d2, b, xc, yc, zc, d3, c, xd, yd, zd, d4, d);
        //
        //
        //        }
        //    }
        //
        //
        //}

        public class card_counter
        {
            public string id;
            public int without_move;
            public int packet_receive;
            public int exist;

        }
        List<card_counter> myCardExistLists = new List<card_counter>();



        //存入資料庫redis或Mysql時資料用card_data_set的物件儲存
        public class card_data_set
        {
            public string id;
            public string X;
            public string Y;
            public string Z;
            public string port1;
            public string distance1;
            public string port2;
            public string distance2;
            public string port3;
            public string distance3;
            public string port4;
            public string distance4;
            public string port5;
            public string distance5;
            public string Gsensor;
            public string battery;
            public string sleeptime;
            public string without_move;
            public string last_receive;
            public string intervals;
            public string packet_loss;
            public string packet_receive;
            public string check;
            public string time;
            public string coord;
            public int exist;

        }



        //各基站所抓取的距離
        double d1;
        double d2;
        double d3;
        double d4;
        double d5;

        

        //各基站的名稱
        string port1_calc;
        string port2_calc;
        string port3_calc;
        string port4_calc;
        string port5_calc;


        public void MyTimer_Tick(object sender, EventArgs e)
        {

            string StrReferID;
            ListViewItem item = null;
            foreach(KeyValuePair<string,CardImg> tag in CardImgs)
            {
                
                card_data_set redis_data = new card_data_set(); //宣告一個新的card_data_set物件給redis存入資料，名稱為 redis_data
                card_counter card_counter_data = new card_counter(); //宣告一個新的card_counter物件來看資料
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");//大家使用redis的話應該都是本機端
                //IDatabase db_set = redis.GetDatabase(0);  //連結到指定的資料庫，這樣代表就是db1
                IDatabase db = redis.GetDatabase(1);  //連結到指定的資料庫，這樣代表就是db1


                //int a;
                //for(a=0; a< myPortList.Count; a++)
                //{
                //    myPortList[a].distance = -1;
                //}

                //对1到5的基站排序显示
                SortPort[] CurSortPort = new SortPort[5];
                for (int i = 0; i < CurSortPort.Length; i++)
                {
                    CurSortPort[i] = new SortPort();
                }
                if (tag.Value.ReportRouters.Count >= 1)
                {
                    System.Buffer.BlockCopy(tag.Value.ReportRouters[0].id, 0, CurSortPort[0].ID, 0, 2);
                    StrReferID = tag.Value.ReportRouters[0].id[0].ToString("X2") + tag.Value.ReportRouters[0].id[1].ToString("X2");
                    CurSortPort[0].Name = Ini.GetValue(Ini.PortPath, StrReferID, Ini.Name);
                    CurSortPort[0].isOptimal = true;
                    CurSortPort[0].Distanse = tag.Value.ReportRouters[0].dis;
                    CurSortPort[0].SigQuality = tag.Value.ReportRouters[0].SigQuality;
                }

                if (tag.Value.ReportRouters.Count >= 2)
                {
                    System.Buffer.BlockCopy(tag.Value.ReportRouters[1].id, 0, CurSortPort[1].ID, 0, 2);
                    StrReferID = tag.Value.ReportRouters[1].id[0].ToString("X2") + tag.Value.ReportRouters[1].id[1].ToString("X2");
                    CurSortPort[1].Name = Ini.GetValue(Ini.PortPath, StrReferID, Ini.Name);
                    CurSortPort[1].isOptimal = true;
                    CurSortPort[1].Distanse = tag.Value.ReportRouters[1].dis;
                    CurSortPort[1].SigQuality = tag.Value.ReportRouters[1].SigQuality;
                }
                if (tag.Value.ReportRouters.Count >= 3)
                {
                    System.Buffer.BlockCopy(tag.Value.ReportRouters[2].id, 0, CurSortPort[2].ID, 0, 2);
                    StrReferID = tag.Value.ReportRouters[2].id[0].ToString("X2") + tag.Value.ReportRouters[2].id[1].ToString("X2");
                    CurSortPort[2].Name = Ini.GetValue(Ini.PortPath, StrReferID, Ini.Name);
                    CurSortPort[2].isOptimal = true;
                    CurSortPort[2].Distanse = tag.Value.ReportRouters[2].dis;
                    CurSortPort[2].SigQuality = tag.Value.ReportRouters[2].SigQuality;
                }
                if (tag.Value.ReportRouters.Count >= 4)
                {
                    System.Buffer.BlockCopy(tag.Value.ReportRouters[3].id, 0, CurSortPort[3].ID, 0, 2);
                    StrReferID = tag.Value.ReportRouters[3].id[0].ToString("X2") + tag.Value.ReportRouters[3].id[1].ToString("X2");
                    CurSortPort[3].Name = Ini.GetValue(Ini.PortPath, StrReferID, Ini.Name);
                    CurSortPort[3].isOptimal = false;
                    CurSortPort[3].Distanse = tag.Value.ReportRouters[3].dis;
                    CurSortPort[3].SigQuality = tag.Value.ReportRouters[3].SigQuality;
                }
                if (tag.Value.ReportRouters.Count >= 5)
                {
                    System.Buffer.BlockCopy(tag.Value.ReportRouters[4].id, 0, CurSortPort[4].ID, 0, 2);
                    StrReferID = tag.Value.ReportRouters[4].id[0].ToString("X2") + tag.Value.ReportRouters[4].id[1].ToString("X2");
                    CurSortPort[4].Name = Ini.GetValue(Ini.PortPath, StrReferID, Ini.Name);
                    CurSortPort[4].isOptimal = false;
                    CurSortPort[4].Distanse = tag.Value.ReportRouters[4].dis;
                    CurSortPort[4].SigQuality = tag.Value.ReportRouters[4].SigQuality;
                }
                //对CurSortPort数组中的对象按距离排序
                BubbleSortPortFun(CurSortPort);
                if(CardList_listView.Items.ContainsKey(tag.Key))
                {
                    //檢查目前tag的名稱
                    Console.WriteLine("tag.Key");
                    Console.WriteLine(tag.Key);

                    ListViewItem[] items = CardList_listView.Items.Find(tag.Key,false);
                    StrReferID = CurSortPort[0].ID[0].ToString("X2") + CurSortPort[0].ID[1].ToString("X2");
                    if ("0000".Equals(StrReferID))
                    {
                        items[0].SubItems[1].Text = "****";
                    }
                    else
                    {
                        if (null == CurSortPort[0].Name || "".Equals(CurSortPort[0].Name))
                        {
                            items[0].SubItems[1].Text = StrReferID;
                        }
                        else
                        {
                            StringBuilder strvar = new StringBuilder(CurSortPort[0].Name);
                            strvar.Append("(");
                            strvar.Append(StrReferID);
                            strvar.Append(")");
                            items[0].SubItems[1].Text = strvar.ToString();
                        }
                    }
                    if (CurSortPort[0].Distanse > 0 && CurSortPort[0].Distanse < int.MaxValue)
                    {
                        items[0].SubItems[2].Text = Math.Round(CurSortPort[0].Distanse, 2).ToString() + " CM";

                        d1 = Math.Round(CurSortPort[0].Distanse, 2);//這個是為了計算距離的，所以不傳字串傳數字
                        port1_calc = items[0].SubItems[1].Text;//將基站名稱存入

                        //int portindex = myPortList.FindIndex(x => x.id == port1_calc);
                        //myPortList[portindex].distance = d1;


                        //redis_data是card_data_set的物件，也是資料存入
                        redis_data.id = tag.Key;
                        redis_data.port1 = items[0].SubItems[1].Text;
                        redis_data.distance1 = items[0].SubItems[2].Text;

                    }
                    else
                    {
                        items[0].SubItems[2].Text = "****";

                        d1 = -1;
                        port1_calc = items[0].SubItems[1].Text;
                        redis_data.id = tag.Key;

                        redis_data.port1 = items[0].SubItems[1].Text;
                        redis_data.distance1 = "null";
                    }


                    if (CurSortPort[0].isOptimal && !"****".Equals(items[0].SubItems[1].Text))
                    {
                        items[0].SubItems[1].ForeColor = PriCellColor;
                    }
                    else
                    {
                        items[0].SubItems[1].ForeColor = Color.Black;
                    }

                    StrReferID = CurSortPort[1].ID[0].ToString("X2") + CurSortPort[1].ID[1].ToString("X2");
                    if ("0000".Equals(StrReferID))
                    {
                        items[0].SubItems[3].Text = "****";
                    }
                    else
                    {
                        if (null == CurSortPort[1].Name || "".Equals(CurSortPort[1].Name))
                        {
                            items[0].SubItems[3].Text = StrReferID;
                        }
                        else
                        {
                            items[0].SubItems[3].Text = CurSortPort[1].Name + "(" + StrReferID + ")";
                        }
                    }
                    if (CurSortPort[1].Distanse > 0 && CurSortPort[1].Distanse < int.MaxValue)
                    {
                        items[0].SubItems[4].Text = Math.Round(CurSortPort[1].Distanse, 2).ToString() + " CM";
                        d2 = Math.Round(CurSortPort[1].Distanse, 2);
                        port2_calc = items[0].SubItems[3].Text;


                        //int portindex = myPortList.FindIndex(x => x.id == port2_calc);
                        //myPortList[portindex].distance = d2;


                        redis_data.port2 = items[0].SubItems[3].Text;
                        redis_data.distance2 = items[0].SubItems[4].Text;


                    }
                    else
                    {
                        items[0].SubItems[4].Text = "****";

                        d2 = -1;
                        port2_calc = items[0].SubItems[3].Text;

                        redis_data.port2 = items[0].SubItems[3].Text;
                        redis_data.distance2 = "null";
                    }

                    if (CurSortPort[1].isOptimal && !"****".Equals(items[0].SubItems[3].Text))
                    {
                        items[0].SubItems[3].ForeColor = PriCellColor;
                    }
                    else
                    {
                        items[0].SubItems[3].ForeColor = Color.Black;
                    }

                    StrReferID = CurSortPort[2].ID[0].ToString("X2") + CurSortPort[2].ID[1].ToString("X2");
                    if ("0000".Equals(StrReferID))
                    {
                        items[0].SubItems[5].Text = "****";
                    }
                    else
                    {
                        if (null == CurSortPort[2].Name || "".Equals(CurSortPort[2].Name))
                        {
                            items[0].SubItems[5].Text = StrReferID;
                        }
                        else
                        {
                            items[0].SubItems[5].Text = CurSortPort[2].Name + "(" + StrReferID + ")";
                        }
                    }
                    if (CurSortPort[2].Distanse > 0 && CurSortPort[2].Distanse < int.MaxValue)
                    {
                        items[0].SubItems[6].Text = Math.Round(CurSortPort[2].Distanse, 2).ToString() + " CM";
                        d3 = Math.Round(CurSortPort[2].Distanse, 2);
                        port3_calc = items[0].SubItems[5].Text;

                        //int portindex = myPortList.FindIndex(x => x.id == port3_calc);
                        //myPortList[portindex].distance = d3;


                        redis_data.port3 = items[0].SubItems[5].Text;
                        redis_data.distance3 = items[0].SubItems[6].Text;

                        
                    }
                    else
                    {
                        items[0].SubItems[6].Text = "****";

                        d3 = -1;
                        port3_calc = items[0].SubItems[5].Text;

                        redis_data.port3 = items[0].SubItems[5].Text;
                        redis_data.distance3 = "null";


                    }

                    if (CurSortPort[2].isOptimal && !"****".Equals(items[0].SubItems[5].Text))
                    {
                        items[0].SubItems[5].ForeColor = PriCellColor;
                    }
                    else
                    {
                        items[0].SubItems[5].ForeColor = Color.Black;
                    }


                    StrReferID = CurSortPort[3].ID[0].ToString("X2") + CurSortPort[3].ID[1].ToString("X2");
                   
                    if ("0000".Equals(StrReferID))
                    {
                        items[0].SubItems[7].Text = "****";
                    }
                    else
                    {
                        if (null == CurSortPort[3].Name || "".Equals(CurSortPort[3].Name))
                        {
                            items[0].SubItems[7].Text = StrReferID;
                        }
                        else
                        {
                            items[0].SubItems[7].Text = CurSortPort[3].Name + "(" + StrReferID + ")";
                        }
                    }
                    if (CurSortPort[3].Distanse > 0 && CurSortPort[3].Distanse < int.MaxValue)
                    {
                        items[0].SubItems[8].Text = Math.Round(CurSortPort[3].Distanse, 2).ToString() + " CM";
                        d4 = Math.Round(CurSortPort[3].Distanse, 2);
                        port4_calc = items[0].SubItems[7].Text;

                        //int portindex = myPortList.FindIndex(x => x.id == port4_calc);
                        //myPortList[portindex].distance = d4;

                        redis_data.port4 = items[0].SubItems[7].Text;
                        redis_data.distance4 = items[0].SubItems[8].Text;

                    }
                    else
                    { 
                        items[0].SubItems[8].Text = "****";

                        d4 = -1;
                        port4_calc = items[0].SubItems[7].Text;

                        redis_data.port4 = items[0].SubItems[7].Text;
                        redis_data.distance4 = "null";

                    }
                        

                    if (CurSortPort[3].isOptimal && !"****".Equals(items[0].SubItems[7].Text))
                        items[0].SubItems[7].ForeColor = PriCellColor;
                    else items[0].SubItems[7].ForeColor = Color.Black;

                    StrReferID = CurSortPort[4].ID[0].ToString("X2") + CurSortPort[4].ID[1].ToString("X2");
                    //StrReferName = Ini.GetValue(Ini.PortPath,StrReferID,Ini.Name);
                    if ("0000".Equals(StrReferID))
                    {
                        items[0].SubItems[9].Text = "****";
                    }
                    else
                    {
                        if (null == CurSortPort[4].Name || "".Equals(CurSortPort[4].Name)) items[0].SubItems[9].Text = StrReferID;
                        else items[0].SubItems[9].Text = CurSortPort[4].Name + "(" + StrReferID + ")";
                    }
                    if (CurSortPort[4].Distanse > 0 && CurSortPort[4].Distanse < int.MaxValue)
                    {
                        items[0].SubItems[10].Text = Math.Round(CurSortPort[4].Distanse, 2).ToString() + " CM";
                        d5 = Math.Round(CurSortPort[4].Distanse, 2);
                        port5_calc = items[0].SubItems[9].Text;

                        //int portindex = myPortList.FindIndex(x => x.id == port5_calc);
                        //myPortList[portindex].distance = d5;

                        redis_data.port5 = items[0].SubItems[9].Text;
                        redis_data.distance5 = items[0].SubItems[10].Text;
                    }
                    else
                    {
                        items[0].SubItems[10].Text = "****";

                        d5 = -1;
                        port5_calc = items[0].SubItems[9].Text;

                        redis_data.port5 = items[0].SubItems[9].Text;
                        redis_data.distance5 = "null";
                    }
                        



                    if (CurSortPort[4].isOptimal && !"****".Equals(items[0].SubItems[9].Text))
                        items[0].SubItems[9].ForeColor = PriCellColor;
                    else items[0].SubItems[9].ForeColor = Color.Black;

                    items[0].SubItems[11].Text = "X: " + ((float)tag.Value.GsensorX / 100).ToString("0.00") + "g Y: " + ((float)tag.Value.GsensorY / 100).ToString("0.00") + "g Z: " + ((float)tag.Value.GsensorZ / 100).ToString("0.00") + "g";    
                    redis_data.Gsensor = items[0].SubItems[11].Text;
                    


                    items[0].SubItems[12].Text = tag.Value.Battery.ToString() + " %";
                    redis_data.battery = items[0].SubItems[12].Text;
                    

                    items[0].SubItems[13].Text = ((int)tag.Value.St * 100).ToString() + " ms";
                    redis_data.sleeptime = items[0].SubItems[13].Text;
                    

                    items[0].SubItems[14].Text = tag.Value.No_Exe_Time.ToString() + " S";
                    redis_data.without_move = items[0].SubItems[14].Text;
                    

                    items[0].SubItems[15].Text = tag.Value.ReceiTime.ToString();
                    redis_data.last_receive = items[0].SubItems[15].Text;
                    

                    items[0].SubItems[16].Text = Math.Round((DateTime.Now - tag.Value.ReceiTime).TotalSeconds).ToString() + " s";
                    redis_data.intervals = items[0].SubItems[16].Text;
                    

                    items[0].SubItems[17].Text = tag.Value.LossPack + "";
                    redis_data.packet_loss = items[0].SubItems[17].Text;
                    
          


                    items[0].SubItems[18].Text = tag.Value.TotalPack+"";
                    redis_data.packet_receive = items[0].SubItems[18].Text;



                    temp_last_without_move = tag.Value.No_Exe_Time;
                    temp_last_receive_packet = tag.Value.TotalPack;
                    card_counter_data.id = redis_data.id;

                    //有動有開=0
                    //有動沒開=1
                    //沒動有開=2
                    //沒動沒開=3


                    int index = myCardExistLists.FindIndex(x => x.id == card_counter_data.id);
                    if (index==-1)
                    {
                        card_counter_data.packet_receive = temp_last_receive_packet;
                        card_counter_data.without_move = temp_last_without_move;
                        card_counter_data.exist = 0;
                        myCardExistLists.Add(card_counter_data);
                    }
                    else
                    {
                        if(temp_last_receive_packet == myCardExistLists[index].packet_receive)
                        {
                            myCardExistLists[index].packet_receive = temp_last_receive_packet;
                            myCardExistLists[index].without_move = temp_last_without_move;
                            myCardExistLists[index].exist = 1;
                            redis_data.exist = 1;
                            //if(temp_last_without_move == 0)
                            //{
                            //    myCardExistLists[index].packet_receive = temp_last_receive_packet;
                            //    myCardExistLists[index].without_move = temp_last_without_move;
                            //    myCardExistLists[index].exist = 1;
                            //    redis_data.exist = 1;
                            //}
                            //
                            //if (temp_last_without_move > 0)
                            //{
                            //    myCardExistLists[index].packet_receive = temp_last_receive_packet;
                            //    myCardExistLists[index].without_move = temp_last_without_move;
                            //    myCardExistLists[index].exist = 1;//3
                            //    redis_data.exist = 1;//3
                            //}
                        }
                        else
                        {
                            myCardExistLists[index].packet_receive = temp_last_receive_packet;
                            myCardExistLists[index].without_move = temp_last_without_move;
                            myCardExistLists[index].exist = 0;
                            redis_data.exist = 0;

                            //if (temp_last_without_move == 0)
                            //{
                            //    myCardExistLists[index].packet_receive = temp_last_receive_packet;
                            //    myCardExistLists[index].without_move = temp_last_without_move;
                            //    myCardExistLists[index].exist = 0;
                            //    redis_data.exist = 0;
                            //}
                            //
                            //if (temp_last_without_move > 0)
                            //{
                            //    myCardExistLists[index].packet_receive = temp_last_receive_packet;
                            //    myCardExistLists[index].without_move = temp_last_without_move;
                            //    myCardExistLists[index].exist = 0;//2
                            //    redis_data.exist = 0;//2
                            //}
                        }
                        
                    }

                    //以下註解為呼叫python作法

                    //string[] strArr = new string[2];//引數列表
                    //string sArguments = @"csharp.py";//這裡是python的檔名字
                    //strArr[0] = "2";//參數
                    //strArr[1] = "3";//參數
                    //RunPythonScript(sArguments, "-u", strArr);


                    //string[] dArr = new string[8];//引數列表
                    //string sArguments = @"csharpdis.py";//這裡是python的檔名字
                    //dArr[0] = d1.ToString();//參數
                    //dArr[1] = port1_calc;//參數
                    //Console.WriteLine(dArr[1]);
                    //Console.WriteLine(dArr[0]);

                    //dArr[2] = d2.ToString();//參數
                    //dArr[3] = port2_calc;//參數
                    //Console.WriteLine(dArr[3]);
                    //Console.WriteLine(dArr[2]);

                    //dArr[4] = d3.ToString();//參數
                    //dArr[5] = port3_calc;//參數
                    //Console.WriteLine(dArr[5]);
                    //Console.WriteLine(dArr[4]);
                    //
                    //dArr[6] = d4.ToString();//參數
                    //dArr[7] = port4_calc;//參數
                    //Console.WriteLine(dArr[7]);
                    //Console.WriteLine(dArr[6]);
                    //
                    //
                    //
                    //RunPythonScript(sArguments, "-u", dArr);
                    //Console.WriteLine(temp);


                    //List<port_counter> list_need_calc = myPortList.FindAll(x => x.distance != -1);
                    //
                    //if (list_need_calc.Count >= 3)
                    //{
                    //    int port_list_length = list_need_calc.Count;
                    //    double[] port_z = new double[port_list_length];
                    //    int k;
                    //    for (k = 0; k < port_list_length; k++)
                    //    {
                    //        port_z[k] = list_need_calc[k].position[2];
                    //    }
                    //
                    //    int max_z_count = 0;
                    //    double max_z = 0;
                    //
                    //    foreach (var s in port_z.GroupBy(c => c))
                    //    {
                    //        if (s.Count() > max_z_count)
                    //        {
                    //            max_z_count = s.Count();
                    //            max_z = s.Key;
                    //        }
                    //    }
                    //
                    //
                    //    List<port_counter> listFind_plane_z = list_need_calc.FindAll(x => x.position[2] == max_z);
                    //    List<port_counter> listFind_z = list_need_calc.FindAll(x => x.position[2] != max_z);
                    //
                    //    if (listFind_plane_z.Count >= 3)
                    //    {
                    //        X1[0] = listFind_plane_z[0].position[0];
                    //        X1[1] = listFind_plane_z[0].position[1];
                    //        X1[2] = listFind_plane_z[0].position[2];
                    //        X2[0] = listFind_plane_z[1].position[0];
                    //        X2[1] = listFind_plane_z[1].position[1];
                    //        X2[2] = listFind_plane_z[1].position[2];
                    //        X3[0] = listFind_plane_z[2].position[0];
                    //        X3[1] = listFind_plane_z[2].position[1];
                    //        X3[2] = listFind_plane_z[2].position[2];
                    //
                    //        if (listFind_z.Count >= 1)
                    //        {
                    //            X4[0] = listFind_z[0].position[0];
                    //            X4[1] = listFind_z[0].position[1];
                    //            X4[2] = listFind_z[0].position[2];
                    //
                    //            Console.WriteLine(X1[0]);
                    //            Console.WriteLine(X1[1]);
                    //            Console.WriteLine(X1[2]);
                    //            Console.WriteLine(X2[0]);
                    //            Console.WriteLine(X2[1]);
                    //            Console.WriteLine(X2[2]);
                    //
                    //            Console.WriteLine(X3[0]);
                    //            Console.WriteLine(X3[1]);
                    //            Console.WriteLine(X3[2]);
                    //
                    //            Console.WriteLine(X4[0]);
                    //            Console.WriteLine(X4[1]);
                    //            Console.WriteLine(X4[2]);
                    //
                    //            triposition1(X1[0], X1[1], X1[2], listFind_plane_z[0].distance, listFind_plane_z[0].id, X2[0], X2[1], X2[2], listFind_plane_z[1].distance, listFind_plane_z[1].id, X3[0], X3[1], X3[2], listFind_plane_z[2].distance, listFind_plane_z[2].id, X4[0], X4[1], X4[2], listFind_z[0].distance, listFind_z[0].id);
                    //        }
                    //        else
                    //        {
                    //            X4[0] = -1;
                    //            X4[1] = -1;
                    //            X4[2] = -1;
                    //
                    //            Console.WriteLine(X1[0]);
                    //            Console.WriteLine(X1[1]);
                    //            Console.WriteLine(X1[2]);
                    //            Console.WriteLine(X2[0]);
                    //            Console.WriteLine(X2[1]);
                    //            Console.WriteLine(X2[2]);
                    //
                    //            Console.WriteLine(X3[0]);
                    //            Console.WriteLine(X3[1]);
                    //            Console.WriteLine(X3[2]);
                    //
                    //            Console.WriteLine(X4[0]);
                    //            Console.WriteLine(X4[1]);
                    //            Console.WriteLine(X4[2]);
                    //
                    //            triposition1(X1[0], X1[1], X1[2], listFind_plane_z[0].distance, listFind_plane_z[0].id, X2[0], X2[1], X2[2], listFind_plane_z[1].distance, listFind_plane_z[1].id, X3[0], X3[1], X3[2], listFind_plane_z[2].distance, listFind_plane_z[2].id, X4[0], X4[1], X4[2], -1, "none");
                    //
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (listFind_plane_z.Count < 3 && list_need_calc.Count >= 4)
                    //        {
                    //            X1[0] = list_need_calc[0].position[0];
                    //            X1[1] = list_need_calc[0].position[1];
                    //            X1[2] = list_need_calc[0].position[2];
                    //            X2[0] = list_need_calc[1].position[0];
                    //            X2[1] = list_need_calc[1].position[1];
                    //            X2[2] = list_need_calc[1].position[2];
                    //            X3[0] = list_need_calc[2].position[0];
                    //            X3[1] = list_need_calc[2].position[1];
                    //            X3[2] = list_need_calc[2].position[2];
                    //            X4[0] = list_need_calc[3].position[0];
                    //            X4[1] = list_need_calc[3].position[1];
                    //            X4[2] = list_need_calc[3].position[2];
                    //            triposition2(X1[0], X1[1], X1[2], list_need_calc[0].distance, list_need_calc[0].id, X2[0], X2[1], X2[2], list_need_calc[1].distance, list_need_calc[1].id, X3[0], X3[1], X3[2], list_need_calc[2].distance, list_need_calc[2].id, X4[0], X4[1], X4[2], list_need_calc[3].distance, list_need_calc[3].id);
                    //
                    //        }
                    //        else
                    //        {
                    //            verification = "none";
                    //            verification1 = "none";
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    verification = "none";
                    //    verification1 = "none";
                    //}
                    
                    

                    triposition1(X1[0], X1[1], X1[2], d1, port1_calc, X2[0], X2[1], X2[2], d2, port2_calc, X3[0], X3[1], X3[2], d3, port3_calc, X4[0], X4[1], X4[2], d4, port4_calc);

                    if (verification=="have"&& verification1 == "have")
                    {
                        compare_distance = Math.Pow(ans[0] - ans1[0], 2) + Math.Pow(ans[1] - ans1[1], 2) + Math.Pow(ans[2] - ans1[2], 2);
                        compare_distance = Math.Pow(compare_distance, 0.5);
                        redis_data.check = compare_distance.ToString();
                        final_ans[0] = ans[0];
                        final_ans[1] = ans[1];
                        final_ans[2] = ans[2];

                        redis_data.X = final_ans[0].ToString();
                        redis_data.Y = final_ans[1].ToString();
                        redis_data.Z = final_ans[2].ToString();

                        redis_data.coord = temp;
                    }
                    else if(verification == "have" && verification1 == "none")
                    {
                        redis_data.check = "null3";
                        final_ans[0] = ans[0];
                        final_ans[1] = ans[1];
                        final_ans[2] = ans[2];

                        redis_data.X = final_ans[0].ToString();
                        redis_data.Y = final_ans[1].ToString();
                        redis_data.Z = final_ans[2].ToString();

                        redis_data.coord = temp;
                    }
                    else if(verification == "none" && verification1 == "have")
                    {
                        redis_data.check = "null4";
                        final_ans[0] = ans1[0];
                        final_ans[1] = ans1[1];
                        final_ans[2] = ans1[2];

                        redis_data.X = final_ans[0].ToString();
                        redis_data.Y = final_ans[1].ToString();
                        redis_data.Z = final_ans[2].ToString();

                        redis_data.coord = temp1;
                    }
                    else
                    {
                        redis_data.check = "null_all";

                        //string last_time_data = (String)db.ListLeftPop(redis_data.id);
                        //string[] last_time_word = last_time_data.Split('"');
                        //redis_data.coord = last_time_word[87];

                        //string last_time_data = (String)db.HashGet("distance", redis_data.id);
                        int go = 0;
                        string last_time_data = (String)db.ListGetByIndex("card_location",go);
                        string[] last_time_word = last_time_data.Split('"');
                        while(last_time_word[3] != redis_data.id)
                        {
                            go = go + 1;
                            last_time_data = (String)db.ListGetByIndex("card_location",go);
                            last_time_word = last_time_data.Split('"');
                        }

                        redis_data.X = (last_time_word[7]);
                        redis_data.Y = (last_time_word[11]);
                        redis_data.Z = (last_time_word[15]);
                        redis_data.coord = last_time_word[99];


                    }

                    DateTime myDate_redis = DateTime.Now;
                    redis_data.time = myDate_redis.ToString("yyyy-MM-dd HH:mm:ss");

                    

                    string json = JsonConvert.SerializeObject(redis_data);//把物件壓成可以丟入redis的狀態(value)
                    //db.HashSet("distance",redis_data.id, json);//參數分別是: 資料庫名稱，key，value


                    //db_set.HashSet("setting_data", redis_data.id, redis_data.time);//參數分別是: 資料庫名稱，key，value
                    //db.ListLeftPush(redis_data.id, json);
                    db.ListLeftPush("card_count", myCardExistLists.Count);
                    db.ListLeftPush("card_location", json);

                    


                    String connetStr_mysql = "server=localhost;user=root;password=esfortest; database=test;";//database是資料庫名字
                                                                                                       // server=127.0.0.1/localhost 代表本機，埠號port預設是3306可以不寫
                    MySqlConnection conn_mysql = new MySqlConnection(connetStr_mysql);
                    try
                    {
                        conn_mysql.Open();//開啟通道，建立連線，可能出現異常,使用try catch語句
                        Console.WriteLine("已經建立連線");
                        //在這裡使用程式碼對資料庫進行增刪查改
                        //在 test_table 資料表新增一筆資料
                        //string sql_point = "INSERT INTO coord_table ( `id` ,`point`, `time`) VALUES ('" + redis_data.id + "','" + redis_data.coord + "','" + redis_data.time + "')";
                        //string sql = "INSERT INTO data_table ( `CardID` ,`point`, `checkpoint`,`port1`, `port1Distance`,`port2`, `port2Distance`,`port3`, `port3Distance`,`port4`, `port4Distance`, `port5`, `port5Distance`,`Battery`,`Sleeptime`,`without_moving`,`last_time`,`card_interval`,`packet_loss`,`received_packet`,`mytime`) VALUES ('" + redis_data.id + "','" + redis_data.coord + "','" + redis_data.check + "','" + redis_data.port1 + "','" + redis_data.distance1 + "','" + redis_data.port2 + "','" + redis_data.distance2 + "','" + redis_data.port3 + "','" + redis_data.distance3 + "','" + redis_data.port4 + "','" + redis_data.distance4 + "','" + redis_data.port5 + "','" + redis_data.distance5 + "','" + redis_data.battery + "','" + redis_data.sleeptime + "','" + redis_data.without_move + "','" + redis_data.last_receive + "','" + redis_data.intervals + "','" + redis_data.packet_loss + "','" + redis_data.packet_receive + "','" + redis_data.time + "')";
                        string sql = "INSERT INTO data_table ( `CardID` ,`X`, `Y`,`Z`,`exist`,`checkpoint`,`packet_received`,`mytime`,`port1`, `port1Distance`,`port2`, `port2Distance`,`port3`, `port3Distance`,`port4`, `port4Distance`, `port5`, `port5Distance`,`Battery`,`Sleeptime`,`without_moving`,`last_time`,`card_interval`,`packet_loss`) VALUES ('" + redis_data.id + "','" + final_ans[0] + "','" + final_ans[1] + "','" + final_ans[2] + "','" + redis_data.exist + "','" + redis_data.check + "','" + redis_data.packet_receive + "','" + redis_data.time + "','" + redis_data.port1 + "','" + redis_data.distance1 + "','" + redis_data.port2 + "','" + redis_data.distance2 + "','" + redis_data.port3 + "','" + redis_data.distance3 + "','" + redis_data.port4 + "','" + redis_data.distance4 + "','" + redis_data.port5 + "','" + redis_data.distance5 + "','" + redis_data.battery + "','" + redis_data.sleeptime + "','" + redis_data.without_move + "','" + redis_data.last_receive + "','" + redis_data.intervals + "','" + redis_data.packet_loss + "')";
                        //MySqlCommand cmd_point = new MySqlCommand(sql_point, conn_mysql);
                        MySqlCommand cmd = new MySqlCommand(sql, conn_mysql);
                        int n = cmd.ExecuteNonQuery();
                        //int m = cmd_point.ExecuteNonQuery();
                        //列出新增的筆數
                        //Console.WriteLine("共新增 {0} 筆資料", n);
                    }
                    catch (MySqlException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        conn_mysql.Close();
                        //Console.WriteLine("已經退出連線");
                    }



                    continue;
                }
                item = new ListViewItem();
                item.UseItemStyleForSubItems = false;
                
                item.Name = tag.Key;
                string StrName = Ini.GetValue(Ini.CardPath,tag.Key,Ini.Name);
                if (null == StrName || "".Equals(StrName)) item.Text = tag.Key;
                else item.Text = StrName + "("+tag.Key+")";
                StrReferID = CurSortPort[0].ID[0].ToString("X2") + CurSortPort[0].ID[1].ToString("X2");
                if ("0000".Equals(StrReferID))
                {
                    item.SubItems.Add("****");
                }
                else
                {
                    if (null == CurSortPort[0].Name || "".Equals(CurSortPort[0].Name))
                        item.SubItems.Add(StrReferID);
                    else item.SubItems.Add(CurSortPort[0].Name + "(" + StrReferID + ")");
                }
                if (CurSortPort[0].Distanse > 0 && CurSortPort[0].Distanse < int.MaxValue)
                    item.SubItems.Add(Math.Round(CurSortPort[0].Distanse, 2).ToString() + " CM");
                else item.SubItems.Add("****");

                if (CurSortPort[0].isOptimal && !"****".Equals(item.SubItems[1].Text))
                    item.SubItems[1].ForeColor = PriCellColor;
                else item.SubItems[1].ForeColor = Color.Black;

                StrReferID = CurSortPort[1].ID[0].ToString("X2") + CurSortPort[1].ID[1].ToString("X2");

                if ("0000".Equals(StrReferID))
                {
                    item.SubItems.Add("****");
                }
                else
                {
                    if (null == CurSortPort[1].Name || "".Equals(CurSortPort[1].Name)) item.SubItems.Add(StrReferID);
                    else item.SubItems.Add(CurSortPort[1].Name + "(" + StrReferID + ")");
                }
                if (CurSortPort[1].Distanse > 0 && CurSortPort[1].Distanse < int.MaxValue)
                    item.SubItems.Add(Math.Round(CurSortPort[1].Distanse, 2).ToString() + " CM");
                else item.SubItems.Add("****");

                if (CurSortPort[1].isOptimal && !"****".Equals(item.SubItems[3].Text))
                {
                    item.SubItems[3].ForeColor = PriCellColor;
                }
                else
                {
                    item.SubItems[3].ForeColor = Color.Black;
                }
                StrReferID = CurSortPort[2].ID[0].ToString("X2") + CurSortPort[2].ID[1].ToString("X2");
                if ("0000".Equals(StrReferID))
                {
                    item.SubItems.Add("****");
                }
                else
                {
                    if (null == CurSortPort[2].Name || "".Equals(CurSortPort[2].Name))
                    {
                        item.SubItems.Add(StrReferID);
                    }
                    else
                    {
                        item.SubItems.Add(CurSortPort[2].Name + "(" + StrReferID + ")");
                    }
                }
                if (CurSortPort[2].Distanse > 0 && CurSortPort[2].Distanse < int.MaxValue)
                {
                    item.SubItems.Add(Math.Round(CurSortPort[2].Distanse, 2).ToString() + " CM");
                }
                else
                {
                    item.SubItems.Add("****");
                }

                if (CurSortPort[2].isOptimal && !"****".Equals(item.SubItems[5].Text))
                {
                    item.SubItems[5].ForeColor = PriCellColor;
                }
                else
                {
                    item.SubItems[5].ForeColor = Color.Black;
                }

                StrReferID = CurSortPort[3].ID[0].ToString("X2") + CurSortPort[3].ID[1].ToString("X2");
                //StrReferName = Ini.GetValue(Ini.PortPath,StrReferID,Ini.Name);
                if ("0000".Equals(StrReferID))
                {
                    item.SubItems.Add("****");
                }
                else
                {
                    if (null == CurSortPort[3].Name || "".Equals(CurSortPort[3].Name))
                    {
                        item.SubItems.Add(StrReferID);
                    }
                    else
                    {
                        item.SubItems.Add(CurSortPort[3].Name + "(" + StrReferID + ")");
                    }
                }
                if (CurSortPort[3].Distanse > 0 && CurSortPort[3].Distanse < int.MaxValue)
                {
                    item.SubItems.Add(Math.Round(CurSortPort[3].Distanse, 2).ToString() + " CM");
                }
                else
                {
                    item.SubItems.Add("****");
                }

                if (CurSortPort[3].isOptimal && !"****".Equals(item.SubItems[7].Text))
                {
                    item.SubItems[7].ForeColor = PriCellColor;
                }
                else
                {
                    item.SubItems[7].ForeColor = Color.Black;
                }

                StrReferID = CurSortPort[4].ID[0].ToString("X2") + CurSortPort[4].ID[1].ToString("X2");
                
                if ("0000".Equals(StrReferID))
                {
                    item.SubItems.Add("****");
                }
                else
                {
                    if (null == CurSortPort[4].Name || "".Equals(CurSortPort[4].Name))
                    {
                        item.SubItems.Add(StrReferID);
                    }
                    else
                    {
                        item.SubItems.Add(CurSortPort[4].Name + "(" + StrReferID + ")");
                    }
                }
                if (CurSortPort[4].Distanse > 0 && CurSortPort[4].Distanse < int.MaxValue)
                {
                    item.SubItems.Add(Math.Round(CurSortPort[4].Distanse, 2).ToString() + " CM");
                }
                else
                {
                    item.SubItems.Add("****");
                }
                if (CurSortPort[4].isOptimal && !"****".Equals(item.SubItems[9].Text))
                {
                    item.SubItems[9].ForeColor = PriCellColor;
                }
                else
                {
                    item.SubItems[9].ForeColor = Color.Black;
                }
                item.SubItems.Add("X: " + ((float)tag.Value.GsensorX / 100).ToString("0.00") + "g Y: " + ((float)tag.Value.GsensorY / 100).ToString("0.00") + "g Z: " + ((float)tag.Value.GsensorZ / 100).ToString("0.00") + "g");
                item.SubItems.Add(tag.Value.Battery.ToString() + " %");
                item.SubItems.Add(((int)tag.Value.St*100).ToString() + " ms");
                item.SubItems.Add(tag.Value.No_Exe_Time.ToString() + " S");
                item.SubItems.Add(tag.Value.ReceiTime.ToString());
                item.SubItems.Add(Math.Round((DateTime.Now - tag.Value.ReceiTime).TotalSeconds).ToString() + " s");
                item.SubItems.Add(tag.Value.LossPack+"");
                item.SubItems.Add(tag.Value.TotalPack+"");
                CardList_listView.Items.Add(item);
            }
            label14.Text = "Total Number: " + CardList_listView.Items.Count;
        }
        private void Sele_PortInfor_Btn_Click(object sender, EventArgs e)
        {
            if (isStart)
            {
                return;
            }
            PrecisePosition.PortInfor MyPortInfor = new PrecisePosition.PortInfor(this);
            MyPortInfor.ShowDialog();
        }
        private void Map_panel_Click(object sender, EventArgs e)
        {
            Map_panel.Focus();
            //已经开始监控了，不允许此时删除参考点
            if (isStart)
            {
                return;
            }
            if (((MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Left)
            {
                MouseEventArgs ex = (MouseEventArgs)e;
                float x = -1, y = -1;
                double d0, d1, L0, L1, p0, p1;
                string strid = "";
                if (Parameter.isSupportMulArea)
                {
                    foreach(KeyValuePair<string, PrecisePositionLibrary.BsInfo> bs in group.groupbss)
                    {
                        if (bs.Value.Place != null && bs.Value != null)
                        {
                            //计算当前的参考点绝对坐标
                            //其中Br.Value.x、 Br.Value.y是当Scale = 1，中心在面板中心时饿坐标
                            x = (float)bs.Value.Place.x + (int)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
                            y = (float)bs.Value.Place.y + (int)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
                            d0 = d1 = L0 = L1 = p0 = p1 = 0;
                            d0 = Math.Abs(DxfMapParam.CenterY - y);
                            d1 = Math.Abs(DxfMapParam.CenterX - x);
                            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
                            L1 = L0 / DxfMapParam.scale;
                            p0 = (d0 / L0) * L1;
                            p1 = (d1 / L0) * L1;
                            if (x < DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                            {//位于左上象限
                                x = (float)(DxfMapParam.CenterX - p1);
                                y = (float)(DxfMapParam.CenterY - p0);
                            }
                            else if (x > DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                            {//位于右上象限
                                x = (float)(DxfMapParam.CenterX + p1);
                                y = (float)(DxfMapParam.CenterY - p0);
                            }
                            else if (x < DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                            {//位于左下象限
                                x = (float)(DxfMapParam.CenterX - p1);
                                y = (float)(DxfMapParam.CenterY + p0);
                            }
                            else if (x > DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                            {//位于右下象限
                                x = (float)(DxfMapParam.CenterX + p1);
                                y = (float)(DxfMapParam.CenterY + p0);
                            }
                            double CurWidth = PortWidth * 1 / DxfMapParam.scale;
                            double CurHeight = PortHeight * 1 / DxfMapParam.scale;
                            if (ex.X > x - (CurWidth / 2) && ex.X < x + (CurWidth / 2) && ex.Y > y - (CurHeight / 2) && ex.Y < y + (CurHeight / 2))
                            {
                                //点击了参考点
                                if (isMove)
                                {
                                    return;
                                }
                                if (null != group)
                                {
                                    strid = group.id[0].ToString("X2") + group.id[1].ToString("X2");
                                }
                                AddPort MyAddPort = null;
                                if (Parameter.isSupportMulArea)
                                {
                                    MyAddPort = new AddPort(this, bs.Key, strid);
                                }
                                else
                                {
                                    MyAddPort = new AddPort(this, bs.Key);
                                }
                                MyAddPort.ShowDialog();
                                Map_panel_Paint(null, null);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, PrecisePositionLibrary.BsInfo> bs in InnerPorts)
                    {
                        if (bs.Value.Place != null && bs.Value != null)
                        {
                            //计算当前的参考点绝对坐标
                            //其中Br.Value.x、 Br.Value.y是当Scale = 1，中心在面板中心时饿坐标
                            x = (float)bs.Value.Place.x + (int)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
                            y = (float)bs.Value.Place.y + (int)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
                            d0 = d1 = L0 = L1 = p0 = p1 = 0;
                            d0 = Math.Abs(DxfMapParam.CenterY - y);
                            d1 = Math.Abs(DxfMapParam.CenterX - x);
                            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
                            L1 = L0 / DxfMapParam.scale;
                            p0 = (d0 / L0) * L1;
                            p1 = (d1 / L0) * L1;
                            if (x < DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                            {//位于左上象限
                                x = (float)(DxfMapParam.CenterX - p1);
                                y = (float)(DxfMapParam.CenterY - p0);
                            }
                            else if (x > DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                            {//位于右上象限
                                x = (float)(DxfMapParam.CenterX + p1);
                                y = (float)(DxfMapParam.CenterY - p0);
                            }
                            else if (x < DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                            {//位于左下象限
                                x = (float)(DxfMapParam.CenterX - p1);
                                y = (float)(DxfMapParam.CenterY + p0);
                            }
                            else if (x > DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                            {//位于右下象限
                                x = (float)(DxfMapParam.CenterX + p1);
                                y = (float)(DxfMapParam.CenterY + p0);
                            }
                            double CurWidth = PortWidth * 1 / DxfMapParam.scale;
                            double CurHeight = PortHeight * 1 / DxfMapParam.scale;
                            if (ex.X > x - (CurWidth / 2) && ex.X < x + (CurWidth / 2) && ex.Y > y - (CurHeight / 2) && ex.Y < y + (CurHeight / 2))
                            {
                                //点击了参考点
                                if (isMove) return;
                                AddPort MyAddPort = new AddPort(this, bs.Key);
                                MyAddPort.ShowDialog();
                                Map_panel_Paint(null, null);
                                return;
                            }
                        }
                    }
                }
            }
        }
        private void Map_panel_DoubleClick(object sender, EventArgs e)
        {
            if (isStart)
            {
                return;
            }
            MouseEventArgs ex = (MouseEventArgs)e;
            PrecisePositionLibrary.Point point = new PrecisePositionLibrary.Point(ex.X, ex.Y, 0);

            double MapWidth = 0, MapHeight = 0;
            if (Parameter.isSupportMulArea)
            {
                MapWidth = group.scale * group.actualwidth / DxfMapParam.scale;
                MapHeight = group.scale * group.actualheight / DxfMapParam.scale;
            }
            else
            {
                if (Img_RealDisRelation > 0)
                {
                    MapWidth = Img_RealDisRelation * Parameter.RealWidth / DxfMapParam.scale;
                    MapHeight = Img_RealDisRelation * Parameter.RealHeight / DxfMapParam.scale;
                }
            }
            if (ex.X < DxfMapParam.CenterX - MapWidth / 2 || ex.X > DxfMapParam.CenterX + MapWidth / 2 || ex.Y < DxfMapParam.CenterY - MapHeight / 2 || ex.Y > DxfMapParam.CenterY + MapHeight / 2)
            {
                MessageBox.Show("The position of the reference point can't exceed map！");
                return;
            }
            
            if (Parameter.isSupportMulArea)
            {
                String strid = "";
                // 获取当前选择的区域ID
                String strarea =  SelectAreaCB.Text;
                int index1 = strarea.LastIndexOf("(");
                int index2 = strarea.LastIndexOf(")");
                if (index1 >= 0 && index2 >= 0)
                {
                    strid = strarea.Substring(index1 + 1, strarea.Length - index1 - 2);
                }
                else
                {
                    strid = strarea;
                }
                AddPort MyAddPort = new AddPort(this, point, strid);
                MyAddPort.ShowDialog();
                foreach (KeyValuePair<string, BsInfo> bsinfor in group.groupbss)
                {
                    InnerPorts.TryAdd(bsinfor.Key, bsinfor.Value);
                }
            }
            else
            {

                AddPort MyAddPort = new AddPort(this, point);
                MyAddPort.ShowDialog();
            }
            Map_panel_Paint(null, null);
        }
        private void SinglePort(Graphics g, String StrID, String StrName,PortType mporttype,int X, int Y)
        {
            Brush brush = null;
            if (null == g)
            {
                return;
            }
            if (null == StrID || "".Equals(StrID))
            {
                return;
            }
            StringBuilder strtaginfor = new StringBuilder();
            //判断当前是单点还是多点
            if (Parameter.isEnableReferType)
            {
                if (mporttype == PortType.SingleMode)
                {
                    brush = Brushes.Black;
                }
                else
                {
                    brush = Brushes.Blue;
                }
            }
            else
            {
                brush = Brushes.Blue;
            }
            if (null == StrName || "".Equals(StrName))
            {
                if (Parameter.isEnableReferType)
                {
                    if (mporttype == PortType.SingleMode)
                    {
                        strtaginfor.Append("Single-");
                        strtaginfor.Append(StrID);
                    }
                    else
                    {
                        strtaginfor.Append("Three-");
                        strtaginfor.Append(StrID);
                    }
                }
                else
                {
                    strtaginfor.Append(StrID);
                }
            }
            else
            {
                if (Parameter.isEnableReferType)
                {
                    if (mporttype == PortType.SingleMode)
                    {
                        strtaginfor.Append("Single-");
                        strtaginfor.Append(StrName);
                        strtaginfor.Append("(");
                        strtaginfor.Append(StrID);
                        strtaginfor.Append(")");
                    }
                    else
                    {
                        strtaginfor.Append("Three-");
                        strtaginfor.Append(StrName);
                        strtaginfor.Append("(");
                        strtaginfor.Append(StrID);
                        strtaginfor.Append(")");
                    }
                }
                else
                {
                    strtaginfor.Append(StrName);
                    strtaginfor.Append("(");
                    strtaginfor.Append(StrID);
                    strtaginfor.Append(")");
                }
            }
            Pen pen = Pens.Black;
            Brush StrBrush = Brushes.Red;
            Font font = null;
            if (DxfMapParam.scale > 10)
            {
                font = new Font("宋体", 1);
            }
            else
            {
                font = new Font("宋体", (int)(10 / DxfMapParam.scale));
            }
            float x = -1, y = -1;
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            double CurWidth = PortWidth, CurHeight = PortHeight;
            //此时记录下的X、Y是scale = 1,且面板为CenterX=Map_panel.Width/2,CenterY = Map_panel.Height/2时的坐标
            x = (float)X + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)Y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {// 位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {// 位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {// 位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {// 位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            // 需要考虑面板的相对比例
            CurWidth = CurWidth * 1 / DxfMapParam.scale;
            CurHeight = CurHeight * 1 / DxfMapParam.scale;
            g.FillRectangle(brush, x - (float)CurWidth / 2, y - (float)CurHeight / 2, (float)CurWidth, (float)CurHeight);
            g.DrawString(strtaginfor.ToString(), font, StrBrush, (int)(x + CurWidth / 2), (int)(y - CurHeight / 2 + Str_OffSet));
        }

        private void DrawAllLimitArea(Graphics g)
        {
            if (isLimit && null != CurLimitArea)
            {// 画出当前的限制区域
                DrawLimitArea(g, CurLimitArea);
            }
            if (Parameter.isSupportMulArea)
            {//支持多区域
                if(null != group)
                {
                    foreach(KeyValuePair<string, LimitArea> area in group.grouplimiares)
                    {
                        if (null == area.Value)
                        {
                            continue;
                        }
                        DrawLimitArea(g, area.Value);
                    }
                }
            }
            else
            {//支持单区域 
                foreach (KeyValuePair<string, LimitArea> area in Areas)
                {
                    if (null == area.Value)
                    {
                        continue;
                    }
                    DrawLimitArea(g, area.Value);
                }
            }
        }
        private void DrawLimitArea(Graphics g,LimitArea area)
        {
            float x = -1, y = -1, ex = -1, ey = -1;
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            double CurWidth = PortWidth, CurHeight = PortHeight;
            //此时记录下的X、Y是 scale = 1,且面板为 CenterX = Map_panel.Width / 2,CenterY = Map_panel.Height / 2时的坐标
            
            x = (float)area.startpoint.x + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            ex = (float)area.endpoint.x + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)area.startpoint.y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            ey = (float)area.endpoint.y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {//位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {//位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {//位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {//位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            d0 = Math.Abs(DxfMapParam.CenterY - ey);
            d1 = Math.Abs(DxfMapParam.CenterX - ex);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (ex <= DxfMapParam.CenterX && ey < DxfMapParam.CenterY)
            {//位于左上象限
                ex = (float)(DxfMapParam.CenterX - p1);
                ey = (float)(DxfMapParam.CenterY - p0);
            }
            else if (ex > DxfMapParam.CenterX && ey <= DxfMapParam.CenterY)
            {//位于右上象限
                ex = (float)(DxfMapParam.CenterX + p1);
                ey = (float)(DxfMapParam.CenterY - p0);
            }
            else if (ex <= DxfMapParam.CenterX && ey > DxfMapParam.CenterY)
            {//位于左下象限
                ex = (float)(DxfMapParam.CenterX - p1);
                ey = (float)(DxfMapParam.CenterY + p0);
            }
            else if (ex > DxfMapParam.CenterX && ey >= DxfMapParam.CenterY)
            {//位于右下象限
                ex = (float)(DxfMapParam.CenterX + p1);
                ey = (float)(DxfMapParam.CenterY + p0);
            }
            //需要考虑面板的相对比例
            CurWidth = CurWidth * 1 / DxfMapParam.scale;
            CurHeight = CurHeight * 1 / DxfMapParam.scale;
            Font font = null;
            if (DxfMapParam.scale > 10)
            {
                font = new Font("宋体", 1);
            }
            else
            {
                font = new Font("宋体", (int)(10 / DxfMapParam.scale));
            }
            string strareamsg = "";
            if (area.ID[0] != 0 || area.ID[1] != 0)
            {
                if (null == area.Name || "".Equals(area.Name))
                {
                    strareamsg = area.ID[0].ToString("X2") + area.ID[1].ToString("X2");
                }
                else
                {
                    strareamsg = area.Name;
                }
            }
            g.DrawRectangle(new Pen(Brushes.Red, 2), x, y, Math.Abs(ex - x), Math.Abs(ey - y));
            //写字
            g.DrawString(strareamsg, font, Brushes.Blue, x + 1, y + 1);
        }
        /// <summary>
        /// 画多区域基站位置
        /// </summary>
        /// <param name="g"></param>
        /// <param name="strgroupid"></param>
        private void DrawMulPort(Graphics g,string strgroupid)
        {
            Group group = null;
            if (!Groups.TryGetValue(strgroupid, out group))
            {
                return;
            }
            PortType mporttype = PortType.ThreeMode;
            string StrName = "";
            foreach (KeyValuePair<string, BsInfo> bs in group.groupbss)
            {
                if (null == bs.Value)
                {
                    continue;
                }
                Bsmsg bsmsg = null;
                StrName = "";
                if (Bsmsgs.TryGetValue(bs.Key, out bsmsg))
                {
                    StrName = bsmsg.Name;
                    mporttype = bsmsg.porttype;
                }
                else
                {
                    mporttype = PortType.ThreeMode;
                }
                SinglePort(g, bs.Key, StrName, mporttype, (int)bs.Value.Place.x, (int)bs.Value.Place.y);
                if ((mporttype == PortType.SingleMode && Parameter.isEnableReferType) && (!isStart || isShowGuidesLine) && null != bsmsg)
                {
                    DrawSinglePortRange(group,bsmsg, (int)bs.Value.Place.x, (int)bs.Value.Place.y, g);
                }
            }
        }
        /// <summary>
        /// 画单区域基站位置
        /// </summary>
        /// <param name="g"></param>
        private void DrawPort(Graphics g) {
            Brush brush = Brushes.Blue;
            Pen pen = Pens.Black;
            Brush StrBrush = Brushes.Red;
            Font font = new Font("宋体", 10);
            string StrName = "";
            PortType mporttype = PortType.ThreeMode;
            foreach (KeyValuePair<string, PrecisePositionLibrary.BsInfo> port in InnerPorts)
            {
                if (null == port.Value)
                {
                    continue;
                }
                Bsmsg bsmsg = null;
                StrName = "";
                if (Bsmsgs.TryGetValue(port.Key, out bsmsg))
                {
                    StrName = bsmsg.Name;
                    mporttype = bsmsg.porttype;
                }
                SinglePort(g, port.Key, StrName, mporttype, (int)port.Value.Place.x, (int)port.Value.Place.y);
                if ((mporttype == PortType.SingleMode && Parameter.isEnableReferType) && (!isStart || isShowGuidesLine) && null != bsmsg)
                {//--我们画出单点定位基站范围--
                    DrawSinglePortRange(group, bsmsg, (int)port.Value.Place.x, (int)port.Value.Place.y, g);
                }
            }
        }
        private Object obj_draw = new Object();
        /// <summary>
        /// 使用线程刷新
        /// </summary>
        private void UpdateDrawFun()
        {
            // 重新定义一个图片，防止与其他画图部分冲突
            Bitmap DrawMap = null;
            // 画图面板的宽和高
            int boxwidth = 0, boxheight = 0;
            Graphics g = null;
            boxwidth = Map_panel.Width;
            boxheight = Map_panel.Height;
            List<KeyValuePair<String, LimitArea>> listares = null;
            List<KeyValuePair<String, PrecisePositionLibrary.BsInfo>> listbss = null;
            while (isUpdate)
            {
                Thread.Sleep(UpdateInterval);
                try
                {
                    lock (obj_draw) {
                        DrawMap = DxfMapParam.GetDxfMap(StrMapPath, DxfMapParam.scale, DxfMapParam.CenterX, DxfMapParam.CenterY, boxwidth, boxheight);
                    }
                    #region 没有获取到地图时，在面板中画出 "No Map"
                    if (null == DrawMap) {
                        //当没有获取到地图时画出“No Map”
                        DrawMap = new Bitmap((int)boxwidth, (int)boxheight);
                        g = Graphics.FromImage(DrawMap);
                        g.DrawString("No Map", new Font("宋体", 32), Brushes.Red, (float)(boxwidth / 2) - 60, (float)boxheight / 2 - 10);
                        continue;
                    }
                    #endregion
                    g = Graphics.FromImage(DrawMap);
                    #region 画出限制区域
                    if (Parameter.isEnableLimitArea) {
                        if (Parameter.isSupportMulArea) { //多区域
                            if (null != group) {
                                listares = group.grouplimiares.ToList();
                            }
                        } else { //单区域
                            listares = Areas.ToList();
                        }
                        if (null != listares) {
                            foreach (KeyValuePair<string, LimitArea> area in listares) {
                                DrawLimitArea(g, area.Value);
                            }
                        }
                    }
                    #endregion
                    #region 画出基站坐标
                    if (Parameter.ShowPlacePort) {
                        Brush brush = Brushes.Blue;
                        Pen pen = Pens.Black;
                        Brush StrBrush = Brushes.Red;
                        Font font = new Font("宋体", 10);
                        string StrName = "";
                        PortType mporttype = PortType.ThreeMode;
                        if (Parameter.isSupportMulArea)
                        {
                            if (null != group)
                            {
                                listbss = group.groupbss.ToList();
                            }
                        }
                        else
                        {
                            listbss = InnerPorts.ToList();
                        }
                        if (null != listbss)
                        {
                            foreach (KeyValuePair<string, PrecisePositionLibrary.BsInfo> bs in listbss)
                            {
                                if (null == bs.Value)
                                {
                                    continue;
                                }
                                Bsmsg bsmsg = null;
                                StrName = "";
                                if (Bsmsgs.TryGetValue(bs.Key, out bsmsg))
                                {
                                    StrName = bsmsg.Name;
                                    mporttype = bsmsg.porttype;
                                }
                                else
                                {
                                    mporttype = PortType.ThreeMode;
                                }
                                SinglePort(g, bs.Key, StrName, mporttype, (int)bs.Value.Place.x, (int)bs.Value.Place.y);
                                if ((mporttype == PortType.SingleMode && Parameter.isEnableReferType) && (!isStart || isShowGuidesLine))
                                {//我们画出单点定位基站范围
                                    DrawSinglePortRange(group, bsmsg, (int)bs.Value.Place.x, (int)bs.Value.Place.y, g);
                                }

                            }
                        }
                    }
                    #endregion
                    #region 画出卡片位置，即添加警告讯息
                    if (isStart)
                    {
                        foreach (KeyValuePair<string, CardImg> CardImg in CardImgs)
                        {
                            if (Parameter.isSupportMulArea)
                            {
                                if (isTrace)
                                {
                                    if (!CardImg.Key.Equals(TraceTagId))
                                    {
                                        continue;
                                    }
                                }
                            }
                            if (null == CardImg.Value)
                            {
                                continue;
                            }
                            // 超过指定超时时间没有上报可以不显示卡片
                            if (((DateTime.Now - CardImg.Value.ReceiTime).TotalSeconds > Parameter.OverTime1) && Parameter.NoShow_OverTime_NoRecei)
                            {
                                CardImg.Value.isShowImg = false;
                            }
                            else
                            {
                                CardImg.Value.isShowImg = true;
                            }
                            if (Parameter.RecordOverTimeNoReceiInfo)
                            {
                                TimeSpan timeSpan = DateTime.Now - CardImg.Value.ReceiTime;
                                if (timeSpan.TotalSeconds > Parameter.OverNoReceiveWarmTime
                                && !CardImg.Value.isTimeOutReceiveWarm)
                                {
                                    string StrID = CardImg.Value.ID[0].ToString("X2") + CardImg.Value.ID[1].ToString("X2");
                                    string StrName = Ini.GetValue(Ini.CardPath, Ini.CardSeg, StrID);
                                    StringBuilder strtagbuilder = null, strwarmbuilder = null;
                                    if (null == StrName || "".Equals(StrName))
                                    {
                                        strtagbuilder = new StringBuilder(StrID);
                                    }
                                    else
                                    {
                                        strtagbuilder = new StringBuilder(StrName);
                                        strtagbuilder.Append("(");
                                        strtagbuilder.Append(StrID);
                                        strtagbuilder.Append(")");
                                    }
                                    strwarmbuilder = new StringBuilder("Warning message: ");
                                    strwarmbuilder.Append("Didn't receive overtime ");
                                    strwarmbuilder.Append(strtagbuilder.ToString());
                                    strwarmbuilder.Append(" Tag location information！");
                                    AlarmInfors.Add(strwarmbuilder.ToString());
                                    this.Invoke(new Action(() => {
                                        Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                        Alarminfor_textBox.Visible = true;
                                    }));
                                    CardImg.Value.isTimeOutReceiveWarm = true;
                                }
                            }
                            if (CardImg.Value.isLowBattery && !CardImg.Value.isLowBatteryWarm)
                            {
                                string StrID = CardImg.Value.ID[0].ToString("X2") + CardImg.Value.ID[1].ToString("X2");
                                string StrName = Ini.GetValue(Ini.CardPath, Ini.CardSeg, StrID);
                                StringBuilder strtagbuilder = null, strwarmbuilder = null;
                                if (null == StrName || "".Equals(StrName))
                                {
                                    strtagbuilder = new StringBuilder(StrID);
                                }
                                else
                                {
                                    strtagbuilder = new StringBuilder(StrName);
                                    strtagbuilder.Append("(");
                                    strtagbuilder.Append(StrID);
                                    strtagbuilder.Append(")");
                                }
                                strwarmbuilder = new StringBuilder("Warning message: The number ");
                                strwarmbuilder.Append(strtagbuilder.ToString());
                                strwarmbuilder.Append(" Tag is ");
                                strwarmbuilder.Append(CardImg.Value.Battery);
                                strwarmbuilder.Append("% below ");
                                strwarmbuilder.Append(Parameter.LowBattry);
                                strwarmbuilder.Append("%");
                                string str = strwarmbuilder.ToString();
                                AlarmInfors.Add(str);
                                this.Invoke(new Action(() =>
                                {
                                    Alarminfor_textBox.Text = str;
                                    Alarminfor_textBox.Visible = true;
                                }));
                                CardImg.Value.isLowBatteryWarm = true;
                            }
                            if (CardImg.Value.isShowImg && CardImg.Value.isShowTag)
                            {
                                PosPortinfor minport = null;
                                if (Parameter.isEnableReferType)
                                {//基站模式选择单点和多点模式
                                    minport = GetMinDisPort(CardImg.Value);
                                    if (null != minport && PortType.SingleMode == minport.porttype
                                        && minport.distanse < minport.rangeR)
                                    {
                                        //此时我们需要将当前tag画到最近基站旁边
                                        DrawTagToPortNear(CardImg.Value, minport, g);
                                    }
                                    else
                                    {
                                        DrawCard(CardImg.Value, g);
                                    }
                                }
                                else
                                {
                                    DrawCard(CardImg.Value, g);
                                }
                                //画出辅助线，只画当前区域的辅助线
                                if (isShowGuidesLine)
                                {
                                    if (Parameter.isSupportMulArea)
                                    {
                                        if ((CardImg.Value.GroupID[0].ToString("X2") + CardImg.Value.GroupID[1].ToString("X2")).Equals(group.id[0].ToString("X2") + group.id[1].ToString("X2")))
                                        {
                                            DrawGuidsLine(g, CardImg.Value);
                                        }
                                    }
                                    else
                                    {
                                        DrawGuidsLine(g, CardImg.Value);
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region 将地图画到面板上
                    try
                    {
                        if (null != DrawMap && null != Map_panel)
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (!Map_panel.IsDisposed)
                                {
                                    Map_panel.CreateGraphics().DrawImageUnscaled(DrawMap, 0, 0);
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    #endregion
                } catch { 
                    
                }
            }
        }
        private void UpdateDrawTimer_Tick(object obj,EventArgs args)
        { 
            LoadDxfMap();
        }
        public void LoadDxfMap()
        {
            //获取矢量图花47ms
            lock (obj_draw)
            {
                MapBitMap = DxfMapParam.GetDxfMap(StrMapPath, DxfMapParam.scale, DxfMapParam.CenterX, DxfMapParam.CenterY, Map_panel.Width, Map_panel.Height);
            }
            if (null == MapBitMap)
            {
                return;
            }
            //画图大概花费16s
            Map_panel_Paint(null, null);
        }
        public void DrawNoMap()
        {
            MapBitMap = new Bitmap(Map_panel.Width, Map_panel.Height);
            Graphics g = Graphics.FromImage(MapBitMap);
            g.DrawString("No Map", new Font("宋体", 32), Brushes.Red, (Map_panel.Width / 2) - 6 * 20 / 2, Map_panel.Height / 2 - 10);
        }
        public void Map_panel_Paint(object sender, PaintEventArgs e)
        {
            //在地图上画出参考点
            if (null == MapBitMap)
            {
                DrawNoMap();
            }
            Graphics g = Graphics.FromImage(MapBitMap);
            //画限制区域
            if (Parameter.isEnableLimitArea)
            {
                DrawAllLimitArea(g);
            }
            //画出基站位置
            if (Parameter.ShowPlacePort)
            {
                if (!Parameter.isSupportMulArea) {
                    DrawPort(g);
                } else {
                    if(null != group) {
                        DrawMulPort(g, group.id[0].ToString("X2") + group.id[1].ToString("X2"));
                    }
                }
            }
            //判断是否开启了监听
            if (isStart)
            {
                DrawWorkTags(g);
            }
            //画出卡片
            lock (obj_lock)
            {
                if (null != MapBitMap && null != Map_panel && !Map_panel.IsDisposed)
                {
                    Map_panel.CreateGraphics().DrawImageUnscaled(MapBitMap, 0, 0);
                }
            }
        }
        /// <summary>
        /// 画出单点基站范围
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="g"></param>
        private void DrawSinglePortRange(Group group,Bsmsg bs,int X,int Y,Graphics g)
        {
            //计算坐标Scale为1时实际半径值
            double basescaleR = 0.0d;
            if (Parameter.isSupportMulArea && null != group)
            {
                if (null != bs)
                {
                    basescaleR = bs.rangeR * group.scale;
                }
               
            }
            else
            {
                basescaleR = bs.rangeR * Img_RealDisRelation;
            }
            //根据缩放比例scale计算地图的半径值
            double scaleR = basescaleR / DxfMapParam.scale;
            //计算Port的地图坐标
            float x = -1, y = -1;
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            double CurWidth = PortWidth, CurHeight = PortHeight;
            //此时记录下的X、Y是scale = 1,且面板为CenterX = Map_panel.Width/2,CenterY = Map_panel.Height/2时的坐标
            x = (float)X + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)Y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {//位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {//位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {//位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {//位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            g.DrawEllipse(Pens.Black, (float)(x - scaleR), (float)(y - scaleR), (float)(2 * scaleR), (float)(2 * scaleR));
        }

        /// <summary>
        /// 画辅助线
        /// </summary>
        /// <param name="g"></param>
        /// <param name="tag"></param>
        public void DrawGuidsLine(Graphics g,CardImg tag)
        {
            string strid = "";
            BsInfo bs = null;
            BsInfo selebs = null;
            List<ReportRouterInfor> tagreportrouters = tag.ReportRouters;
            try
            {
                for (int i = 0; i < tagreportrouters.Count; i++)
                {

                    strid = tag.ReportRouters[i].id[0].ToString("X2") + tag.ReportRouters[i].id[1].ToString("X2");
                    if (Parameter.isSupportMulArea)
                    {
                        if (null != group)
                        {
                            group.groupbss.TryGetValue(strid, out bs);
                        }
                        //若发现当前的辅助基站不在这个区域中是可以不用画辅助圆
                        if (IsExistRefer(strid, out selebs))
                        {//存在
                            if (selebs.GroupID[0] != tag.GroupID[0] || selebs.GroupID[1] != tag.GroupID[1])
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        InnerPorts.TryGetValue(strid, out bs);
                    }
                    if (null != bs)
                    {
                        //报错索引超出范围
                        float dis = 0f;
                        try
                        {
                            dis = (float)tag.ReportRouters[i].dis;
                        }
                        catch
                        {
                            continue;
                        }
                        Point pt = new Point(bs.Place.x, bs.Place.y, bs.Place.z);
                        if (Parameter.isSupportMulArea)
                        {
                            if (tag.ReportRouters[i].ResidualValue == 0) { //说明这个点是未选择的点
                                DrawGuidRound(g, pt, dis, false, group);
                            } else {
                                DrawGuidRound(g, pt, dis, true, group);
                            }
                        }
                        else
                        {
                            if (tag.ReportRouters[i].SigQuality >= 4) { //说明这个点是未选择的点
                                DrawGuidRound(g, pt, dis, false, group);
                            }
                            else
                            {
                                DrawGuidRound(g, pt, dis, true, group);
                            }
                        }
                    }
                }
            }
            catch { 
                
            }
        }
        
        /// <summary>
        /// 画出基站的辅助圆
        /// </summary>
        /// <param name="g"></param>
        /// <param name="point"></param>
        /// <param name="dis"></param>
        /// <param name="isCheck"></param>
        public void DrawGuidRound(Graphics g,Point point,float dis,bool isCheck,Group group)
        {
            float x = -1, y = -1, relatedis = 0;
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            //此时记录下的X、Y是scale = 1,且面板为CenterX=Map_panel.Width/2,CenterY = Map_panel.Height/2时的坐标
            x = (float)point.x + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)point.y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {//位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {//位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {//位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {//位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            //考虑圆半径在地图上的大小（dis是实际地图上的坐标）
            if (null != group && Parameter.isSupportMulArea)
            {
                relatedis = (float)(dis * group.scale / DxfMapParam.scale);
            }
            else
            {
                relatedis = (float)(dis * Img_RealDisRelation / DxfMapParam.scale);
            }

            
            if (isCheck)
            {
                g.DrawEllipse(Pens.Red, (float)(x - relatedis), (float)(y - relatedis), 2 * relatedis, 2 * relatedis);
            }
            else
            {
                g.DrawEllipse(Pens.Green, (float)(x - relatedis), (float)(y - relatedis), 2 * relatedis, 2 * relatedis);
            }
        }
        
        /// <summary>
        /// 鼠标按下 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Map_panel_MouseDown(object sender, MouseEventArgs e)
        {
            isMove = false;
            float x = -1, y = -1;
            double d0, d1, L0, L1, p0, p1;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {//鼠标右键按下说明是设置限制区域
                if (isStart)
                {
                    return;
                }
                if (!Parameter.isEnableLimitArea)
                {
                    return;
                }
                double MapWidth = 0, MapHeight = 0;
                if (Parameter.isSupportMulArea && null != group)
                {
                    MapWidth = group.scale * group.actualwidth / DxfMapParam.scale;
                    MapHeight = group.scale * group.actualheight / DxfMapParam.scale;
                }
                else
                {
                    MapWidth = Img_RealDisRelation * Parameter.RealWidth / DxfMapParam.scale;
                    MapHeight = Img_RealDisRelation * Parameter.RealHeight / DxfMapParam.scale;
                }
                if (e.X > DxfMapParam.CenterX + MapWidth / 2 || e.X < DxfMapParam.CenterX - MapWidth / 2 || e.Y > DxfMapParam.CenterY + MapHeight / 2 || e.Y < DxfMapParam.CenterY - MapHeight / 2)
                {
                    MessageBox.Show("Area restrictions start to go beyond the map!");
                    return;
                }
                isLimit = true;
                CurLimitArea = new LimitArea();
                //需要我们将当前的坐标转化为scale = 1时的坐标
                //计算当前的参考点绝对坐标
                //其中ex.X、ex.Y是参考点绝对位置
                x = (float)e.X;
                y = (float)e.Y;
                d0 = d1 = L0 = L1 = p0 = p1 = 0;
                d0 = Math.Abs(DxfMapParam.CenterY - y);
                d1 = Math.Abs(DxfMapParam.CenterX - x);
                L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
                L1 = L0 * DxfMapParam.scale;
                p0 = (d0 / L0) * L1;
                p1 = (d1 / L0) * L1;

                if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                {// 位于左上象限
                    x = (float)(DxfMapParam.PanelCenterX - p1);
                    y = (float)(DxfMapParam.PanelCenterY - p0);
                }
                else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
                {// 位于右上象限
                    x = (float)(DxfMapParam.PanelCenterX + p1);
                    y = (float)(DxfMapParam.PanelCenterY - p0);
                }
                else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                {// 位于左下象限
                    x = (float)(DxfMapParam.PanelCenterX - p1);
                    y = (float)(DxfMapParam.PanelCenterY + p0);
                }
                else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
                {// 位于右下象限
                    x = (float)(DxfMapParam.PanelCenterX + p1);
                    y = (float)(DxfMapParam.PanelCenterY + p0);
                }
                List<KeyValuePair<String, LimitArea>> listareas = null;
                // 判断是否存在多区域或者单区域
                if (Parameter.isSupportMulArea)
                {
                    if (null != group)
                    { //多区域
                        listareas = group.grouplimiares.ToList();
                    }
                    else
                    {
                        MessageBox.Show("Sorry, the area doesn't exist!");
                        return;
                    }
                }
                else
                { //单区域
                    listareas = Areas.ToList();
                }
                // 判断当前区域是否存在重合部分
                foreach (KeyValuePair<string, LimitArea> area in listareas)
                {
                    if (null == area.Value)
                    {
                        continue;
                    }
                    if (x >= area.Value.startpoint.x && x <= area.Value.endpoint.x && y >= area.Value.startpoint.y && y <= area.Value.endpoint.y)
                    {
                        isLimit = false;
                        LimitAreaSet MyLimitAreaSet = null;
                        if (Parameter.isSupportMulArea)
                        {
                            MyLimitAreaSet = new LimitAreaSet(area.Value, this, group.id[0].ToString("X2") + group.id[1].ToString("X2"));
                        }
                        else
                        {
                            MyLimitAreaSet = new LimitAreaSet(area.Value, this);
                        }
                        MyLimitAreaSet.ShowDialog();
                        CurLimitArea = null;
                        LoadDxfMap();
                        return;
                    }
                }
                CurLimitArea.startpoint = new Point(x, y, 0);
            }
            else
            {
                List<KeyValuePair<string, PrecisePositionLibrary.BsInfo>> listbss = null;
                if(Parameter.isSupportMulArea)
                {
                    if (null != group)
                    {
                        listbss = group.groupbss.ToList();
                    }
                    else
                    {
                        MessageBox.Show("Sorry, the area doesn't exist!");
                        return;
                    }
                }else
                {
                    listbss = InnerPorts.ToList();
                }
                foreach (KeyValuePair<string, PrecisePositionLibrary.BsInfo> port in listbss)
                {
                    if (null == port.Value)
                    {
                        continue;
                    }
                    if (null == port.Value.Place)
                    {
                        continue;
                    }
                    //计算当前的参考点绝对坐标
                    //其中Br.Value.x、 Br.Value.y是当Scale = 1，中心在面板中心时饿坐标
                    x = (float)port.Value.Place.x + (int)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
                    y = (float)port.Value.Place.y + (int)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);

                    d0 = d1 = L0 = L1 = p0 = p1 = 0;
                    d0 = Math.Abs(DxfMapParam.CenterY - y);
                    d1 = Math.Abs(DxfMapParam.CenterX - x);
                    L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
                    L1 = L0 / DxfMapParam.scale;
                    p0 = (d0 / L0) * L1;
                    p1 = (d1 / L0) * L1;

                    if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                    {//位于左上象限
                        x = (float)(DxfMapParam.CenterX - p1);
                        y = (float)(DxfMapParam.CenterY - p0);
                    }
                    else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
                    {//位于右上象限
                        x = (float)(DxfMapParam.CenterX + p1);
                        y = (float)(DxfMapParam.CenterY - p0);
                    }
                    else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                    {//位于左下象限
                        x = (float)(DxfMapParam.CenterX - p1);
                        y = (float)(DxfMapParam.CenterY + p0);
                    }
                    else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
                    {//位于右下象限
                        x = (float)(DxfMapParam.CenterX + p1);
                        y = (float)(DxfMapParam.CenterY + p0);
                    }

                    double CurWidth = PortWidth * 1 / DxfMapParam.scale;
                    double CurHeight = PortHeight * 1 / DxfMapParam.scale;

                    if (e.X > x - (CurWidth / 2) && e.X < x + (CurWidth / 2) && e.Y > y - (CurHeight / 2) && e.Y < y + (CurHeight / 2))
                    {
                        if (isStart)
                        {
                            DragMapStart(e);
                            return;
                        }
                        isDrag = true; CanDragPortID = port.Key;
                    }
                    if(!Parameter.isSupportMulArea)
                    {
                        String StrPath = Ini.GetValue(Ini.ConfigPath, Ini.MapSeg, Ini.MapKey_Path);
                        if (null == StrPath || "".Equals(StrPath))
                        {
                            MessageBox.Show("The path of the map can't be empty!");
                            return;
                        }
                    }
                }
                DragMapStart(e);
            }
        }
        
        /// <summary>
        /// 开始拖动地图初始化变量
        /// </summary>
        /// <param name="e"></param>
        public void DragMapStart(MouseEventArgs e)
        {
            if (!isDrag)
            {
                IsMapDrag = true;
                startX = e.X;
                startY = e.Y;
                Cursor = Cursors.Hand;
            }
        }
        
        /// <summary>
        /// 拖动地图结束时改变变量的值
        /// </summary>
        public void DragMapEnd()
        {
            IsMapDrag = false;
            startX = 0;
            startY = 0;
            Cursor = Cursors.Default;
        }
        
        private void Map_panel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (isStart)
                    return;
                if (!Parameter.isEnableLimitArea)
                    return;
                isLimit = false;
                if (null == CurLimitArea)
                {
                    return;
                }
                if(CurLimitArea.endpoint.x > 0 && CurLimitArea.endpoint.y > 0 && Math.Abs(CurLimitArea.endpoint.x - CurLimitArea.startpoint.x) * Math.Abs(CurLimitArea.endpoint.y - CurLimitArea.startpoint.y) > 10)
                {
                    //弹出区域设置框
                    LimitAreaSet MyLimitAreaSet = null;
                    if (Parameter.isSupportMulArea)
                    {
                        if (null != group)
                        {
                            MyLimitAreaSet = new LimitAreaSet(CurLimitArea, this, group.id[0].ToString("X2") + group.id[1].ToString("X2"));
                        }
                        else
                        {
                            MessageBox.Show("Sorry, the area you selected does not exist!");
                            return;
                        }
                    }
                    else
                    {
                        MyLimitAreaSet = new LimitAreaSet(CurLimitArea, this);
                    }
                    MyLimitAreaSet.ShowDialog();
                    CurLimitArea = null;
                    LoadDxfMap();
                }
            }
            else
            {
                //鼠标松开
                if (isDrag)
                { 
                    isDrag = false;
                    CanDragPortID = "";
                }
                else if (IsMapDrag)
                {
                    DragMapEnd();
                }
            }
        }
        private void Map_panel_MouseMove(object sender, MouseEventArgs e)
        {
            //计算当前的参考点绝对坐标
            float x = -1, y = -1;
            double d0, d1, L0, L1, p0, p1;
            //判断是否是多区域
            double MapWidth = 0, MapHeight = 0;
            if (Parameter.isSupportMulArea && null != group)
            {
                MapWidth = group.scale * group.actualwidth / DxfMapParam.scale;
                MapHeight = group.scale * group.actualheight / DxfMapParam.scale;
            }
            else
            {
                MapWidth = Img_RealDisRelation * Parameter.RealWidth / DxfMapParam.scale;
                MapHeight = Img_RealDisRelation * Parameter.RealHeight / DxfMapParam.scale;
            }
            isMove = true;
            if (IsMapDrag)
            {
                DxfMapParam.CenterX += (e.X - startX);
                DxfMapParam.CenterY += (e.Y - startY);
                startX = e.X;
                startY = e.Y;
                LoadDxfMap();
            }
            else if (isDrag)
            {
                if (e.X > DxfMapParam.CenterX + MapWidth / 2 || e.X < DxfMapParam.CenterX - MapWidth / 2 || e.Y > DxfMapParam.CenterY + MapHeight / 2 || e.Y < DxfMapParam.CenterY - MapHeight / 2)
                {

                }
                else
                {
                    /* 其中e.X、e.Y是当前参考点在地图中的实际位置,
                     * 需要将其转化为scale = 1,中心在面板中心的位置
                     */
                    x = e.X; y = e.Y;
                    //此时x,y表示中心到原点时的x、y的相对坐标
                    d0 = d1 = L0 = L1 = p0 = p1 = 0;
                    d0 = Math.Abs(DxfMapParam.CenterY - y);
                    d1 = Math.Abs(DxfMapParam.CenterX - x);
                    L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
                    L1 = L0 * DxfMapParam.scale;
                    p0 = (d0 / L0) * L1;
                    p1 = (d1 / L0) * L1;
                    if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                    {//位于左上象限
                        x = (float)(DxfMapParam.PanelCenterX - p1);
                        y = (float)(DxfMapParam.PanelCenterY - p0);
                    }
                    else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
                    {//位于右上象限
                        x = (float)(DxfMapParam.PanelCenterX + p1);
                        y = (float)(DxfMapParam.PanelCenterY - p0);
                    }
                    else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                    {//位于左下象限
                        x = (float)(DxfMapParam.PanelCenterX - p1);
                        y = (float)(DxfMapParam.PanelCenterY + p0);
                    }
                    else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
                    {//位于右下象限
                        x = (float)(DxfMapParam.PanelCenterX + p1);
                        y = (float)(DxfMapParam.PanelCenterY + p0);
                    }
                    PrecisePositionLibrary.BsInfo port = null;
                    if (Parameter.isSupportMulArea)
                    {
                        if (group.groupbss.TryGetValue(CanDragPortID, out port))
                        {
                            port.Place.x = (int)x;
                            port.Place.y = (int)y;
                        }
                    }
                    else
                    {
                        if (InnerPorts.TryGetValue(CanDragPortID, out port))
                        {
                            port.Place.x = (int)x;
                            port.Place.y = (int)y;
                        }
                    }
                }
                LoadDxfMap();
            }
            else if (isLimit)
            {  /*
                1、开始设置限制区域
                2、需要我们将当前的坐标转化为scale = 1时的坐标
                3、计算当前的参考点绝对坐标
                4、其中ex.X、ex.Y是参考点绝对位置
                */
                if (!Parameter.isEnableLimitArea)
                {//判断是否能够设置限制区域
                    return;
                }
                //下面是把e.X和e.Y设置成Scale = 1,中心点在坐标面板中心时的坐标
                x = (float)e.X;
                y = (float)e.Y;
                d0 = d1 = L0 = L1 = p0 = p1 = 0;
                d0 = Math.Abs(DxfMapParam.CenterY - y);
                d1 = Math.Abs(DxfMapParam.CenterX - x);
                L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
                L1 = L0 * DxfMapParam.scale;
                p0 = (d0 / L0) * L1;
                p1 = (d1 / L0) * L1;
                if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                {//位于左上象限
                    x = (float)(DxfMapParam.PanelCenterX - p1);
                    y = (float)(DxfMapParam.PanelCenterY - p0);
                }
                else if (x >= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
                {//位于右上象限
                    x = (float)(DxfMapParam.PanelCenterX + p1);
                    y = (float)(DxfMapParam.PanelCenterY - p0);
                }
                else if (x < DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
                {//位于左下象限
                    x = (float)(DxfMapParam.PanelCenterX - p1);
                    y = (float)(DxfMapParam.PanelCenterY + p0);
                }
                else if (x >= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
                {//位于右下象限
                    x = (float)(DxfMapParam.PanelCenterX + p1);
                    y = (float)(DxfMapParam.PanelCenterY + p0);
                }
                List<KeyValuePair<String, LimitArea>> listareas = null;
                // 判断是否支持多区域或者单区域
                if (Parameter.isSupportMulArea)
                {
                    if (null != group)
                    {//多区域
                        listareas = group.grouplimiares.ToList();
                    }
                    else
                    {
                        MessageBox.Show("Sorry, the area doesn't exist!");
                        return;
                    }
                }
                else
                {   //单区域
                    listareas = Areas.ToList();
                }
                foreach (KeyValuePair<string, LimitArea> area in listareas)
                {
                    if (null == area.Value)
                    {
                        continue;
                    }
                    if ((x >= area.Value.startpoint.x && x <= area.Value.endpoint.x && y >= area.Value.startpoint.y && y <= area.Value.endpoint.y) ||
                        (CurLimitArea.startpoint.x >= area.Value.startpoint.x && CurLimitArea.startpoint.x <= area.Value.endpoint.x && y >= area.Value.startpoint.y && y <= area.Value.endpoint.y) ||
                        (x >= area.Value.startpoint.x && x <= area.Value.endpoint.x && CurLimitArea.startpoint.y >= area.Value.startpoint.y && CurLimitArea.startpoint.y <= area.Value.endpoint.y) ||
                        (CurLimitArea.startpoint.y < area.Value.startpoint.y && CurLimitArea.endpoint.y > area.Value.endpoint.y &&
                         CurLimitArea.endpoint.x > area.Value.startpoint.x && CurLimitArea.endpoint.x < area.Value.endpoint.x) ||
                        (CurLimitArea.startpoint.x < area.Value.startpoint.x && CurLimitArea.endpoint.x > area.Value.endpoint.x &&
                         CurLimitArea.endpoint.y > area.Value.startpoint.y && CurLimitArea.endpoint.y < area.Value.endpoint.y))
                    {
                        MessageBox.Show("I'm sorry that the limit can't overlap!");
                        isLimit = false;
                        LimitAreaSet MyLimitAreaSet = null;
                        if (Parameter.isSupportMulArea)
                        {
                            if (null != group)
                            {
                                MyLimitAreaSet = new LimitAreaSet(CurLimitArea, this, group.id[0].ToString("X2") + group.id[1].ToString("X2"));
                            }
                            else
                            {
                                MessageBox.Show("Sorry, the area you selected does not exist!");
                                return;
                            }
                        }
                        else
                        {
                            MyLimitAreaSet = new LimitAreaSet(CurLimitArea, this);
                        }
                        MyLimitAreaSet.ShowDialog();
                        CurLimitArea = null;
                        LoadDxfMap();
                        return;
                    }
                }
                //查看当前是否超出限制区域
                if (e.X > DxfMapParam.CenterX + MapWidth / 2 || e.X < DxfMapParam.CenterX - MapWidth / 2 || e.Y > DxfMapParam.CenterY + MapHeight / 2 || e.Y < DxfMapParam.CenterY - MapHeight / 2)
                {
                    
                }
                else
                {
                    if (CurLimitArea != null)
                    {
                        if (x < CurLimitArea.startpoint.x)
                        {
                            x = (float)CurLimitArea.startpoint.x + 1;
                        }
                        if (y < CurLimitArea.startpoint.y)
                        {
                            y = (float)CurLimitArea.startpoint.y + 1;
                        }
                        CurLimitArea.endpoint = new Point(x, y, 0);
                    }
                }
                LoadDxfMap();
            }
        }
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="strpath"></param>
        public void SeralizeObject(Object obj, string strpath)
        {
            FileStream fstr = null;
            try
            {
                fstr = new FileStream(strpath, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fstr, obj);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (null != fstr)
                    fstr.Close();
            }
        }
        /// <summary>
        /// 反序列化对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="strpath"></param>
        public void DeserializeObject(out Object obj, string strpath)
        {
            FileStream fstr = null;
            try
            {
                fstr = new FileStream(strpath, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                obj = bf.Deserialize(fstr);
                fstr.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                obj = null;
            }
            finally
            {
                if (null != fstr) fstr.Close();
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Parameter.isSupportMulArea)
            {
                SaveMulAreas();
            }
            else
            {
                SavePorts();
            }
            SaveLimitArea();
            PrecisePositionLibrary.PrecisePosition.Stop();

            #region  关闭刷新地图线程
            if (null != UpdateMapThread)
            {
                isUpdate = false;
                Thread.Sleep(100);
                try
                {
                    if (!UpdateMapThread.IsAlive)
                    {
                        UpdateMapThread.Abort();
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    UpdateMapThread = null;
                }
            }
            if (null != regularRegionAlarmReport)
            {
                regularRegionAlarmReport.stop();
                regularRegionAlarmReport = null;
            }
            #endregion
            SaveRecord();
        }
        /// <summary>
        /// 清除Map文件夹中的无关地图
        /// </summary>
        public void ClearMap()
        {
            if(!Directory.Exists(Ini.AreaMapPath))
                return;
            //获取所有地图的名称
            string[] StrMaps = Directory.GetFiles(Ini.AreaMapPath);
            //得到当前单区域地图
            string CurMap = Ini.GetValue(Ini.ConfigPath,Ini.MapSeg,Ini.MapKey_Path);
            string strname = "";
            int start = -1;
            bool mark = false;
            foreach (string strmap in StrMaps)
            {
                if (null == strmap)
                {
                    continue;
                }
                start = strmap.LastIndexOf("\\");
                strname = strmap.Substring(start + 1, strmap.Length - start - 1);
                if (strname.Equals(CurMap)) 
                    continue;
                //查看多区域的集合中是否有包含当前地图的
                mark = false;
                foreach(KeyValuePair<string,Group> group in Groups)
                {
                    if (null == group.Value)
                        continue;
                    if (group.Value.grouppath.Equals(strname))
                    {//说明多区域集合中也需要当前的地图，不能删除
                        mark = true;
                        break;
                    }
                }
                if (!mark)
                {
                    File.Delete(strmap);
                }
            }
        }
        /// <summary>
        /// 保存限制区域
        /// </summary>
        public void SaveLimitArea()
        {
            if (Ini.Clear(Ini.SaveLimitsAreaPath))
            { 
                foreach(KeyValuePair<string,LimitArea> area in Areas)
                {
                    if (null == area.Value) continue;
                    Ini.SetValue(Ini.SaveLimitsAreaPath,area.Key,Ini.Name,area.Value.Name);
                    Ini.SetValue(Ini.SaveLimitsAreaPath,area.Key,Ini.LimitStartX,area.Value.startpoint.x+"");
                    Ini.SetValue(Ini.SaveLimitsAreaPath, area.Key, Ini.LimitStartY, area.Value.startpoint.y + "");

                    Ini.SetValue(Ini.SaveLimitsAreaPath, area.Key, Ini.LimitEndX, area.Value.endpoint.x + "");
                    Ini.SetValue(Ini.SaveLimitsAreaPath, area.Key, Ini.LimitEndY, area.Value.endpoint.y + "");
                }
            }
        }
        /// <summary>
        /// 保存多区域讯息
        /// </summary>
        public void SaveMulAreas()
        {
            //清除原来文件中的内容
            Ini.Clear(Ini.SaveMulAreaPath);
            int num = 0;

            List<KeyValuePair<String, Group>> listgps = Groups.OrderBy(key=>key.Key).ToList();

            foreach (KeyValuePair<string, Group> group in listgps)
            {
                if (null == group.Value)
                {
                    continue;
                }
                //保存区域的名称
                Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.Name, group.Value.name);
                //保存实际宽高
                Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.ActualWidth, group.Value.actualwidth.ToString());
                Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.ActualHeight, group.Value.actualheight.ToString());
                //保存区域的地图
                Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.MapKey_Path, group.Value.grouppath);
                //保存比例
                Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.RealScale, group.Value.scale.ToString());
                //保存基站讯息
                num = 0;
                foreach (KeyValuePair<string, BsInfo> bs in group.Value.groupbss)
                {
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.BsID_ + num, bs.Key);
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.BsGroupID_ + num, bs.Value.GroupID[0].ToString("X2") + bs.Value.GroupID[1].ToString("X2"));
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.BsX_ + num, bs.Value.Place.x + "");
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.BsY_ + num, bs.Value.Place.y + "");
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.BsZ_ + num, bs.Value.Place.z + "");
                    num ++;
                }
                // 保存限制区域讯息
                num = 0;
                foreach(KeyValuePair<string, LimitArea> liarea in group.Value.grouplimiares)
                {
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.LimitAreaID_ + num, liarea.Key);
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.LimitAreaName_ + num, liarea.Value.Name);
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.LimitAreaStartX_ + num, liarea.Value.startpoint.x.ToString());
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.LimitAreaStartY_ + num, liarea.Value.startpoint.y.ToString());
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.LimitAreaEndX_ + num, liarea.Value.endpoint.x.ToString());
                    Ini.SetValue(Ini.SaveMulAreaPath, group.Key, Ini.LimitAreaEndY_ + num, liarea.Value.endpoint.y.ToString());
                    num++;
                }
            }
        }
        /// <summary>
        /// 判断指定ID的基站是否存在
        /// </summary>
        /// <param name="strReferID"></param>
        /// <param name="bs"></param>
        /// <returns></returns>
        public bool IsExistRefer(string strReferID,out BsInfo bs)
        { 
            foreach(KeyValuePair<String,Group> group in Groups)
            {
                if (group.Value.groupbss.TryGetValue(strReferID, out bs))
                {
                    return true;
                }
            }
            bs = null;
            return false;
        }
        /// <summary>
        ///  判断指定ID的区域是否存在
        /// </summary>
        /// <param name="strAreaID"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        public bool IsExistArea(string strAreaID,out LimitArea area)
        {
            foreach (KeyValuePair<string, Group> group in Groups)
            {
                if (group.Value.grouplimiares.TryGetValue(strAreaID, out area))
                {
                    return true;
                }
            }
            area = null;
            return false;
        }

        /// <summary>
        /// 保存设置的参考点
        /// </summary>
        public void SavePorts()
        {
            if (Ini.Clear(Ini.SavePortsPath))
            {
               
                foreach (KeyValuePair<string, PrecisePositionLibrary.BsInfo> pt in InnerPorts)
                {
                    if (null == pt.Value)
                    {
                        continue;
                    }
                    Ini.SetValue(Ini.SavePortsPath,pt.Key,Ini.Loca_X,pt.Value.Place.x.ToString());
                    Ini.SetValue(Ini.SavePortsPath,pt.Key,Ini.Loca_Y,pt.Value.Place.y.ToString());
                    Ini.SetValue(Ini.SavePortsPath,pt.Key,Ini.Loca_Z,pt.Value.Place.z.ToString());
                }
            }
        }
        private void StartList_button_Click(object sender, EventArgs e)
        {
            if (!isStart) {
                //精准定位
                string StrIP = Ini.GetValue(Ini.ConfigPath,Ini.NetSeg,Ini.NetKey_IP);
                string StrPort = Ini.GetValue(Ini.ConfigPath,Ini.NetSeg,Ini.NetKey_Port);
                if (StrIP==null || StrPort==null)
                {
                    MessageBox.Show("Please set the IP address and port number！");
                    return;
                }
                if (StrIP.Equals("")||StrPort.Equals(""))
                {
                    MessageBox.Show("Please set the IP address and port number！");
                    return;
                }
                int Port = 0;
                try
                {
                    Port = Convert.ToInt32(StrPort);
                }
                catch (Exception)
                {
                    MessageBox.Show("Port format setting is wrong！");
                    return;
                }
                if (Port > 65535 || Port < 1025)
                {
                    MessageBox.Show("Port should be greater than 1024 and less than or equal to 65535！");
                    return;
                }
                IPAddress ip = null;
                if (!IPAddress.TryParse(StrIP, out ip))
                {
                    MessageBox.Show("The IP address format is wrong！");
                    return;
                }
                if (IsCnn)
                {
                    MessageBox.Show("Sorry,Please turn off the list view！");
                    return;
                }
                if(!Parameter.isSupportMulArea)
                {
                    string StrMap = Ini.GetValue(Ini.ConfigPath,Ini.MapSeg,Ini.MapKey_Path);
                    if (StrMap == null)
                    {
                        MessageBox.Show("Please choose photos！");
                        return;
                    }
                    if (StrMap.Equals(""))
                    {
                        MessageBox.Show("Please choose photos！");
                        return;
                    }
                }
                float wscale = 0.0f, hscale = 0.0f, MapWidth = 0.0f, MapHeight = 0.0f;
                List<BsInfo> bss = new List<BsInfo>();
                if (Parameter.isSupportMulArea)
                {//多区域
                    List<KeyValuePair<string, Group>> listgp = Groups.OrderBy(key=>key.Key).ToList();
                    foreach (KeyValuePair<string, Group> group in listgp)
                    {
                        MapWidth = (float)group.Value.scale * group.Value.actualwidth;
                        MapHeight = (float)group.Value.scale * group.Value.actualheight;
                        foreach (KeyValuePair<string, BsInfo> curbs in group.Value.groupbss)
                        {
                            BsInfo bs = new BsInfo();
                            System.Buffer.BlockCopy(curbs.Value.ID, 0, bs.ID, 0, 2);
                            System.Buffer.BlockCopy(group.Value.id, 0, bs.GroupID, 0, 2);
                            double relateX = curbs.Value.Place.x - (DxfMapParam.PanelCenterX - MapWidth / 2);
                            double relateY = curbs.Value.Place.y - (DxfMapParam.PanelCenterY - MapHeight / 2);
                            bs.Place.x = relateX / group.Value.scale;
                            bs.Place.y = relateY / group.Value.scale;
                            bs.Place.z = curbs.Value.Place.z;
                            bss.Add(bs);
                        }
                    }
                } else {   //单区域
                    //得到真实的宽与高
                    string StrRealWidth = Ini.GetValue(Ini.ConfigPath, Ini.MapSeg, Ini.RealWidth);
                    string StrRealHeight = Ini.GetValue(Ini.ConfigPath, Ini.MapSeg, Ini.RealHeight);
                    if (StrRealWidth == null || StrRealHeight == null)
                    {
                        MessageBox.Show("Please set the actual width of the picture！");
                        return;
                    }
                    if (StrRealWidth.Equals(""))
                    {
                        MessageBox.Show("Please set the actual width of the picture！");
                        return;
                    }
                    if (StrRealHeight.Equals(""))
                    {
                        MessageBox.Show("Please set the actual height of the picture！");
                    }
                    try
                    {
                        Parameter.RealWidth = Convert.ToDouble(StrRealWidth);
                        Parameter.RealHeight = Convert.ToDouble(StrRealHeight);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Picture of the real width and the height of the real format is wrong！");
                        return;
                    }
                    if (Parameter.RealWidth != 0 && Parameter.RealWidth != 0)
                    {
                        wscale = (float)Map_panel.Width / (float)Parameter.RealWidth;
                        hscale = (float)Map_panel.Height / (float)Parameter.RealHeight;
                        Img_RealDisRelation = wscale > hscale ? hscale : wscale;
                    }
                    //计算当scale = 1时,面板上的像素与实际距离间的比值
                    MapWidth  = (float)(Img_RealDisRelation * Parameter.RealWidth);
                    MapHeight = (float)(Img_RealDisRelation * Parameter.RealHeight);
                    foreach (KeyValuePair<string, BsInfo> kv in InnerPorts)
                    {
                        BsInfo bs = new BsInfo();
                        Array.Copy(kv.Value.ID, 0, bs.ID, 0, 2);
                        double relateX = kv.Value.Place.x - (DxfMapParam.PanelCenterX - MapWidth / 2);
                        double relateY = kv.Value.Place.y - (DxfMapParam.PanelCenterY - MapHeight / 2);
                        bs.Place.x = relateX / Img_RealDisRelation;
                        bs.Place.y = relateY / Img_RealDisRelation;
                        bs.Place.z = kv.Value.Place.z;
                        bss.Add(bs);
                    }
                }
                PrecisePositionLibrary.PrecisePosition.InitBasicStations(bss);
                foreach (BsInfo bs in bss)
                {
                    Console.WriteLine("id : " + (bs.ID[0].ToString("X2") + bs.ID[1].ToString("X2")) + ", x: " + bs.Place.x + ", y: " + bs.Place.y);
                }
                List<string> lists = null;
                List<TagInfo> tginfos = null;
                string strheight = "";
                double height = -1;
                string strname = "";
                try
                {
                    if (Ini.GetAllSegment(Ini.CardPath, out lists))
                    {
                        if (null != lists)
                        {
                            tginfos = new List<TagInfo>();
                            byte[] tgId = new byte[2];
                            foreach (string str in lists)
                            {
                                if (null == str)
                                {
                                    break;
                                }
                                if (str.Length != 4)
                                {
                                    continue;
                                }
                                try
                                {
                                    tgId[0] = Convert.ToByte(str.Substring(0, 2), 16);
                                    tgId[1] = Convert.ToByte(str.Substring(2, 2), 16);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                strheight = Ini.GetValue(Ini.CardPath, str, Ini.Height);
                                if (null != strheight && !"".Equals(strheight.Trim()))
                                {
                                    try
                                    {
                                        height = Convert.ToDouble(strheight);
                                    }
                                    catch (Exception)
                                    {
                                        height = -1;
                                    }
                                    if (height < 0)
                                    {
                                        continue;
                                    }
                                    PrecisePositionLibrary.TagInfo tg = new TagInfo();
                                    System.Buffer.BlockCopy(tgId, 0, tg.Id, 0, 2);
                                    tg.height = height;
                                    tginfos.Add(tg);
                                }
                                strname = Ini.GetValue(Ini.CardPath, str, Ini.Name);
                                if (null != strname && !"".Equals(strname))
                                {
                                    Tagmsg TG = new Tagmsg();
                                    System.Buffer.BlockCopy(tgId, 0, TG.ID, 0, 2);
                                    TG.Name = strname;
                                    tgmsgs.Add(str, TG);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
                if (null != tginfos)
                {
                    if (tginfos.Count > 0)
                    {
                        PrecisePositionLibrary.PrecisePosition.InitTag(tginfos.ToArray<TagInfo>());
                    }
                }
                PrecisePositionLibrary.PrecisePosition.InitNet(ip, Port);
                CurReportMode = ReportMode.ImgMode;
                try
                {
                    if (PrecisePositionLibrary.PrecisePosition.Start(this.Handle, CurReportMode, Parameter.positionmode, (Parameter.isUse3Station ? AfewDPos.Pos2Dim : AfewDPos.Pos3Dim), Parameter.isUseTagHeightRange, Parameter.TagHeightRangeLow, Parameter.TagHeightRangeHigh))
                    {
                        //开启定时器,设置刷新线程的时间间隔
                        UpdateInterval = (Parameter.isDefineInterval ? Parameter.DefineInterval : 1000);
                        UpdateMapThread = new Thread(UpdateDrawFun);
                        if (Parameter.isRegionAlarmRateTime) {
                            regularRegionAlarmReport = new RegularRegionAlarmReport(Parameter.regionAlarmRateTime, tgmsgs, this);
                            regularRegionAlarmReport.start();
                        }
                        //将标志位置为真
                        isUpdate = true;
                        UpdateMapThread.Start();
                        isStart = true;
                        StartList_button.Text = "Disconnect the monitoring";
                        ListShowCard_groupBox.Enabled = true;
                        CardList_panel.Controls.Clear();
                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    MessageBox.Show("对不起，请求的地址无效!");
                }
                catch (Exception)
                {
                }
            }
            else 
            {
                PrecisePositionLibrary.PrecisePosition.Stop();
                if (null != UpdateMapThread)
                {
                    isUpdate = false;
                    Thread.Sleep(100);
                    try
                    {
                        if (!UpdateMapThread.IsAlive)
                        {
                            UpdateMapThread.Abort();
                        }
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        UpdateMapThread = null;
                    }
                }
                if (null != regularRegionAlarmReport)
                {
                    regularRegionAlarmReport.stop();
                    regularRegionAlarmReport = null;
                }
                CurReportMode = ReportMode.UnKnown;
                isStart = false;
                StartList_button.Text = "Start monitoring";
                ListShowCard_groupBox.Enabled = false;
                CardImgs.Clear();
                tgmsgs.Clear();
                Ports.Clear();
                Map_panel_Paint(null, null);
                Alarminfor_textBox.Visible = false;
                System.Threading.Tasks.Task.Factory.StartNew(SaveRecord);
            }
        }
        public void updateWarnDraw(String content) {
            this.Invoke(new Action(() => {
                Alarminfor_textBox.Text = content;
                AlarmInfors.Add(content);
            }));
        }
        /// <summary>
        /// 转化坐标
        /// </summary>
        private void ConversionOfCoor(double abx, double aby, Group curgrp, ref float x, ref float y)
        {
            //获取真实的比例
            double MapWidth = 0.0d, MapHeight = 0.0d, MarginLeft = 0.0d, MarginTop = 0.0d;
            if (null != curgrp)
            {
                MapWidth = curgrp.scale * curgrp.actualwidth;
                MapHeight = curgrp.scale * curgrp.actualheight;

                MarginLeft = curgrp.scale * abx;
                MarginTop = curgrp.scale * aby;
            }
            else
            {
                MapWidth = Img_RealDisRelation * Parameter.RealWidth;
                MapHeight = Img_RealDisRelation * Parameter.RealHeight;

                MarginLeft = (float)(Img_RealDisRelation * abx);
                MarginTop = (float)(Img_RealDisRelation * aby);
            }
            /*此时的Tag坐标是他的实际坐标，需要将其转化为地图上的坐标
            将实际坐标转化为scale = 1，中点在面板中心时的坐标
            MarginLeft和MarginTop分别指Tag距离地图左边距与上边距的距离*/

            x = (float)(DxfMapParam.PanelCenterX - MapWidth / 2 + MarginLeft);
            y = (float)(DxfMapParam.PanelCenterY - MapHeight / 2 + MarginTop);


        }

        private void AbsCardXY(double abx, double aby,ref float x, ref float y)
        {
            float X = 0, Y = 0;
            ConversionOfCoor(abx, aby,group,ref X, ref Y);
            //此时的x、y是scale=1,且中心点在面板中心时的坐标，需要将他它转化为当前的坐标
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            double CurWidth = PortWidth, CurHeight = PortHeight;
            //此时记录下的X、Y是scale = 1,且面板为CenterX=Map_panel.Width/2,CenterY = Map_panel.Height/2时的坐标
            x = (float)X + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)Y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {//位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {//位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {//位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {//位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
        }
        private void AbsCardXY(double abx,double aby,Group curgrp, ref float x,ref float y)
        {
            float X = 0,Y = 0;
            ConversionOfCoor(abx, aby, curgrp, ref X, ref Y);
            //此时的x、y是scale=1,且中心点在面板中心时的坐标，需要将他它转化为当前的坐标
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            double CurWidth = PortWidth, CurHeight = PortHeight;
            //此时记录下的X、Y是scale = 1,且面板为CenterX=Map_panel.Width/2,CenterY = Map_panel.Height/2时的坐标
            x = (float)X + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)Y + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY-y);
            d1 = Math.Abs(DxfMapParam.CenterX-x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0*1/DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {//位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {//位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {//位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {//位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
        }
        //在地图中画出卡片的内容
        public void DrawCard(CardImg card, Graphics g)
        {

            Brush DrawCardBrush = Brushes.Green;
            Brush StrBrush = Brushes.Red;
            Font StrFont = null;
            if (Parameter.TagShowOver)
            {
                if (DxfMapParam.scale < 10)
                {
                    StrFont = new Font("宋体", (float)(10 / DxfMapParam.scale));
                }
                else
                {
                    StrFont = new Font("宋体", 1);
                }
            }
            else
            {
                StrFont = new Font("宋体", (float)10);
            }
            if (card.LocaType == TPPID.TagCommonLocal)
            {
                StrBrush = Brushes.Green;
            }
            if (card.isOverNoMove)
            {
                DrawCardBrush = Brushes.Black;//黑色
                StrBrush = Brushes.Black;
            }
            if (card.isLowBattery)
            {
                DrawCardBrush = Brushes.Indigo;//紫色
                StrBrush = Brushes.Indigo;
            }
            if (card.isShowRed)
            {
                DrawCardBrush = Brushes.Red;//红色
                StrBrush = Brushes.Red;
            }
            if (card.isShowRed)
            {
                if (Environment.TickCount - card.ShowRedTick > 1500)
                {
                    card.isShowRed = false;
                }
            }
            string StrID = card.ID[0].ToString("X2") + card.ID[1].ToString("X2");
            string StrName = null;
            Tagmsg tgmsg = null;
            if (tgmsgs.TryGetValue(StrID, out tgmsg))
            {
                StrName = tgmsg.Name;
            }
            float x = -1, y = -1, CurTagR = TagR,z = -1;
            Group gp = null;
            if (Parameter.isSupportMulArea)
            {
                if (!Groups.TryGetValue(card.GroupID[0].ToString("X2") + card.GroupID[1].ToString("X2"), out gp))
                {
                    return;
                }
            }
            else
            {
                gp = group;
            }
            #region 计算Scale = 1时的坐标
            if (Parameter.isSupportMulArea)
            {
                if (Parameter.isKalman)
                {
                    if (card.KalmanPoint.x <= 0 && card.KalmanPoint.y <= 0)
                    {
                        return;
                    }
                    else
                    {
                        ConversionOfCoor(card.KalmanPoint.x, card.KalmanPoint.y, gp, ref x, ref y);
                        z = (float)card.KalmanPoint.z;
                    }
                }
                else
                {
                    if (card.CardPoint.x <= 0 && card.CardPoint.y <= 0)
                    {
                        return;
                    }
                    else
                    {
                        ConversionOfCoor(card.CardPoint.x, card.CardPoint.y, gp, ref x, ref y);
                        z = (float)card.CardPoint.z;
                    }
                }
            }
            else
            {
                if (Parameter.isKalman)
                {
                    if (card.KalmanPoint.x <= 0 && card.KalmanPoint.y <= 0)
                    {
                        return;
                    }
                    else
                    {
                        ConversionOfCoor(card.KalmanPoint.x, card.KalmanPoint.y, group, ref x, ref y);
                        z = (float)card.KalmanPoint.z;
                    }
                }
                else
                {
                    if (card.CardPoint.x <= 0 && card.CardPoint.y <= 0)
                    {
                        return;
                    }
                    else
                    {
                        ConversionOfCoor(card.CardPoint.x, card.CardPoint.y, group, ref x, ref y);
                        z = (float)card.CardPoint.z;
                    }
                }
            }
            #endregion
            if (Parameter.isEnableLimitArea)
            {//这里需要我们传实际坐标才行
                if (!AddAreaAlarmRecord(card,new Point(x,y)))
                {
                    card.curareatype = AreaType.CommonArea;
                    card.curstrid = "";
                    if (Parameter.isRegionAlarmRateTime)
                    {
                        regularRegionAlarmReport.removeTag(card);
                    }
                }
            }
            if (Parameter.isSupportMulArea)
            {
                #region 需要我们判断当前的组别中是否需要我们画Tag
                if (!(card.GroupID[0].ToString("X2") + card.GroupID[1].ToString("X2")).Equals(group.id[0].ToString("X2") + group.id[1].ToString("X2")))
                {
                    return;
                }
                #endregion
            }
            #region 计算得到缩放、平移后的坐标
            if (Parameter.isSupportMulArea)
            {
                if (Parameter.isKalman) {
                    AbsCardXY(card.KalmanPoint.x, card.KalmanPoint.y, gp, ref x, ref y);
                } else {
                    AbsCardXY(card.CardPoint.x, card.CardPoint.y, gp, ref x, ref y);
                }
            }
            else
            {
                if (Parameter.isKalman) {
                    AbsCardXY(card.KalmanPoint.x, card.KalmanPoint.y, ref x, ref y);
                } else {
                    AbsCardXY(card.CardPoint.x, card.CardPoint.y, ref x, ref y);
                }
            }
            #endregion
            if (Parameter.TagShowOver)
            {
                CurTagR = (float)(TagR / DxfMapParam.scale);
            }
            else
            {
                CurTagR = TagR;
            }
            card.curpostype = PortType.ThreeMode;
            //画出尾部
            if (Parameter.isShowTrace)
            {
                float x1 = -1, y1 = -1, x2 = -1, y2 = -1, x3 = -1, 
                      y3 = -1, x4 = -1, y4 = -1, x5 = -1, y5 = -1;
                if (null != card.CardPoint1)
                {
                    if (Parameter.isSupportMulArea)
                    {
                        AbsCardXY(card.CardPoint1.x, card.CardPoint1.y, gp, ref x1, ref y1);
                    }
                    else
                    {
                        AbsCardXY(card.CardPoint1.x, card.CardPoint1.y, ref x1, ref y1);
                    }
                    g.FillEllipse(new SolidBrush(Color.FromArgb(192, 255, 192)), x1 - CurTagR / 2, y1 - CurTagR / 2, CurTagR * 2, CurTagR * 2);
                }
                if (null != card.CardPoint2)
                {
                    //#0xFAFFF0
                    if (Parameter.isSupportMulArea)
                    {
                        AbsCardXY(card.CardPoint2.x, card.CardPoint2.y, gp, ref x2, ref y2);
                    }
                    else
                    {
                        AbsCardXY(card.CardPoint2.x, card.CardPoint2.y, ref x2, ref y2);
                    }
                    g.FillEllipse(Brushes.Gray, x2 - CurTagR / 2, y2 - CurTagR / 2, CurTagR * 2, CurTagR * 2);
                }
                if (null != card.CardPoint3)
                {
                    if (Parameter.isSupportMulArea)
                    {
                        AbsCardXY(card.CardPoint3.x, card.CardPoint3.y, gp, ref x3, ref y3);
                    }
                    else
                    {
                        AbsCardXY(card.CardPoint3.x, card.CardPoint3.y, ref x3, ref y3);
                    }
                    g.FillEllipse(Brushes.Silver, x3 - CurTagR / 2, y3 - CurTagR / 2, CurTagR * 2, CurTagR * 2);
                }
                if (null != card.CardPoint4)
                {
                    if (Parameter.isSupportMulArea)
                    {
                        AbsCardXY(card.CardPoint4.x, card.CardPoint4.y, gp, ref x4, ref y4);
                    }
                    else
                    {
                        AbsCardXY(card.CardPoint4.x, card.CardPoint4.y, ref x4, ref y4);
                    }
                    g.FillEllipse(new SolidBrush(Color.FromArgb(224, 224, 224)), x4 - CurTagR / 2, y4 - CurTagR / 2, CurTagR * 2, CurTagR * 2);
                }
                if (null != card.CardPoint5)
                {
                    if (Parameter.isSupportMulArea)
                    {
                        AbsCardXY(card.CardPoint5.x, card.CardPoint5.y, gp, ref x5, ref y5);
                    }
                    else
                    {
                        AbsCardXY(card.CardPoint5.x, card.CardPoint5.y, ref x5, ref y5);
                    }
                    g.DrawEllipse(Pens.Gray, x5 - CurTagR / 2, y5 - CurTagR / 2, CurTagR * 2, CurTagR * 2);
                }
            }
            if (x - CurTagR / 2 <= 0 || y - CurTagR / 2 <= 0)
            {
                return;
            }
            //当放大缩小地图时,Tag是否跟随变化
            StringBuilder strname = new StringBuilder();
            if (null != StrName && !"".Equals(StrName))
            {
                strname.Append(StrName);
                strname.Append("(");
                strname.Append(StrID);
                strname.Append(")");
            }
            else
            {
                strname.Append(StrID);
            }
            if (!Parameter.isUse3Station && !float.IsInfinity(z))
            {
                strname.Append(" H:");
                strname.Append(String.Format("{0:N3}", z));
                strname.Append("cm");
            }
            g.DrawString(strname.ToString(), StrFont, StrBrush, x + CurTagR * 2, y - CurTagR / 3);
            g.FillEllipse(DrawCardBrush, x - CurTagR / 2, y - CurTagR / 2, CurTagR * 2, CurTagR * 2);
        }
        /// <summary>
        /// 画出所有的Tag的资料
        /// </summary>
        private void DrawWorkTags(Graphics g)
        {
            foreach (KeyValuePair<string, CardImg> CardImg in CardImgs)
            {
                if(isTrace)
                {
                    if (!CardImg.Key.Equals(TraceTagId))
                    {
                        continue;
                    }
                }
                if(Parameter.isSupportMulArea)
                {
                    if (null != group)
                    {
                        if (CardImg.Value.GroupID[0] != group.id[0] || CardImg.Value.GroupID[1] != group.id[1])
                        {
                            continue;
                        }
                    }
                }

                if (Parameter.isKalman)
                {
                    if (CardImg.Value.KalmanPoint.x <= 0 || CardImg.Value.KalmanPoint.y <= 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (CardImg.Value.CardPoint.x <= 0 || CardImg.Value.CardPoint.y <= 0)
                    {
                        continue;
                    }
                }
                //卡片显示
                if (((DateTime.Now - CardImg.Value.ReceiTime).TotalSeconds > Parameter.OverTime1) && Parameter.NoShow_OverTime_NoRecei)
                {
                    CardImg.Value.isShowImg = false;
                }
                else
                {
                    CardImg.Value.isShowImg = true;
                }
                if (Parameter.RecordOverTimeNoReceiInfo)
                {
                    TimeSpan timeSpan = DateTime.Now - CardImg.Value.ReceiTime;
                    if (timeSpan.TotalSeconds > Parameter.OverNoReceiveWarmTime && !CardImg.Value.isTimeOutReceiveWarm)
                    {
                        string StrID = CardImg.Value.ID[0].ToString("X2") + CardImg.Value.ID[1].ToString("X2");
                        string StrName = Ini.GetValue(Ini.CardPath, Ini.CardSeg, StrID);
                        StringBuilder strtagbuilder = null, strwarmbuilder = null;
                        if (null == StrName || "".Equals(StrName))
                        {
                            strtagbuilder = new StringBuilder(StrID);
                        }
                        else
                        {
                            strtagbuilder = new StringBuilder(StrName);
                            strtagbuilder.Append("(");
                            strtagbuilder.Append(StrID);
                            strtagbuilder.Append(")");
                        }
                        strwarmbuilder = new StringBuilder("Warning message: Didn't receive overtime ");
                        strwarmbuilder.Append(strtagbuilder.ToString());
                        strtagbuilder.Append(" Tag location information!");
                        AlarmInfors.Add(strtagbuilder.ToString());
                        Alarminfor_textBox.Text = strtagbuilder.ToString();
                        Alarminfor_textBox.Visible = true;
                        CardImg.Value.isTimeOutReceiveWarm = true;
                    }
                }
                if (CardImg.Value.isLowBattery && !CardImg.Value.isLowBatteryWarm)
                {
                    string StrID = CardImg.Value.ID[0].ToString("X2") + CardImg.Value.ID[1].ToString("X2");
                    string StrName = Ini.GetValue(Ini.CardPath, Ini.CardSeg, StrID);
                    StringBuilder strtagbuilder = null, strwarmbuilder = null;
                    if (null == StrName || "".Equals(StrName))
                    {
                        strtagbuilder = new StringBuilder(StrID);
                    }
                    else
                    {
                        strtagbuilder = new StringBuilder(StrName);
                        strtagbuilder.Append("(");
                        strtagbuilder.Append(StrID);
                        strtagbuilder.Append(")");
                    }
                    strwarmbuilder = new StringBuilder("Warning message: The number ");
                    strwarmbuilder.Append(strtagbuilder.ToString());
                    strwarmbuilder.Append(" Tag is ");
                    strwarmbuilder.Append(CardImg.Value.Battery);
                    strwarmbuilder.Append("% below ");
                    strwarmbuilder.Append(Parameter.LowBattry);
                    strwarmbuilder.Append("%");
                    string str = strwarmbuilder.ToString();
                    AlarmInfors.Add(str);
                    Alarminfor_textBox.Text = str;
                    Alarminfor_textBox.Visible = true;
                    CardImg.Value.isLowBatteryWarm = true;
                }
                if (CardImg.Value.isShowImg && CardImg.Value.isShowTag)
                {
                    SingleThreePointDrawing(CardImg.Value, g);
                    //画出辅助线
                    if (isShowGuidesLine)
                    {
                        DrawGuidsLine(g, CardImg.Value);
                    }
                }
            }
        }
        /// <summary>
        /// 单点和多点画图
        /// </summary>
        /// <param name="card"></param>
        /// <param name="g"></param>
        private void SingleThreePointDrawing(CardImg card, Graphics g)
        {
            PosPortinfor minport = null;
            if (Parameter.isEnableReferType)
            {//基站模式选择单点和多点模式
                minport = GetMinDisPort(card);
                if (null != minport && PortType.SingleMode == minport.porttype && minport.distanse < minport.rangeR)
                {
                    //此时我们需要将当前tag画到最近基站旁边
                    DrawTagToPortNear(card,minport,g);
                }
                else
                {
                    DrawCard(card, g);
                }
            }
            else
            {
                DrawCard(card,g);
            }
        }
        /// <summary>
        /// 添加区域警报讯息
        /// </summary>
        /// <param name="card"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool AddAreaAlarmRecord(CardImg card,Point point)
        {
            bool isCurrentAlert = false;
            string strtagname = "", strtagid = "";
            strtagid = card.ID[0].ToString("X2") + card.ID[1].ToString("X2");
            Tagmsg tgmsg = null;
            if (tgmsgs.TryGetValue(strtagid, out tgmsg)) {
                strtagname = tgmsg.Name;
            }
            //查看当前卡片是否进入限制区域
            if (Parameter.isSupportMulArea)
            {
                #region 多区域模式下
                List<KeyValuePair<string, Group>> gpps = Groups.OrderBy(key => key.Key).ToList();
                foreach (KeyValuePair<string, Group> gp in gpps)
                {
                    if (null == gp.Value)
                    {
                        continue;
                    }
                    if (!gp.Key.Equals(card.GroupID[0].ToString("X2") + card.GroupID[1].ToString("X2")))
                    {//说明卡片不在这一个组别上
                        continue;
                    }
                    foreach (KeyValuePair<string, LimitArea> liarea in gp.Value.grouplimiares)
                    {
                        if (null == liarea.Value)
                        {
                            continue;
                        }
                        if (gp.Key.Equals(card.GroupID[0].ToString("X2") + card.GroupID[1].ToString("X2")))
                        {
                            //此时这个x,y是在地图上的坐标Scale = 1,中心点在中心位置的坐标
                            //判断Tag是否进入指定的区域
                            if (point.x >= liarea.Value.startpoint.x &&
                                point.x < liarea.Value.endpoint.x &&
                                point.y >= liarea.Value.startpoint.y &&
                                point.y <= liarea.Value.endpoint.y) {
                                //说明此时已经进入限制区域
                                isCurrentAlert = true;
                                if (card.curareatype == AreaType.CommonArea) { //之前是非限制区域，而现在又是限制区域
                                    #region 之前是非限制区域，而现在又是限制区域
                                    if (Parameter.isRegionAlarmRateTime)
                                    {
                                        regularRegionAlarmReport.addCardImg(card, liarea.Value);
                                    }
                                    else {
                                        StringBuilder strwarmbuilder = new StringBuilder("Warning message：");
                                        strwarmbuilder.Append(DateTime.Now.ToString());
                                        strwarmbuilder.Append(" => ");
                                        if (null == strtagname || "".Equals(strtagname))
                                        {
                                            strwarmbuilder.Append(strtagid);
                                            strwarmbuilder.Append(" enters ");
                                            if (null == liarea.Value.Name || "".Equals(liarea.Value.Name))
                                            {
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(" restricted area...");
                                                AlarmInfors.Add(strwarmbuilder.ToString());
                                                strwarmbuilder.Clear();
                                                strwarmbuilder.Append("Warning message：");
                                                strwarmbuilder.Append(strtagid);
                                                strwarmbuilder.Append(" enters ");
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(" restricted area...");
                                                this.Invoke(new Action(() =>
                                                {
                                                    Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                }));
                                            }
                                            else
                                            {
                                                strwarmbuilder.Append(liarea.Value.Name);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(") restricted area...");
                                                AlarmInfors.Add(strwarmbuilder.ToString());
                                                strwarmbuilder.Clear();
                                                strwarmbuilder.Append("Warning message：");
                                                strwarmbuilder.Append(strtagid);
                                                strwarmbuilder.Append(" enters ");
                                                strwarmbuilder.Append(liarea.Value.Name);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(") restricted area...");
                                                this.Invoke(new Action(() =>
                                                {
                                                    Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                }));
                                            }
                                        }
                                        else
                                        {
                                            strwarmbuilder.Append(strtagname);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(strtagid);
                                            strwarmbuilder.Append(") enters ");
                                            if (null == liarea.Value.Name || "".Equals(liarea.Value.Name))
                                            {
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(" restricted area...");
                                                AlarmInfors.Add(strwarmbuilder.ToString());
                                                strwarmbuilder.Clear();
                                                strwarmbuilder.Append("Warning message：");
                                                strwarmbuilder.Append(strtagname);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(strtagid);
                                                strwarmbuilder.Append(") enters ");
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(" restricted area...");
                                                this.Invoke(new Action(() =>
                                                {
                                                    Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                }));
                                            }
                                            else
                                            {
                                                strwarmbuilder.Append(liarea.Value.Name);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(") restricted area...");
                                                AlarmInfors.Add(strwarmbuilder.ToString());
                                                strwarmbuilder.Clear();
                                                strwarmbuilder.Append("Warning message：");
                                                strwarmbuilder.Append(strtagname);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(strtagid);
                                                strwarmbuilder.Append(") enters ");
                                                strwarmbuilder.Append(liarea.Value.Name);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(liarea.Key);
                                                strwarmbuilder.Append(") restricted area...");
                                                this.Invoke(new Action(() =>
                                                {
                                                    Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                }));
                                            }
                                        }
                                    }
                                    this.Invoke(new Action(() =>
                                    {
                                        Alarminfor_textBox.Visible = true;
                                    }));
                                    card.curareatype = AreaType.LimitArea;
                                    card.curstrid = liarea.Key;
                                    #endregion
                                }
                                else
                                {
                                    #region 之前是限制区域，后面又是限制区域
                                    if (!liarea.Key.Equals(card.curstrid))
                                    {
                                        // 两次不一样
                                        if (Parameter.isRegionAlarmRateTime)
                                        {
                                            regularRegionAlarmReport.updateCardRegion(card, liarea.Value);
                                        }
                                        else {
                                            StringBuilder strwarmbuilder = new StringBuilder("Warning message：");
                                            strwarmbuilder.Append(DateTime.Now.ToString());
                                            strwarmbuilder.Append(" => ");
                                            if (null == strtagname || "".Equals(strtagname))
                                            {
                                                strwarmbuilder.Append(strtagid);
                                                strwarmbuilder.Append(" enters ");
                                                if (null == liarea.Value.Name || "".Equals(liarea.Value.Name))
                                                {
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(" restricted area...");
                                                    AlarmInfors.Add(strwarmbuilder.ToString());
                                                    strwarmbuilder.Clear();
                                                    strwarmbuilder.Append("Warning message：");
                                                    strwarmbuilder.Append(strtagid);
                                                    strwarmbuilder.Append(" enters ");
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(" restricted area...");
                                                    this.Invoke(new Action(() =>
                                                    {
                                                        Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                    }));

                                                }
                                                else
                                                {
                                                    strwarmbuilder.Append(liarea.Value.Name);
                                                    strwarmbuilder.Append("(");
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(") restricted area...");
                                                    AlarmInfors.Add(strwarmbuilder.ToString());
                                                    strwarmbuilder.Clear();
                                                    strwarmbuilder.Append("Warning message：");
                                                    strwarmbuilder.Append(strtagid);
                                                    strwarmbuilder.Append(" enters ");
                                                    strwarmbuilder.Append(liarea.Value.Name);
                                                    strwarmbuilder.Append("(");
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(") restricted area...");
                                                    this.Invoke(new Action(() =>
                                                    {
                                                        Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                    }));

                                                }
                                            }
                                            else
                                            {
                                                strwarmbuilder.Append(strtagname);
                                                strwarmbuilder.Append("(");
                                                strwarmbuilder.Append(strtagid);
                                                strwarmbuilder.Append(") enters ");
                                                if (null == liarea.Value.Name || "".Equals(liarea.Value.Name))
                                                {
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(" restricted area...");
                                                    AlarmInfors.Add(strwarmbuilder.ToString());
                                                    strwarmbuilder.Clear();
                                                    strwarmbuilder.Append("Warning message：");
                                                    strwarmbuilder.Append(strtagname);
                                                    strwarmbuilder.Append("(");
                                                    strwarmbuilder.Append(strtagid);
                                                    strwarmbuilder.Append(") enters ");
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(" restricted area...");
                                                    this.Invoke(new Action(() =>
                                                    {
                                                        Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                    }));
                                                }
                                                else
                                                {
                                                    strwarmbuilder.Append(liarea.Value.Name);
                                                    strwarmbuilder.Append("(");
                                                    strwarmbuilder.Append(liarea.Key);

                                                    strwarmbuilder.Append(") restricted area...");
                                                    AlarmInfors.Add(strwarmbuilder.ToString());
                                                    strwarmbuilder.Clear();
                                                    strwarmbuilder.Append("Warning message：");
                                                    strwarmbuilder.Append(strtagname);
                                                    strwarmbuilder.Append("(");
                                                    strwarmbuilder.Append(strtagid);
                                                    strwarmbuilder.Append(") enters ");
                                                    strwarmbuilder.Append(liarea.Value.Name);
                                                    strwarmbuilder.Append("(");
                                                    strwarmbuilder.Append(liarea.Key);
                                                    strwarmbuilder.Append(") restricted area...");
                                                    this.Invoke(new Action(() =>
                                                    {
                                                        Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                                    }));
                                                }
                                            }
                                        }
                                        this.Invoke(new Action(() =>
                                        {
                                            Alarminfor_textBox.Visible = true;
                                        }));
                                        card.curstrid = liarea.Key;
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region 单区域模式下
                foreach (KeyValuePair<string, LimitArea> area in Areas)
                {
                    if (null == area.Value)
                        continue;
                    if (point.x >= area.Value.startpoint.x &&
                        point.x < area.Value.endpoint.x &&
                        point.y >= area.Value.startpoint.y &&
                        point.y <= area.Value.endpoint.y)
                    {
                        //说明此时进入了限制区域
                        isCurrentAlert = true;
                        if (card.curareatype == AreaType.CommonArea)
                        {
                            #region 之前是非限制区域，现在变为限制区域
                            if (Parameter.isRegionAlarmRateTime)
                            {
                                regularRegionAlarmReport.addCardImg(card, area.Value);
                            }
                            else {
                                StringBuilder strwarmbuilder = new StringBuilder("Warning message：");
                                strwarmbuilder.Append(DateTime.Now.ToString());
                                strwarmbuilder.Append(" => ");
                                if (null == strtagname || "".Equals(strtagname))
                                {
                                    strwarmbuilder.Append(strtagid);
                                    strwarmbuilder.Append(" enters ");
                                    if (null == area.Value.Name || "".Equals(area.Value.Name))
                                    {
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(" restricted area...");
                                        AlarmInfors.Add(strwarmbuilder.ToString());
                                        strwarmbuilder.Clear();
                                        strwarmbuilder.Append("Warning message：");
                                        strwarmbuilder.Append(strtagid);
                                        strwarmbuilder.Append(" enters ");
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(" restricted area...");
                                        this.Invoke(new Action(() =>
                                        {
                                            Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                        }));
                                    }
                                    else
                                    {
                                        strwarmbuilder.Append(area.Value.Name);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(") restricted area...");
                                        AlarmInfors.Add(strwarmbuilder.ToString());
                                        strwarmbuilder.Clear();
                                        strwarmbuilder.Append("Warning message：");
                                        strwarmbuilder.Append(strtagid);
                                        strwarmbuilder.Append(" enters ");
                                        strwarmbuilder.Append(area.Value.Name);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(") restricted area...");
                                        this.Invoke(new Action(() =>
                                        {
                                            Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                        }));
                                    }
                                }
                                else
                                {
                                    strwarmbuilder.Append(strtagname);
                                    strwarmbuilder.Append("(");
                                    strwarmbuilder.Append(strtagid);
                                    strwarmbuilder.Append(") enters ");
                                    if (null == area.Value.Name || "".Equals(area.Value.Name))
                                    {
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(" restricted area...");
                                        AlarmInfors.Add(strwarmbuilder.ToString());
                                        strwarmbuilder.Clear();
                                        strwarmbuilder.Append("Warning message：");
                                        strwarmbuilder.Append(strtagname);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(strtagid);
                                        strwarmbuilder.Append(") enters ");
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(" restricted area...");
                                        this.Invoke(new Action(() =>
                                        {
                                            Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                        }));
                                    }
                                    else
                                    {
                                        strwarmbuilder.Append(area.Value.Name);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(") restricted area...");
                                        AlarmInfors.Add(strwarmbuilder.ToString());
                                        strwarmbuilder.Clear();
                                        strwarmbuilder.Append("Warning message：");
                                        strwarmbuilder.Append(strtagname);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(strtagid);
                                        strwarmbuilder.Append(") enters ");
                                        strwarmbuilder.Append(area.Value.Name);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(area.Key);
                                        strwarmbuilder.Append(") restricted area...");
                                        this.Invoke(new Action(() =>
                                        {
                                            Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                        }));
                                    }
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                Alarminfor_textBox.Visible = true;
                            }));

                            card.curareatype = AreaType.LimitArea;
                            card.curstrid = area.Key;
                            #endregion
                        }
                        else
                        {
                            #region 前面也是在限制区域，判断两次限制区域是否相同，不相同的话还是需要报警
                            if (!area.Key.Equals(card.curstrid))
                            {
                                if (Parameter.isRegionAlarmRateTime)
                                {
                                    regularRegionAlarmReport.updateCardRegion(card, area.Value);
                                }
                                else {
                                    StringBuilder strwarmbuilder = new StringBuilder("Warning message：");
                                    strwarmbuilder.Append(DateTime.Now.ToString());
                                    strwarmbuilder.Append(" => ");
                                    if (null == strtagname || "".Equals(strtagname))
                                    {
                                        strwarmbuilder.Append(strtagid);
                                        strwarmbuilder.Append(" enters ");
                                        if (null == area.Value.Name || "".Equals(area.Value.Name))
                                        {
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(" restricted area...");
                                            AlarmInfors.Add(strwarmbuilder.ToString());
                                            strwarmbuilder.Clear();
                                            strwarmbuilder.Append("Warning message：");
                                            strwarmbuilder.Append(strtagid);
                                            strwarmbuilder.Append(" enters ");
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(" restricted area...");
                                            this.Invoke(new Action(() =>
                                            {
                                                Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                            }));
                                        }
                                        else
                                        {
                                            strwarmbuilder.Append(area.Value.Name);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(") restricted area...");
                                            AlarmInfors.Add(strwarmbuilder.ToString());
                                            strwarmbuilder.Clear();
                                            strwarmbuilder.Append("Warning message：");
                                            strwarmbuilder.Append(strtagid);
                                            strwarmbuilder.Append(" enters ");
                                            strwarmbuilder.Append(area.Value.Name);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(") restricted area...");
                                            this.Invoke(new Action(() =>
                                            {
                                                Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                            }));
                                        }
                                    }
                                    else
                                    {
                                        strwarmbuilder.Append(strtagname);
                                        strwarmbuilder.Append("(");
                                        strwarmbuilder.Append(strtagid);
                                        strwarmbuilder.Append(") enters ");
                                        if (null == area.Value.Name || "".Equals(area.Value.Name))
                                        {
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(" restricted area...");
                                            AlarmInfors.Add(strwarmbuilder.ToString());
                                            strwarmbuilder.Clear();
                                            strwarmbuilder.Append("Warning message：");
                                            strwarmbuilder.Append(strtagname);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(strtagid);
                                            strwarmbuilder.Append(") enters ");
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(" restricted area...");
                                            this.Invoke(new Action(() =>
                                            {
                                                Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                            }));
                                        }
                                        else
                                        {
                                            strwarmbuilder.Append(area.Value.Name);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(") restricted area...");
                                            AlarmInfors.Add(strwarmbuilder.ToString());
                                            strwarmbuilder.Clear();
                                            strwarmbuilder.Append("Warning message：");
                                            strwarmbuilder.Append(strtagname);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(strtagid);
                                            strwarmbuilder.Append(") enters ");
                                            strwarmbuilder.Append(area.Value.Name);
                                            strwarmbuilder.Append("(");
                                            strwarmbuilder.Append(area.Key);
                                            strwarmbuilder.Append(") restricted area...");
                                            this.Invoke(new Action(() =>
                                            {
                                                Alarminfor_textBox.Text = strwarmbuilder.ToString();
                                            }));
                                        }
                                    }
                                }
                                this.Invoke(new Action(() =>
                                {
                                    Alarminfor_textBox.Visible = true;
                                }));
                                card.curstrid = area.Key;
                            }
                            #endregion
                        }
                    }
                }
                #endregion
            }
            return isCurrentAlert;
        }
        /// <summary>
        /// 单点模式下,画Tag
        /// </summary>
        /// <param name="card"></param>
        /// <param name="minport"></param>
        /// <param name="g"></param>
        private void DrawTagToPortNear(CardImg card, PosPortinfor minport, Graphics g)
        {
            string strportid = "";
            BsInfo mbs = null;
            Brush DrawCardBrush = Brushes.Green;
            Brush StrBrush = Brushes.Red;
            Font StrFont = null;
            if (Parameter.TagShowOver)
            {
                if (DxfMapParam.scale < 10)
                {
                    StrFont = new Font("宋体", (float)(10 / DxfMapParam.scale));
                }
                else
                {
                    StrFont = new Font("宋体", 1);
                }
            }
            else
            {
                StrFont = new Font("宋体", (float)10);
            }
            if (card.LocaType == TPPID.TagCommonLocal)
            {
                StrBrush = Brushes.Green;
            }
            if (card.isOverNoMove)
            {
                DrawCardBrush = Brushes.Black;//黑色
                StrBrush = Brushes.Black;
            }
            if (card.isLowBattery)
            {
                DrawCardBrush = Brushes.Indigo;//紫色
                StrBrush = Brushes.Indigo;
            }
            if (card.isShowRed)
            {
                DrawCardBrush = Brushes.Red;//红色
                StrBrush = Brushes.Red;
            }
            if (card.isShowRed) {
                if (Environment.TickCount - card.ShowRedTick > 1500) {
                    card.isShowRed = false;
                }
            }
            string StrID = card.ID[0].ToString("X2") + card.ID[1].ToString("X2");
            string StrName = null;
            Tagmsg tgmsg = null;
            if (tgmsgs.TryGetValue(StrID, out tgmsg)) {
                StrName = tgmsg.Name;
            }
            float BaseX = -1, BaseY = -1, CurTagR = TagR, CurReferR = ReferNearR;
            //获取基站的坐标
            strportid = minport.ID[0].ToString("X2") + minport.ID[1].ToString("X2");
            //获取基站的位置
            if (Parameter.isSupportMulArea) {

                #region 需要我们判断当前的组别中是否需要我们画Tag
                if (!(card.GroupID[0].ToString("X2") + card.GroupID[1].ToString("X2")).Equals(group.id[0].ToString("X2") + group.id[1].ToString("X2")))
                {
                    return;
                }
                #endregion
                if (!IsExistRefer(strportid, out mbs)) {
                    return;
                }
            } else {
                InnerPorts.TryGetValue(strportid, out mbs);
                if (null == mbs) {
                    return;
                }
            }
            //此时只要单点的基站坐标确定后即可判断是否区域警报
            if(Parameter.isEnableLimitArea) {
                if(!AddAreaAlarmRecord(card,mbs.Place)) { 
                    //不在限制区域中
                    card.curareatype = AreaType.CommonArea;
                    card.curstrid = "";
                    if (Parameter.isRegionAlarmRateTime) {
                        regularRegionAlarmReport.removeTag(card);
                    }
                }
            }
            //说明前面一次是三点，而当前又是单点，需要重新获取坐标
            // if (card.curpostype == PortType.ThreeMode)
            {
                BaseX = (float)mbs.Place.x;
                BaseY = (float)mbs.Place.y;
                //从360中取一个数来表示tag在基站周围的度数
                double MrdDegree = mrd.NextDouble() * 2 * Math.PI;
                //计算Tag的绝对坐标
                card.BaseX = (float)(ReferNearR * Math.Cos(MrdDegree)) + BaseX;
                card.BaseY = (float)(ReferNearR * Math.Sin(MrdDegree)) + BaseY;
                card.curpostype = PortType.SingleMode;
            }
            //计算基站周围的相对半径
            CurReferR = (float)(ReferNearR / DxfMapParam.scale);
            //计算Tag的相对半径
            CurTagR = (float)(TagR / DxfMapParam.scale);
            //计算Tag的相对坐标
            float x = -1, y = -1;
            double d0, d1, L0, L1, p0, p1;
            d0 = d1 = L0 = L1 = p0 = p1 = 0;
            double CurWidth = PortWidth, CurHeight = PortHeight;
            //此时记录下的X、Y是scale = 1,且面板为CenterX=Map_panel.Width/2,CenterY = Map_panel.Height/2时的坐标
            x = (float)card.BaseX + (float)(DxfMapParam.CenterX - DxfMapParam.PanelCenterX);
            y = (float)card.BaseY + (float)(DxfMapParam.CenterY - DxfMapParam.PanelCenterY);
            d0 = Math.Abs(DxfMapParam.CenterY - y);
            d1 = Math.Abs(DxfMapParam.CenterX - x);
            L0 = Math.Pow(Math.Pow(d0, 2) + Math.Pow(d1, 2), 0.5);
            L1 = (L0 * 1 / DxfMapParam.scale);
            p0 = (d0 / L0) * L1;
            p1 = (d1 / L0) * L1;
            if (x <= DxfMapParam.CenterX && y < DxfMapParam.CenterY)
            {//位于左上象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x > DxfMapParam.CenterX && y <= DxfMapParam.CenterY)
            {//位于右上象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY - p0);
            }
            else if (x <= DxfMapParam.CenterX && y > DxfMapParam.CenterY)
            {//位于左下象限
                x = (float)(DxfMapParam.CenterX - p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            else if (x > DxfMapParam.CenterX && y >= DxfMapParam.CenterY)
            {//位于右下象限
                x = (float)(DxfMapParam.CenterX + p1);
                y = (float)(DxfMapParam.CenterY + p0);
            }
            if (x - CurTagR / 2 <= 0 || y - CurTagR / 2 <= 0)
            {
                return;
            }
            //当放大缩小地图时,Tag是否跟随变化
            StringBuilder strname = new StringBuilder();
            if (null == StrName || "".Equals(StrName))
            {
                strname.Append(StrID);
            }
            else
            {
                strname.Append(StrName);
                strname.Append("(");
                strname.Append(StrID);
                strname.Append(")");
            }
            if (!Parameter.isUse3Station && Double.MinValue != card.CardPoint.z)
            {
                strname.Append(" H:");
                strname.Append(String.Format("{0:N3}", card.CardPoint.z));
                strname.Append("cm");
            }
            g.DrawString(strname.ToString(), StrFont, StrBrush, x + CurTagR * 2, y - CurTagR / 3);
            g.FillEllipse(DrawCardBrush, x - CurTagR / 2, y - CurTagR / 2, CurTagR * 2, CurTagR * 2);
        }
        private PosPortinfor GetMinDisPort(CardImg card)
        {
            string strid = "";
            Bsmsg mbs = null;
            List<PosPortinfor> posports = new List<PosPortinfor>();
            List<ReportRouterInfor> rps = card.ReportRouters.ToList();
            PosPortinfor port = null;
            foreach (ReportRouterInfor rp in rps)
            {
                if (null == rp || null == rp.id) {
                    continue;
                }
                PosPortinfor psp = new PosPortinfor();
                port = new PosPortinfor();
                if (rp.id.Length != 2) {
                    continue;
                }
                System.Buffer.BlockCopy(rp.id, 0, port.ID, 0, 2);
                port.distanse = rp.dis;
                strid = port.ID[0].ToString("X2") + port.ID[1].ToString("X2");
                Bsmsgs.TryGetValue(strid, out mbs);
                if (null == mbs)
                {
                    port.porttype = PortType.ThreeMode;
                }
                else
                {
                    port.porttype = mbs.porttype;
                    port.rangeR = mbs.rangeR;
                }
                posports.Add(port);
            }
            if (posports.Count <= 0)
            {
                return null;
            }
            PosPortinfor MinDisPort = posports.First();
            for (int i = 1; i < posports.Count;i++)
            {
                if (posports[i].distanse < MinDisPort.distanse)
                {
                    MinDisPort = posports[i];
                }
            }
            //增加单点距离限制
            if (null != MinDisPort && MinDisPort.distanse > 0)
            { 
                return MinDisPort;
            }
            else 
            { 
                return null;
            }
        }
        private void DrawCurRouter(CardImg card, Bitmap bitmap)
        {
            Graphics g = Graphics.FromImage(bitmap);
            //当卡片距离三个基站的距离都大于0时，画出此时Router等的圆的信息
            foreach (ReportRouterInfor rprt in card.ReportRouters)
            {
                string strid = rprt.id[0].ToString("X2") + rprt.id[1].ToString("X2");
                PrecisePositionLibrary.BsInfo port = null;
                if (rprt.dis > 0)
                {
                    if (InnerPorts.TryGetValue(strid, out port))
                    {
                        int EllR1 = (int)(rprt.dis * Img_RealDisRelation * 0.01);
                        g.DrawEllipse(Pens.Black, (int)port.Place.x - EllR1, (int)port.Place.y - EllR1, 2 * EllR1, 2 * EllR1);
                    }
                }
            }
        }
        private void Show_OnLineCard_Btn_Click(object sender, EventArgs e)
        {
            //显示在线卡片
            Current_OnLine MyCurrent_OnLine = new Current_OnLine(this);
            MyCurrent_OnLine.ShowDialog();
        }
        private void AlarmInfor_Btn_Click(object sender, EventArgs e)
        {
            if (null == MyAlarmInfor)MyAlarmInfor = new AlarmInfor(this);
            MyAlarmInfor.ShowDialog();
        }
        private void CheckRecordBtn_Click(object sender, EventArgs e)
        {
            HistoryrecordWin myHistoryrecordWin = new HistoryrecordWin(this);
            myHistoryrecordWin.Show();
        }
        private void guidBtn_Click(object sender, EventArgs e)
        {
            if (null == MyGuidRecord || MyGuidRecord.IsDisposed)MyGuidRecord = new GuidRecord(this);
            //设置轨迹窗体的位置
            MyGuidRecord.Show();
        }
        private void guidcb_CheckedChanged(object sender, EventArgs e)
        {
            if (guidcb.Checked)
            {
                isShowGuidesLine = true;
                //设置轨迹窗体的位置
                if (null == MyGuidRecord || MyGuidRecord.IsDisposed)
                {
                    MyGuidRecord = new GuidRecord(this);
                }
                MyGuidRecord.Show();
            }
            else
            {
                isShowGuidesLine = false;
                if (null != MyGuidRecord && !MyGuidRecord.IsDisposed)
                    MyGuidRecord.Close();
            }
            loadreferflag = false;
            this.OnLoad(e);
        }
        //列表比较器
        class ListViewCompareor : IComparer
        {
            public int col{set;get;}
            public SortOrder order{set;get;}
            public ListViewCompareor()
            {
                col = 0;
                order = SortOrder.Ascending;
            }
            public ListViewCompareor(int col,SortOrder order)
            {
                this.col = col;
                this.order = order;
            }
            public int Compare(object x, object y)
            { 
                int comparevalue = -1;
                if (order == SortOrder.Ascending)comparevalue = String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
                else comparevalue = -String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
                return comparevalue;
            }
        }
        private void CardList_listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ((ListViewCompareor)(this.CardList_listView.ListViewItemSorter)).col = e.Column;
            if(((ListViewCompareor)(this.CardList_listView.ListViewItemSorter)).order == SortOrder.Ascending)
            {
                ((ListViewCompareor)(this.CardList_listView.ListViewItemSorter)).order = SortOrder.Descending;
            }
            else
            {
                ((ListViewCompareor)(this.CardList_listView.ListViewItemSorter)).order = SortOrder.Ascending;
            }
            this.CardList_listView.Sort();
            //同时修改列表头
            SortColumnText(this.CardList_listView, e.Column);
        }
        private void SortColumnText(ListView mlistView,int col)
        {
             for (int i = 0; i < mlistView.Columns.Count;i++)
             {
                string str = mlistView.Columns[i].Text;
                int asc = str.IndexOf("(Ascending)");
                int des = str.IndexOf("(Descending)");
                if (asc >= 0)
                {
                    mlistView.Columns[i].Text = str.Substring(0, asc);
                }
                if (des >= 0)
                {
                    mlistView.Columns[i].Text = str.Substring(0, des);
                }
             }
             string coltext =  mlistView.Columns[col].Text;
             if (((ListViewCompareor)(this.CardList_listView.ListViewItemSorter)).order == SortOrder.Ascending)
             {
                 mlistView.Columns[col].Text = coltext + "(Ascending)";
             }
             else if (((ListViewCompareor)(this.CardList_listView.ListViewItemSorter)).order == SortOrder.Descending)
             {
                 mlistView.Columns[col].Text = coltext + "(Descending)";
             }
             else mlistView.Columns[col].Text = coltext;
        }
        private void SigOrderCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListSortMode = SigOrderCb.SelectedIndex;
        }
        private void PortBtn_Click(object sender, EventArgs e)
        {//基站讯息
            OnlineBaseForm CurOnlineBaseForm = new OnlineBaseForm(this);
            CurOnlineBaseForm.ShowDialog();
        }
        private void refertypeCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (refertypeCb.SelectedIndex == 1)
            {//单点定位时，设置一个半径范围
                SingleRangeBox.Visible = true;
                UpdateLeftPort_Btn.Location = new System.Drawing.Point(30, 218);
                SingleRangeBox.Location = new System.Drawing.Point(12, 156);
                groupBox1.Height = 283;

            }
            else
            {
                SingleRangeBox.Visible = false;
                UpdateLeftPort_Btn.Location = new System.Drawing.Point(30, 168);
                groupBox1.Height = 230;
            }
        }
        /// <summary>
        /// 选择切换不同区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectAreaCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            String strid = "";
            String strarea = SelectAreaCB.Text;
            int index1 = strarea.LastIndexOf("(");
            int index2 = strarea.LastIndexOf(")");
            if (index1 >= 0 && index2 >= 0)
            {
                strid = strarea.Substring(index1 + 1, strarea.Length - index1 - 2);
            }
            else
            {
                strid = strarea;
            }
            if (!Groups.TryGetValue(strid, out group))
            {
                MessageBox.Show("The selected area does not exist!");
                return;
            }

            //得到区域地图
            String strpath = group.grouppath;
            StrMapPath = strpath;
            try
            {
                DxfMapParam.ClearDxf();
                DxfMapParam.CenterX = Map_panel.Width / 2;
                DxfMapParam.CenterY = Map_panel.Height / 2;
                LoadDxfMap();
            }
            catch(Exception)
            {
                MessageBox.Show("Sorry, there was an exception when the vector diagram was loaded!");
                return;
            }
            Map_panel_Paint(null, null);
        }
        private void Form1_Activated(object sender, EventArgs e)
        {

        }

        private void tracecb_CheckedChanged(object sender, EventArgs e)
        {
            if (tracecb.Checked)
            {
                string strtag = tracetagtb.Text;
                if ("".Equals(strtag))
                {
                    MessageBox.Show("Sorry, the card message cannot be empty!");
                    tracecb.Checked = false;
                    tracecb.Enabled = true;
                    return;
                }
                //判断当前的字符串是否是卡片的ID还是卡片的名称
                if (strtag.Length == 4)
                {
                    byte[] id = new byte[2];
                    try
                    {
                        id[0] = Convert.ToByte(strtag.Substring(0, 2), 16);
                        id[1] = Convert.ToByte(strtag.Substring(2, 2), 16);
                    }
                    catch
                    {
                        MessageBox.Show("Sorry, the card message format is wrong!");
                        tracecb.Checked = false;
                        tracetagtb.Enabled = true;
                        return;
                    }
                    isTrace = true;
                    TraceTagId = strtag.ToUpper();
                    tracetagtb.Enabled = false;
                    return;
                }
                //说明这个可能是卡片的名称
                string strtagid = GetTagFromName(strtag);
                if (null != strtagid)
                {
                    TraceTagId = strtagid;
                }
                else
                { //说明当前填写的卡片讯息即不是卡片的ID也不是卡片的名称
                    MessageBox.Show("Sorry, the card information does not conform to the specification!");
                    tracecb.Checked = false;
                    tracetagtb.Enabled = true;
                    return;
                }
                tracetagtb.Enabled = false;
                isTrace = true;
            }
            else
            {
                isTrace = false;
                tracetagtb.Enabled = true;
            }
        }

        public String GetTagFromName(String strtagname)
        {
            List<String> lists = null;
            if (!Ini.GetAllSegment(Ini.CardPath, out lists))
            {
                return null;
            }
            for (int i = 0; i < lists.Count; i ++)
            {
                string strname = Ini.GetValue(Ini.CardPath,lists[i],Ini.Name);
                if (null == strname || "".Equals(strname))
                {
                    continue;
                }
                if (strname.Equals(strtagname))
                {
                    return lists[i];
                }
            }
            return null;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FormCustomData customForm = new FormCustomData(this);
            customForm.Owner = this;
            customForm.ShowDialog();
        }
    }
    //文件操作函数
    public class Ini
    {
        private Ini() { }
        private static Encoding encoder = Encoding.UTF8;
        static FileStream MyFileStream = null;
        public const int MaxLen = 100;//数组大小
        public static String ConfigPath = Environment.CurrentDirectory + "\\Config.ini";
        public static String PortPath = Environment.CurrentDirectory + "\\Port.ini";
        public static String PortSeg = "PORT";
        public static String CardPath = Environment.CurrentDirectory + "\\Card.ini";
        public static String CardSeg = "CARD";
        public const String NetSeg = "SERVER";
        public const String NetKey_IP = "IP";
        public const String NetKey_Port = "PORT";
        public const String MapSeg = "MAP";
        public const String MapKey_Path = "Path";
        public const String RealWidth = "RealWidth";
        public const String RealHeight = "RealHeight";
        public const String RealScale = "REALSCALE";

        public const string BatterySeg = "BATT";
        public const string IsLowBattery = "BATTFLAG";
        public const string LowBattery = "LOWBATT";

        public const string regionAlarmOver = "ALARMOVERTIME";
        public const string isRegionAlarmOverTime = "ALARMOVERTIMEFLAG";
        public const string regionAlarmOverTime = "ALARMOVERTIME";

        public const string AreaAramSeg = "AREAARM";
        public const string EnableAreaAlarm = "LIMITAREA";

        public const string LimitStartX = "STARTX";
        public const string LimitStartY = "STARTY";

        public const string LimitEndX = "ENDX";
        public const string LimitEndY = "ENDY";

        public const string ClearSeg = "CLEARSEG";

        public const string IsClearKey = "ISCLEAR";
        public const string ClearTimeKey = "CLEARTIME";

        public const string OTNoReceiveSeg = "OVERRECEIVE";
        public const string OTNoReveiveKey = "OVERRECEIVEKEY";
        public const string OTNoReceiveWarmTime = "OVERRECEIVETIME";

        public const string ShowSeg = "SHOW";
        public const string ShowReferKey = "REFERSHOWKEY";
        public const string LongTime_NoExeKey = "LONGNOEXEKEY";
        public const string NoExeTime = "NOEXETIME";

        public const string Use3Place = "USE3PLACE";

        public const string UseTagHeightRange = "USETAGHEIGHTRANGE";
        public const string TagHeightRangeLow = "TAGHEIGHTRANGELOW";
        public const string TagHeightRangeHigh = "TAGHEIGHTRANGEHIGH";

        public const string StrResolution = "RESOLUTION";
        public const string StrSolutionWidth = "SOLUWIDTH";
        public const string StrSolutionHeight = "SOLUHEIGHT";

        public const string StrPositionMode = "POSITION";
        public const string StrMode = "MODE";

        public const string TagShowOver = "TAGOVER";
        public const string TagShowOverKey = "TAGSHOWOVERKEY";

        public const string ShowRefreshKey = "REFRESHKEY";
        public const string ShowRefreshTime = "REFRESHTIME";

        public const string EnableMulAreaMode = "MULMODE";

        public const String isKalmanKey = "KALMAN";
        public const String MNosieCovar = "MNOSIECOVAR";
        public const String ProNosieCovar = "PRONOSIECOVAR";
        public const String LastStatePre = "LASTSTATEPRE";

        public const String isEnableReferType = "ENREFERTYPE";

        public const string ShowTraceKey = "TRACE";

        public const string TimeOutNoShowKey = "NOSHOWTIMEOUTKEY";
        public const string OutTime = "OVERTIME";

        public const string Name = "NAME";
        public const string Height = "HEIGHT";
        public const string Type = "TYPE";
        public const string Range = "RANGE";

        public const string ActualWidth = "ACTUALWIDTH";
        public const string ActualHeight = "ACTUALHEIGHT";

        public const string BsID_ = "BASESTATIONID_";
        public const string BsX_ = "BASESTATIONX_";
        public const string BsY_ = "BASESTATIONY_";
        public const string BsZ_ = "BASESTATIONZ_";

        public const string BsGroupID_ = "GROUPID_";

        public const string LimitAreaID_ = "LIMITAREAID_";
        public const string LimitAreaName_ = "LIMITAREANAME_";
        public const string LimitAreaStartX_ = "LIMITAREASTARTX_";
        public const string LimitAreaStartY_ = "LIMITAREASTARTY_";
        public const string LimitAreaEndX_ = "LIMITAREAENDX_";
        public const string LimitAreaEndY_ = "LIMITAREAENDY_";


        public static String AreaMapPath = Environment.CurrentDirectory + "\\Map";
        //浏览视图中保存参考点
        public static String SavePortsPath = Environment.CurrentDirectory + "\\SavePorts.ini";
        public static String SaveMulAreaPath = Environment.CurrentDirectory + "\\MulArea.ini";

        public static String SaveLimitsAreaPath = Environment.CurrentDirectory + "\\RestrictedAreas.ini";
        public const String Loca_X = "X";
        public const String Loca_Y = "Y";
        public const String Loca_Z = "Z";
        public static bool Open(String FileStr, bool Create)
        {
            //判断文件是否存在
            try
            {
                if (!File.Exists(FileStr))
                {
                    if (!Create)
                    {
                        return false;
                    }
                    MyFileStream = File.Create(FileStr);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            try
            {
                MyFileStream = new FileStream(FileStr, FileMode.Open, FileAccess.ReadWrite);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
        //关闭文件流
        public static bool Close(String FileStr)
        {
            if (!File.Exists(FileStr)) { //判断文件流是否存在
                return true;
            }
            if (MyFileStream != null) {
                try {
                    MyFileStream.Close();
                } catch {
                } finally {
                    MyFileStream = null;
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取.dxf文件的名称
        /// </summary>
        /// <returns></returns>
        public static String GetFileName(String AbsPath)
        {
            int index = -1;
            if (AbsPath.EndsWith(".dxf") || AbsPath.EndsWith(".dwg")) {
                index = AbsPath.LastIndexOf("\\");
            }
            if (index <= 0) {
                return null;
            }
            String MapName = AbsPath.Substring(index + 1, AbsPath.Length - index - 1);
            return MapName;
        }
        /// <summary>
        /// 查看指定的文件名称是否存在于某一个目录下
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dirpath"></param>
        /// <returns></returns>
        public static bool isFileNameConflict(string filename,string dirpath)
        {
            if (!Directory.Exists(dirpath))
            {
                return false;
            }
            String[] filenames = Directory.GetFiles(dirpath);
            for (int i = 0; i < filenames.Length; i ++)
            {
                int index = filenames[i].LastIndexOf(@"\");
                string str = filenames[i].Substring(index + 1, filenames[i].Length - index - 1);
                if (str.Equals(filename))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 将指定文件复制到指定目录下
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="DirPath"></param>
        /// <returns></returns>
        public static bool CopyFileToDir(string filepath, string DirPath)
        {
            if (!File.Exists(filepath))
            {
                return false;
            }
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }
            string StrMapName = "";
            try
            {
                StrMapName = GetFileName(filepath);
                File.Copy(filepath, DirPath + "//" + StrMapName, true);
            } catch (Exception) {
                return false;
            }
            if (!File.Exists(DirPath + "//" + StrMapName)) {
                return false;
            }
            return true;
        }
        public static bool GetAllSegment(String FileStr, out List<string> lists) {
            lists = new List<string>();
            if (!Open(FileStr, true))
                return false;
            //得到所有分组信息
            FileInfo fileinfo = new FileInfo(FileStr);
            int len = (int)fileinfo.Length + 1;
            byte[] bytes = new byte[len];
            MyFileStream.Read(bytes, 0, len);
            if (!Close(FileStr))
                return false;
            //将字节转化为字符串
            String Str = encoder.GetString(bytes, 0, bytes.Length);
            String _Str;
            //.ini中的字符串
            int start, end;
            start = Str.IndexOf('[', 0, Str.Length);
            //找到‘[’中括号
            while (start >= 0) { //找到‘[’括号
                end = Str.IndexOf(']', start, Str.Length - start);
                _Str = Str.Substring(start + 1, end - start - 1);
                lists.Add(_Str);
                start = Str.IndexOf('[', end, Str.Length - end);
            }
            return true;
        }
        //得到Segment区中的所有Key值
        public static bool GetAllKey(String filePath, String segment, out List<string> lists) {
            lists = new List<string>();
            if (String.IsNullOrEmpty(filePath) || String.IsNullOrEmpty(segment))
            {
                return false;
            }
            if (!File.Exists(filePath))
            { // 文件不存在, 我们重新创建这个文件
                return false;
            }
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            FileInfo fileinfo = new FileInfo(filePath);
            int len = (int)fileinfo.Length + 1;
            byte[] bytes = new byte[len];
            // 读取这个文件
            fileStream.Read(bytes, 0, len);
            // 读完文件讯息, 我们应该关闭
            if (null != fileStream)
            {
                fileStream.Close();
                fileStream = null;
            }
            String Str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            //  得到所有Key
            String _Str = "[" + segment + "]";
            int start;
            //在所有字符串中找到segment键值的字符串
            start = Str.IndexOf(_Str, 0, Str.Length);
            if (start >= 0)
            {//找到了Segment的值
                Str = Str.Substring(start, Str.Length - start);
                start = Str.IndexOf("\r\n");
                if (start >= 0)
                {//找到第一个换行符
                    Str = Str.Substring(start + 2, Str.Length - start - 2);
                    start = Str.IndexOf('[', start, Str.Length - start);
                    if (start >= 0)
                    {
                        Str = Str.Substring(0, start);
                    }
                    //得到所有的键值对
                    start = 0;
                    start = Str.IndexOf('=', 0, Str.Length);
                    while (start >= 0)
                    {
                        _Str = Str.Substring(0, start);
                        lists.Add(_Str);
                        Str = Str.Substring(start + 1, Str.Length - start - 1);
                        start = Str.IndexOf("\r\n");
                        Str = Str.Substring(start + 2, Str.Length - start - 2);
                        start = Str.IndexOf('=', 0, Str.Length);
                    }
                    return true;
                }
            }
            return false;
        }
        public static String GetValue(String FileStr, String segment, String key)
        {//得到键值
            List<string> lists = null;
            String value;
            int index = 0;
            if (GetAllKey(FileStr, segment,out lists))
            {//得到所有的键值
                if (!Open(FileStr, true))
                    return null;
                if (null == lists)
                    return null;

                FileInfo fileinfo = new FileInfo(FileStr);
                int len = (int)fileinfo.Length + 1;

                byte[] bytes = new byte[len];
                MyFileStream.Read(bytes, 0, len);
                if (!Close(FileStr))
                    return null;
                String Str_Seg, Str_Key;
                String Str = encoder.GetString(bytes, 0, bytes.Length);
                for (int i = 0; i < lists.Count; i++)
                {
                    if (key.Equals(lists[i]))
                    {
                        Str_Seg = "[" + segment + "]";
                        Str_Key = key + "=";
                        index = Str.IndexOf(Str_Seg);
                        Str = Str.Substring(index, Str.Length - index);
                        index = Str.IndexOf(Str_Key);
                        Str = Str.Substring(index + Str_Key.Length, Str.Length - index - Str_Key.Length);
                        value = Str.Substring(0, Str.IndexOf("\r\n"));
                        return value;
                    }
                }
            }
            return null;
        }
        public static bool SetValue(String FileStr, String segment, String key, String value)
        {
            List<string> lists = new List<string>();
            if (!Open(FileStr, true))
                return false;
            FileInfo fileinf = new FileInfo(FileStr);
            int len = (int)(fileinf.Length + 1);
            byte[] bytes = new byte[len];
            MyFileStream.Read(bytes, 0, len);
            if (!Close(FileStr))
                return false;
            String Str = encoder.GetString(bytes, 0, bytes.Length);
            String StrFile = Str;
            int i, start, index_Key, index_right;
            if (GetAllKey(FileStr, segment, out lists))
            {
                String _Str = "[" + segment + "]";
                String Str_Key = key + "=";
                for (i = 0; i < lists.Count; i++)
                {
                    if (lists[i] != null)
                    {
                        if (key.Equals(lists[i]) && !lists[i].Equals(""))
                        {//找到键值
                            start = Str.IndexOf(_Str, 0);
                            //找到左端的位置
                            index_Key = Str.IndexOf(Str_Key, start);
                            //找到右端的位置
                            index_right = Str.IndexOf("\r\n", index_Key);
                            StrFile = Str.Substring(0, index_Key + Str_Key.Length)
                                + value + Str.Substring(index_right, Str.Length - index_right);
                            break;
                        }
                    }
                }
                if (i >= lists.Count)
                {//说明在该段中没有这个键值,重新添加键值
                    start = Str.IndexOf(_Str, 0);
                    index_right = Str.IndexOf("[", start + _Str.Length);
                    //找到段位
                    if (index_right < 0)
                    {
                        StrFile = Str.Substring(0, Str.IndexOf('\0')) + key + "=" + value + "\r\n";
                    }
                    else
                    {
                        _Str = key + "=" + value + "\r\n";
                        StrFile = Str.Substring(0, index_right) + _Str + Str.Substring(index_right, Str.Length - index_right);
                    }
                }
            }
            else
            {
                StrFile = Str.Substring(0, Str.IndexOf('\0')) + "[" + segment + "]\r\n" + key + "=" + value + "\r\n";
            }
            if (!Open(FileStr, true))
            {
                return false;
            }
            bytes = encoder.GetBytes(StrFile);
            MyFileStream.Write(bytes, 0, bytes.Length);
            if (!Close(FileStr))
            {
                return false;
            }
            return true;
        }
        //清除文件中的所有项
        public static bool Clear(String FilePath)
        {
            if (!Open(FilePath, true))
                return false;
            MyFileStream.SetLength(0);
            if (!Close(FilePath))
                return false;
            return true;
        }
    }
    //设置的参数
    public class Parameter
    {
        public const string StrOriginalsize = "Original Size";
        public const string StrScreenAdaptive = "Screen Adaptive";
        public static double Px_Dis = -1;
        public static bool NoShow_OverTime_NoRecei = true;
        public static int OverTime1 = 60;
        public static bool AlarmInfor_TabToShowMode = true;
        public static bool ShowPlacePort = true;
        public static bool TagShowOver = true;
        public static bool LongTime_NoExe_ToBlackShow = true;
        public static int OverTime2 = 60;
        public static double RealWidth = 0;
        public static double RealHeight = 0;
        public static bool RecordOverTimeNoReceiInfo = false;
        public static UInt16 OverNoReceiveWarmTime = 100;
        public static bool RecordBatteryLessCard = true;
        public static int LowBattry = 10;
        // 是否警报超时
        public static bool isRegionAlarmRateTime = false;
        // 区域警报频率时间
        public static int regionAlarmRateTime = 15;

        public static string RecordDir = Environment.CurrentDirectory + "\\Record";
        public static bool isClearHistory = true;
        public static int ClearHistoryTime = 30;
        public static bool isShowTrace = false;
        public static bool isKalman = true;
        public static double KalmanMNosieCovar = 0.1;
        public static double KalmanProNosieCovar = 0.2;
        public static double KalmanLastStatePre = 0.5;

        public static bool isDefineInterval = false;
        public static int DefineInterval = 1000;
        public static bool isEnableReferType = false;
        public static bool isEnableLimitArea = true;
        public static int ResolutionWidth = 0;
        public static int ResolutionHeight = 0;
        public static bool isTwoSizeMode = false;
        public static int SelectSizeMode = -1;
        public static PosititionMode positionmode = 0;
        public static bool isSupportMulArea = true;
        //是否采用三个基站定位
        public static bool isUse3Station = false;

        public static bool isUseTagHeightRange = false;
        public static int TagHeightRangeLow = 50;
        public static int TagHeightRangeHigh = 200;
    }
    public class LimitArea
    {
        public byte[] ID = new byte[2];
        public string Name = "";
        public Point startpoint = new Point();
        public Point endpoint = new Point();
    }
    /// <summary>
    /// 地图区域
    /// </summary>
    [Serializable]
    public class Group
    {
        public byte[] id = new byte[2];
        public String name;
        public String grouppath;
        public float actualwidth, actualheight;
        public float scale;//真实的比例
        public Dictionary<string, BsInfo> groupbss = new Dictionary<string, BsInfo>();
        public Dictionary<string, LimitArea> grouplimiares = new Dictionary<string, LimitArea>();
    }
    [Serializable]
    public class ReportRouterInfor
    { 
        public byte[] id = new byte[2];
        public double dis = -1;
        public int SigQuality = -1;
        public double ResidualValue = -1;
    }
    [Serializable]
    public class CardImg
    {
        public byte[] ID = new byte[2];   //卡片的ID
        public byte[] GroupID = new byte[2];//组别的ID
        public string Name = "";
        public List<ReportRouterInfor> ReportRouters = new List<ReportRouterInfor>();
        public PrecisePositionLibrary.Point CardPoint = new PrecisePositionLibrary.Point();//卡片的位置
        public PrecisePositionLibrary.Point KalmanPoint = new PrecisePositionLibrary.Point();//卡尔曼优化的位置
        public PrecisePositionLibrary.Point CardPoint1, CardPoint2, CardPoint3, CardPoint4, CardPoint5;

        public short GsensorX, GsensorY, GsensorZ;
        public byte Battery; //卡片的电量
        public ushort St;
        public bool isLowBattery = false;
        public bool isLowBatteryWarm = false;
        public bool isTimeOutReceiveWarm = false;
        public bool LowBatteryShow = false;
        public bool isShow;
        public int No_Exe_Time;
        public bool isOverNoMove = false;
        public Kalman curkalmanX = null;
        public Kalman curkalmanY = null;
        public Kalman curkalmanZ = null;
        public byte LocaType = 255;
        public DateTime ReceiTime = new DateTime();
        public int LossPack;
        public int TotalPack;
        public int InterverTime = 0;
        public int Index;//序列号
        public bool isShowTag = true;
        public bool isShowBlack = false;
        public bool isShowImg = true;
        public PortType curpostype = PortType.ThreeMode;
        public float BaseX, BaseY;
        public AreaType curareatype = AreaType.CommonArea;
        public string curstrid = "";
        public bool isShowRed = false;
        public int ShowRedTick = 0;
     }
    //用于在列表中给基站排序
    class SortPort
    { 
        public byte[] ID = new byte[2];
        public string Name = "";
        public double Distanse = -1;
        public int SigQuality = -1;
        public bool isOptimal;//是否是最优基站

    }
    class PosPortinfor
    { 
        public byte[] ID = new byte[2];
        public double distanse;
        public PortType porttype;
        public double rangeR;
    }
    public enum PortType
    { 
        SingleMode,
        ThreeMode
    }
    public enum AreaType
    { 
        LimitArea,
        CommonArea
    }
}
