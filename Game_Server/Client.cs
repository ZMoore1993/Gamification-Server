using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
//using System.Net.WebSockets;
using System.Threading;
using System.IO;
using System;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;


namespace Game_Server
{
    //Class to handle each client by itself
    internal class Client : WebSocketBehavior
    {
        //Logging
        private Logger logger;

        //Networking
        private bool _hasReceivedFirstMessage;
        private bool _hasValidClientVersion;

        //Database
        private Database _database;  //This is just a library for the DB functions

        //Misc
        public string Username { get; private set; }
        private string _uniqueId;
        private bool _loggedIn;
        private static Encoding _encoder = Encoding.UTF8;
        //public UdpListener UdpListener;       //Send to UdpNetwork objects

        //World variables - these are read by World via thread-safe functions to send to other players
        //public World _world;                        //Reference to the world that this exists in
        private int _playerId;                      //Used in all byte arrays
        //private Tuple<int, byte[]> _positionTuple;  //Position variable
        //private byte[] _textMessage;                //Text variable
        //private byte[] _rosterArray;                //Roster variable

        //Text variables - text messages are sent multiple times in case of minor packet loss
        private int _textMessageSendCount;
        private static int _textMessageSendingLimit = 3;   //Send each text message 3 times

        //Roster variables - unlike the other variables, these are set by the main TCP thread (and thus require no locking)
        private int _level;             //Player level
        private int _ascensionLevel;

        //private int _spriteIndex;       //Player sprite - male or female are the only options for now
        //private string _characterName;  //In-game player name

        //World variables, used for special scenarios
        //They're generated here to save the World thread precious CPU cycles
        //private byte[] _infinityPosition;
        //private byte[] _logoutMessage;

        //Lock object, to ensure text is properly updated (as it is represented by two different variables)
        //No lock is needed for position, as it is a single reference type variable (so updating position is atomic)
        //Also, as position updates are extremely frequent, removing the need for a lock will help performance
        private object _textLock = new object();

        //Variable to notify if UDP is ready
        //0 = not connected, 1 = connected but hasn't notified client, 2 = has notified client
        //public int UdpConnectionStatus = 0;

        //*
        protected override void OnOpen()
        {

            logger = Server.logger;
            _loggedIn = _hasValidClientVersion = false;
            _database = new Database_Mongo(Server.Players);

            base.OnOpen();
            //Send("VERSION: " + Server.AvailableClientVersion);
        }//*/

        //constructor
        public Client()//, int playerId)  //, UdpListener udpListener, Socket udpSocket)
        {

        }



        private List<List<string>> SplitReceivedString(string msg)
        {
            string[] split_msg = msg.Split('\n');
            List<List<string>> result = new List<List<string>>();
            foreach (string cmd in split_msg)
            {
                string[] split_cmd = cmd.Split(' ');
                List<string> res_cmd = split_cmd.ToList(); //= new List<string>();
                result.Add(res_cmd);
            }

            return result;
        }

