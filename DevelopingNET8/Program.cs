using System;

namespace UnitTest1
{
    class Program
    {
        static void Main(string[] args)
        {
            IMessageSource messageSource = new UdpMessageSource();
            var server = new Server(messageSource);
            server.Work();
        }
    }
}


