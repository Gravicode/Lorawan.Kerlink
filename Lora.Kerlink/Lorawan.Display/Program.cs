using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
//using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Glide.UI;
using GHI.Glide;
using GHI.Glide.Display;
using System.Text;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;

namespace Lorawan.Display
{
    public partial class Program
    {
        void ProgramStarted()
        {
            window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.Form1));

            GlideTouch.Initialize();

            GHI.Glide.UI.Button btn = (GHI.Glide.UI.Button)window.GetChildByName("btn");
            btn.TapEvent += OnTap;

            txtGas = (TextBlock)window.GetChildByName("txtGas");
            txtHumid = (TextBlock)window.GetChildByName("txtHumid");
            txtLight = (TextBlock)window.GetChildByName("txtLight");
            txtTemp = (TextBlock)window.GetChildByName("txtTemp");
            txtStatus = (TextBlock)window.GetChildByName("txtStatus");
            txtMessage = (TextBlock)window.GetChildByName("txtMessage");

            txtGas.TextAlign = HorizontalAlignment.Left;
            txtHumid.TextAlign = HorizontalAlignment.Left;
            txtLight.TextAlign = HorizontalAlignment.Left;
            txtTemp.TextAlign = HorizontalAlignment.Left;
            txtStatus.TextAlign = HorizontalAlignment.Left;
            txtMessage.TextAlign = HorizontalAlignment.Left;


            Glide.MainWindow = window;

            UART = new SimpleSerial(GHI.Pins.FEZRaptor.Socket1.SerialPortName, 57600);
            UART.ReadTimeout = 0;
            UART.DataReceived += UART_DataReceived;
            Debug.Print("57600");
            Debug.Print("RN2483 Test");
            PrintToLcd("RN2483 Test");
            OutputPort reset = new OutputPort(GHI.Pins.FEZRaptor.Socket1.Pin6, false);
            OutputPort reset2 = new OutputPort(GHI.Pins.FEZRaptor.Socket1.Pin3, false);

            reset.Write(true);
            reset2.Write(true);

            Thread.Sleep(100);
            reset.Write(false);
            reset2.Write(false);

            Thread.Sleep(100);
            reset.Write(true);
            reset2.Write(true);

            Thread.Sleep(100);

            waitForResponse();

            sendCmd("sys factoryRESET");
            sendCmd("sys get hweui");
            sendCmd("mac get deveui");

            // For TTN
            sendCmd("mac set devaddr AAABBBEE");  // Set own address
            Thread.Sleep(1000);
            sendCmd("mac set appskey 2B7E151628AED2A6ABF7158809CF4F3D");
            Thread.Sleep(1000);

            sendCmd("mac set nwkskey 2B7E151628AED2A6ABF7158809CF4F3D");
            Thread.Sleep(1000);

            sendCmd("mac set adr off");
            Thread.Sleep(1000);

            sendCmd("mac set rx2 3 868400000");//869525000
            Thread.Sleep(1000);

            sendCmd("mac join abp");
            sendCmd("mac get status");
            sendCmd("mac get devaddr");
            Thread.Sleep(1000);
          
