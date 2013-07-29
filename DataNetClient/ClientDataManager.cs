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

        private const string TblMissingBarException = "tblMissingBarException";
        private const string TblSessionHolidayTimes = "tblSessionHolidayTimes";
        private const string Tblfullreport = "tblfullreport";

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

        public static List<SymbolModel> GetSymbols(int userId)
        {
            var symbolsList = new List<SymbolModel>();

            var userGroups = GetGroups(userId);
            foreach (var groupModel in userGroups)
            {
                var listSymbols = GetSymbolsInGroup(groupModel.GroupId);
                foreach (var symbolModel in listSymbols)
                {
                    if(! symbolsList.Exists(a=>a.SymbolName == symbolModel.SymbolName))
                    {
                        symbolsList.Add(symbolModel);
                    }
                }
                
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

            var sql = "SELECT * FROM " + TblGroupsForUsers + " WHERE UserID=" + userId + "; COMMIT;";
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

        public static void AddToQueue(string sql)
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

        #region COLLECTING & MISSINGBARS

        public static void CreateTickTable(string symbol)
        {
            var str = symbol.Trim().Split('.');
            var sql = "CREATE TABLE IF NOT EXISTS `t_tick_" + str[str.Length - 1] + "` (";
            sql += "`ID` INT(12) NOT NULL AUTO_INCREMENT,";
            sql += "`Symbol` VARCHAR(30) NULL DEFAULT NULL,";
            sql += "`Price` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`Volume` INT(25) NULL DEFAULT NULL,";
            sql += "`TickTime` DATETIME NULL DEFAULT NULL,";
            sql += "`CollectTime` DATETIME NULL DEFAULT NULL,";
            sql += "`ContinuationType` VARCHAR(50) NULL DEFAULT NULL,";
            sql += "`PriceType` VARCHAR(30) NULL DEFAULT NULL,";
            sql += "`GroupID` INT(12) NULL DEFAULT NULL,";
            sql += "PRIMARY KEY (`ID`),";
            sql += "UNIQUE INDEX `UNQ_DATA_INDEX` (`Symbol`, `CollectTime`, `GroupID`)";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";
            DoSql(sql);
        }

        public static void CreateBarsTable(string symbol, string tableType)
        {
            var str = symbol.Trim().Split('.');
            var sql = "CREATE TABLE IF NOT EXISTS `t_candle_" + str[str.Length - 1] + "_" + tableType + "` (";
            sql += "`ID` INT(11) NOT NULL AUTO_INCREMENT,";
            sql += "`cdSymbol` VARCHAR(30) NULL DEFAULT NULL,";
            sql += "`cdOpen` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdHigh` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdLow` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdClose` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdTickVolume` INT(25) NULL DEFAULT NULL,";
            sql += "`cdActualVolume` INT(25) NULL DEFAULT NULL,";
            sql += "`cdAskVol` INT(25) NULL DEFAULT NULL,";
            sql += "`cdBidVol` INT(25) NULL DEFAULT NULL,";
            sql += "`cdAvg` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdDT` DATETIME NULL DEFAULT NULL,";
            sql += "`cdSystemDT` DATE NULL DEFAULT NULL,";
            sql += "`cddatenum` DATETIME NULL DEFAULT NULL,";
            sql += "`cn_type` VARCHAR(25) NULL DEFAULT NULL,";
            sql += "`cdHLC3` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdMid` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdOpenInterest` INT(11) NULL DEFAULT NULL,";
            sql += "`cdRange` CHAR(30) NULL DEFAULT NULL,";
            sql += "`cdTrueHigh` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdTrueLow` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdTrueRange` FLOAT(9,5) NULL DEFAULT NULL,";
            sql += "`cdTimeInterval` DATETIME NULL DEFAULT NULL,";
            sql += "PRIMARY KEY (`ID`),";
            sql += "UNIQUE INDEX `UNQ_DATA_INDEX` (`cdSymbol`, `cddatenum`)";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";
            DoSql(sql);
        }

        public static void CreateMissingBarExceptionTable()
        {

            var sql = "CREATE TABLE IF NOT EXISTS `" + TblMissingBarException + "` (";

            sql += "`Instrument` VARCHAR(30) NOT NULL ,";
            sql += "`RefreshTimestamp` DATETIME NOT NULL ,";
            sql += "`Timestamp` DATETIME NULL ,";

            sql += "`MissingOpen` BOOL NULL DEFAULT NULL,";
            sql += "`MissingHigh` BOOL NULL DEFAULT NULL,";
            sql += "`MissingLow` BOOL NULL DEFAULT NULL,";
            sql += "`MissingClose` BOOL NULL DEFAULT NULL,";
            sql += "`MissingVolume` BOOL NULL DEFAULT NULL,";
            sql += "PRIMARY KEY (`Instrument`,`RefreshTimestamp`,`Timestamp`)";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";
            DoSql(sql);

        }

        public static void CreateSessionHolidayTimesTable()
        {
            var sql = "CREATE TABLE IF NOT EXISTS `" + TblSessionHolidayTimes + "` (";

            sql += "`Instrument` VARCHAR(30) NOT NULL ,";
            sql += "`Exchange` VARCHAR(30) NOT NULL ,";
            sql += "`StartTime` Datetime NOT NULL ,";
            sql += "`EndTime` Datetime NOT NULL ,";
            sql += "`Status` VARCHAR(30) NOT NULL ,";

            sql += "`WorkingDays` VARCHAR(30)  NULL ,";
            sql += "`DayStartsYesterday` BOOL NULL ,";
            sql += "`PrimaryFlag` BOOL  NULL ,";
            sql += "`Number` int NULL ,";
            sql += "`FirstCollect` Datetime NOT  NULL ,";
            sql += "`RefreshTimestamp` Datetime NOT  NULL ,";
            sql += "PRIMARY KEY (`Instrument`,`StartTime`,`WorkingDays`)";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";

            DoSql(sql);
        }

        public static void AddToSessionTable(string instrument, string exchange, DateTime timeStart, DateTime timeEnd, string status,
            string workingDays, bool dayStartsYesterday, bool primary, int number, DateTime refresh)
        {
            MySqlDataReader reader = null;
            try
            {
                // add
                bool rowExists = false;
                string timeSStr = Convert.ToDateTime(timeStart).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                string timeEStr = Convert.ToDateTime(timeEnd).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                string timeRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                DateTime firstCollect = DateTime.Now;
                reader = GetReader("SELECT * FROM " + TblSessionHolidayTimes + " WHERE `Instrument` = '" + instrument + "' AND `StartTime` = '" + timeSStr + "' AND `EndTime` = '" + timeEStr + "' AND `WorkingDays` = '" + workingDays + "'");
                if (reader.Read())
                {
                    rowExists = true;
                    firstCollect = reader.GetDateTime(9);
                }
                reader.Close();

                if (rowExists && (refresh - firstCollect).TotalDays < 30)
                {
                    DoSql("UPDATE " + TblSessionHolidayTimes + " SET " +
                        " `RefreshTimestamp` = '" + timeRefresh + "' " +
                        "WHERE `Instrument` = '" + instrument + "' AND `StartTime` = '" + timeSStr + "' AND `EndTime` = '" + timeEStr + "'");
                }
                else
                {
                    DoSql("INSERT INTO " + TblSessionHolidayTimes + "(`Instrument`,`Exchange`,`StartTime`,`EndTime`,`Status`,"
                        + "`WorkingDays`,`DayStartsYesterday`,`PrimaryFlag`,`Number`,`FirstCollect`,`RefreshTimestamp`) " +
                        "VALUES('" + instrument + "', '" + exchange + "', '" + timeSStr + "', '" + timeEStr + "', '" + status + "', '" +
                        workingDays + "', " + dayStartsYesterday + " , " + primary + " , " + number + " , '" + timeRefresh + "' , '" + timeRefresh + "');COMMIT;");
                }
            }
            catch (Exception ex)
            {
                //logger.LogAdd("AddToSessionTable. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        public static void ClearMissingBar(string table)
        {
            MySqlDataReader reader = null;
            try
            {

                string instr = string.Empty;

                reader = GetReader("SELECT * FROM " + table);
                if (reader.Read())
                {
                    instr += reader.GetValue(1);
                }
                reader.Close();

                if (instr != string.Empty)
                    DoSql("DELETE FROM "+TblMissingBarException+" WHERE `Instrument` = '" + instr + "';");
            }
            catch (Exception)
            {
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        public static void ChangeBarStatusInMissingTable(string instrument, DateTime refresh, DateTime dateTime)
        {
            MySqlDataReader reader = null;
            try
            {
                // add
                bool rowExists = false;
                string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                string dateStr = Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);



                reader = GetReader("SELECT * FROM "+TblMissingBarException+" WHERE `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "'");
                if (reader.Read())
                {
                    rowExists = true;
                }
                reader.Close();

                if (rowExists)
                {
                    DoSql("UPDATE " + TblMissingBarException + " SET " +
                        "`RefreshTimestamp` = '" + dateRefresh + "', `MissingOpen` = 0,`MissingHigh` = 0,`MissingLow` = 0,`MissingClose` = 0,`MissingVolume` = 0 " +
                        " WHERE  `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';COMMIT;");
                }
            }
            catch (Exception ex)
            {
//                logger.LogAdd("ChangeBarStatusInMissingTable. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        public static void DelFromReport(string instrument)
        {
            var sql = "DELETE FROM " + Tblfullreport + " WHERE Instrument = '" + instrument + "'";
            DoSql(sql);
        }

        public static bool TableExists(string p)
        {
            return true;
        }

        public static string GetTableFromSymbol(string symbol)
        {
            string str5 = symbol.Trim();
            string[] str = str5.Split('.');
            str5 = str[str.Length - 1];

            return "t_candle_" + str5 + "_1m";
        }

        public static List<DateTime> GetAllDates(string tableName, int maxCount = 0)
        {
            MySqlDataReader reader = null;
            var result = new List<DateTime>();
            try
            {
                reader = maxCount == 0 ? GetReader("SELECT cdDT FROM " + tableName + " order by cdDT ") : GetReader("SELECT cdDT FROM " + tableName + " order by cdDT DESC LIMIT " + maxCount);
                while (reader.Read())
                {
                    var a = (DateTime)reader.GetValue(0);
                    if (!result.Contains(a.Date))
                        result.Add(a.Date);
                }

                reader.Close();

                if (maxCount != 0)
                    result.Reverse();

                return result;
            }
            catch (Exception ex)
            {
                //MessageBox.Show();
                //logger.LogAdd("Error in : DBS LoadTableForRequest. :" + ex.Message, Category.Error);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }

        }

        public static List<DateTime> GetAllDateTimes(string tableName, int maxCount = 0)
        {
            MySqlDataReader reader = null;
            var result = new List<DateTime>();
            try
            {
                reader = maxCount == 0 ? GetReader("SELECT cdDT FROM " + tableName + " order by cdDT") : GetReader("SELECT cdDT FROM " + tableName + " order by cdDT DESC LIMIT " + maxCount);
                while (reader.Read())
                {
                    var a = (DateTime)reader.GetValue(0);
                    result.Add(a);
                }

                reader.Close();

                if (maxCount != 0)
                    result.Reverse();
                //CloseConnection();

                return result;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error in : DBS LoadTableForRequest");
                //logger.LogAdd("Error in :  getAllDateTimes. :" + ex.Message, Category.Error);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        public static bool HolidaysContains(string tableName, DateTime dateTime)
        {
            String dt = "'" + Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) + "'";

            MySqlDataReader reader = null;

            try
            {
                reader = GetReader("SELECT * FROM `" + TblSessionHolidayTimes + "` WHERE  `Instrument`='" + GetSymbolFromTable(tableName) + "' and `StartTime` = " + dt + " and `Status` = 'Holiday';");

                if (reader.Read())
                {
                    reader.Close();
                    return true;
                }
                reader.Close();
                return false;
            }
            catch (Exception ex)
            {
                //logger.LogAdd("HolidaysContains. " + ex.Message, Category.Error);
                return false;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        private static string GetSymbolFromTable(string tableName)
        {

            MySqlDataReader reader = null;
            string result = "";
            try
            {

                reader = GetReader("SELECT cdSymbol FROM " + tableName + " LIMIT 1");
                if (reader.Read())
                {
                    result = (string)reader.GetValue(0);
                }

                reader.Close();
                //CloseConnection();
                return result;
            }
            catch (Exception ex)
            {
                //logger.LogAdd("getSymbolFromTable. " + exception, Category.Error);
                //MessageBox.Show("Error in : DBS LoadTableForRequest");
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }

        }

        public static void ChangeBarStatusInMissingTableWithOutCommit(string instrument, DateTime refresh, DateTime dateTime)
        {
            //ChangeBarStatusInMissingTable
            string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string dateStr = Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);


            string query = "UPDATE " + TblMissingBarException + " SET " +
                        "`RefreshTimestamp` = '" + dateRefresh + "', `MissingOpen` = 0,`MissingHigh` = 0,`MissingLow` = 0,`MissingClose` = 0,`MissingVolume` = 0 " +
                        " WHERE  `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';COMMIT;";
            AddToQueue(query);
        }

        public static void AddToReport(string instrument, DateTime curDate, string state, string startDay, DateTime sTime, string endDay, DateTime eTime)
        {
            string currDate = Convert.ToDateTime(curDate).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string startDate = Convert.ToDateTime(sTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string endDate = Convert.ToDateTime(eTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);


            string query = "INSERT IGNORE INTO " + Tblfullreport + "(`Instrument`,`Date`,`State`,`StartDay`,`StartTime`,`EndDay`,`EndTime`) " +
                    "VALUES('" + instrument + "', '" + currDate + "', '" + state + "', '" + startDay + "', '" + startDate + "', '" + endDay + "', '" + endDate + "');";

            DoSql(query);
        }

        public static IEnumerable<DateTime> GetMissedBarsForSymbol(string smb1)
        {
            var aRes = new List<DateTime>();
            MySqlDataReader reader = null;
            try
            {
                //string instr = string.Empty;
                reader = GetReader("SELECT * FROM `" + TblMissingBarException + "` WHERE `Instrument` = '" + smb1 + "' and `MissingOpen` <> 0" +
                    " ORDER BY `Timestamp`");
                while (reader.Read())
                {
                    aRes.Add(reader.GetDateTime(2));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                //logger.LogAdd("getMissedBarsForSymbol. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return aRes;
        }

        public static void DelFromReport(string instrument, DateTime from)
        {
            string fromDate = Convert.ToDateTime(from.Date).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string sql = "DELETE FROM "+Tblfullreport+" WHERE Instrument = '" + instrument + "' AND Date >= '" + fromDate + "'";
            DoSql(sql);
        }

        public static List<ReportItem> GetReport(string instrument)
        {
            var result = new List<ReportItem>();
            MySqlDataReader reader = null;

            try
            {
                //string timeSStr = Convert.ToDateTime(missedItem).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                reader = GetReader("SELECT * FROM " + Tblfullreport + " WHERE  `Instrument` = '" + instrument + "' ");

                while (reader.Read())
                {
                    string aState = reader.GetString(3);
                    DateTime aCurrDate = reader.GetDateTime(2);
                    DateTime aStartDate = reader.GetDateTime(5);
                    DateTime aEndDate = reader.GetDateTime(7);
                    var ri = new ReportItem { Instrument = instrument, State = aState, CurDate = aCurrDate, STime = aStartDate, ETime = aEndDate };
                    result.Add(ri);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                //logger.LogAdd("GetReport. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            return result;
        }

        public static void AddToMissingTableWithOutCommit(string instrument, DateTime refresh, DateTime curTime)
        {
            //AddToMissingTable(instrument, refresh, curTime);

            string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string dateStr = Convert.ToDateTime(curTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            string qu = "DELETE FROM " + TblMissingBarException + " WHERE `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';";


            string query = "INSERT IGNORE INTO " + TblMissingBarException + "(`Instrument`,`RefreshTimestamp`,`Timestamp`,`MissingOpen`,`MissingHigh`,`MissingLow`,`MissingClose`,`MissingVolume`) " +
                    "VALUES('" + instrument + "', '" + dateRefresh + "', '" + dateStr + "', 1, 1, 1, 1, 1);";

            AddToQueue(qu);
            AddToQueue(query);
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

    public struct TimeRange
    {
        public DateTime StartTime;
        public DateTime EndTime;
        public String StrTF_Tyoe;
        public String StrContinuationType;
    }

    public struct ReportItem
    {
        public string Instrument;
        public DateTime CurDate;
        public string State;
        public string StartDay;
        public DateTime STime;
        public string EndDay;
        public DateTime ETime;
    }
}
