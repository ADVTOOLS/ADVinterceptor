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

namespace Advtools.AdvInterceptor
{
    #region Enumerations
    /// <summary>
    /// Level of the messages to log
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Message for debugging</summary>
        Debug,
        /// <summary>Message about the configuration of the application</summary>
        Config,
        /// <summary>Informative messages</summary>
        Info,
        /// <summary>Warnings</summary>
        Warning,
        /// <summary>Error messages</summary>
        Error
    }
    #endregion 
    
    internal interface ILogger
    {
        void Log(string message);
        void Log(string format, params object[] arg);
    }

    internal class Logger
    {
        private ILogger logger_;
        public LogLevel Level { get; set; }

        public Logger(LogLevel level = LogLevel.Config)
        {
            Level = level;
            logger_ = new ConsoleLogger();
        }

        public Logger(ILogger logger, LogLevel level = LogLevel.Config)
        {
            Level = level;
            logger_ = logger;
        }

        private void Log(LogLevel level, string message)
        {
            if(level < Level)
                return; 
            logger_.Log("[" + level.ToString().ToUpper() + "] " + message);
        }

        public void Log(LogLevel level, string format, params object[] arg)
        {
            if(level < Level)
                return;
            logger_.Log("[" + level.ToString().ToUpper() + "] " + format, arg);
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Debug(string format, params object[] arg)
        {
            Log(LogLevel.Debug, format, arg);
        }

        public void Config(string message)
        {
            Log(LogLevel.Config, message);
        }

        public void Config(string format, params object[] arg)
        {
            Log(LogLevel.Config, format, arg);
        }
        
        public void Information(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Information(string format, params object[] arg)
        {
            Log(LogLevel.Info, format, arg);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void Warning(string format, params object[] arg)
        {
            Log(LogLevel.Warning, format, arg);
        }
        
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Error(string format, params object[] arg)
        {
            Log(LogLevel.Error, format, arg);
        }

        public void Exception(Exception e, string message)
        {
            Log(LogLevel.Error, message);
            Log(LogLevel.Error, e.Message);
        }

        public void Exception(Exception e, string format, params object[] arg)
        {
            Log(LogLevel.Error, format, arg);
            Log(LogLevel.Error, e.Message);
        }
    }

    internal class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void Log(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
    }
}
