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
using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Trinity.UnitTest;

namespace Afinity
{
    /// <summary>
    /// This menu functions is used to give access to some function by sending commands through the websocket.
    /// Originally, the ConSol offered full control over C# (you could send a real C# command, and it would get executed)
    /// Now that ProteoProfile has been designed to run server side, remote code execution has been removed.
    /// Only functions listed here will be executed, to prevent security flaws. Anything added here is automatically available
    /// from the command line interface. 
    /// TODO Remove ToString and other "object" class methods from the options listed by the ls command.
    /// </summary>
    public class MenuFunctions
    {
        public string YeastSampleTest(UserContext context)
        {
            YeastSample.Launch();
            return "Yeast sample test executed!";
        }

        public string SettePeptideSampleTest(UserContext context)
        {
            SettePeptideSample.Launch();
            return "Sette Peptide sample executed";
        }

        public string MhcSampleTest(UserContext context)
        {
            MhcSample.Launch();
            return "Mhc sample test executed";
        }

        public string ls(UserContext context)
        {
            string output = "";
            Type thisType = this.GetType();
            System.Reflection.MethodInfo[] theMethods = thisType.GetMethods();
            foreach (System.Reflection.MethodInfo method in theMethods)
            {
                output += ", " + method.Name;
            }
            return output.Substring(2);
        }
    }

    /// <summary>
    /// Main class with websocket initialisation. Also creates a ConSole object to handle log files generation and 
    /// </summary>
    class TrinityMain
    {
        /// <summary>
        /// Store the list of online users. Wish I had a ConcurrentList. 
        /// </summary>
        protected static ConcurrentDictionary<UserContext, string> OnlineUsers = new ConcurrentDictionary<UserContext, string>();
                
        /// <summary>
        /// Console wrapper with integrated log files and command compiler (through reflection)
        /// </summary>
        public static ConSol Sol;
        
        /// <summary>
        /// Console wrapper with integrated log files and command compiler (through reflection)
        /// </summary>
        public static MenuFunctions Instance = new MenuFunctions();

        /// <summary>
        /// Initialize the application and start the Alchemy Websockets server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //Start SOL

            //Console <controller>
            Sol = new ConSol();// = new vsConSOLe(new UpdateMethod(UpdateMethodHandler));                        
            string command = "";

            //RawStatsUnitTest.Run();

            //Tests.Run();
            NBSample.Launch();
            //Tests.YangLiuPeptidesWithAllProteins();
            //Tests.LysineConservation();
            //NoEnzymeDigestUnitTest.Run();
            //Instance.MhcSampleTest(null); 
            //Instance.YeastSampleTest(null);
            //Tests.LysineConservation();
            //Tests.MascotCompare();


            // Initialize the server on port 81, accept any IPs, and bind events.
            var aServer = new WebSocketServer(81, IPAddress.Any)
            {
                OnReceive = OnReceive,
                OnSend = OnSend,
                OnConnected = OnConnect,
                OnDisconnect = OnDisconnect,
                TimeOut = new TimeSpan(150, 0, 0)
            };

            aServer.Start();

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

            aServer.Stop();
        }
        
        /// <summary>
        /// Event fired when a client connects to the Alchemy Websockets server instance.
        /// Adds the client to the online users list.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnConnect(UserContext context)
        {
            Sol.WriteLine("Client Connection From : " + context.ClientAddress);            
            OnlineUsers.TryAdd(context, String.Empty);
        }

