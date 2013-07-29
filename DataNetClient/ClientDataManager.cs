using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DataNetClient.Structs;
using MySql.Data.MySqlClient;

namespace DataNetClient
{
    static class ClientDataManager
    {

        #region VARIABLES

        private static bool _currentDbIsShared;

        private static string _connectionStringToShareDb;
        private static string _connectionStringTolocalDb;
        

        private static MySqlConnection _connectionToDb;
        private static MySqlCommand _sqlCommandToDb;        

        private const string TblUsers = "tbl_users";
        private const string TblLogs = "tbl_logs";
        private const string TblSymbols = "tbl_symbols";
        private const string TblSymbolsGroups = "tbl_symbols_groups";
        private const string TblSymbolsInGroups = "tbl_symbols_in_groups";
        private const string TblGroupsForUsers = "tbl_groups_for_users";

        private static readonly List<string> QueryQueue = new List<string>();
        private const int MaxQueueSize = 500;

        public delegate void ConnectionStatusChangedHandler(bool connected, bool isShared);
        public static event ConnectionStatusChangedHandler ConnectionStatusChanged;

        #endregion

        #region Symbols (Get, Add, Edit)

        public static bool AddNewSymbol(string symbolName)
        {
            var sql = "INSERT IGNORE INTO " + TblSymbols
                    + " (`SymbolName`)"
                    + "VALUES('" + symbolName + "');COMMIT;";

            return DoSql(sql);
        }

        public static bool EditSymbol(string oldName, string newName)
        {
            var sql = "UPDATE `" + TblSymbols + "` SET `SymbolName`='" + newName + "' WHERE `SymbolName`='" + oldName + "';COMMIT;";

            if (DoSql(sql))
            {
                var grSql = "UPDATE `" + TblSymbolsInGroups + "` SET `SymbolName`='" + newName + "' WHERE `SymbolName`='" + oldName + "';COMMIT;";

                return DoSql(grSql);
            }

            return false;

            //TODO: Rename TICKS and BARS tables
        }

        public static List<SymbolModel> GetSymbols()
        {
            var symbolsList = new List<SymbolModel>();

            const string sql = "SELECT * FROM " + TblSymbols;
            MySqlDataReader reader = GetReader(sql);
            if (reader != null)
            {
                while (reader.Read())
                {
                    var symbol = new SymbolModel { SymbolId = reader.GetInt32(0), SymbolName = reader.GetString(1) };
                    symbolsList.Add(symbol);
                }

                reader.Close();
            }
            return symbolsList;
        }

        #endregion

        #region GROUPS OF SYMBOLS

        public static bool AddGroupOfSymbols(GroupModel group)
        {
            string startDateStr = Convert.ToDateTime(group.Start).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string endDateStr = Convert.ToDateTime(group.End).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            String query = "INSERT IGNORE INTO " + TblSymbolsGroups;
            query += "(GroupName, TimeFrame, Start, End, CntType) VALUES";
            query += "('" + group.GroupName + "',";
            query += " '" + group.TimeFrame + "',";
            query += " '" + startDateStr + "',";
            query += " '" + endDateStr + "',";
            query += " '" + group.CntType + "');COMMIT;";
            return DoSql(query);
        }

        public static bool DeleteGroupOfSymbols(int groupId)
        {
            string query = "DELETE FROM `" + TblSymbolsGroups + "` WHERE ID = " + groupId + " ;COMMIT;";

            if (DoSql(query))
            {
                query = "DELETE FROM `" + TblSymbolsInGroups + "` WHERE GroupID = " + groupId + " ;COMMIT;";

                DoSql(query);

                query = "DELETE FROM `" + TblGroupsForUsers + "` WHERE GroupID = " + groupId + " ;COMMIT;";

                return DoSql(query);
            }

            return false;
        }

        public static bool EditGroupOfSymbols(int groupId, GroupModel group)
        {
            string startDateStr = Convert.ToDateTime(group.Start).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string endDateStr = Convert.ToDateTime(group.End).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            String query = "UPDATE " + TblSymbolsGroups
                        + " SET GroupName = '" + group.GroupName
                        + "', TimeFrame = '" + group.TimeFrame
                        + "', Start = '" + startDateStr
                        + "', End = '" + endDateStr
                        + "', CntType = '" + group.CntType
                        + "' WHERE ID = '" + groupId + "' ; COMMIT;";

            if (DoSql(query))
            {
                query = "UPDATE " + TblGroupsForUsers
                    + " SET GroupName = '" + group.GroupName
                    + "', TimeFrame = '" + group.TimeFrame
                    + "', Start = '" + startDateStr
                    + "', End = '" + endDateStr
                    + "', CntType = '" + group.CntType
                    + "' WHERE GroupID = '" + groupId + "' ; COMMIT;";

                return DoSql(query);
            }

            return false;
        }

