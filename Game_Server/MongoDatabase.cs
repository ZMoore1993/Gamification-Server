using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Game_Server
{
    //Mongo Database, implements the Database interface
    class Database_Mongo : Database
    {
        //Logger
        private Logger logger;

        //Lots of documents and stuff
        private MongoCollection<BsonDocument> PlayerCollection;
        private BsonDocument PlayerDocument; 
        private BsonArray PlayerLogins; //all the player's logins
		private BsonDocument Login; //current login info
        private BsonValue PlayersSeeker;
		private BsonValue PlayersConqueror;
		private BsonValue PlayersMastermind;
		private BsonValue PlayersIncremental;
        private BsonDocument PlayerAchievements;
        //private BsonDocument PlayerMilestones;
        private BsonDocument PlayerAchievementStats;
        //private BsonDocument PlayerRecipes;
        private BsonDocument PlayerPlayerStats;     //Silly name, I know
        private BsonDocument PlayerStatistics;      //It's because this also exists
        //private List<Bonus> Bonuses;
        private BsonArray Player;
		
		

        //Other stuff
        private string Username;
        private bool RosterChanged;

        //Constructor
        public Database_Mongo(MongoCollection<BsonDocument> playerCollection)
        {
            PlayerCollection = playerCollection;
            logger = Server.logger;
            RosterChanged = false;
        }

        //Creates account for player
        public void CreateAccount(List<string> details)
        {
            BsonDocument newPlayer = new BsonDocument();
            newPlayer.SetElement(new BsonElement("Username",details[0]));

            //hash password
            string hashed_password = EncryptPassword(details[1]);
            newPlayer.SetElement(new BsonElement("Password", hashed_password));
            newPlayer.Set("Logins", new BsonArray());
            newPlayer.SetElement(new BsonElement("Incremental", ""));
            newPlayer.SetElement(new BsonElement("Seeker", ""));
            newPlayer.SetElement(new BsonElement("Conqueror", ""));
            newPlayer.SetElement(new BsonElement("Mastermind", ""));

            PlayerCollection.Save(newPlayer);
        }

        //Encrypts password
        private string EncryptPassword(string password)
        {

            //16 bit salt
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            //20 bit hash (MAKE SURE the number of iterations matches the server code!)
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            //Combine salt and hash in to single 36 bit array (salt goes first)
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            //Convert to Base64 string
            return Convert.ToBase64String(hashBytes);

        }

        //Saves the player document
        public void Save()
        {
            if (PlayerDocument != null) PlayerCollection.Save(PlayerDocument);
        }

        public string LogIn(string inputUsername, string inputPassword, ref int status)
        {
            //Check if the player exists
            PlayerDocument = PlayerCollection.FindOne(new QueryDocument("Username", inputUsername));
            //If username is not found in PlayerCollection, it sets PlayerDocument to null, return false in this case
            if (PlayerDocument == null)
            {
                status = -1;  //Error: Username not found
                return null;
            }
            //Verify password
            //Get the saved hash and separate it into separate hash and salt arrays
            string savedHash = PlayerDocument.GetElement("Password").Value.ToString();
            byte[] hashBytes = Convert.FromBase64String(savedHash);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            //Compute the hash for the entered password
            var pbkdf2 = new Rfc2898DeriveBytes(inputPassword, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            //Compare byte by byte. If any bytes don't match, the passwords are incorrect
            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                {
                    status = -2;
                    return null;
                }
            }

            //Credentials match, so log player in
            Username = inputUsername;
            string loginCookie;

            //Set login time
            if (!PlayerDocument.Contains("Logins"))
                PlayerDocument.Set("Logins", new BsonArray());
            PlayerLogins = PlayerDocument.GetValue("Logins").AsBsonArray;
            //int LoginIndex = PlayerLogins.Count;
            logger.Debug(LogType.DATABASE, DateTime.Now + ": user " + Username + " logged in");
			
			Login = new BsonDocument();
			Login.SetElement(new BsonElement("LoginTime", System.DateTime.Now));
            PlayerLogins.Add(Login);
            //Login = PlayerLogins[LoginIndex] as BsonDocument;
            //Login.SetElement(new BsonElement("LoginTime", System.DateTime.Now));

            //Generate unique login cookie
            byte[] lCookie = new byte[16];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(lCookie);
            loginCookie = Convert.ToBase64String(lCookie);
            if (!PlayerDocument.Contains("LoginCookie"))
                PlayerDocument.SetElement(new BsonElement("LoginCookie", loginCookie));
            SetElement(PlayerDocument, "LoginCookie", loginCookie);

            //Save document now, to store the new login cookie
            PlayerCollection.Save(PlayerDocument);

            //Initialize player's games
            if (!PlayerDocument.Contains("Seeker"))
                PlayerDocument.SetElement(new BsonElement("Seeker", ""));
            PlayersSeeker = PlayerDocument.GetValue("Seeker");
			
			if (!PlayerDocument.Contains("Conqueror"))
				PlayerDocument.SetElement(new BsonElement("Conqueror", ""));
            PlayersConqueror = PlayerDocument.GetValue("Conqueror");
			
			if (!PlayerDocument.Contains("Mastermind"))
				PlayerDocument.SetElement(new BsonElement("Mastermind", ""));
            PlayersMastermind = PlayerDocument.GetValue("Mastermind");

            /*
			if (!PlayerDocument.Contains("Survival"))
				PlayerDocument.Set("Survival", new BsonDocument());
			PlayersSurvival = PlayerDocument.GetValue("Survival").AsBsonDocument;*/

            if (!PlayerDocument.Contains("Incremental"))
            {
                //PlayerDocument.Set("Incremental", "");
                IncrementalData data = new IncrementalData();
                data.stamina.cur = 0;
                PlayerDocument.Set("Incremental",JsonConvert.SerializeObject(data));
            }
            PlayersIncremental = PlayerDocument.GetValue("Incremental");

            //Initialize player's list of achievements
            /*
            if (!PlayerDocument.Contains("Achievements"))
                PlayerDocument.Set("Achievements", new BsonDocument());
            PlayerAchievements = PlayerDocument.GetValue("Achievements").AsBsonDocument;

            //Initialize player's list of achievement milestones
            if (!PlayerDocument.Contains("AchievementMilestones"))
                PlayerDocument.Set("AchievementMilestones", new BsonDocument());
            PlayerMilestones = PlayerDocument.GetValue("AchievementMilestones").AsBsonDocument;

            //Initialize player's list of achievement stats
            if (!PlayerDocument.Contains("AchievementStats"))
                PlayerDocument.Set("AchievementStats", new BsonDocument());
            PlayerAchievementStats = PlayerDocument.GetValue("AchievementStats").AsBsonDocument;

            //Initialize player's list of bonus codes
            if (!PlayerDocument.Contains("BonusCodes"))
                PlayerDocument.Set("BonusCodes", new BsonArray());
            Bonuses = new List<Bonus>();
            PlayerBonusCodes = PlayerDocument.GetValue("BonusCodes").AsBsonArray;

            //Initialize player's inventory
            if (!PlayerDocument.Contains("Inventory"))
                PlayerDocument.Set("Inventory", new BsonArray());
            PlayerInventory = PlayerDocument.GetValue("Inventory").AsBsonArray;

            //Initialize recipes
            if (!PlayerDocument.Contains("Recipes"))
                PlayerDocument.Set("Recipes", new BsonDocument());
            PlayerRecipes = PlayerDocument.GetValue("Recipes").AsBsonDocument;

            //*/

            //Return the login cookie
            return loginCookie;
        }

        

        //Logs the player out. If it was not due to a mismatched key, save the document as well
        public void Logout(bool keyError)
        {
            if (Login != null) Login.SetElement(new BsonElement("LogoutTime", DateTime.Now));
            if (!keyError && PlayerDocument != null) PlayerCollection.Save(PlayerDocument);
        }

        //Gets the current login key
        public string GetLoginKey()
        {
            return PlayerCollection.FindOne(new QueryDocument("Name", Username)).GetValue("LoginCookie").AsString;
        }

        //Get document as JSON string. Return null if document isn't found
        public string GetDocument(string documentName)
        {
            BsonValue document = PlayerDocument.GetValue(documentName);
            if (document == null) return null;
            return document.ToJson();
        }

        
        //Gets all data required for login as a single document
        public string GetLoginDocument()
        {
            //Get number of achievement rewards redeemed today
            //int redeemedCount = 0;
            //string statName = "AchievementReward/" + DateTime.Now.ToShortDateString();
            //if (PlayerPlayerStats.Contains(statName)) redeemedCount = PlayerPlayerStats[statName].AsInt32;

            //Construct a BSONDocument with each component as a child
            BsonDocument loginDocument = new BsonDocument();
            /*loginDocument.Set("Achievements", PlayerAchievements);
            loginDocument.Set("AchievementMilestones", PlayerMilestones);
            loginDocument.Set("AchievementStats", PlayerAchievementStats);
            loginDocument.Set("AchievementRewardsCollectedToday", redeemedCount);
            loginDocument.Set("BonusCodes", GetActiveBonusCodes()); 
            loginDocument.Set("Equipment", PlayerEquipment);
            loginDocument.Set("Inventory", PlayerInventory);
            loginDocument.Set("PlayerStats", PlayerPlayerStats);
            loginDocument.Set("Quests", PlayerQuests);
            loginDocument.Set("Skills", PlayerSkills);
            loginDocument.Set("Statistics", PlayerStatistics);
            loginDocument.Set("Recipes", PlayerRecipes);*/
			loginDocument.Set("Seeker", PlayersSeeker);
			loginDocument.Set("Conqueror", PlayersConqueror);
			loginDocument.Set("Mastermind", PlayersMastermind);
			//loginDocument.Set("Survival", PlayersSurvival);
			loginDocument.Set("Incremental", PlayersIncremental);
            return loginDocument.ToJson();
        }


        /****************************************************************************
        --------------------------- SEEKER SECTION ---------------------------------
        *****************************************************************************
        -This section is for seeker minigame: inventory add, inventory delete, story update, quest log (needs work), functions to update town population, towns visited, player stats, current town
        ****************************************************************************/

       //updates seeker game
       public void UpdateSeeker(List<string> details)
        {
            SetElement(PlayerDocument, "Seeker", details[0]);
        }
		
	
		
		/****************************************************************************
        --------------------------- CONQUEROR SECTION ---------------------------------
        *****************************************************************************
        -This section is for conqueror minigame: update stats, updating equipped gun and ability
        ****************************************************************************/
		
        public void UpdateConq(List<string> details)
        {
            SetElement(PlayerDocument, "Conqueror", details[0]);
        }


		
		
		/****************************************************************************
        --------------------------- MASTERMIND SECTION ---------------------------------
        *****************************************************************************
        -This section is for mastermind minigame: update hint count, update current board time
        ****************************************************************************/
		
        public void UpdateMm(List<string> details)
        {
            SetElement(PlayerDocument, "Mastermind", details[0]);
        }

		
        /****************************************************************************
        --------------------------- INCREMENTAL SECTION ---------------------------------
        *****************************************************************************
        -This section is for incremental minigame: update coins, update progress bar, update player's level, update ascension level, update ascension pts, add/remove quests, add/remove achievements, update stamina
        ****************************************************************************/
        //updates incremental game's string
        //format: string incrementalDocumentString
        
        public int UpdateInc(List<string> details)
        {
            SetElement(PlayerDocument, "Incremental", details[0]);
            return 0;
        }
               
        
        

        //Format: int id, int state
        /*public int Achievement_MilestoneUpdate(List<string> details)
        {
            if (!PlayerMilestones.Contains(details[0]))
            {
                SetElement(PlayerMilestones, details[0], 0);
            }
            else
            {
                int state = int.Parse(details[1]);
                SetElement(PlayerMilestones, details[0], state);
                Server.logger.Debug(LogType.DATABASE, "Setting milestone " + details[0] + " to " + details[1]);
                //If this is being set to stage 2, record it
                if (state == 2)
                {
                    string statName = "AchievementReward/" + DateTime.Now.ToShortDateString();
                    int currentAmount = 0;
                    if (PlayerPlayerStats.Contains(statName)) currentAmount = PlayerPlayerStats[statName].AsInt32;
                    PlayerStat_Update(new List<string>() { statName, (currentAmount + 1).ToString() });
                }
            }

            //Always succeeds
            return 0;
        }*/

        //Format: int id, int state
        public int Achievement_StatUpdate(List<string> details)
        {
            if (!PlayerAchievementStats.Contains(details[0]))
            {
                SetElement(PlayerAchievementStats, details[0], 0);
            }
            else
            {
                SetElement(PlayerAchievementStats, details[0], int.Parse(details[1]));
            }

            //Always succeeds
            return 0;
        }


        /********************************************************************************
        ------------------------------- BONUS CODES -------------------------------------
        *********************************************************************************
        - Players get bonus codes from going to class. This section will be expanded upon later.
        ********************************************************************************/

        //Stores activated bonus code
        /*public int ActivateBonusCode(string code)
        {
            BsonDocument newCode = new BsonDocument();
            SetElement(newCode, "Code", code.ToUpper());
            SetElement(newCode, "Time", System.DateTime.Now);
            PlayerBonusCodes.Add(newCode);

            //Always succeeds
            return 0;
        }

        private BsonArray GetActiveBonusCodes()
        {
            try
            {
                //Reload file
                using (StreamReader sr = new StreamReader("BonusCodes.csv"))
                {
                    //Reset Bonuses list
                    Bonuses.Clear();

                    CSVHelper csv = new CSVHelper(sr.ReadToEnd(), ",");
                    foreach (string[] line in csv)
                    {
                        //Start time: year,month,day,hour,minute
                        int sY = int.Parse(line[0]);
                        int sM = int.Parse(line[1]);
                        int sD = int.Parse(line[2]);
                        int sH = int.Parse(line[3]);
                        int sMi = int.Parse(line[4]);
                        //End time: year,month,day,hour,minute
                        int eY = int.Parse(line[5]);
                        int eM = int.Parse(line[6]);
                        int eD = int.Parse(line[7]);
                        int eH = int.Parse(line[8]);
                        int eMi = int.Parse(line[9]);
                        //Bonus code
                        string bonusCode = line[10];
                        //Bonus itself - rest of strings
                        List<string> bonuses = new List<string>();
                        for (int i = 11; i < line.Length; i++)
                        {
                            bonuses.Add(line[i]);
                        }
                        //Create and add bonus
                        Bonuses.Add(new Bonus(new DateTime(sY, sM, sD, sH, sMi, 0, DateTimeKind.Local),
                            new DateTime(eY, eM, eD, eH, eMi, 0, DateTimeKind.Local), bonusCode, bonuses));
                    }
                }
            }
            catch(Exception ex)
            {
                Server.logger.Debug(LogType.ERROR, "!!!! Unable to read from bonus codes file");
                Server.logger.Debug(LogType.ERROR, ex.Message);
                Server.logger.Debug(LogType.ERROR, ex.StackTrace);
            }

            //Create a BsonArray that contains every bonus code that is currently active and hasn't been used
            BsonArray bonusCodeDocument = new BsonArray();
            foreach(Bonus bonus in Bonuses)
            {
                if(bonus.Active() && !BonusCodeUsed(bonus.Code))
                {
                    BsonDocument newDoc = new BsonDocument();
                    SetElement(newDoc, "Code", bonus.Code);
                    BsonArray array = new BsonArray();
                    foreach(string str in bonus.Rewards)
                    {
                        array.Add(str);
                    }
                    SetElement(newDoc, "Bonus", array);
                    bonusCodeDocument.Add(newDoc);
                }
            }
            return bonusCodeDocument;
        }

        private bool BonusCodeUsed(string code)
        {
            foreach(BsonDocument doc in PlayerBonusCodes)
            {
                if (doc["Code"].AsString.ToLower() == code.ToLower())
                {
                    return true;
                }
            }
            return false;
        }*/


        /****************************************************************************
        ----------------------- PLAYER SKILLS SECTION -------------------------------
        *****************************************************************************
        -This section is for saving and loading the player's skills
        ****************************************************************************/

        //Format:	int skillId, int slotNumber
        //Updates skill slot assignment (-1 means not assigned)
        /*public int Skills_Update(List<string> details)
        {
            int skillId = int.Parse(details[0]);
            int slotNumber = int.Parse(details[1]);

            //Update it if it already exists
            foreach (BsonDocument skill in PlayerSkills)
            {
                if (skill.GetValue("ID").AsInt32 == skillId)
                {
                    SetElement(skill, "Slot", slotNumber);
                    return 0;
                }
            }
            //Create skill document if it doesn't
            BsonDocument newSkill = new BsonDocument();
            SetElement(newSkill, "ID", skillId);
            SetElement(newSkill, "Slot", slotNumber);
            PlayerSkills.Add(newSkill);

            return 0;
        }*/


        /****************************************************************************
        --------------------------- RECIPES SECTION ---------------------------------
        *****************************************************************************
        -This section is for saving and loading the player's recipes
        ****************************************************************************/

        //Format:   int recipeId, int state
        //Updates recipe state
        /*public int Recipe_Update(List<string> details)
        {
            int state = int.Parse(details[1]);
            SetElement(PlayerRecipes, details[0], state);

            //Always succeeds
            return 0;
        }*/


        /****************************************************************************
        ------------------- STATISTIC COLLECTION SECTION ----------------------------
        *****************************************************************************
        -This section holds a collection of methods that are responsible for interacting
        with the database for the purpose of collecting statistics.
        ****************************************************************************/

        //Format:   string stat, <T> value
        //Updates statistic in both current login document and overall statistics document
        public int Statistics_Update(List<string> details)
        {
            string stat = details[0];
            //Try parsing as an int
            int intValue = 0;
            if (int.TryParse(details[1], out intValue))
            {
                AddToElement(Login, stat, intValue);
            }
            //If more data was defined, add it to the PlayerStatistics document
            if (details.Count > 2)
            {
                AddToElement(PlayerStatistics, details[2], intValue);
            }
            else
            {
                AddToElement(PlayerStatistics, stat, intValue);
            }

            //Always succeeds
            return 0;
        }


        /**************************************************************************
        ------------------------ QUEST SECTION ------------------------------------
        ***************************************************************************
        -This section is for loading the player's current quest progress and saving
        any quest changes (objective completions, quest completions, etc) to the
        database
        **************************************************************************/

        //Format:   double questID, int state
        //Update quest state or create quest document if it doesn't exist
        /*public int Quest_UpdateState(List<string> details)
        {
            double questId = double.Parse(details[0]);
            int state = int.Parse(details[1]);
            //Update it if it already exists
            foreach (BsonDocument quest in PlayerQuests)
            {
                if (quest.GetValue("ID").AsDouble == questId)
                {
                    quest.SetElement(new BsonElement("State", state));
                    return 0;
                }
            }
            //Create quest document if it doesn't
            BsonDocument q = new BsonDocument();
            SetElement(q, "ID", questId);
            SetElement(q, "State", state);
            SetElement(q, "NumberOfCompletions", 0);
            SetElement(q, "ActiveObjectives", new BsonArray());
            SetElement(q, "CompletedObjectives", new BsonArray());
            PlayerQuests.Add(q);

            //Always succeeds
            return 0;
        }

        //Format:   double questID, int numCompletions
        //Update quest number of completions
        public int Quest_UpdateCompletionCount(List<string> details)
        {
            double questId = double.Parse(details[0]);
            int numberOfCompletions = int.Parse(details[1]);

            foreach (BsonDocument quest in PlayerQuests)
            {
                if (quest.GetValue("ID").AsDouble == questId)
                {
                    quest.SetElement(new BsonElement("NumberOfCompletions", numberOfCompletions));
                    return 0;
                }
            }
            //Quest was not found
            return -1;
        }

        //Format:   double questID, double objectiveID, char from, char to
        //Update quest objective and move it into the appropriate list (Active, Completed, or none)
        public int Quest_UpdateObjective(List<string> details)
        {
            double questId = double.Parse(details[0]);
            double objectiveId = double.Parse(details[1]);
            char from = char.Parse(details[2]);
            char to = char.Parse(details[3]);

            foreach (BsonDocument quest in PlayerQuests)
            {
                if (quest.GetValue("ID").AsDouble == questId)
                {
                    //Remove from existing list
                    //Don't need to remove from 'i'
                    if (from == 'a')
                    {
                        quest.GetValue("ActiveObjectives").AsBsonArray.Remove(objectiveId);
                    }
                    if (from == 'c')
                    {
                        quest.GetValue("CompletedObjectives").AsBsonArray.Remove(objectiveId);
                    }
                    //Add to new list
                    //Don't need to add to 'i'
                    if (to == 'a')
                    {
                        quest.GetValue("ActiveObjectives").AsBsonArray.Add(objectiveId);
                    }
                    if (to == 'c')
                    {
                        quest.GetValue("CompletedObjectives").AsBsonArray.Add(objectiveId);
                    }
                    return 0;
                }
            }
            //Quest not found
            return -1;
        }

        //Format:   double questID
        //Clears all quest objectives, useful for restarting a quest
        public int Quest_ClearObjectives(List<string> details)
        {
            double questId = double.Parse(details[0]);

            foreach (BsonDocument quest in PlayerQuests)
            {
                if (quest.GetValue("ID").AsDouble == questId)
                {
                    quest.GetValue("ActiveObjectives").AsBsonArray.Clear();
                    quest.GetValue("CompletedObjectives").AsBsonArray.Clear();
                    return 0;
                }
            }
            //Quest not found
            return -1;
        }
		*/


        /**************************************************************************
        ------------------------ ROSTER SECTION -----------------------------------
        ***************************************************************************
        -This section is for loading the player's roster related variables
        **************************************************************************/

        public bool Roster_Changed()
        {
            return RosterChanged;
        }

        public int Roster_GetPlayerLevel() {
            RosterChanged = false;
            return PlayerPlayerStats.GetValue("Level").AsInt32;
        }

        public int Roster_GetSpriteIndex()
        {
            RosterChanged = false;
            if (!PlayerPlayerStats.Contains("SpriteIndex"))
                PlayerPlayerStats.Set("SpriteIndex", 0);
            return PlayerPlayerStats.GetValue("SpriteIndex").AsInt32;
        }

        public string Roster_GetPlayerName()
        {
            RosterChanged = false;
            if (!PlayerPlayerStats.Contains("PlayerName"))
                PlayerPlayerStats.Set("PlayerName", "Player");
            return PlayerPlayerStats.GetValue("PlayerName").AsString;
        }

        //=====================//
        //  Private functions  //
        //=====================//

        //Default player stats
        //They are set here because it greatly simplifies the account creation process

        //private BsonDocument GetDefault

        //Sets default player stats if none are found. SET DEFAULT VALUES HERE
        private BsonDocument GetDefaultPlayerStats()
        {
            return new BsonDocument
            {
                { "PAtk" , 10 },
                { "MAtk" , 10 },
                { "PDef" , 10 },
                { "MDef" , 10 },
                { "Speed" , 10 },
                { "XP" , 0 },
                { "Level" , 1 },
                { "Gold" , 0 },
                { "CurrentScene", "Tutorial Clearing" },
                { "CurrentSceneSpawn", 0 },
                { "CurrentPositionX", -9.93},
                { "CurrentPositionY", 1.45},
                { "CurrentSceneManager", ""},
                { "SkillPoints", 0 },
                { "StatPoints", 0 },
                { "PlayerName", PlayerDocument.GetValue("Name").AsString },
                { "SpriteIndex", 0 }
            };
        }

        //Sets default statistics if none are found. SET DEFAULT VALUES HERE
        private BsonDocument GetDefaultStatistics()
        {
            return new BsonDocument
            {
                { "ItemsPickedUp", 0 },
                { "GoldCollected", 0 },
                { "ItemsCrafted", 0 },
                { "QuestRewardsEarned", 0 },
                { "CombatRewardsEarned", 0 },
                { "EnemiesDefeated", 0 },
                { "NumberOfBattles", 0 },
                { "BattlesWon", 0 },
                { "BattlesLost", 0 },
                { "AttacksExecuted", 0 },
                { "XPGained", 0 },
                { "LevelsGained", 0 },
                { "InstanceSwitches", 0 },
                { "NumberOfClicks", 0 },
                { "QuestsCompleted", 0 }
            };
        }

        //Sets default skills if none are found. SET DEFAULT VALUES HERE
        private void SetDefaultSkills()
        {
			/*
            BsonDocument punch = new BsonDocument
            {
                { "ID", 1 },
                { "Slot", 0 }
            };
            BsonDocument adrenalineRush = new BsonDocument
            {
                { "ID", 19 },
                { "Slot", 1 }
            };

            PlayerSkills = PlayerDocument.GetValue("Skills").AsBsonArray;
            PlayerSkills.Add(punch);
            PlayerSkills.Add(adrenalineRush); //*/
        }

        //Basic database interactions

        //Used to set an element in a document to a value
        private void SetElement(BsonDocument document, string name, BsonValue value)
        {
            document.Set(name, value);
        }

        //Add to element if it exists, or set it to value if it doesn't
        private void AddToElement(BsonDocument document, string name, BsonValue value)
        {
            //Element doesn't exist
            if (!document.Contains(name))
            {
                SetElement(document, name, value);
            }
            else
            {
                BsonValue newValue = document.GetElement(name).Value;
                if (newValue.IsDouble)
                {
                    SetElement(document, name, value.AsDouble + newValue.AsDouble);
                }
                else if (newValue.IsInt32)
                {
                    SetElement(document, name, value.AsInt32 + newValue.AsInt32);
                }
                else if(newValue.IsString)
                {
                    SetElement(document, name, value.AsString);
                }
            }
        }

        public int SetInitialized(List<string> details)
        {
            //SetElement(PlayersSeeker, "Initialized", details[0]);
            return 0;
        }
    }
}
