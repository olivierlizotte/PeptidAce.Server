using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeptidAce.Utilities.Interfaces;
using Fleck;

namespace PeptidAce
{
    public class ConSolServer : ConSolBasic
    {
        /// <summary>
        /// Store the list of online users. Wish I had a ConcurrentList. 
        /// </summary>
        protected ConcurrentDictionary<string, ConSolUser> OnlineUsersByName = new ConcurrentDictionary<string, ConSolUser>();

        /// <summary>
        /// Store the list of online users. Wish I had a ConcurrentList. 
        /// </summary>
        protected ConcurrentDictionary<IWebSocketConnection, ConSolUser> OnlineUsersByContext = new ConcurrentDictionary<IWebSocketConnection, ConSolUser>();

        public ConSolServer(UpdateMethod updateMethod = null):base(updateMethod)
        {
        }

        public override void WriteLine(string msg)
        {
            if (funcUpdate != null)
                funcUpdate(msg);
            else
            {
                foreach (ConSolUser u in OnlineUsersByName.Values)//TODO for debugging purposes, send outputs everywhere
                    u.WriteLine(msg);
                Console.WriteLine(msg);
            }
        }

        public ConSolUser GetConSolUser(IWebSocketConnection context)
        {
            if (OnlineUsersByContext.ContainsKey(context))
                return OnlineUsersByContext[context];
            else
                return null;
        }

        public ConSolUser CreateUserConSol(IWebSocketConnection context, string name, Func<string, IWebSocketConnection, int> sendMessageFunc)
        {
            if (OnlineUsersByName.ContainsKey(name))
                return OnlineUsersByName[name];
            else
            {
                ConSolUser user = new ConSolUser(context, name, Logs, sendMessageFunc);
                OnlineUsersByName.TryAdd(name, user);
                OnlineUsersByContext.TryAdd(context, user);
                return user;
            }
        }

        public IEnumerable<ConSolUser> GetAllConnectedClients()
        {
            foreach (ConSolUser user in OnlineUsersByName.Values)
                yield return user;
        }

        public ConSolUser RemoveUserConSol(IWebSocketConnection context)
        {
            try
            {
                if (OnlineUsersByContext.ContainsKey(context))
                {
                    ConSolUser user = OnlineUsersByContext[context];
                    OnlineUsersByContext.TryRemove(context, out user);
                    OnlineUsersByName.TryRemove(user.Name, out user);
                    return user;
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
            return null;
        }
    }

    public class ConSolUser : IConSol
    {
        private Logger Logs;
        public string Name = String.Empty;
        public IWebSocketConnection Context { get; set; }
        private Func<string, IWebSocketConnection, int> SendMessageFunc;

        public ConSolUser(IWebSocketConnection userContext, string name, Logger log, Func<string, IWebSocketConnection, int> sendMessage)
        {
            this.Context = userContext;
            this.Name = name;
            this.Logs = log;
            this.SendMessageFunc = sendMessage;
        }

        public void WriteLine(string msg)
        {
            Logs.Add(msg, Name);
            SendMessageFunc(msg, Context);
        }
    }
}
