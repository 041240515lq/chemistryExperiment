using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.IO.Ports;
using System.IO;

namespace chemistryExp
{
    public partial class Form1 : Form
    {

        //用于测PR650的串口
        SerialPort sp2 = new SerialPort();

        //定义委托
        private delegate void UpdateTextEventHandler(string text);
         
        //定义事件,展示亮度
        private event UpdateTextEventHandler textChanged;

        //定义事件,展示电压
        private event UpdateTextEventHandler textVoltChanged;
        int time; //用于测试电压和亮度的时间间隔
        int[] dataBuffer = new int[5120];
        int[] YavParam = new int[4];
        int[] CNTBuffer = new int[3];
        int[] IOBuffer = new int[3];
        double val1, val2, val3,val4, val5, val6, val7, val8; 
        List<float> list1 = new List<float>();
        String I_minIntensity = "0";
        String I_maxIntensity;
        List<float> list2 = new List<float>(); 
        float n = 0;
        String foldPath;//文件所在的父目录
        String fileName;
        DateTime start; // 记录点击开始时的时间
        DateTime end_cancel; // 记录点击取消时的时间
        DateTime specEnd; //记录点击测光谱时的时间
        List<String[]> spectralData;
        List<String> illuminaceList;  //用于存储亮度的链表
        List<String> voltList;  //用于存储电压的链表
        List<String> timespanList;
        private void Form1_Load(object sender, EventArgs e)
        {
            sp2.PortName = "COM4"; //PC机右侧，上方的一个串口
            sp2.BaudRate = 9600;
            sp2.DataBits = 8;
            sp2.StopBits = StopBits.One;
            sp2.Parity = Parity.None; 
            sp2.RtsEnable = true;
            sp2.DtrEnable = true;
            sp2.ReadTimeout = 1000;
            sp2.WriteTimeout = 1000;
            sp2.ReceivedBytesThreshold = 1;
          
            sp2.DataReceived += new SerialDataReceivedEventHandler(sp2_DataReceived_pr650);
            
            timer1.Enabled = false;
            timer2.Enabled = false;
            timer3.Enabled = false;  //用于控制timer1和timer2的停止
            Control.CheckForIllegalCrossThreadCalls = false;    //这个类中我们不检查跨线程的调用是否合法(因为.net 2.0以后加强了安全机制,，不允许在winform中直接跨线程访问控件的属性)
            textChanged += new UpdateTextEventHandler(ChangeText);
            textVoltChanged += new UpdateTextEventHandler(ChangeVoltText);
        }

      
       

        public Form1()
        {
            InitializeComponent();
            button2.Enabled = true;
            button3.Enabled = false;
        }

        //事件处理方法，显示亮度
        private void ChangeText(string text)
        {
            label19.Text = text; 
        }

        //事件处理方法,显示电压
        private void ChangeVoltText(string text)
        {
            label21.Text = text; 
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //选择文件的保存路径
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径"; 
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                  foldPath = dialog.SelectedPath;
                //MessageBox.Show("已选择文件夹:" + foldPath, "选择文件夹提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                label1.Text = foldPath; 
            }
        }

        //点击开始按钮发生的事件
        private void button2_Click(object sender, EventArgs e)
        {
            //每次点击开始测试时，都将flag的值置为false
            flag = false; //表示先采集亮度的数据
            
            //获取最大电流密度
            int  I_intensity = Convert.ToInt32(textBox6.Text);
            int  len = Convert.ToInt32(textBox4.Text);
            int  width = Convert.ToInt32(textBox5.Text);
            double d = (I_intensity / (len * width)) * 1000;
            I_maxIntensity = d.ToString("G");
            label14.Text = I_maxIntensity;

            //设置计时器的时间间隔
            time = int.Parse(textBox1.Text) * 1000;
            timer1.Interval = time;  //用于测试亮度的计时器
            timer2.Interval = time;  //用于测试电压的计时器
            String timer3_interval = textBox7.Text;
            timer3.Interval = int.Parse(timer3_interval)*60000; //分钟转为毫秒
            
            start = DateTime.Now;
            illuminaceList = new List<string>();
            voltList = new List<string>();
            timespanList = new List<string>();
            button2.Enabled = false;
            button3.Enabled = true;
            if (!sp2.IsOpen)
            { 
                sp2.Open();
                sp2.Close();
                Thread.Sleep(1000);
                sp2.Open();
                String command = "d\n";
                sp2.WriteLine(command);
                String command0 = "S,,,,,,,1\n";
                sp2.WriteLine(command0);
            }
            else
            { 
                sp2.Close();
                Thread.Sleep(1000);
                sp2.Open();
                String command = "d\n";
                sp2.WriteLine(command);
                String command0 = "S,,,,,,,1\n";
                sp2.WriteLine(command0);
            }

           

            int num = DllClass.FindDevice();
            Console.WriteLine("设备数： " + num); 
            if (num > 0)
            {
                int openFlag = DllClass.OpenYavDevice(0); 
                if (openFlag == 1)
                {  
                    Byte[] param = { 0x07, 0, 0, 0 };
                    //设置设备 0 的采样率为 1K
                    int result = DllClass.SetYavParam(0, 0xFA, ref param[0]);
                    //只有配置正确后，才能进行测试电压和亮度
                    timer1.Enabled = true;
                    timer2.Enabled = true;
                    timer3.Enabled = true;

                }
                else
                {
                    MessageBox.Show("usb设备未打开");
                }
            }
            else
            {
                MessageBox.Show("没有发现usb设备");
            }
           
        }