        //*
        //received message from client
        protected override void OnMessage(MessageEventArgs e)
        {
            bool keyError = false;
            List<List<string>> commands = null;
            int status = 0;
            int perCommandStatus = 0;
            bool die = false;

            //if (e.Data.Equals("VALID CLIENT"))
            //    _hasValidClientVersion = true;
            /*
            if (e.Data.Equals("Client Version: "))
            {
                //determine if valid version
                string version = e.Data.Substring(16);
                logger.Debug(LogType.GENERAL,"Client version:" + version);

                if (version.Equals(Server.AvailableClientVersion))
                {
                    _hasValidClientVersion = true;
                    Send("VERSION OK");
                }
                else
                    _hasValidClientVersion = false;
            }//*/

            /*
            if (!_hasValidClientVersion)
            {
                logger.Debug(LogType.ERROR, ID + " needs to update their client.");
                return;
            }//*/
            if(e.Data.Equals("users"))
            {
                
                Send(Server.Players.ToJson());
                return;
            }

            try
            {
                //assume data is received by server with \n separating commands
                logger.Debug(LogType.GENERAL, "Received message from ID: " + ID);
                commands = SplitReceivedString(e.Data); //find out the format the data is received and split string for commands
                                                        //Exit if client timed out
                if (commands == null)
                {
                    die = true;
                    return;
                }

                /*
                //Get key from this
                receivedKey = commands.Last()[0].ToString();

                //If command isn't a login request, verify the key
                //If key doesn't match, ignore everything
                if (_loggedIn)
                {
                    string savedKey = _database.GetLoginKey();
                    if (receivedKey != savedKey)
                    {
                        logger.Debug(LogType.ERROR, "Key mismatch!");
                        TcpNetwork _tcp;
                        _tcp.SendMessage(-8, "KEYERROR");
                        keyError = true;
                        break;
                    }
                }
                if (keyError) break;
                //else logger.Debug("Key is correct. Commands: " + (commands.Count - 1), 2);
                //*/ //end of block comment here

                //Process each command individually
                status = 0;
                for (int j = 0; j < commands.Count; j++)
                {
                    //logger.Debug(LogType.TCP, "Processing command " + j + ": " + commands[j][0]);
                    logger.Debug(LogType.WEBSOCKET, "Processing command " + j + ": " + commands[j][0]);

                    perCommandStatus = ProcessCommand(commands[j]);
                    if (perCommandStatus == -6)
                    {
                        //logger.Debug("Dying!");
                        logger.Debug(LogType.GENERAL, "Dying!");
                        die = true;
                        break;
                    }
                    if (perCommandStatus != 0 && status != 5) status = perCommandStatus;
                }
                if (die) return;
                else logger.Debug(LogType.WEBSOCKET, "Commands all processed");

                //Check if the roster needs to be updated
                /*if (_database.Roster_Changed())
                {
                    UpdateRoster();
                }//*/ //end of block comment here

                //Save the changes
                _database.Save();
                //Notify the user that these have all been processed
                if (status == 0)//(!_tcp.MessageSent)
                {
                    Send("OK");//_tcp.SendMessage(status, "OK");
                }


            }
            catch (IOException)
            {
                //Do nothing, client has exited normally
                //logger.Debug(e);
            }
            catch (Exception ex)
            {
                //Something else caused an issue
                logger.Log(ex);
            }
            finally
            {
                //Close the client and stream
                //_tcp.Close();

                //_udp.Close();
                //Have the database log the player out
                _database.Logout(keyError);
                //Notify the main server thread that the client has closed
                //OnExit(this, new EventArgs());
            }
        }
        //*/

        /*
        public void ReceivedMessage(string message)
        {
            int requestCount = 0;
            bool keyError = false;
            List<List<string>> commands = null;
            int status = 0;
            int perCommandStatus = 0;
            bool die = false;
            //string receivedKey;

            try
            {
                //Initiate the TCP connection
                //_tcp.Initialize();

                //requestCount++;
                //_tcp.MessageSent = false;
                //assume data is received by server with \n separating commands
                logger.Debug(LogType.GENERAL, "Received message from client ");
                commands = SplitReceivedString(message); //find out the format the data is received and split string for commands
                                                         //commands = _tcp.ReceiveMessage();
                                                         //Exit if client timed out
                if (commands == null)
                {
                    die = true;
                    return;
                }

                /*
                //Get key from this
                receivedKey = commands.Last()[0].ToString();

                //If command isn't a login request, verify the key
                //If key doesn't match, ignore everything
                if (_loggedIn)
                {
                    string savedKey = _database.GetLoginKey();
                    if (receivedKey != savedKey)
                    {
                        logger.Debug(LogType.ERROR, "Key mismatch!");
                        TcpNetwork _tcp;
                        _tcp.SendMessage(-8, "KEYERROR");
                        keyError = true;
                        break;
                    }
                }
                if (keyError) break;
                //else logger.Debug("Key is correct. Commands: " + (commands.Count - 1), 2);


                //Process each command individually
                status = 0;
                for (int j = 0; j < commands.Count; j++)
                {
                    //logger.Debug(LogType.TCP, "Processing command " + j + ": " + commands[j][0]);
                    logger.Debug(LogType.WEBSOCKET, "Processing command " + j + ": " + commands[j][0]);

                    perCommandStatus = ProcessCommand(commands[j]);
                    if (perCommandStatus == -6)
                    {
                        //logger.Debug("Dying!");
                        logger.Debug(LogType.GENERAL, "Dying!");
                        die = true;
                        break;
                    }
                    if (perCommandStatus != 0 && status != 5) status = perCommandStatus;
                }
                if (die) return;
                else logger.Debug(LogType.WEBSOCKET, "Commands all processed");//(LogType.TCP, "Commands all processed");

                //Check if the roster needs to be updated
                /*if (_database.Roster_Changed())
                {
                    UpdateRoster();
                }

                //Save the changes
                _database.Save();
                //Notify the user that these have all been processed
                if (status == 0)//(!_tcp.MessageSent)
                {

                    //Send("OK");//_tcp.SendMessage(status, "OK");
                }


            }
            catch (IOException)
            {
                //Do nothing, client has exited normally
                //logger.Debug(e);
            }
            catch (Exception ex)
            {
                //Something else caused an issue
                logger.Log(ex);
            }
            finally
            {
                //Close the client and stream
                //_tcp.Close();

                //_udp.Close();
                //Have the database log the player out
                _database.Logout(keyError);
                //Notify the main server thread that the client has closed
                //OnExit(this, new EventArgs());
            }
        }
        //*/

