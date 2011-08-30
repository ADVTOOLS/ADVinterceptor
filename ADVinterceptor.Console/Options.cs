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
using NDesk.Options;

namespace Advtools.AdvInterceptor
{
    internal class Options
    {
        public bool Intercept { get; private set; }
        public LogLevel Level { get; private set; }
        public string ConfigFile { get; private set; }
        public bool DefaultConfig { get; private set; }

        public Options()
        {
            Intercept = true;
            Level = LogLevel.Config;
            ConfigFile = "config.xml";
            DefaultConfig = false;
        }

        /// <summary>
        /// Parse the command-line arguments
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>Return true if the application can continue, false if it has to stop there</returns>
        public bool ParseCommandLine(string[] args)
        {
            // Help requested? (by default, no)
            bool help = false;

            // Definition of the command-line arguments and how to set the related configuration data
            var options = new OptionSet()
            {
                { "intercept=", "Intercept or forward requests",    (bool o) => Intercept = o },
                { "log=", "Level of messages to log",               (LogLevel o) => Level = o },
                { "config=", "Configuration file",                  (string o) => ConfigFile = o },
                { "default", "Generate a default configuration",    o => DefaultConfig = o != null },
                { "h|help", "Show this message",                    o => help = o != null }
            };

            try
            {
                // Parse the command-line arguments (thanks NDesk!)
                List<string> extra = options.Parse(args);
                // If there are some arguments not parsed, no name of pipe or an invalid port number, show the help
                if(extra.Count > 0)
                {
                    Console.WriteLine("Unknow parameter: {0}", extra[0]);
                    Console.WriteLine();
                    ShowHelp(options);
                    return false; // Do not continue the application
                }
            }
            catch(OptionException e)
            {
                // Something wrong with the arguments
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'ADVinterceptor --help' for more information.");
                return false;
            }

            // Show the help?
            if(help)
            {
                ShowHelp(options);
                return false;
            }

            // Continue the application
            return true;

        }

        /// <summary>
        /// Display some help about this application
        /// </summary>
        /// <param name="options"></param>
        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("Usage: ADVinterceptor [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}