        public static List<GroupModel> GetGroups(int userId)
        {           
            var groupList = new List<GroupModel>();

            var sql = "SELECT * FROM " + TblGroupsForUsers + " WHERE UserID=" + userId.ToString() + "; COMMIT;";
            var reader = GetReader(sql);
            if (reader != null)
            {
                while (reader.Read())
                {
                    var group = new GroupModel
                    {
                        GroupId = reader.GetInt32(2),
                        GroupName = reader.GetString(3),
                        TimeFrame = reader.GetString(4),
                        Start = reader.GetDateTime(5),
                        End = reader.GetDateTime(6),
                        CntType = reader.GetString(7)
                    };

                    groupList.Add(group);
                }

                reader.Close();
            }
            return groupList;
        }

        #endregion

        #region SYMBOLS AND GROUPS RELATIONS

        public static List<SymbolModel> GetSymbolsInGroup(int groupId)
        {
            var symbolsList = new List<SymbolModel>();

            string sql = "SELECT * FROM " + TblSymbolsInGroups + " WHERE GroupID = '" + groupId + "' ; COMMIT;";
            var reader = GetReader(sql);
            if (reader != null)
            {
                while (reader.Read())
                {
                    var symbol = new SymbolModel { SymbolId = reader.GetInt32(2), SymbolName = reader.GetString(3) };
                    symbolsList.Add(symbol);
                }
                reader.Close();
            }
            return symbolsList;
        }

        public static bool AddSymbolIntoGroup(int groupId, SymbolModel symbol)
        {
            var sql = "INSERT IGNORE INTO " + TblSymbolsInGroups
                    + " (`GroupID`, `SymbolID`, `SymbolName`)"
                    + "VALUES('" + groupId + "',"
                    + " '" + symbol.SymbolId + "',"
                    + " '" + symbol.SymbolName + "');COMMIT;";

            return DoSql(sql);
        }

        public static bool DeleteSymbolFromGroup(int groupId, int symbolId)
        {
            var sql = "DELETE FROM `" + TblSymbolsInGroups + "` WHERE `GroupID`='" + groupId + "' AND `SymbolID` = '" + symbolId + "';COMMIT;";

            return DoSql(sql);
        }

        #endregion

        #region MAIN FUNCTIONS (Connect, IsOpen, DoSql, GetReader, AddToQueue)

        public static bool ConnectToShareDb(string connectionStringToShareDb, int uId)
        {
            CloseConnectionToDb();
            _currentDbIsShared = true;
            _connectionStringToShareDb = connectionStringToShareDb;
            if (_connectionToDb != null && _connectionToDb.State == ConnectionState.Open)
            {
                CloseConnectionToDb();
            }

            _connectionToDb = new MySqlConnection(_connectionStringToShareDb);

            var res =  OpenConnectionToDb();
            ConnectionStatusChanged(res, _currentDbIsShared);
            return res;
        }

        public static bool ConnectToLocalDb(string connectionStringToShareDb, int uId)
        {
            return false;
            _currentDbIsShared = false;
            CloseConnectionToDb();
            _connectionStringToShareDb = connectionStringToShareDb;
            if (_connectionToDb != null && _connectionToDb.State == ConnectionState.Open)
            {
                CloseConnectionToDb();
            }

            _connectionToDb = new MySqlConnection(_connectionStringToShareDb);

            return OpenConnectionToDb();
        }

        private static bool OpenConnectionToDb()
        {
            try
            {
                _connectionToDb.Open();

                if (_connectionToDb.State == ConnectionState.Open)
                {
                    _sqlCommandToDb = _connectionToDb.CreateCommand();
                    _sqlCommandToDb.CommandText = "SET AUTOCOMMIT=0;";
                    _sqlCommandToDb.ExecuteNonQuery();

                    return true;
                }
            }
            catch (MySqlException)
            {
                return false;
            }
            return false;
        }

        public static void CloseConnectionToDb()
        {
            if (_connectionToDb == null) return;
            if ((_connectionToDb.State != ConnectionState.Open) || (_connectionToDb.State == ConnectionState.Broken))
                return;
            if (_sqlCommandToDb != null)
            {
                _sqlCommandToDb.CommandText = "COMMIT;";
                _sqlCommandToDb.ExecuteNonQuery();
            }

            _connectionToDb.Close();
        }

        public static bool IsConnected()
        {
            return _connectionToDb.State == ConnectionState.Open;
        }

        private static bool DoSql(string sql)
        {
            try
            {
                if (_connectionToDb.State != ConnectionState.Open)
                {
                    OpenConnectionToDb();
                }
                _sqlCommandToDb.CommandText = sql;
                _sqlCommandToDb.ExecuteNonQuery();
                return true;

            }
            catch (MySqlException)
            {
                return false;
            }
        }