        private void stopExp()
        {
            end_cancel = DateTime.Now;
            button2.Enabled = true;
            button3.Enabled = false;
            timer1.Enabled = false;
            timer2.Enabled = false;
            timer3.Enabled = false;  //到达规定的时间后，计时器停止。
            sp2.Close();
            int cyd = DllClass.CloseYavDevice(0);
            Console.WriteLine("cyd " + cyd);
            if (cyd != 1)  //1代表设备正常关闭，来源：厂商技术人员说的。
            {
                MessageBox.Show("采集卡设备未正常关闭！");
            } 
            saveSpectralAndVoltData();
        }

        //停止采集数据，让测试电压和亮度的timer都停下来。并且关闭采集卡设备和串口。存储采集到的数据。
        private void button3_Click(object sender, EventArgs e)
        {
            stopExp(); 
           // Console.WriteLine("list1的长度 " + list1.Count + "   list2的长度 " + list2.Count);
        }
         
        //timer1的函数，用于测量亮度。
        private void timer1_Tick(object sender, EventArgs e)
        {
            
                 Console.WriteLine("d1 "+ DateTime.Now);
                Console.WriteLine("timer1开始运行");
                String command1 = "m1\n";
                sp2.WriteLine(command1);
            
           
          
            //if (result.Length == 0)
            //{
            //    Console.WriteLine("接收函数未响应");
            //    label19.Text = "NO";
            //    illuminaceList.Add("NO");
                
            //}
            //result = "";
        }

        //timer2的函数，用于得到电压
        private void timer2_Tick(object sender, EventArgs e)
        {
            
                DateTime time_record = DateTime.Now;  //记录每次采集数据时的时间，目的在于形成时间戳。
                TimeSpan timespan = time_record - start;
                timespanList.Add((timespan.TotalSeconds).ToString("G")); 
                int deviceTempID = DllClass.GetYavData(0, ref dataBuffer[0], 512, ref YavParam[0], ref CNTBuffer[0], ref IOBuffer[0]);
                Console.WriteLine("dataBuffer[1]: " + dataBuffer[1]);
                // val1 = (dataBuffer[1] * 30.0 / 4095) * 1.011 + 0.01;  //最小二乘函数
                val1 = (dataBuffer[1] * 30.0 / 4095) * 1.011 + 0.002;  //最小二乘函数
                voltList.Add(val1.ToString());
                this.Invoke(textVoltChanged, new string[] { val1 + "" });
                Console.WriteLine("val1: " + val1);
           
           
        }

        //用于终止测量
        private void timer3_Tick(object sender, EventArgs e)
        {
            stopExp(); 
        }
         
        String rec = "";
        String tmp = "";

        
        //发送测光谱的命令
        private void button4_Click(object sender, EventArgs e)
        {

           
                timer1.Enabled = false;
                timer2.Enabled = false;
                if (!sp2.IsOpen)
                {
                    sp2.Open();
                }
                specEnd = System.DateTime.Now;
                spectralData = new List<string[]>();
                i = 0;  //每次测光谱时，都初始化i的值。
                        //String command1 = "D120\n";
                        //sp2.WriteLine(command1); 
                String command2 = "m5\n";  //测波长和亮度 
            sp2.WriteLine(command2);
            
        }

