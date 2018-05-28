using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lorawan.SendFTP
{
    class Program
    {
        const string IPKerlinkGateway ="192.168.100.113";
        static void Main(string[] args)
        {
            Console.WriteLine("sending data to kerlink gateway...");
            Thread th1 = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    var datas = new List<DataCommand>();
                    datas.Add(new DataCommand() { mote = "AAABBBEE", payload = "01234567", port = 2, trycount = 5, txmsgid = "" });
                    SendFTPToKerlink(datas);
                    Thread.Sleep(5000);
                }
            }
            ));
            th1.Start();
            Console.ReadLine();
        }

        static void SendFTPToKerlink( List<DataCommand> datas,string FileName="data.json"){
            // Get the object used to communicate with the server.  
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(string.Format("ftp://"+IPKerlinkGateway+"/tx_data/{0}",FileName));
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // This example assumes the FTP site uses anonymous logon.  
            request.Credentials = new NetworkCredential("admin", "spnpwd");
            //request.Credentials = new NetworkCredential("anonymous", "janeDoe@contoso.com");
            // Copy the contents of the file to the request stream.  
            byte[] fileContents;
            var json = JsonConvert.SerializeObject(datas);
            fileContents = Encoding.UTF8.GetBytes(json);

            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);
            }
        }
    }

    public class DataCommand
    {
        public string mote { set; get; }
        public string payload { set; get; }
        public int port { set; get; }
        public int trycount { set; get; }
        public string txmsgid { set; get; }
    }
}