        private static MySqlDataReader GetReader(String sql)
        {
            try
            {
                if (_connectionToDb.State != ConnectionState.Open)
                {
                    OpenConnectionToDb();
                }

                var command = _connectionToDb.CreateCommand();
                command.CommandText = sql;
                var reader = command.ExecuteReader();

                return reader;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private static void AddToQueue(string sql)
        {
            QueryQueue.Add(sql);
            if (QueryQueue.Count >= MaxQueueSize)
            {
                CommitQueue();
            }
        }

        internal static void CommitQueue()
        {
            if (QueryQueue.Count <= 0) return;

            var fullSql = QueryQueue.Aggregate("", (current, t) => current + t);
            fullSql += "COMMIT;";
            DoSql(fullSql);

            QueryQueue.Clear();
        }

        #endregion

        #region OTHER
        
        private static void CreateDataBase(string dataBaseName)
        {
            var sql = "CREATE DATABASE IF NOT EXISTS `" + dataBaseName + "`;COMMIT;";
            DoSql(sql);
        }

        private static void CreateTables()
        {
            const string createUsersSql = "CREATE TABLE  IF NOT EXISTS `" + TblUsers + "` ("
                                     + "`ID` INT(12) UNSIGNED  NOT NULL AUTO_INCREMENT,"
                                     + "`UserName` VARCHAR(50) NOT NULL,"
                                     + "`UserPassword` VARCHAR(50) NOT NULL,"
                                     + "`UserFullName` VARCHAR(100) NULL,"
                                     + "`UserEmail` VARCHAR(50) NULL,"
                                     + "`UserPhone` VARCHAR(50) NULL,"
                                     + "`UserIpAddress` VARCHAR(50) NULL,"
                                     + "`UserBlocked` BOOLEAN NULL,"
                                     + "`UserAllowDataNet` BOOLEAN NULL,"
                                     + "`UserAllowTickNet` BOOLEAN NULL,"
                                     + "`UserAllowLocal` BOOLEAN NULL,"
                                     + "`UserAllowRemote` BOOLEAN NULL,"
                                     + "`UserAllowAnyIP` BOOLEAN NULL,"
                                     + "`UserAllowMissBars` BOOLEAN NULL,"
                                     + "`UserAllowCollectFrCQG` BOOLEAN NULL,"

                                     + "PRIMARY KEY (`ID`,`UserName`)"
                                     + ")"
                                     + "COLLATE='latin1_swedish_ci'"
                                     + "ENGINE=InnoDB;";
            DoSql(createUsersSql);

            const string createSymbolsSql = "CREATE TABLE  IF NOT EXISTS `" + TblSymbols + "` ("
                                     + "`ID` INT(10) UNSIGNED  NOT NULL AUTO_INCREMENT,"
                                     + "`SymbolName` VARCHAR(50) NULL,"
                                     + "PRIMARY KEY (`ID`,`SymbolName`)"
                                     + ")"
                                     + "COLLATE='latin1_swedish_ci'"
                                     + "ENGINE=InnoDB;";
            DoSql(createSymbolsSql);

            const string createLogsSql = "CREATE TABLE  IF NOT EXISTS `" + TblLogs + "` ("
                                     + "`ID` INT(10) UNSIGNED  NOT NULL AUTO_INCREMENT,"
                                     + "`UserID` INT(10) NULL,"
                                     + "`MsgType` VARCHAR(50) NULL,"
                                     + "`Date` DateTime NULL, "
                                     + "`Description` VARCHAR(100) NULL,"
                                     + "PRIMARY KEY (`ID`)"
                                     + ")"
                                     + "COLLATE='latin1_swedish_ci'"
                                     + "ENGINE=InnoDB;";
            DoSql(createLogsSql);

            const string createSymbolsGroups = "CREATE TABLE  IF NOT EXISTS `" + TblSymbolsGroups + "` ("
                                             + "`ID` INT(10) UNSIGNED  NOT NULL AUTO_INCREMENT,"
                                             + "`GroupName` VARCHAR(50) NULL,"
                                             + "PRIMARY KEY (`ID`,`GroupName`)"
                                             + ")"
                                             + "COLLATE='latin1_swedish_ci'"
                                             + "ENGINE=InnoDB;";
            DoSql(createSymbolsGroups);

            const string createSymbolsInGroups = "CREATE TABLE  IF NOT EXISTS `" + TblSymbolsInGroups + "` ("
                                             + "`ID` INT(10) UNSIGNED  NOT NULL AUTO_INCREMENT,"
                                             + "`GroupID` INT(10) NULL,"
                                             + "`SymbolID` INT(10) NULL,"
                                             + "`SymbolName` VARCHAR(50) NOT NULL,"
                                             + "PRIMARY KEY (`ID`, `GroupID`, `SymbolID`)"
                                             + ")"
                                             + "COLLATE='latin1_swedish_ci'"
                                             + "ENGINE=InnoDB;";
            DoSql(createSymbolsInGroups);

            const string createGroupsForUsers = "CREATE TABLE  IF NOT EXISTS `" + TblGroupsForUsers + "` ("
                                             + "`ID` INT(10) UNSIGNED  NOT NULL AUTO_INCREMENT,"
                                             + "`UserID` INT(10) NULL,"
                                             + "`GroupID` INT(10) NULL,"
                                             + "`GroupName` VARCHAR(50) NOT NULL,"
                                             + "PRIMARY KEY (`ID`)"
                                             + ")"
                                             + "COLLATE='latin1_swedish_ci'"
                                             + "ENGINE=InnoDB;";
            DoSql(createGroupsForUsers);
        }

        #endregion
    }
}
