﻿using System;

namespace DevelopingNET6
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