        //Process command. Routes the command to the appropriate function
        private int ProcessCommand(List<string> command)
        {
            //logger.Debug("Processing command: " + command[0]);
            switch (command[0])
            {
                //Login related functions
                case "LOGIN": return LogIn(command[1], command[2], command[3]);
                case "LOGIN_GETDATA": return GetLoginDocument();

                //The following functions are deprecated - use LOGIN_GETDATA instead
                //case "LOGIN_GETBONUSCODES": return GetDocument("BonusCodes");
                //case "LOGIN_GETEQUIPMENT": return GetDocument("Equipment");
                //case "LOGIN_GETINVENTORY": return GetDocument("Inventory");
                //case "LOGIN_GETSKILLS": return GetDocument("Skills");
                //case "LOGIN_GETPLAYERSTATS": return GetDocument("PlayerStats");
                //case "LOGIN_GETQUESTS": return GetDocument("Quests");
                //case "LOGIN_GETSTATISTICS": return GetDocument("Statistics");
                //case "LOGIN_GETACHIEVEMENTS": return GetDocument("Achievements");
                //case "LOGIN_GETACHIEVEMENTMILESTONES": return GetDocument("AchievementMilestones");
                //case "LOGIN_GETACHIEVEMENTSTATS": return GetDocument("AchievementStats");
                //Seeker
                //case "SEEKER_INVENTORY_UPDATE": return SeekerInventory_Update(command.GetRange(1, command.Count - 1));
                case "SEEKER": return Seeker_Update(command.GetRange(1, command.Count - 1));

                //Conqueror
                case "CONQUEROR": return Conqueror_Update(command.GetRange(1, command.Count - 1));

                //Mastermind
                case "MASTERMIND": return Mastermind_Update(command.GetRange(1, command.Count - 1));


                //Incremental
                case "INCREMENTAL": return UpdateInc(command.GetRange(1, command.Count - 1));



                //Survival

                //Achievements
                //case "ACHIEVEMENT_UPDATE": return Achievement_Update(command.GetRange(1, command.Count - 1));
                //case "ACHIEVEMENT_MILESTONE_UPDATE": return Achievement_MilestoneUpdate(command.GetRange(1, command.Count - 1));
                //case "ACHIEVEMENT_STAT_UPDATE": return Achievement_StatUpdate(command.GetRange(1, command.Count - 1));
                //Bonus codes
                //case "BONUSCODE_ACTIVATE": return BonusCode_Activate(command.GetRange(1, command.Count - 1));
                //Equipment management
                //case "EQUIPMENT_ADD": return Equipment_Add(command.GetRange(1, command.Count - 1));
                //case "EQUIPMENT_REMOVE": return Equipment_Remove(command.GetRange(1, command.Count - 1));
                //Item management
                //case "ITEM_ADD": return Item_Add(command.GetRange(1, command.Count - 1));
                //case "ITEM_REMOVE": return Item_Remove(command.GetRange(1, command.Count - 1));
                //Skills
                //case "SKILLS_UPDATE": return Skills_Update(command.GetRange(1, command.Count - 1));
                //Player stats
                //case "PLAYERSTAT_UPDATE": return PlayerStat_Update(command.GetRange(1, command.Count - 1));
                //Statistics
                case "STATISTIC_UPDATE": return Statistics_Update(command.GetRange(1, command.Count - 1));
                //Quests
                //case "QUEST_UPDATESTATE": return Quest_UpdateState(command.GetRange(1, command.Count - 1));
                //case "QUEST_UPDATECOMPLETIONCOUNT": return Quest_UpdateCompletionCount(command.GetRange(1, command.Count - 1));
                //case "QUEST_UPDATEOBJECTIVE": return Quest_UpdateObjective(command.GetRange(1, command.Count - 1));
                //case "QUEST_CLEAROBJECTIVES": return Quest_ClearObjectives(command.GetRange(1, command.Count - 1));
                //Recipes
                //case "RECIPE_UPDATE": return Recipe_Update(command.GetRange(1, command.Count - 1));
                //Misc
                case "PING": Send("PONG"); return 0;//_tcp.SendMessage(0, "PONG"); return 0;
                //case "TEST": _tcp.SendMessage(0, command[1].ToUpper()); _udp.Send(System.Text.Encoding.ASCII.GetBytes("HELLO!"), 56); return 0;
                case "QUIT": return -6;
                //case "UDP_READY": return MultiplayerIsReady();
                //Command not found
                default: logger.Debug(LogType.WEBSOCKET, "Command " + command[0] + " not found!"); return -5;
            }
        }

