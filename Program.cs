/*
 * Copyright 2013 
 * Licensed under the MIT license: <http://www.opensource.org/licenses/mit-license.php>
 */

/*
 * Includes a reference to the Json.NET library <http://json.codeplex.com>, used under 
 * MIT license. See <http://www.opensource.org/licenses/mit-license.php>  for license details. 
 * Json.NET is copyright 2007 James Newton-King
 */

/* 
 * Includes the Alchemy Websockets Library 
 * <http://www.olivinelabs.com/index.php/projects/71-alchemy-websockets>, 
 * used under LGPL license. See <http://www.gnu.org/licenses/> for license details. 
 * Alchemy Websockets is copyright 2011 Olivine Labs.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Trinity.UnitTest;
using Fleck;

namespace Afinity
{
    /// <summary>
    /// Main class with websocket initialisation. Also creates a ConSole object to handle log files generation and 
    /// </summary>
    public class TrinityMain
    {
        /// <summary>
        /// Store the list of online users. Wish I had a ConcurrentList. 
        /// </summary>
        //protected static ConcurrentDictionary<IWebSocketConnection, ConSolUser> OnlineUsers = new ConcurrentDictionary<IWebSocketConnection, ConSolUser>();
                
        /// <summary>
        /// Console wrapper with integrated log files and command compiler (through reflection)
        /// </summary>
        public static ConSol Sol;

        /// <summary>
        /// Pointer to an instance of the main class
        /// </summary>
        public static TrinityMain Instance = new TrinityMain();

        /// <summary>
        /// Initialize the application and start the Alchemy Websockets server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Sol = new ConSol();                    
            string command = "";

            var server = new WebSocketServer("ws://localhost:8181");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    SendRegisterInfo(socket);
                };
                socket.OnClose = () =>
                {
                    OnDisconnect(socket);
                };
                socket.OnMessage = message =>
                {
                    OnReceive(socket, message);                    
                };
            });
            
            Sol.WriteLine("Welcome to ConSOLe : the command line, server-side interface of Trinity!");
            Sol.WriteLine("Type in the commands you wish to be executed");
            
            do
            {/*
                if (!string.IsNullOrEmpty(command))
                    Sol.Execute(command);//TODO Repair Reflective compilation//*/
                command = Console.ReadLine();
            }
            while (command != null && string.Compare(command, "exit") != 0);

            Sol.WriteLine("---+---+--+--+-+-      ProteoProfile will now close     -+-+--+--+---+---");
            Sol.UpdateLogFile();                        
        }
        
        /// <summary>
        /// Event fired when a data is received from the Alchemy Websockets server instance.
        /// Parses data as JSON and calls the appropriate message or sends an error message.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnReceive(IWebSocketConnection socket, string message)
        {
          //  Sol.OutputLine("Received Data From :" + context.ClientAddress);

            try
            {
                ConSolUser user = Sol.GetConSolUser(socket);                
                if(user == null)
                {
                    if(!message.Contains("Register"))
                        SendRegisterInfo(socket);
                    else
                    {                    
                        dynamic obj = JsonConvert.DeserializeObject(message);
                        if(((string)obj.Type).CompareTo("Register") == 0)                            
                            Register(obj.Name.Value, socket);
                    }
                }
                else
                {
                    // <3 dynamics
                    dynamic obj = JsonConvert.DeserializeObject(message);

                    switch ((string)obj.Type)
                    {
                        case "Message":
                            break;
                        case "Menu":
                            break;
                        case "Command":
                            Execute(obj.Command.Value, user);
                                SendError("You must log in if you want to send commands to the Trinity Server", socket);
                            //Sol.Execute(obj.Command.Value);
                            break;
                        case "NameChange":
                            //NameChange(obj.Name.Value, context);
                            break;
                    }//*/
                }
            }
            catch (Exception e) // Bad JSON! For shame.
            {
                Console.WriteLine("Received uninterpretable data from " + socket.ConnectionInfo);
                if (!string.IsNullOrEmpty(message))
                    Console.WriteLine(" => " + message);
                //var r = new Response { Type = "Error", Message = e.Message };
                //SendError(JsonConvert.SerializeObject(r), context);
                //context.Send(JsonConvert.SerializeObject(r));
            }
        }

        /// <summary>
        /// Event fired when the Alchemy Websockets server instance sends data to a client.
        /// Logs the data to the console and performs no further action.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static int SendToTerminal(string message, IWebSocketConnection socket)
        {
            //Properties
            var r = new Response { Type = "Info", Message = message };
            socket.Send(JsonConvert.SerializeObject(r));
            return 1;
        }
        
        public static int SendRegisterInfo(IWebSocketConnection socket)
        {
            //Properties
            var r = new Response { Type = "Register", Data = { } };

            socket.Send(JsonConvert.SerializeObject(r));
            return 1;
        }

        /// <summary>
        /// Event fired when the Alchemy Websockets server instance sends data to a client.
        /// Logs the data to the console and performs no further action.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static int SendDataToClient(object serializableObj, IWebSocketConnection socket)
        {
            //Properties
            var r = new Response { Type = "Data", Data = serializableObj };

            socket.Send(JsonConvert.SerializeObject(r));
            return 1;
        }

        /// <summary>
        /// Event fired when a client disconnects from the Alchemy Websockets server instance.
        /// Removes the user from the online users list and broadcasts the disconnection message
        /// to all connected users.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnDisconnect(IWebSocketConnection socket)
        {
            try
            {
                Sol.WriteLine("Client Disconnected : " + socket.ConnectionInfo);

                Sol.RemoveUserConSol(socket);
            }
            catch (Exception e) // Disconnection not valid
            {
                Sol.WriteLine("Could not Disconnect client : " + socket.ConnectionInfo);
            }
        }

        /// <summary>
        /// Register a user's context for the first time with a username, and add it to the list of online users
        /// </summary>
        /// <param name="name">The name to register the user under</param>
        /// <param name="context">The user's connection context</param>
        private static void Register(string name, IWebSocketConnection socket)
        {
            if (ValidateName(name))
            {
                Sol.CreateUserConSol(socket, name, SendToTerminal);

                var r = new Response();
                r.Type = "Connection";
                r.Data = new { name };
                r.Message = "User " + name + " is now connected";
                 
                Broadcast(JsonConvert.SerializeObject(r));
            }
            else
                SendError("Selected name is not accepted", socket);
        }

        /// <summary>
        /// Broadcasts a message to all users, or if users is populated, a select list of users
        /// </summary>
        /// <param name="message">Message to be broadcast</param>
        /// <param name="users">Optional list of users to broadcast to. If null, broadcasts to all. Defaults to null.</param>
        private static void Broadcast(string message)
        {
            foreach (ConSolUser user in Sol.GetAllConnectedClients())
                user.Context.Send(message);
        }

        /// <summary>
        /// Broadcasts an error message to the client who caused the error
        /// </summary>
        /// <param name="errorMessage">Details of the error</param>
        /// <param name="context">The user's connection context</param>
        private static void SendError(string errorMessage, IWebSocketConnection socket)
        {
            var r = new Response { Type = "Error", Message = errorMessage };

            socket.Send(JsonConvert.SerializeObject(r));
        }
        
        /// <summary>
        /// Checks validity of a user's name
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <returns></returns>
        private static bool ValidateName(string name)
        {
            var isValid = false;
            if (name.Length > 3 && name.Length < 25)
            {
                isValid = true;
            }

            return isValid;
        }

        /// <summary>
        /// Defines the response object to send back to the client
        /// </summary>
        public class Response
        {
            public string Type { get; set; }
            public dynamic Data { get; set; }
            public dynamic Message { get; set; }
        }

        private static Type GetClassType(string className, Type refType)
        {
            //Is the command a fullname class?
            Type theType = refType.Assembly.GetType(className);

            //Is the command a class in "Trinity" ?
            if (theType == null)
                theType = refType.Assembly.GetType("Trinity." + className);

            //Is the command a class in another Trinity namespace?
            if (theType == null)
                foreach (Type tttype in refType.Assembly.GetTypes())
                    if (tttype.Name.CompareTo(className) == 0)
                        theType = tttype;
            return theType;
        }

        /// <summary>
        /// Launches, on a different thread, the method stored in "command". This method must be from the MenuFunctions class object
        /// </summary>
        /// <param name="command">
        /// Name of the method to execute (must be part of MenuFunctions)
        /// </param>
        /// <param name="context">
        /// User launching the method
        /// TODO Console outputs generated by commands of this user should be available to this user only, and flaged appropriatly in the Logs
        /// </param>
        public static void Execute(string command, ConSolUser user)
        {
            try
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Type refType = typeof(Trinity.Peptide);
                        string[] splits = command.Split(' ');

                        //List all possible classes, filter by keyword (second param)
                        if ("ls".CompareTo(splits[0]) == 0)
                        {
                            string strCommands = "";
                            foreach (Type tttype in refType.Assembly.GetTypes())
                                if(tttype.IsClass && tttype.IsPublic && (splits.Length == 1 || tttype.FullName.Contains(splits[1])))
                                    strCommands += "\n" + tttype.Name;
                            if(!string.IsNullOrEmpty(strCommands))
                                SendToTerminal(strCommands.Substring(1), user.Context);
                        }
                        else
                        {
                            string[] splitsDot = command.Split('.');
                            int iterIndexSplit = 0;
                            string className = splitsDot[iterIndexSplit];
                            string methodName = "Launch";
                            for (iterIndexSplit = 1; iterIndexSplit < splitsDot.Length - 1; iterIndexSplit++)
                                className += "." + splitsDot[iterIndexSplit];

                            Type theType = GetClassType(className, refType);
                            if (theType == null)//No specified method, use default "Launch()" method
                            {
                                className += "." + splitsDot[iterIndexSplit];
                                iterIndexSplit++;
                                theType = GetClassType(className, refType);
                            }
                            
                            if (theType != null)
                            {
                                System.Reflection.MethodInfo theMethod = null;
                                if(iterIndexSplit < splitsDot.Length)
                                    methodName = splitsDot[iterIndexSplit];
                                
                                theMethod = theType.GetMethod(methodName);
                                object result = null;
                                if (theMethod != null)
                                    result = theMethod.Invoke(Instance, new[] { user });
                                else
                                    SendError("Could not find method " + className + "." + methodName + "(ConSolUser user)", user.Context);

                                if (result != null)
                                    SendDataToClient(result, user.Context);                                
                            }
                            else
                                SendError("Could not find class " + command, user.Context);
                        }
                    }
                    catch (Exception)
                    {
                        SendError("Could not run command", user.Context);
                    }
                });
            }
            catch (Exception)
            {
                SendError("Could not run command", user.Context);
            }
        }
    }
}