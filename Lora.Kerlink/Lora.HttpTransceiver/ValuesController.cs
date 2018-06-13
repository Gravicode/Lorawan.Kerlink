using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Lora.HttpTransceiver
{
    public class ValuesController : ApiController
    {
        // GET api/values 
        public List<Model.Transmitter.TransmitModel> Get()
        {
            var newResp = new List<Model.Transmitter.TransmitModel>();
            var newNode = new Model.Transmitter.TransmitModel();
            newNode.tx = new Model.Transmitter.Tx();
            newNode.tx.moteeui = "AAABBBEE";
            newNode.tx.trycount = 5;
            newNode.tx.txmsgid = "000000000001";
            newNode.tx.txsynch = false;
            newNode.tx.userdata = new Model.Transmitter.Userdata();
            newNode.tx.userdata.payload = "Njg2NTZjNmM2ZjIwNjM2ZjZkNzA3NTc0NjU3Mg==";
            newNode.tx.userdata.port = 2;
            newResp.Add(newNode);
            return newResp;
            
        }

        // GET api/values/5 
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values 
        public List<Model.Transmitter.TransmitModel> Post([FromBody]List<Model.Receiver.ReceiveModel> value)
        {
            foreach (var item in value)
            {
                Console.WriteLine("from kerlink:" + item.rx.userdata.seqno + ":" + item.rx.userdata.payload);

            }
            var newResp = new List<Model.Transmitter.TransmitModel>();
            var newNode = new Model.Transmitter.TransmitModel();
            newNode.tx = new Model.Transmitter.Tx();
            newNode.tx.moteeui = "AAABBBEE";
            newNode.tx.trycount = 5;
            newNode.tx.txmsgid = "000000000001";
            newNode.tx.txsynch = false;
            newNode.tx.userdata = new Model.Transmitter.Userdata();
            newNode.tx.userdata.payload = "Njg2NTZjNmM2ZjIwNjM2ZjZkNzA3NTc0NjU3Mg==";
            newNode.tx.userdata.port = 2;
            newResp.Add(newNode);
            return newResp;
            
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5 
        public void Delete(int id)
        {
        }
    } 

}

namespace Model.Transmitter
{
    public class Userdata
    {
        public int port { get; set; }
        public string payload { get; set; }
    }

    public class Tx
    {
        public string moteeui { get; set; }
        public string txmsgid { get; set; }
        public int trycount { get; set; }
        public bool txsynch { get; set; }
        public Userdata userdata { get; set; }
    }

    public class TransmitModel
    {
        public Tx tx { get; set; }
    }
}

namespace Model.Receiver
{
    public class TxInd
    {
        public string txmsgid { get; set; }
        public string status { get; set; }
    }

    public class Motetx
    {
        public int freq { get; set; }
        public string modu { get; set; }
        public string datr { get; set; }
        public string codr { get; set; }
    }

    public class Userdata
    {
        public int seqno { get; set; }
        public int port { get; set; }
        public string payload { get; set; }
        public Motetx motetx { get; set; }
    }

    public class Gwrx
    {
        public string eui { get; set; }
        public string time { get; set; }
        public int chan { get; set; }
        public int rfch { get; set; }
        public int rssi { get; set; }
        public string lsnr { get; set; }
    }

    public class Rx
    {
        public string moteeui { get; set; }
        public Userdata userdata { get; set; }
        public List<Gwrx> gwrx { get; set; }
    }

    public class ReceiveModel
    {
        public TxInd tx_ind { get; set; }
        public Rx rx { get; set; }
    }
}