        private int Mastermind_Update(List<string> details)
        {
            _database.UpdateMm(details);
            return 0;
        }

        private int Conqueror_Update(List<string> details)
        {
            _database.UpdateConq(details);
            return 0;
        }

        private int Seeker_Update(List<string> details)
        {
            _database.UpdateSeeker(details);
            return 0;
        }

        private int UpdateInc(List<string> details)
        {
            _database.UpdateInc(details);
            return 0;
        }

        //Used to retrieve large documents: Inventory, Quests, and any future documents
        private int GetDocument(string documentName)
        {
            string document = _database.GetDocument(documentName);
            Action<bool> IsSendCompleted = delegate { LogSentDocument(document); };
            if (document != null)
            {
                //int udpStatus = MultiplayerIsReady();
                //SendMessage(int status, string doc)
                //_tcp.SendMessage(0, document);//(udpStatus > 0 ? udpStatus : 0), document);
                //Action<bool> test;

                SendAsync(document, IsSendCompleted);
                return 0;
            }
            //Else return error
            return -1;
        }

        private void LogSentDocument(string doc)
        {
            logger.Log("Sent document " + doc + " to " + ID);
        }

        //*
        //Sends all data required for login as one big document
        private int GetLoginDocument()
        {
            string loginDocument = _database.GetLoginDocument();
            Action<bool> completed = delegate { LogSentLoginDoc(loginDocument); };
            if (loginDocument != null)
            {
                //int udpStatus = MultiplayerIsReady();
                //_tcp.SendMessage(0, loginDocument);//((udpStatus > 0 ? udpStatus : 0), loginDocument);

                SendAsync(loginDocument, completed);
                return 0;
            }
            //Else return error
            return -1;
        }//*/

        private void LogSentLoginDoc(string loginDoc)
        {
            logger.Log("Sent LoginDocument " + loginDoc + " to " + ID);
        }

        /* Log user in. Returns an int depending on what occurred.
         *  0   success
         * -1   username not found
         * -2   password does not match
         */
        //If login is successful, it creates a UDP connection (as the login token is the required unique identifier)
        //*
        private int LogIn(string inputUsername, string inputPassword, string inputVersion)
        {
            if (!inputVersion.Equals(Server.AvailableClientVersion))
            {
                Send("ERROR: version out of date");
                return -3;

            }
            Action<bool> iscompleted = delegate { LogLoginResponse(inputUsername, false); };
            int status = 0;
            string loginCookie = _database.LogIn(inputUsername, inputPassword, ref status);
            //Error: Username not found
            if (status == -1)
            {
                //_tcp.SendMessage(-1, "ERROR");
                //SendAsync("ERROR: username not found", iscompleted);
                Send("ERROR: username not found");
                return -1;
            }
            //Error: Password incorrect
            else if (status == -2)
            {
                //_tcp.SendMessage(-2, "ERROR");
                //SendAsync("ERROR: incorrect password", iscompleted);
                Send("ERROR: incorrect password");
                return -2;
            }
            //Login successful!
            //_tcp.Username = inputUsername;
            //Get data required for player roster (UDP)
            //_level = _database.Roster_GetPlayerLevel();
            //_spriteIndex = _database.Roster_GetSpriteIndex();
            //_characterName = _database.Roster_GetPlayerName();
            //UpdateRoster();

            //UpdatePosition(1.0f, 2.0f, 1);

            //Login successful, send the player the login cookie
            Username = inputUsername;
            //_udp.Username = inputUsername;
            //_tcp.SendMessage(0, loginCookie);
            //iscompleted = delegate { LogLoginResponse(Username, true); }; //used for sendasync call below
            Send("login cookie: " + loginCookie);//, iscompleted); //--previous was sendasync()
            LogLoginResponse(Username, true);

            //Let main thread know to verify login cookies from now on
            _loggedIn = true;

            //Notify the server and have it assign the client to a world
            //OnLogin(this, new EventArgs());

            return 0;
        }//*/

