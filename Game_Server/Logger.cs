using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace Game_Server
{
    //Enum for debugging types
    public enum LogType { GENERAL, DATABASE, TCP, UDP, ERROR, WEBSOCKET }

    //This class will be expanded to provide nice and pretty logs
    //For now, it just does Console.WriteLine() for all the input
    public class Logger
    {
        //Various variables
        private string _serverVersion;

        private int _loggingLevel;  //0 = no debug output, 1 = debug output. More options may be added later
        private BlockingCollection<Tuple<LogType, object>> _queue;
        private CancellationTokenSource _cancel;
        private CancellationToken _cancellationToken;

        //Basic constructor
        public Logger(int logLevel,  string serverVersion)
        {
            _loggingLevel = logLevel;
            _serverVersion = serverVersion;

            _queue = new BlockingCollection<Tuple<LogType, object>>();
            _cancel = new CancellationTokenSource();
            _cancellationToken = _cancel.Token;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Starting logger! Server version: " + _serverVersion);
            Console.ResetColor();

            Task.Factory.StartNew(LoggingThread, _cancellationToken);
        }

        private void LoggingThread() {
            while (true)
            {
                //Wait for a message to arrive in the queue
                Tuple<LogType, object> tuple = _queue.Take();

                LogType type = tuple.Item1;
                object message = tuple.Item2;

                switch(type)
                {
                    //Database
                    case LogType.DATABASE:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    //Client communications (TCP or Websockets)
                    case LogType.WEBSOCKET:
                    case LogType.TCP:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    //UDP
                    case LogType.UDP:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    //Error
                    case LogType.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    //General
                    case LogType.GENERAL:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
                //Always write general messages, write all others as well if logging level is > 0
                if (type == LogType.GENERAL || _loggingLevel > 0)
                {
                    Console.WriteLine(message);
                }
                Console.ResetColor();
            }
        }

        //The main logging function accessible to the outer world
        public void Log(object message)
        {
            _queue.Add(new Tuple<LogType, object>(LogType.GENERAL, message));
        }

        //The main debugging function
        public void Debug(LogType type, object message)
        {
            _queue.Add(new Tuple<LogType, object>(type, "---- " + message));
        }
    }
}