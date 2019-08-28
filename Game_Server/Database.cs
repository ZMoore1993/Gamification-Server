using System.Collections.Generic;

namespace Game_Server
{
    //Database interface. The function declarations should be straightforward.
    //Specific input formats are specified for each function that needs them
    internal interface Database
    {
        //Creates account for player
        void CreateAccount(List<string> details);

        //Saves the player document
        void Save();

        //Attempts to log player in, returns a login cookie if successful
        string LogIn(string inputUsername, string inputPassword, ref int status);

        //Logs the player out. Save the document if logout isn't occurring because of a key error
        void Logout(bool keyError);

        //Get the current login key
        string GetLoginKey();

        //Get document as JSON string. Return null if document isn't found
        string GetDocument(string documentName);

        //Gets all data needed to login as a single document
        string GetLoginDocument();




        /********************************************************************************
        ------------------------------ ACHIEVEMENTS -------------------------------------
        *********************************************************************************
        - All functions related to tracking statistics
        ********************************************************************************/

        //Format: int id, int state
        //Updates the state of an achievement, milestone, or statistic respectively
        //int Achievement_Update(List<string> details);
        //int Achievement_MilestoneUpdate(List<string> details);
        //int Achievement_StatUpdate(List<string> details);

        /****************************************************************************
        --------------------------- SEEKER SECTION ---------------------------------
        *****************************************************************************
        -This section is for seeker minigame: inventory add, inventory delete, story update, quest log (needs work), functions to update town population, towns visited, player stats, current town
        ****************************************************************************/

        //FORMAT: string seekerSerializedJsonString
        void UpdateSeeker(List<string> details);


        /****************************************************************************
        --------------------------- CONQUEROR SECTION ---------------------------------
        *****************************************************************************
        -This section is for conqueror minigame: update stats, updating equipped gun and ability
        ****************************************************************************/

        //FORMAT: string conquerorSerializedJsonString
        void UpdateConq(List<string> details);

        /****************************************************************************
        --------------------------- MASTERMIND SECTION ---------------------------------
        *****************************************************************************
        -This section is for mastermind minigame: update hint count, update current board time
        ****************************************************************************/

        //FORMAT: string mastermindSerializedJsonString
        void UpdateMm(List<string> details);



        /****************************************************************************
        --------------------------- INCREMENTAL SECTION ---------------------------------
        *****************************************************************************
        -This section is for incremental minigame: update coins, update progress bar, update player's level, update ascension level, update ascension pts, add/remove achievements, update stamina
        ****************************************************************************/
        //used for serialized json strings
        int UpdateInc(List<string> details);
        
		

        /********************************************************************************
        ------------------------------- BONUS CODES -------------------------------------
        *********************************************************************************
        -This section will be expanded upon later.
        ********************************************************************************/

        //Store activated bonus code
        //int ActivateBonusCode(string code);


       


        /****************************************************************************
        ----------------------- PLAYER CHARACTER MANIPULATION -----------------------
        *****************************************************************************
        -This section holds all the functions that interact with the stats of a player.
        ****************************************************************************/

        //Format:   string stat, double/int/float value
        //Update specific player stat
        //int PlayerStat_Update(List<string> details);


        /****************************************************************************
        ----------------------- PLAYER SKILLS SECTION -------------------------------
        *****************************************************************************
        -This section is for saving and loading the player's skills
        ****************************************************************************/

        //Format:	int skillId, int slotNumber
        //Update skill slot assignment (-1 means not assigned)
        //int Skills_Update(List<string> details);


        /****************************************************************************
        --------------------------- RECIPES SECTION ---------------------------------
        *****************************************************************************
        -This section is for saving and loading the player's recipes
        ****************************************************************************/

        //Format:   int recipeId, int state
        //int Recipe_Update(List<string> details);


        /****************************************************************************
        ------------------- STATISTIC COLLECTION SECTION ----------------------------
        *****************************************************************************
        -This section holds a collection of methods that are responsible for interacting
        with the database for the purpose of collecting statistics.
        ****************************************************************************/

        //Format:   string stat, <T> value
        //Update statistic and record it in both the current login session and the overall statistics
        int Statistics_Update(List<string> details);


        /**************************************************************************
        ------------------------ QUEST SECTION ------------------------------------
        ***************************************************************************
        -This section is for loading the player's current quest progress and saving
        any quest changes (objective completions, quest completions, etc) to the
        database
        **************************************************************************/

        //Format:   double questID, int state
        //Update quest state or create quest document if it doesn't exist
        //int Quest_UpdateState(List<string> details);

        //Format:   double questID, int numCompletions
        //Update quest number of completions
        //int Quest_UpdateCompletionCount(List<string> details);

        //Format:   double questID, double objectiveID, char from, char to
        //Update quest objective and move it into the appropriate list (Active, Completed, or none)
        //int Quest_UpdateObjective(List<string> details);

        //Format:   double questID
        //Clear all quest objectives, useful for restarting a quest
        //int Quest_ClearObjectives(List<string> details);


        /**************************************************************************
        ------------------------ ROSTER SECTION -----------------------------------
        ***************************************************************************
        -This section is for loading the player's roster related variables
        **************************************************************************/

        //Check if any roster variables have updated
        bool Roster_Changed();

        //Get current player level
        int Roster_GetPlayerLevel();

        //Get current sprite index
        int Roster_GetSpriteIndex();

        //Get current player name
        string Roster_GetPlayerName();
        
    }
}
