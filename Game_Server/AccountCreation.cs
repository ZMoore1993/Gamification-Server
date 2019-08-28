using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Game_Server
{
    class AccountCreation : WebSocketBehavior
    {
        private Database _db;
        //received message from client
        //FORMAT: CREATE username password
        protected override void OnMessage(MessageEventArgs e)
        {

            if (e.Data.Contains("CREATE"))
            {
                string[] info = e.Data.Substring(7).Split(' ');
                _db.CreateAccount(info.ToList<string>());
            }

        }

        protected override void OnOpen()
        {
            _db = new Database_Mongo(Server.Players);
            base.OnOpen();
        }

    }
}