        String result = ""; 
        int i = 0;
        Boolean flag = false; //设立标志是为了在接收串口返回的数据时，判断是发的测亮度的命令还是测光谱的命令。
        private void sp2_DataReceived_pr650(object sender, SerialDataReceivedEventArgs e)
        {
            if (!sp2.IsOpen)
            {
                sp2.Open();
            }
            try
            {  
                rec += sp2.ReadExisting();
                //去掉进入远程模式后的返回值
                if (rec.Length == 9 && rec.Equals("000\r\n00\r\n"))
                {
                    rec = rec.Substring(9);
                    Console.WriteLine(rec.Length);
                }
                //根据指令格式的差异，判断要处理的是哪个命令的返回值 
                //以下是测光谱的接受代码块  
                //去掉发送测试指令后，去掉非目标数据
                while (rec.Length >= 17 && rec.Substring(0, 5).Equals("00,0\r"))
                {
                    Console.WriteLine("=======要测光谱了==============================================================");
                    String t = rec.Substring(0, 16);
                    Console.Write("开头 " + t);
                    rec = rec.Substring(17);
                    Console.WriteLine("rec的长度:" + rec.Length);
                    Console.WriteLine("rec的值:" + rec + "===============");
                    flag = true;
                }

                if (flag)
                {
                    //开始处理目标数据,得到光谱数据
                    while (rec.Length >= 17)
                    {
                        Console.WriteLine("rec准备被处理之前的长度 " + rec.Length);
                        String str = rec.Substring(0, 15);
                        Console.Write("数据 " + str);
                        //用一个数组存储我想要的数据
                        String[] data = new string[2];
                        data[0] = str.Substring(0, 4);
                        Console.WriteLine("data[0]的值: " + data[0]);
                        data[1] = str.Substring(6, 9);
                        Console.WriteLine("data[1]的值: " + data[1]);
                        spectralData.Add(data);
                        Console.WriteLine("rec处理之前的值 " + rec + " rec取子串之前的长度 " + rec.Length);
                        rec = rec.Substring(17);
                        Console.WriteLine("rec处理之后的值 " + rec + " rec准备被处理之后的长度 " + rec.Length);
                        ++i;
                        if (i == 101)
                        {
                            flag = false; 
                            saveSpectralData();
                            Console.WriteLine("========================已经接受完毕了，准备开始电压和亮度的再次测试=================");
                            rec = "";
                            //光谱测量完后，让测电压和测亮度的函数继续运行，在子线程中更新主线程中的timer值
                             this.Invoke(new Action(()=> {
                                 timer2.Enabled = true;
                                 timer1.Enabled = true;
                             }));
                           
                            Console.WriteLine("========================两个timer已开启=================");
                        }
                    }
                }
                else
                {
                    // 以下这段是测亮度的接收代码块 
                    while (rec.Length >= 28)
                    { 
                        tmp = rec.Substring(0, 28);
                        Console.WriteLine("tmp " + tmp);
                        result = tmp.Substring(5, 9);
                        Console.WriteLine("result " + result);
                        illuminaceList.Add(result);
                        this.Invoke(textChanged, new string[] { result });
                      
                        rec = rec.Substring(28);
                        Console.WriteLine("rec.Length " + rec.Length);
                    }
                    
                } 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        //存储光谱数据
        private void saveSpectralData()
        {
            TimeSpan inter = specEnd - start;
            fileName = "\\" + specEnd.ToString("yyyyMMddHHmmssfff") + "_" + Convert.ToInt32(inter.TotalMilliseconds); //这儿必须要有\\ 目的是转义
            String filePath = foldPath + "" + fileName + ".txt";  //@的意思是禁止将字符串中的斜杠解释为转义字符 
            if (!File.Exists(filePath))
            {
                FileStream fs = new FileStream(filePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                foreach (String[] rs in spectralData)
                {
                    sw.Write(rs[0] + "\t" + rs[1] +"\t"+ val1 + "\r\n");
                }
                sw.Flush();
                sw.Close();
                fs.Close();
            }
            else
            {
                MessageBox.Show("文件已存在");
            }
           
        }

        //存储电压和亮度数据
        private void saveSpectralAndVoltData()
        {
             TimeSpan diff = end_cancel - start ; 
             fileName = "\\"+ end_cancel.ToString("yyyyMMddHHmmssfff") +"_"+ Convert.ToInt32(diff.TotalMilliseconds); //这儿必须要有\\ 目的是转义
            String filePath = foldPath+""+ fileName+".txt";  //@的意思是禁止将字符串中的斜杠解释为转义字符 
             if (!File.Exists(filePath))
            {
                FileStream fs = new FileStream(filePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                Console.WriteLine("illuminaceList的长度 "+ illuminaceList.Count+ "  voltList的长度 "+ voltList.Count);
                for(int i = 0; i < illuminaceList.Count; i++)
                {
                    //存储亮度，电压 等等 根据需求存储什么值
                    sw.Write(timespanList.ElementAt(i) +"\t"+illuminaceList.ElementAt(i) + "\t" +
                        voltList.ElementAt(i)+"\t" + I_maxIntensity + "\t"+ I_minIntensity+"\r\n");
                } 
                sw.Flush();
                sw.Close();
                fs.Close();
            }
            else
            {
                MessageBox.Show("文件已存在,未存储成功！");
            }
            rec = ""; //处理完数据后，将rec重置为空字符串。
        }
    }
}
