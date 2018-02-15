using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.SerialCOM
{
    public class SerialCOM
    {
        
            static SerialPort comm = new SerialPort();
            public static StringBuilder builder = new StringBuilder();
            public static String result = "";

            public static void SerialPortInit(String PortName, int BaudRate)
            {
                comm.PortName = PortName;
                comm.BaudRate = BaudRate;
                comm.Parity = System.IO.Ports.Parity.None;
                comm.DataBits = 8;
                comm.StopBits = System.IO.Ports.StopBits.One;
                comm.RtsEnable = true;
                comm.NewLine = "\r\n";
                comm.DataReceived += new SerialDataReceivedEventHandler(comm_DataReceived);
                SerialPortOpen();
                SendData("status");

            }

            public static void SerialPortOpen()
            {
                if (comm.IsOpen)
                    return;
                else
                    comm.Open();
            }

            public static void SendData(String data)
            {
                String command = data + comm.NewLine;
                if (comm.IsOpen)
                    comm.Write(command);
            }

            private static void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                int n = comm.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据

                comm.Read(buf, 0, n);//读取缓冲数据
                builder.Remove(0, builder.Length);//清除字符串构造器的内容

                //直接按ASCII规则转换成字符串
                builder.Append(Encoding.ASCII.GetString(buf));
                result = builder.ToString();
                Console.WriteLine(builder.ToString());
            }
        }
    }