        private void LogLoginResponse(string username, bool issuccess)
        {
            if (issuccess)
                logger.Log(username + " logged in and cookie sent");
            else
                logger.Log(username + " failed to log in");
        }
       

        //Update achievement
        /*private int Achievement_Update(List<string> details)
        {
            return _database.Achievement_Update(details);
        }

        //Update achievement milestone
        private int Achievement_MilestoneUpdate(List<string> details)
        {
            return _database.Achievement_MilestoneUpdate(details);
        }

        //Update achievement statistic
        private int Achievement_StatUpdate(List<string> details)
        {
            return _database.Achievement_StatUpdate(details);
        }*/

        //Activate bonus code
        /*private int BonusCode_Activate(List<string> details) {
            return _database.ActivateBonusCode(details[0]);
		}*/

        //Add item to equipment
        /*private int Equipment_Add(List<string> details)
        {
            return _database.Equipment_Add(details);
        }
        
        //Remove equipment from given equipment slot
        private int Equipment_Remove(List<string> details)
        {
            return _database.Equipment_Remove(details);
        }
        
        //Add item to inventory
        private int Item_Add(List<string> details)
        {
            return _database.Item_Add(details);
        }
        
        //Remove item from inventory
        private int Item_Remove(List<string> details)
        {
            return _database.Item_Remove(details);
        }
        
        //Update specific player stat
        private int PlayerStat_Update(List<string> details)
        {
            return _database.PlayerStat_Update(details);
        }
        
        //Updates skill slot assignment (-1 means not assigned)
		private int Skills_Update(List<string> details) {
            return _database.Skills_Update(details);
		}*/

        //Updates statistic in both current login document and overall statistics document
        private int Statistics_Update(List<string> details)
        {
            return _database.Statistics_Update(details);
        }
        
        //Update quest state or create quest document if it doesn't exist
        /*private int Quest_UpdateState(List<string> details)
        {
            return _database.Quest_UpdateState(details);
        }
        
        //Update quest number of completions
        private int Quest_UpdateCompletionCount(List<string> details)
        {
            return _database.Quest_UpdateCompletionCount(details);
        }
        
        //Update quest objective and move it into the appropriate list (Active, Completed, or none)
        private int Quest_UpdateObjective(List<string> details)
        {
            return _database.Quest_UpdateObjective(details);
        }
        
        //Clears all quest objectives, useful for restarting a quest
        private int Quest_ClearObjectives(List<string> details)
        {
            return _database.Quest_ClearObjectives(details);
        }

        //Update recipe state
        private int Recipe_Update(List<string> details)
        {
            return _database.Recipe_Update(details);
        }*/

        //-----------------------//
        //  Multiplayer Section  //
        //-----------------------//

