using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Http.Headers;
using System.Threading;
using System.Runtime.Serialization;

namespace AuthenticationTest
{
    partial class WebCommunicator : IDisposable
    {
        /**
            This class is by Nate Fitzgerald. It is intended as a generic web communiaction class for use in any application. Once should be run on each machine, one starting in listen mode by calling 
            the constructor with a port and no address and the other with the address and port in the constructor. Once the connection is negotiated, any serializable object can be passed
            into Send() including strings
        **/

        private HttpListener listener;
        private HttpClient client;
        private RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
        private RandomNumberGenerator rng = new RNGCryptoServiceProvider();
        private BinaryFormatter binForm = new BinaryFormatter();
        private RSAParameters pubkey;
        private RSAParameters privkey;
        private int port = 0;
        private Task listenTask;
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private string baseURL;

        public STATE state { get; private set; } = STATE.INITIALIZING;

        public WebCommunicator(int port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(@"http://*:8112/");
            this.baseURL = @"http://*";
            listener.Start();
            state = STATE.WAITING;
            HandshakeReceived += new ReceiveData(HandleIncomingHandshake);
            Task contextTask = Task.Run(() =>
            {
                var context = listener.GetContextAsync().Result;
                HandshakeReceived(context);
            });

        }

        private delegate void ReceiveData(HttpListenerContext context);
        private event ReceiveData HandshakeReceived;
        private event ReceiveData ReceiveObject;
        private void HandleIncomingHandshake(HttpListenerContext context)
        {
            port = Int32.Parse(context.Request.Headers["port"]);
            
            Stream s = context.Request.InputStream;
            try
            {
                pubkey = (RSAParameters)binForm.Deserialize(s);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            ReceiveObject += new ReceiveData(Receive);
            listener = new HttpListener();
            string addr = baseURL + ":" + port + "/";
            listener.Prefixes.Add(addr);
            Listen(listener);
        }

        private void Listen(HttpListener listener)
        {
            listener.Start();
            state = STATE.OPEN;
            while (!cancel.IsCancellationRequested)
            {
                var newContext = listener.GetContextAsync().Result;
                ReceiveObject(newContext);
            }
        }

        private List<object> received = new List<object>();
        private void Receive(HttpListenerContext context)
        {
            RSA.ImportParameters(pubkey);
            byte[] buf = new byte[1024];
            int index = 0;
            int tempbyte = 0;
            while ((tempbyte = context.Request.InputStream.ReadByte()) != -1) buf[index++] = (byte)tempbyte;
            buf = RSA.Decrypt(buf, false);
            Stream s = new MemoryStream(buf);
            received.Add(binForm.Deserialize(s));
        }
        

        private void SelectPort()
        {
            byte[] p = new byte[2];
            rng.GetBytes(p);
            port = BitConverter.ToInt16(p, 0);
            if (port < 1000) port += 1000;
        }

        private void Initialize()
        {

        }
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                this.state = STATE.DISPOSING;
                if(listener != null) listener.Close();
                if( client != null)  client.Dispose();
                disposedValue = true;
                this.state = STATE.DISPOSED;
            }
        }
        
        ~WebCommunicator()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    public enum STATE
    {
        WAITING, 
        OPEN,
        CLOSED,
        DISPOSING,
        DISPOSED,
        INITIALIZING
    }
}