            Thread th1 = new Thread(new ThreadStart(Loop));
            th1.Start();

        }
        private static string[] _dataInLora;
        private static string rx;


        void UART_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
           
            _dataInLora = UART.Deserialize();
            for (int index = 0; index < _dataInLora.Length; index++)
            {
                rx = _dataInLora[index];
                //if error
                if (_dataInLora[index].Length > 5)
                {
                   
                    //if receive data
                    if (rx.Substring(0, 6) == "mac_rx")
                    {
                        string hex = _dataInLora[index].Substring(10);
                        
                        //update display
                        txtMessage.Text = hex;//Unpack(hex);
                        txtMessage.Invalidate();
                        window.Invalidate();
                    }
                }
            }
            Debug.Print(rx);
        }

        Window window;
        TextBlock txtTemp;
        TextBlock txtGas;
        TextBlock txtHumid;
        TextBlock txtLight;
        TextBlock txtStatus;
        TextBlock txtMessage;



        private static void OnTap(object sender)
        {
            Debug.Print("Button tapped.");
        }
        void Loop()
        {
            int counter = 0;
            while (true)
            {
                counter++;
                var data = new SensorData()
                {
                    Temp = tempHumidSI70.TakeMeasurement().Temperature,
                    Humid = tempHumidSI70.TakeMeasurement().RelativeHumidity,
                    Light = lightSense.GetIlluminance()
                };
                var jsonStr = Json.NETMF.JsonSerializer.SerializeObject(data);
                Debug.Print("kirim :" + jsonStr);
                PrintToLcd("send count: " + counter);
                sendData(jsonStr);
                Thread.Sleep(5000);
                byte[] rx_data = new byte[20];

                if (UART.CanRead)
                {
                    var count = UART.Read(rx_data, 0, rx_data.Length);
                    if (count > 0)
                    {
                        Debug.Print("count:" + count);
                        var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                        Debug.Print("read:" + hasil);
                        txtStatus.Text = hasil;
                        txtStatus.Invalidate();
                        //mac_rx 2 AABBCC
                    }
                }
                var gas = gasSense.ReadProportion();
                var light = lightSense.GetIlluminance();
                var temp = tempHumidSI70.TakeMeasurement().Temperature;
                var humid = tempHumidSI70.TakeMeasurement().RelativeHumidity;
                displayT35.BacklightEnabled = true;
                var font = Resources.GetFont(Resources.FontResources.NinaB);
                txtTemp.Text = "Temp: " + System.Math.Round(temp) + "C";
                txtGas.Text = "Gas: " + System.Math.Round(gas) + "%";
                txtHumid.Text = "Humid: " + System.Math.Round(humid) + "%";
                txtLight.Text = "Light: " + System.Math.Round(light) + "Lux";
                txtTemp.Invalidate();
                txtGas.Invalidate();
                txtHumid.Invalidate();
                txtLight.Invalidate();
                window.Invalidate();

                Thread.Sleep(2000);
            }

        }
        SimpleSerial UART = null;

        void PrintToLcd(string Message)
        {
            txtStatus.Text = Message;
            txtStatus.Invalidate();
            window.Invalidate();
        }

      

        void sendCmd(string cmd)
        {
            byte[] rx_data = new byte[20];
            Debug.Print(cmd);
            Debug.Print("\n");
            // flush all data
            UART.Flush();
            // send some data
            var tx_data = Encoding.UTF8.GetBytes(cmd);
            UART.Write(tx_data, 0, tx_data.Length);
            tx_data = Encoding.UTF8.GetBytes("\r\n");
            UART.Write(tx_data, 0, tx_data.Length);
            Thread.Sleep(100);
            while (!UART.IsOpen)
            {
                UART.Open();
                Thread.Sleep(100);
            }
            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count cmd:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read cmd:" + hasil);
                }
            }
        }

        void waitForResponse()
        {
            byte[] rx_data = new byte[20];

            while (!UART.IsOpen)
            {
                UART.Open();
                Thread.Sleep(100);
            }
            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count res:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read res:" + hasil);
                }

            }
        }
        public static string Unpack(string input)
        {
            byte[] b = new byte[input.Length / 2];

            for (int i = 0; i < input.Length; i += 2)
            {
                b[i / 2] = (byte)((FromHex(input[i]) << 4) | FromHex(input[i + 1]));
            }
            return new string(Encoding.UTF8.GetChars(b));
        }
         public static int FromHex(char digit)
         {
             if ('0' <= digit && digit <= '9')
             {
                 return (int)(digit - '0');
             }

             if ('a' <= digit && digit <= 'f')
                 return (int)(digit - 'a' + 10);

             if ('A' <= digit && digit <= 'F')
                 return (int)(digit - 'A' + 10);

             throw new ArgumentException("digit");
         }

        char getHexHi(char ch)
        {
            int nibbleInt = ch >> 4;
            char nibble = (char)nibbleInt;
            int res = (nibble > 9) ? nibble + 'A' - 10 : nibble + '0';
            return (char)res;
        }
        char getHexLo(char ch)
        {
            int nibbleInt = ch & 0x0f;
            char nibble = (char)nibbleInt;
            int res = (nibble > 9) ? nibble + 'A' - 10 : nibble + '0';
            return (char)res;
        }

        void sendData(string msg)
        {
            byte[] rx_data = new byte[20];
            char[] data = msg.ToCharArray();
            Debug.Print("mac tx uncnf 1 ");
            var tx_data = Encoding.UTF8.GetBytes("mac tx uncnf 1 ");
            UART.Write(tx_data, 0, tx_data.Length);

            // Write data as hex characters
            foreach (char ptr in data)
            {
                tx_data = Encoding.UTF8.GetBytes(new string(new char[] { getHexHi(ptr) }));
                UART.Write(tx_data, 0, tx_data.Length);
                tx_data = Encoding.UTF8.GetBytes(new string(new char[] { getHexLo(ptr) }));
                UART.Write(tx_data, 0, tx_data.Length);


                Debug.Print(new string(new char[] { getHexHi(ptr) }));
                Debug.Print(new string(new char[] { getHexLo(ptr) }));
            }
            tx_data = Encoding.UTF8.GetBytes("\r\n");
            UART.Write(tx_data, 0, tx_data.Length);
            Debug.Print("\n");
            Thread.Sleep(5000);

            if (UART.CanRead)
            {
                var count = UART.Read(rx_data, 0, rx_data.Length);
                if (count > 0)
                {
                    Debug.Print("count after:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read after:" + hasil);
                }
            }
        }
    }

    public class SensorData
    {
        public double Humid { get; set; }
        public double Light { get; set; }
        public double Temp { get; set; }
    }
}