        /*/
        //Get if multiplayer is ready
        private int MultiplayerIsReady()
        {
            if (UdpConnectionStatus == 0)
                return -1;
            if (UdpConnectionStatus == 1)
            {
                UdpConnectionStatus++;
                logger.Debug(LogType.UDP, "UDP ready!");
                return 5;
            }
            return 0;
        }

        //Get position - called from World
        public Tuple<int, byte[]> GetPosition()
        {
            //No lock needed as this is a reference type
            return _positionTuple;
        }

        //Update position - called from UDP receive thread
        public void UpdatePosition(float xCoord, float yCoord, int sceneIndex)
        {
            //First update the position array
            byte[] newPosArray = new byte[12];
            //Player ID
            byte[] numBuf = BitConverter.GetBytes(_playerId);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, newPosArray, 0, 4);
            //X coord
            numBuf = BitConverter.GetBytes(xCoord);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, newPosArray, 4, 4);
            //Y coord
            numBuf = BitConverter.GetBytes(yCoord);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, newPosArray, 8, 4);

            //Save position
            //This is an atomic operation, so no locks are needed
            //_logger.Debug("Position update for " + Username, 3);
            _positionTuple = new Tuple<int, byte[]>(sceneIndex, newPosArray);
        }

        //Get infinity position - called from World
        public byte[] GetInfinityPosition()
        {
            return _infinityPosition;
        }

        //Generate infinity position - generates a special position message
        //that tells clients the player has left the level
        private byte[] GenerateInfinityPosition()
        {
            //First update the position array
            byte[] infinityArray = new byte[12];
            //Player ID
            byte[] numBuf = BitConverter.GetBytes(_playerId);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, infinityArray, 0, 4);
            //X coord
            numBuf = BitConverter.GetBytes(float.PositiveInfinity);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, infinityArray, 4, 4);
            //Y coord
            numBuf = BitConverter.GetBytes(float.PositiveInfinity);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, infinityArray, 8, 4);
            //Return array
            return infinityArray;
        }

        //Gets message - called from world
        public byte[] GetMessage()
        {
            lock (_textLock)
            {
                if (_textMessageSendCount > 0)
                {
                    _textMessageSendCount--;
                }
                if (_textMessageSendCount == 0)
                {
                    _textMessage = null;
                }
            }
            return _textMessage;
        }

        //Set text message - called from UDP receive thread
        public void UpdateTextMessage(string message)
        {
            List<byte> messageBytes = new List<byte>();
            //Player ID
            byte[] numBuf = BitConverter.GetBytes(_playerId);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            messageBytes.AddRange(numBuf);
            //Message length
            numBuf = BitConverter.GetBytes(_encoder.GetByteCount(message));
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            messageBytes.AddRange(numBuf);
            //Message
            messageBytes.AddRange(_encoder.GetBytes(message));

            //Must save these in a thread-safe way
            lock (_textLock)
            {
                _textMessage = messageBytes.ToArray();
                _textMessageSendCount = _textMessageSendingLimit;
            }
        }

        //Get roster - called from World
        public byte[] GetRoster()
        {
            return _rosterArray;
        }

        //Set roster array - called from UDP receive thread and from TCP main thread
        public void UpdateRoster()
        {
            //This roster isn't a fixed length, so we'll use a list and convert to an array at the end
            List<byte> newRoster = new List<byte>();
            //Player ID (doesn't ever change)
            byte[] numBuf = BitConverter.GetBytes(_playerId);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            newRoster.AddRange(numBuf);
            //Level
            numBuf = BitConverter.GetBytes(_level);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            newRoster.AddRange(numBuf);
            //Sprite ID
            numBuf = BitConverter.GetBytes(_spriteIndex);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            newRoster.AddRange(numBuf);
            //Convert name to byte array
            byte[] nameArray = World.encoder.GetBytes(_characterName);
            //Name length
            numBuf = BitConverter.GetBytes(nameArray.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            newRoster.AddRange(numBuf);
            //Name
            newRoster.AddRange(nameArray);

            _rosterArray = newRoster.ToArray();
        }//*/

        //Logout message, sent on, you guessed it, logout!
        private byte[] GenerateLogoutMessage()
        {
            //Logout message
            byte[] logoutMessage = new byte[16];
            //Player ID
            byte[] numBuf = BitConverter.GetBytes(_playerId);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, logoutMessage, 0, 4);
            //Level = -1 (this indicates logout)
            numBuf = BitConverter.GetBytes(-1);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, logoutMessage, 4, 4);
            //Sprite index = -1 (because why not
            numBuf = BitConverter.GetBytes(-1);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, logoutMessage, 8, 4);
            //Name length = 0
            numBuf = BitConverter.GetBytes(0);
            if (BitConverter.IsLittleEndian) Array.Reverse(numBuf);
            Array.Copy(numBuf, 0, logoutMessage, 12, 4);
            //Return array
            return logoutMessage;
        }

        //Called once, when logging out
        /*public byte[] GetLogoutMessage()
        {
            return _logoutMessage;
        }*/

        //Send via UDP
        /*/
        void UdpSend(byte[] message, int messageType)
        {
            _udp.Send(message, messageType);
        }//*/
    }
}
