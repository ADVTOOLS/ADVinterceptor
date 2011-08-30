/*
 * This file is part of ADVinterceptor
 * Copyright (c) 2011 - ADVTOOLS SARL
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advtools.AdvInterceptor;

namespace Advtools.AdvInterceptor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ShowInformation();
                Options options = ParseCommandLine(args);
                if (options == null)
                    return;
                StartServers(options);
                Pause();
            }
            catch(ApplicationException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static Options ParseCommandLine(string[] args)
        {
            Options options = new Options();
            if(!options.ParseCommandLine(args))
                return null;
            return options;
        }

        private static void ShowInformation()
        {
            Console.WriteLine("ADVinterceptor version 2.0");
            Console.WriteLine("Copyright (c) 2011 ADVTOOLS - www.advtools.com");
            Console.WriteLine();
        }

        private static void StartServers(Options options)
        {
            Configuration config = null;
            if(options.DefaultConfig)
            {
                config = Configuration.Default;
                config.Save(options.ConfigFile);
            }
            else
                config = Configuration.Load(options.ConfigFile);

            State state = new State(config, options.Level);

            AdvDnsServer dns = new AdvDnsServer(state, options.Intercept);
            dns.Start();

            if(options.Intercept)
            {
                AdvWebServer web = new AdvWebServer(state);
                web.Start();
            }
        }

        private static void Pause()
        {
            for(;;)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to stop...");
                Console.WriteLine();
                Console.ReadKey(true);
                Console.WriteLine("Stop the application? Press Escape to stop");
                ConsoleKeyInfo info = Console.ReadKey(true);
                if(info.Key == ConsoleKey.Escape)
                    break;
            }
            Console.WriteLine("stopping...");
        }
    }
}
