using System;

namespace TestProject
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


