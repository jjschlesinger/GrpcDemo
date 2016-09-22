using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core.Logging;

namespace GrpcDemo.Server
{
    public class GrpcLogger : ILogger
    {
        private readonly Serilog.ILogger _serilogLogger;

        public GrpcLogger(Serilog.ILogger serilogLogger)
        {
            _serilogLogger = serilogLogger;
        }

        public GrpcLogger()
        {
            _serilogLogger = Serilog.Log.Logger;
        }

        public ILogger ForType<T>()
        {
            return new GrpcLogger(_serilogLogger.ForContext<T>());
        }

        public void Debug(string message)
        {
            _serilogLogger.Debug(message);
        }

        public void Debug(string format, params object[] formatArgs)
        {
            _serilogLogger.Debug(format, formatArgs);
        }

        public void Info(string message)
        {
            _serilogLogger.Information(message);
        }

        public void Info(string format, params object[] formatArgs)
        {
            _serilogLogger.Information(format, formatArgs);
        }

        public void Warning(string message)
        {
            _serilogLogger.Warning(message);
        }

        public void Warning(string format, params object[] formatArgs)
        {
            _serilogLogger.Warning(format, formatArgs);
        }

        public void Warning(Exception exception, string message)
        {
            _serilogLogger.Warning(exception, message);
        }

        public void Error(string message)
        {
            _serilogLogger.Error(message);

        }

        public void Error(string format, params object[] formatArgs)
        {
            _serilogLogger.Error(format, formatArgs);
        }

        public void Error(Exception exception, string message)
        {
            _serilogLogger.Error(exception, message);
        }
    }
}
