using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationTest
{
    partial class WebCommunicator : IDisposable
    {
        public WebCommunicator(string URL = @"http://*:8112/") //ip addr like "192.168.1.1" as a string.
        {
            SelectPort();
            this.baseURL = URL.Split(':')[0];
            try
            {
                client = new HttpClient();
                RSAParameters RSAParams = RSA.ExportParameters(false);
                Stream s = new MemoryStream();
                binForm.Serialize(s, RSAParams);
                s.Position = 0;
                HttpContent content = new StreamContent(s);
                content.Headers.Add("port", port.ToString());
                Task.WaitAll(Task.Run(() => client.PostAsync(URL, content)));
                state = STATE.OPEN;
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        public Task<bool> Send(object toSend)
        {
            return Task.Run(() =>
            {
                try
                {
                    string finalURL = baseURL + port.ToString();
                    Type objectType = toSend.GetType();
                    if (!objectType.IsSerializable) return false;
                    MemoryStream s = new MemoryStream();
                    binForm.Serialize(s, toSend);
                    s.Position = 0;

                    RSA.ImportParameters(privkey);
                    byte[] ciphertext = RSA.Encrypt(s.ToArray(), false);
                    s = new MemoryStream(ciphertext);

                    client = new HttpClient();
                    HttpContent content = new StreamContent(s);
                    Task.WaitAll(Task.Run(() => client.PostAsync(finalURL, content)));

                    return true;
                }
                catch { return false; }
            });
        }
    }
}
