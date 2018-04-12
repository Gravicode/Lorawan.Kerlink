using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using System.Text;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;

namespace Lora.Kerlink
{
    public partial class Program
    {
        SerialPort UART = null;

        void PrintToLcd(string Message)
        {
            characterDisplay.Clear();
            characterDisplay.Print(Message);
        }
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            characterDisplay.BacklightEnabled = true;
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            UART = new SerialPort(GHI.Pins.FEZSpiderII.Socket11.SerialPortName, 57600);
            UART.ReadTimeout = 0;
            Debug.Print("57600");
            Debug.Print("RN2483 Test");
            PrintToLcd("RN2483 Test");
            OutputPort reset = new OutputPort(GHI.Pins.FEZSpiderII.Socket11.Pin6, false);
            OutputPort reset2 = new OutputPort(GHI.Pins.FEZSpiderII.Socket11.Pin3, false);

            reset.Write(true);
            reset2.Write(true);

            Thread.Sleep(50);
            reset.Write(false);
            reset2.Write(false);

            Thread.Sleep(50);
            reset.Write(true);
            reset2.Write(true);

            Thread.Sleep(50);

            waitForResponse();

            sendCmd("sys factoryRESET");
            sendCmd("sys get hweui");
            sendCmd("mac get deveui");

            // For TTN
            sendCmd("mac set devaddr AABBCCDD");  // Set own address
            sendCmd("mac set appskey 2B7E151628AED2A6ABF7158809CF4F3C");
            sendCmd("mac set nwkskey 2B7E151628AED2A6ABF7158809CF4F3C");
            sendCmd("mac set adr off");
            sendCmd("mac set rx2 3 868400000");//869525000
            sendCmd("mac join abp");
            sendCmd("mac get status");
            sendCmd("mac get devaddr");
            Thread.Sleep(1000);
            Thread th1 = new Thread(new ThreadStart(Loop));
            th1.Start();
        }

        void Loop()
        {
            characterDisplay.Clear();
            characterDisplay.Print("Ready");
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
                Debug.Print("kirim :" +jsonStr);
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
                        characterDisplay.Clear();
                        characterDisplay.Print(hasil);

                    }
                }
                Thread.Sleep(5000);
            }
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
                    Debug.Print("count:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read:" + hasil);
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
                    Debug.Print("count:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read:" + hasil);
                }

            }
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
                    Debug.Print("count:" + count);
                    var hasil = new string(System.Text.Encoding.UTF8.GetChars(rx_data));
                    Debug.Print("read:" + hasil);
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
