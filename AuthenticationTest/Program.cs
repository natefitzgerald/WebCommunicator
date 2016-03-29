using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //  WebCommunicator client = new WebCommunicator(@"http://localhost:8112", 0);
            WebCommunicator server = new WebCommunicator(8112);
            while (server.state != STATE.OPEN) Task.Delay(100) ;

        }
    }
}