        /// <summary>
        /// Event fired when a data is received from the Alchemy Websockets server instance.
        /// Parses data as JSON and calls the appropriate message or sends an error message.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnReceive(UserContext context)
        {
          //  Sol.OutputLine("Received Data From :" + context.ClientAddress);

            try
            {
                var user = OnlineUsers.Keys.Where(o => o.ClientAddress == context.ClientAddress).Single();
                if (user != null)
                {
                    //Sol.OutputLine("-----------------JSON Data------------------");
                    //Sol.OutputLine(context.DataFrame.ToString());
                    //Sol.OutputLine("-----------------   END   ------------------");

                    var json = context.DataFrame.ToString();

                    // <3 dynamics
                    dynamic obj = JsonConvert.DeserializeObject(json);

                    switch ((string)obj.Type)
                    {
                        case "Register":
                            Register(obj.Name.Value, context);
                            break;
                        case "Message":
                            ChatMessage(obj.Message.Value, context);
                            break;
                        case "Menu":
                            DisplayMenu(obj.Message.Value, context);
                            break;
                        case "Command":
                            Execute(obj.Command.Value, context);
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
                var r = new Response { Type = "Error", Message = e.Message };

                context.Send(JsonConvert.SerializeObject(r));
            }
        }

        /// <summary>
        /// Event fired when the Alchemy Websockets server instance sends data to a client.
        /// Logs the data to the console and performs no further action.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnSend(UserContext context)
        {
          //  Sol.OutputLine("Data Send To : " + context.ClientAddress);
        }

        /// <summary>
        /// Event fired when a client disconnects from the Alchemy Websockets server instance.
        /// Removes the user from the online users list and broadcasts the disconnection message
        /// to all connected users.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnDisconnect(UserContext context)
        {
            try
            {
                Sol.WriteLine("Client Disconnected : " + context.ClientAddress);
            
                var user = OnlineUsers.Keys.Where(o => o.ClientAddress == context.ClientAddress).Single();

                string trash; // Concurrent dictionaries make things weird
                OnlineUsers.TryRemove(user, out trash);

                ConSolUser userCS = Sol.RemoveUserConSol(context);//TODO pass name or context?

                if (userCS != null)
                {
                    string msg = "User " + userCS.Name + " disconnected";
                    var r = new Response { Type = "Disconnect", Data = new { userCS.Name }, Message = msg };

                    Broadcast(JsonConvert.SerializeObject(r));
                }
                //BroadcastNameList();
            }
            catch (Exception e) // Disconnection not valid
            {
                Sol.WriteLine("Could not Disconnect client");

                var r = new Response { Type = "Error", Message = e.Message };

                context.Send(JsonConvert.SerializeObject(r));
            }
        }

        /// <summary>
        /// Register a user's context for the first time with a username, and add it to the list of online users
        /// </summary>
        /// <param name="name">The name to register the user under</param>
        /// <param name="context">The user's connection context</param>
        private static void Register(string name, UserContext context)
        {
            var u = OnlineUsers.Keys.Where(o => o.ClientAddress == context.ClientAddress).Single();
            var r = new Response();

            if (ValidateName(name))
            {
                Sol.CreateUserConSol(context, name);

                r.Type = "Connection";
                r.Data = new { name };
                r.Message = "User " + name + " is now connected";
                 
                Broadcast(JsonConvert.SerializeObject(r));

                BroadcastNameList();
                OnlineUsers[u] = name;
            }
            else
            {
                SendError("Selected name is not accepted", context);
            }
        }

        /// <summary>
        /// Broadcasts a chat message to all online users
        /// </summary>
        /// <param name="message">The chat message to be broadcasted</param>
        /// <param name="context">The user's connection context</param>
        private static void ChatMessage(string message, UserContext context)
        {
            var u = OnlineUsers.Keys.Where(o => o.ClientAddress == context.ClientAddress).Single();
            string name = OnlineUsers[u];
            var r = new Response { Type = "Message", Data = new { name } , Message = message  };

            Broadcast(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Return the menu options of a particular context
        /// </summary>
        /// <param name="message">The current page's context</param>
        /// <param name="context">The user's connection context</param>
        private static void DisplayMenu(string page, UserContext context)
        {
            var u = OnlineUsers.Keys.Where(o => o.ClientAddress == context.ClientAddress).Single();
            string methods = "";

            //Properties
            System.Reflection.MethodInfo[] methodArray = Instance.GetType().GetMethods();//Use flags to restrict power for certain users
            foreach (System.Reflection.MethodInfo method in methodArray)
                methods += ", " + method.Name;
            if (!string.IsNullOrEmpty(methods))
                methods = methods.Substring(1);
            string msg = "Available methods : " + methods;
            var r = new Response { Type = "Menu", Data = methods, Message = msg};

            context.Send(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Broadcasts an error message to the client who caused the error
        /// </summary>
        /// <param name="errorMessage">Details of the error</param>
        /// <param name="context">The user's connection context</param>
        private static void SendError(string errorMessage, UserContext context)
        {
            var r = new Response { Type = "Error", Message = errorMessage };

            context.Send(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Broadcasts a list of all online users to all online users
        /// </summary>
        private static void BroadcastNameList()
        {
            var r = new Response
            {
                Type = "UserCount",
                Data = new { Users = OnlineUsers.Values.Where(o => !String.IsNullOrEmpty(o)).ToArray() }
            };
            Broadcast(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Broadcasts a message to all users, or if users is populated, a select list of users
        /// </summary>
        /// <param name="message">Message to be broadcast</param>
        /// <param name="users">Optional list of users to broadcast to. If null, broadcasts to all. Defaults to null.</param>
        private static void Broadcast(string message, ICollection<UserContext> users = null)
        {
            if (users == null)
            {
                foreach (var u in OnlineUsers.Keys)
                    u.Send(message);
            }
            else
            {
                foreach (var u in OnlineUsers.Keys.Where(users.Contains))
                    u.Send(message);
            }
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
        public static void Execute(string command, UserContext context)
        {
            try
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    try{
                        Type thisType = Instance.GetType();
                        System.Reflection.MethodInfo theMethod = thisType.GetMethod(command);
                        string output = theMethod.Invoke(Instance, new[] { context }) as string;
                        if (!string.IsNullOrEmpty(output))
                        {
                            var r = new Response { Type = "Result", Message = output };
                            context.Send(JsonConvert.SerializeObject(r));
                        }
                    }
                    catch (Exception)
                    {
                        SendError("Could not run command", context);
                    }
                });
            }
            catch (Exception)
            {
                SendError("Could not run command", context);
            }
        }
    }
}