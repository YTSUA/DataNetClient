using System.Collections.Generic;
using System.Data;
using MySql.Data.Types;
using System.Linq;
using CQG;
using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;
using System.Globalization;

namespace DataNetClient
{
    public struct TimeRange
    {
        public DateTime StartTime;
        public DateTime endTime;
        public String strTF_Tyoe;
        public String strContinuationType;
    }

    internal sealed class DbSelector
    {
        private MySqlConnection m_conn;
        private MySqlCommand m_SQL_Command;

        internal  string server;
        internal  string database;
        internal  string uid;
        internal  string pass;

        private static DbSelector exemplar;

        public static DbSelector GetInstance()
        {
            return exemplar ?? (exemplar = new DbSelector());
        }

        readonly Logger logger;
        private DbSelector()
        {
            logger = Logger.GetInstance(null);
            //this.OpenConnection();
            //Initialize()
        }
        public void Initialize(string HOST, string DB, string UID, string PASS)
        {
            server = HOST;
            database = DB;
            uid = UID;
            pass = PASS;

            string connectionString = "SERVER=" + server + ";DATABASE=" + database + ";UID=" + uid + ";PASSWORD=" + pass;
            m_conn = new MySqlConnection(connectionString);
            //if (!IsConnected())
              //  this.OpenConnection();
            
        }

        private void CreateAllTables()
        {           
            string sql = "CREATE TABLE IF NOT EXISTS `t_symbols` (";            
	        sql +="`ID` INT(11) NOT NULL AUTO_INCREMENT,";
	        sql +="`s_symcode` VARCHAR(30) NOT NULL,";
            sql += "PRIMARY KEY (`ID`),";
	        sql +="UNIQUE INDEX `s_symcode` (`s_symcode`)";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";
            doSQL(sql);

            sql = "CREATE TABLE IF NOT EXISTS `t_cm_list_names` (";
            sql += "`id` INT(11) NOT NULL AUTO_INCREMENT,";
            sql += "`smName` VARCHAR(100) NULL DEFAULT NULL,";
            sql += "`Start_TF` DATETIME NULL DEFAULT NULL,";
            sql += "`End_TF` DATETIME NULL DEFAULT NULL,";
            sql += "`TF_TYPE` VARCHAR(20) NOT NULL,";
            sql += "`Continuation_TYPE` VARCHAR(22) NOT NULL,";
            sql += "PRIMARY KEY (`id`),";
            sql += "UNIQUE INDEX `SWM_UNIQ_KEY` (`smName`)";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";
            doSQL(sql);

            sql = "CREATE TABLE IF NOT EXISTS `t_cm_list` (";
            sql += "`id` INT(11) NOT NULL AUTO_INCREMENT,";
            sql += "`symbol_id` INT(11) NULL DEFAULT NULL,";
            sql += "`lst_name_ptr` INT(11) NULL DEFAULT NULL,";
            sql += "PRIMARY KEY (`id`),";
            sql += "INDEX `t_cm_list_fk` (`lst_name_ptr`),";
            sql += "INDEX `symbol_id` (`symbol_id`)";
            //sql += ",CONSTRAINT `t_cm_list_fk` FOREIGN KEY (`lst_name_ptr`) REFERENCES `t_cm_list_names` (`id`) ON DELETE CASCADE,";
            //sql += "CONSTRAINT `t_cm_list_fk_Symbol_Link` FOREIGN KEY (`symbol_id`) REFERENCES `t_symbols` (`ID`) ON DELETE CASCADE";
            sql += ")";
            sql += "COLLATE='latin1_swedish_ci'";
            sql += "ENGINE=InnoDB;";
            doSQL(sql);

            sql = "CREATE TABLE IF NOT EXISTS `tblfullreport` ("
            + "`Id` INT(11) NOT NULL AUTO_INCREMENT,"
            + "`Instrument` VARCHAR(30) NULL,"
            + "`Date` DATETIME NULL DEFAULT NULL,"
            + "`State` VARCHAR(30) NULL,"

            + "`StartDay` VARCHAR(30) NULL,"
            + "`StartTime` DATETIME NULL DEFAULT NULL,"
            + "`EndDay` VARCHAR(30) NULL,"
            + "`EndTime` DATETIME NULL DEFAULT NULL,"
            
            + "PRIMARY KEY (`Id`)"        
            + ")"
            + "COLLATE='latin1_swedish_ci'"
            + "ENGINE=InnoDB;";
            doSQL(sql);

        }

        public void COMMIT()
        {
            CloseConnection();
            OpenConnection();
        }
        public bool OpenConnection()
        {
            try
            {
                if(m_conn.State != ConnectionState.Open)
                    m_conn.Open();
                m_SQL_Command = m_conn.CreateCommand();
                m_SQL_Command.CommandText = "SET AUTOCOMMIT=0;";
                m_SQL_Command.ExecuteNonQuery();

                CreateAllTables();

                return true;
            }
            catch (MySqlException exception)
            {
                if (exception.Number == 0)
                {
                    //MessageBox.Show("Cannot connect to server.  Contact administrator");
                }
                return false;
            }
        }
        public bool CloseConnection()
        {
            try
            {
                if ((m_conn.State == ConnectionState.Open) && (m_conn.State != ConnectionState.Broken))
                {
                    m_SQL_Command.CommandText = "COMMIT;";
                    m_SQL_Command.ExecuteNonQuery();
                    m_conn.Close();
                    return true;
                }
                return false;
            }
            catch (MySqlException exception)
            {                
                logger.LogAdd("CloseConnection."+exception.Message,Category.Error);
                return false;
            }
        }

        public void DeleteDBSymbol(string sQL)
        {
            try
            {
                if (m_conn.State.ToString() != "Open")
                {
                    OpenConnection();
                }
                else
                {
                    new MySqlCommand(sQL, m_conn).ExecuteNonQuery();                    
                }
            }
            catch (Exception exception)
            {                
                logger.LogAdd("DeleteDBSymbol. " + exception, Category.Error);
            }
        }

        public void DumpRecord_DBS(string SQL)
        {
            try
            {
                if ((m_conn.State != ConnectionState.Open) || (m_conn.State == ConnectionState.Broken))
                {
                    OpenConnection();
                }
                else
                {
                    //new MySqlCommand("SET AUTOCOMMIT=0;" + sQL, this.m_conn).ExecuteNonQuery();
                    m_SQL_Command.CommandText = SQL;
                    m_SQL_Command.ExecuteNonQuery();
                    //this.CloseConnection();
                }
            }
            catch (Exception exception)
            {
                //MessageBox.Show(SQL, "MySQL ERROR");
                //modErrorHandler.ShowError("DBSelector", "", exception);
                logger.LogAdd("DumpRecord_DBS. " + exception, Category.Error);
            }
        }

        internal void doSQL(String SQL)
        {
            try
            {
                if ((m_conn.State != ConnectionState.Open) || (m_conn.State == ConnectionState.Broken))                
                    OpenConnection();                   

                    m_SQL_Command.CommandText = SQL;
                    m_SQL_Command.ExecuteNonQuery();
                    //this.CloseConnection();
                
            }
            catch (Exception exception)
            {
                //MessageBox.Show(SQL, "MySQL ERROR");
                //modErrorHandler.ShowError("DBSelector", "", exception);
                logger.LogAdd("doSQL. " + exception, Category.Error);
            }
        }

        public void clearDB()
        {
            try
            {
                if ((m_conn.State != ConnectionState.Open) || (m_conn.State == ConnectionState.Broken))
                {
                    m_conn.Dispose();
                    OpenConnection();
                }
                else
                {
                    //new MySqlCommand("SET AUTOCOMMIT=0;" + sQL, this.m_conn).ExecuteNonQuery();
                    m_SQL_Command.CommandText = "DELETE FROM t_candle_data;";
                    m_SQL_Command.ExecuteNonQuery();
                    //this.CloseConnection();
                }
            }
            catch (Exception exception)
            {
                //modErrorHandler.ShowError("DBSelector", "", exception);
                logger.LogAdd("clearDB. " + exception, Category.Error);
            }
        }

       

        public void PopulateSymbolsCombos(ListBox lbx, int xVal, int yVal, int wVal, int hVal, string SQL)
        {
            try
            {
                //lbx.Location = new Point(xVal, yVal);
                //lbx.Size = new Size(wVal, hVal);
                if (m_conn.State.ToString() == "Open")
                {
                    MySqlCommand command = m_conn.CreateCommand();
                    command.CommandText = SQL;
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string item = "";
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            item = item + reader.GetValue(i);
                        }
                        lbx.Items.Add(item);
                    }
                    reader.Close();
                }
                //this.CloseConnection();
            }
            catch(Exception ex)
            {
                logger.LogAdd("PopulateSymbolsCombos. " + ex.Message, Category.Error);                             
            }
        }

        public Dictionary<String, UInt32> LoadSymbolList(ListBox lb)
        {
            try
            {
                if(lb!=null)
                lb.Items.Clear();
                var lstSymbols = new Dictionary<String, UInt32>();
                MySqlDataReader reader = getReader("SELECT * FROM t_symbols;");
                while (reader.Read())
                {
                    string item = "";
                    item = item + reader.GetValue(1);
                    lstSymbols.Add(reader.GetValue(1).ToString(), reader.GetUInt32(0));
                    if (lb != null) 
                        lb.Items.Add(item);
                }
                reader.Close();
                //CloseConnection();
                return lstSymbols;
            }
            catch (Exception ex)
            {
                logger.LogAdd("LoadSymbolList. " + ex.Message, Category.Error);
                return null;
            }
        }


        private List<string> LoadTablesForRequest(string timeFrame)
        {
            var result = new List<string>();
            try
            {

                MySqlDataReader reader = getReader("show tables like 't_candle_%" + timeFrame + "' ");
                while (reader.Read())
                {
                    result.Add((string)reader.GetValue(0));
                }
                reader.Close();
                //CloseConnection();
            }
            catch(Exception ex)
            {
                // MessageBox.Show("Error in : DBS LoadTableForRequest");
                logger.LogAdd("Error in : DBS LoadTableForRequest" + ex.Message, Category.Error);
                return null;
            }
            return result;
        }
        public Dictionary<String, TimeRange> LoadCmList(ListBox cb, object o)
        {
            try
            {
                if (cb != null)
                    cb.Items.Clear();
                var lstSymbols = new Dictionary<String, TimeRange>();
                if (m_conn.State == ConnectionState.Open) m_conn.Close();
                MySqlDataReader reader = getReader("SELECT * FROM t_cm_list_names;");
                while (reader.Read())
                {
                    string item = "";
                    item = item + reader.GetValue(1);
                    TimeRange tr;
                    tr.StartTime = reader.IsDBNull(2) ? new DateTime() : reader.GetDateTime(2);
                    tr.endTime = reader.IsDBNull(3) ? new DateTime() : reader.GetDateTime(3);
                    tr.strTF_Tyoe = reader.GetString(4);
                    tr.strContinuationType = reader.GetString(5);
                    lstSymbols.Add(reader.GetValue(1).ToString(), tr);
                    if (cb != null)
                        cb.Items.Add(item);
                }
                reader.Close();
                return lstSymbols;
            }
            catch
            {
                //MessageBox.Show("Error in : DBS LoadCmList");
                return null;
            }
        }

        public Dictionary<String, TimeRange> LoadCmList(ComboBox cb)
        {
            try
            {
                if (cb != null)
                    cb.Items.Clear();
                var lstSymbols = new Dictionary<String, TimeRange>();
                if (m_conn.State == ConnectionState.Open) m_conn.Close();
                var reader = getReader("SELECT * FROM t_cm_list_names;");
                while (reader.Read())
                {
                    string item = "";
                    item = item + reader.GetValue(1);
                    TimeRange tr;
                    tr.StartTime = reader.IsDBNull(2) ? new DateTime() : reader.GetDateTime(2);
                    tr.endTime = reader.IsDBNull(3) ? new DateTime() : reader.GetDateTime(3);
                    tr.strTF_Tyoe = reader.GetString(4);
                    tr.strContinuationType = reader.GetString(5);
                    lstSymbols.Add(reader.GetValue(1).ToString(), tr);
                    if (cb != null)
                        cb.Items.Add(item);
                }
                reader.Close();
                return lstSymbols;
            }
            catch
            {
                //MessageBox.Show("Error in : DBS LoadCmList");
                return null;
            }
        }

        public List<String> load_cmList(String cmList, ListBox lb = null)
        {
            try
            {
                if (lb != null)
                    lb.Items.Clear();
                var lstSymbols = new List<String>();
                MySqlDataReader reader =
                    getReader(
                        "SELECT * FROM t_symbols WHERE `id` IN (SELECT `symbol_id` FROM `t_cm_list` WHERE `lst_name_ptr` IN (SELECT `id` FROM `t_cm_list_names` WHERE `smName`='" +
                        cmList + "'))");
                while (reader.Read())
                {
                    string item = "";
                    item = item + reader.GetValue(1);
                    lstSymbols.Add(reader.GetValue(1).ToString());
                    if (lb != null)
                        lb.Items.Add(item);
                }
                reader.Close();
                //CloseConnection();
                return lstSymbols;
            }
            catch
            {
                //MessageBox.Show("Error in : DBS loadOnChange_cmList");
                return null;
            }
        }

        public void saveCustomList(String listName, Dictionary<String, UInt32> symbolsList, String TF_TYPE, eTimeSeriesContinuationType ContinuationType, DateTime startTime, DateTime endTime)
        {
            MySqlDataReader reader = null;
            try
            {
                String strContinuationType = ContinuationType.ToString();
                String strStartTime = "'" + Convert.ToDateTime(startTime).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                String strEndTime = "'" + Convert.ToDateTime(endTime).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                Dictionary<String, UInt32>.KeyCollection symbols = symbolsList.Keys;
                reader = getReader("SELECT `id` FROM `t_cm_list_names` WHERE `smName`='" + listName
                                                   + "'");
                if (reader == null)
                {
                    throw new MySqlConversionException("can`t initialize MySQL Reader.");
                }
                int selected_cm_List;
                if (reader.Read())
                {
                    selected_cm_List = (int)reader.GetValue(0);
                    reader.Close();
                    doSQL("UPDATE `t_cm_list_names` SET `Start_TF`=" + strStartTime + ",`End_TF`=" + strEndTime + ",`TF_TYPE`='" + TF_TYPE + "',`Continuation_TYPE`='" + strContinuationType + "' WHERE `smName`='" + listName + "';");
                    doSQL("DELETE FROM `t_cm_list` WHERE lst_name_ptr = " + Convert.ToString(selected_cm_List) + ";COMMIT;");
                    IEnumerator<String> e = symbols.GetEnumerator();
                    while (e.MoveNext())
                    {
                        String id = Convert.ToString(symbolsList[e.Current]);
                        doSQL("INSERT t_cm_list(`symbol_id`,`lst_name_ptr`) VALUES(" + id + "," +
                              Convert.ToString(selected_cm_List) + ");COMMIT;");
                    }
                }
                else
                {
                    reader.Close();
                    if (startTime.Equals(new DateTime()))
                    {
                        doSQL("INSERT INTO t_cm_list_names(`smName`,`Start_TF`,`End_TF`,`TF_TYPE`,`Continuation_TYPE`) VALUES('" + listName + "',NULL,NULL,'" + TF_TYPE + "','" + strContinuationType + "');COMMIT;");
                    }
                    else
                    {
                        doSQL("INSERT INTO t_cm_list_names(`smName`,`Start_TF`,`End_TF`,`TF_TYPE`,`Continuation_TYPE`) VALUES('" + listName + "'," +
                              strStartTime + "," + strEndTime + ",'" + TF_TYPE + "','" + strContinuationType + "');COMMIT;");
                    }
                    reader = getReader("SELECT `id` FROM `t_cm_list_names`WHERE `smName`='" + listName + "';");
                    if (!reader.Read())
                    {
                        //MessageBox.Show("Error in saving custom list", "Error", MessageBoxButtons.OK,MessageBoxIcon.Error);
                        
                        return;
                    }
                    selected_cm_List = (int)reader.GetValue(0);
                    reader.Close();
                    IEnumerator<String> e = symbols.GetEnumerator();
                    while (e.MoveNext())
                    {
                        String id = Convert.ToString(symbolsList[e.Current]);
                        doSQL("INSERT t_cm_list(`symbol_id`,`lst_name_ptr`) VALUES(" + id + "," +
                              Convert.ToString(selected_cm_List) + ");COMMIT;");
                    }
                }
            }
            catch (Exception exception)
            {
                //modErrorHandler.ShowError("DBSelector", "", exception);
                logger.LogAdd("saveCustomList. " + exception, Category.Error);            
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        private MySqlDataReader getReader(String SQL)
        {
            try
            {
                if (m_conn.State != ConnectionState.Open)
                {
                    OpenConnection();
                }

                MySqlCommand command = m_conn.CreateCommand();
                command.CommandText = SQL;
                MySqlDataReader reader = command.ExecuteReader();
                return reader;                
            }
            catch(Exception exception)
            {
                logger.LogAdd("getReader. " + exception, Category.Error);
                return null;
            }

        }

        public void UpdateDBSymbol(string sQL)
        {
            try
            {
                if (m_conn.State != ConnectionState.Open)
                {
                    OpenConnection();
                }
                else
                {
                    new MySqlCommand(sQL, m_conn).ExecuteNonQuery();
                    //this.//CloseConnection();
                }
            }
            catch (Exception exception)
            {
                //modErrorHandler.ShowError("DBSelector", "", exception);
                logger.LogAdd("UpdateDBSymbol. " + exception, Category.Error);
            }
        }

        internal string getTableFromSymbol(string symbol)
        {
            string str5 = symbol.Trim();
            string[] str = str5.Split('.');
            str5 = str[str.Length - 1];

            return "t_candle_"+str5+"_1m";
        }

        internal string getSymbolFromTable(string tableName)
        {

            MySqlDataReader reader = null;
            string result = "";
            try
            {

                reader = getReader("SELECT cdSymbol FROM " + tableName + " LIMIT 1");
                if (reader.Read())
                {
                    result = (string)reader.GetValue(0);
                }

                reader.Close();
                //CloseConnection();
                return result;
            }
            catch (Exception exception)
            {
                logger.LogAdd("getSymbolFromTable. " + exception, Category.Error);
                //MessageBox.Show("Error in : DBS LoadTableForRequest");
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }

        }

        internal List<DateTime> getAllDates(string tableName, int maxCount = 0)
        {
            MySqlDataReader reader = null;
            var result = new List<DateTime>();
            try
            {
                reader = maxCount == 0 ? getReader("SELECT cdDT FROM " + tableName + " order by cdDT ") : getReader("SELECT cdDT FROM " + tableName + " order by cdDT DESC LIMIT " + maxCount);
                while (reader.Read())
                {
                    var a = (DateTime)reader.GetValue(0);
                    if(!result.Contains(a.Date))
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
                logger.LogAdd("Error in : DBS LoadTableForRequest. :" + ex.Message,Category.Error);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }

        }
        internal List<DateTime> getAllDateTimes(string tableName, int maxCount = 0)
        {
            MySqlDataReader reader = null;
            var result = new List<DateTime>();
            try
            {
                reader = maxCount ==0 ? getReader("SELECT cdDT FROM " + tableName + " order by cdDT") : getReader("SELECT cdDT FROM " + tableName + " order by cdDT DESC LIMIT " + maxCount);
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
            catch(Exception ex)
            {
                //MessageBox.Show("Error in : DBS LoadTableForRequest");
                logger.LogAdd("Error in :  getAllDateTimes. :" + ex.Message, Category.Error);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        internal List<string> LoadMissingBarTable(string instrument)
        {
            MySqlDataReader reader=null;
            try
            {
                var lstMissingBar = new List<String>();
                reader = getReader("SELECT * FROM tblMissingBarException WHERE Instrument LIKE '" + instrument + "'");
                while (reader.Read())
                {
                    string item = "";
                    item += "Instrument: " + reader.GetValue(0) + ", "; // Instrument 
                    item += " | Refresh DT: " + reader.GetValue(1);
                    item += " | DateTime: " + reader.GetValue(2) + " "; // Timestamp
                    /*
                    item += "Miss O:" + reader.GetValue(3) + ", "; // Missing Open
                    item += "Miss H:" + reader.GetValue(4) + ", "; // Missing High
                    item += "Miss L:" + reader.GetValue(5) + ", "; // Missing Low
                    item += "Miss C:" + reader.GetValue(5) + ", "; // Missing Close
                    item += "Miss V:" + reader.GetValue(7) + "  "; // Missing Volume
                    */
                    lstMissingBar.Add(item);
                }
                reader.Close();
                //CloseConnection();

                return lstMissingBar;
            }
            catch(Exception ex)
            {
                logger.LogAdd("Error in : DBS LoadMissingBarTable. :" + ex.Message, Category.Error);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        internal List<string> LoadViewResultTable(string instrument)
        {
            MySqlDataReader reader = null;
            try
            {
                var lstMissingBar = new List<String>();
                reader = getReader("SELECT * FROM tblMissingBarException WHERE Instrument LIKE '" + instrument + "'");
                while (reader.Read())
                {
                    string item = "";
                    item += "Instrument: " + reader.GetValue(0) + ", "; // Instrument 
                    item += " | Refresh DT: " + reader.GetValue(1);
                    item += " | DateTime: " + reader.GetValue(2) + " "; // Timestamp
                    /*
                    item += "Miss O:" + reader.GetValue(3) + ", "; // Missing Open
                    item += "Miss H:" + reader.GetValue(4) + ", "; // Missing High
                    item += "Miss L:" + reader.GetValue(5) + ", "; // Missing Low
                    item += "Miss C:" + reader.GetValue(5) + ", "; // Missing Close
                    item += "Miss V:" + reader.GetValue(7) + "  "; // Missing Volume
                    */
                    lstMissingBar.Add(item);
                }
                reader.Close();
                //CloseConnection();

                return lstMissingBar;
            }
            catch(Exception ex)
            {
                logger.LogAdd("Error in : DBS LoadViewResultTable. :" + ex.Message, Category.Error);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }

        }

        internal void AddToMissingTable(string instrument, DateTime refresh, DateTime dateTime)
        {
            MySqlDataReader reader = null;
            try
            {
                // add
                bool rowExists = false;
                string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                string dateStr = Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                
                

                reader = getReader("SELECT * FROM tblMissingBarException WHERE `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "'");
                if (reader.Read())
                {
                    rowExists = true;
                }
                reader.Close();

                if (!rowExists)
                    doSQL("INSERT IGNORE INTO tblMissingBarException(`Instrument`,`RefreshTimestamp`,`Timestamp`,`MissingOpen`,`MissingHigh`,`MissingLow`,`MissingClose`,`MissingVolume`) " +
                    "VALUES('" + instrument + "', '" + dateRefresh + "', '" + dateStr + "', 1, 1, 1, 1, 1);COMMIT;");
                else
                {
                    doSQL("UPDATE tblMissingBarException SET "+
                        "`RefreshTimestamp` = '" + dateRefresh + "',  `MissingOpen` = 1,`MissingHigh` = 1,`MissingLow` = 1,`MissingClose` = 1,`MissingVolume` = 1 "
                    +"WHERE  `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';COMMIT;");
                }
            }
            catch(Exception ex)
            {
                logger.LogAdd("AddToMissingBarsTable. "+ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }
        internal void ChangeBarStatusInMissingTable(string instrument, DateTime refresh, DateTime dateTime)
        {
            MySqlDataReader reader = null;
            try
            {
                // add
                bool rowExists = false;
                string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                string dateStr = Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);



                reader = getReader("SELECT * FROM tblMissingBarException WHERE `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "'");
                if (reader.Read())
                {
                    rowExists = true;
                }
                reader.Close();

                if (rowExists)
                {
                    doSQL("UPDATE tblMissingBarException SET " +
                        "`RefreshTimestamp` = '" + dateRefresh + "', `MissingOpen` = 0,`MissingHigh` = 0,`MissingLow` = 0,`MissingClose` = 0,`MissingVolume` = 0 " +
                        " WHERE  `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';COMMIT;");
                }
            }
            catch (Exception ex)
            {
                logger.LogAdd("ChangeBarStatusInMissingTable. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }
        internal void AddToSessionTable(string instrument, string exchange, DateTime timeStart, DateTime timeEnd, string status,
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
                DateTime  firstCollect = DateTime.Now;
                reader = getReader("SELECT * FROM tblSessionHolidayTimes WHERE `Instrument` = '" + instrument + "' AND `StartTime` = '" + timeSStr + "' AND `EndTime` = '" + timeEStr + "' AND `WorkingDays` = '" + workingDays + "'");
                if (reader.Read())
                {
                    rowExists = true;
                    firstCollect = reader.GetDateTime(9);
                }
                reader.Close();

                if (rowExists && (refresh - firstCollect).TotalDays < 30)
                {
                    doSQL("UPDATE tblSessionHolidayTimes SET "+
                        " `RefreshTimestamp` = '"+timeRefresh+"' " +
                        "WHERE `Instrument` = '" + instrument + "' AND `StartTime` = '" + timeSStr + "' AND `EndTime` = '" + timeEStr + "'");
                }
                else
                {
                    doSQL("INSERT INTO tblSessionHolidayTimes(`Instrument`,`Exchange`,`StartTime`,`EndTime`,`Status`,"
                        + "`WorkingDays`,`DayStartsYesterday`,`PrimaryFlag`,`Number`,`FirstCollect`,`RefreshTimestamp`) " +
                        "VALUES('" + instrument + "', '" + exchange + "', '" + timeSStr + "', '" + timeEStr + "', '" + status + "', '" +
                        workingDays + "', " + dayStartsYesterday + " , " + primary + " , " + number + " , '" + timeRefresh + "' , '" + timeRefresh + "');COMMIT;");
                }
            }
            catch (Exception ex)
            {
                logger.LogAdd("AddToSessionTable. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        internal bool IsConnected()
        {
            return m_conn.State == ConnectionState.Open;
        }

        internal void ClearMissingBar(string table)
        {
            MySqlDataReader reader = null;
            try
            {
                
                string instr = string.Empty;

                reader = getReader("SELECT * FROM " + table);
                if (reader.Read())
                {
                    instr += reader.GetValue(1);
                }
                reader.Close();

                if (instr != string.Empty)
                    doSQL("DELETE FROM tblMissingBarException WHERE `Instrument` = '" + instr + "';");
            }
            catch (Exception)
            {
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        public void CreateBarsTable(string Symbol, string TableType)
        {
            string[] str = Symbol.Trim().Split('.');
            string sql = "CREATE TABLE IF NOT EXISTS `t_candle_" + str[str.Length - 1] + "_" + TableType + "` (";
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
            doSQL(sql);
        }
        public void CreateTickTable(string Symbol)
        {
            string[] str = Symbol.Trim().Split('.');
            string sql = "CREATE TABLE IF NOT EXISTS `t_tick_" + str[str.Length - 1] + "` (";
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
            doSQL(sql);
        }

        public void CreateMissingBarExceptionTable()
        {

            string sql = "CREATE TABLE IF NOT EXISTS `tblMissingBarException` (";

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
            doSQL(sql);

        }

        internal void CreateSessionHolidayTimesTable()
        {
            string sql = "CREATE TABLE IF NOT EXISTS `tblSessionHolidayTimes` (";

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

            doSQL(sql);
        }

        internal bool HolidaysContains(string table_name, DateTime dateTime)
        {
            String dt = "'" + Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) + "'";

            MySqlDataReader reader = null;

            try
            {
                reader = getReader("SELECT * FROM `tblSessionHolidayTimes` WHERE  `Instrument`='" + getSymbolFromTable(table_name) + "' and `StartTime` = " + dt + " and `Status` = 'Holiday';");

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
                logger.LogAdd("HolidaysContains. " + ex.Message, Category.Error);
                return false;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        internal void AddSymbol(string newSymbol)
        {
            try
            {
                string sQL = "INSERT INTO t_symbols (s_symcode) VALUES ('" + newSymbol.ToUpper() + "')";
                doSQL(sQL);
            }
            catch(Exception ex)
            {
                logger.LogAdd("AddSymbol. "+ex.Message,Category.Error);
            }
        }

        internal List<DateTime> getMissedBarsForSymbol(string smb1)
        {
            var aRes = new List<DateTime>();
            MySqlDataReader reader = null;
            try
            {
                //string instr = string.Empty;
                reader = getReader("SELECT * FROM `tblMissingBarException` WHERE `Instrument` = '" + smb1 + "' and `MissingOpen` <> 0" +
                    " ORDER BY `Timestamp`");
                while (reader.Read())
                {
                    aRes.Add(reader.GetDateTime(2));
                }
                reader.Close();                
            }
            catch(Exception ex)
            {
                logger.LogAdd("getMissedBarsForSymbol. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return aRes;
        }

        internal bool rowExists(string tableName, DateTime missedItem)
        {
            MySqlDataReader reader = null;

            try
            {
                string timeSStr = Convert.ToDateTime(missedItem).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                reader = getReader("SELECT * FROM `" + tableName + "` WHERE  `cdDT` = '" + timeSStr + "' ;");

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
                logger.LogAdd("rowExists. " + ex.Message, Category.Error);
                return false;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        internal bool TableExists(string p)
        {
            return true;
        }

        #region Adding to DB through queue

        readonly List<string> QueryQueue = new List<string>();

        internal void AddSQLToQueueWithOutCommit(string query)
        {
            QueryQueue.Add(query);
            if (QueryQueue.Count >= 1000)
            {
                CommitQueue();
            }
        }

        internal void CommitQueue()
        {
            if (QueryQueue.Count == 0) return;

            string OneQuerry = QueryQueue.Aggregate("", (current, q) => current + q);
            OneQuerry += "COMMIT;";

            doSQL(OneQuerry);

            QueryQueue.Clear();
        }

        #endregion

        internal void AddToMissingTableWithOutCommit(string instrument, DateTime refresh, DateTime curTime)
        {
            //AddToMissingTable(instrument, refresh, curTime);

            string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string dateStr = Convert.ToDateTime(curTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            string qu = "DELETE FROM tblMissingBarException WHERE `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';";


            string query = "INSERT IGNORE INTO tblMissingBarException(`Instrument`,`RefreshTimestamp`,`Timestamp`,`MissingOpen`,`MissingHigh`,`MissingLow`,`MissingClose`,`MissingVolume`) " +
                    "VALUES('" + instrument + "', '" + dateRefresh + "', '" + dateStr + "', 1, 1, 1, 1, 1);";

            AddSQLToQueueWithOutCommit(qu);
            AddSQLToQueueWithOutCommit(query);
        }

        internal void ChangeBarStatusInMissingTableWithOutCommit(string instrument, DateTime refresh, DateTime dateTime)
        {
            //ChangeBarStatusInMissingTable
            string dateRefresh = Convert.ToDateTime(refresh).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string dateStr = Convert.ToDateTime(dateTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            
            string query = "UPDATE tblMissingBarException SET " +
                        "`RefreshTimestamp` = '" + dateRefresh + "', `MissingOpen` = 0,`MissingHigh` = 0,`MissingLow` = 0,`MissingClose` = 0,`MissingVolume` = 0 " +
                        " WHERE  `Instrument` = '" + instrument + "' AND `Timestamp` = '" + dateStr + "';COMMIT;";
            AddSQLToQueueWithOutCommit(query);
        }

        internal void DelFromReport(string instrument)
        {
            string sql = "DELETE FROM tblfullreport WHERE Instrument = '" + instrument + "'";
            doSQL(sql);
        }

        internal void DelFromReport(string instrument, DateTime from)
        {
            string fromDate = Convert.ToDateTime(from.Date).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string sql = "DELETE FROM tblfullreport WHERE Instrument = '" + instrument + "' AND Date >= '"+fromDate+"'";
            doSQL(sql);
        }

        internal void AddToReport(string instrument, DateTime curDate, string state, string startDay, DateTime sTime, string endDay, DateTime eTime)
        {
            string currDate = Convert.ToDateTime(curDate).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string startDate = Convert.ToDateTime(sTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            string endDate = Convert.ToDateTime(eTime).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            

            string query = "INSERT IGNORE INTO tblfullreport(`Instrument`,`Date`,`State`,`StartDay`,`StartTime`,`EndDay`,`EndTime`) " +
                    "VALUES('" + instrument + "', '" + currDate + "', '" + state + "', '"+startDay+"', '"+startDate+"', '"+endDay+"', '"+endDate+"');";

            doSQL(query);
        }

        internal List<ReportItem> GetReport(string instrument)
        {
            var result = new List<ReportItem>();
            MySqlDataReader reader = null;

            try
            {
                //string timeSStr = Convert.ToDateTime(missedItem).ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                reader = getReader("SELECT * FROM tblfullreport WHERE  `Instrument` = '" + instrument + "' ");

                while (reader.Read())
                {
                    string aState = reader.GetString(3);
                    DateTime aCurrDate = reader.GetDateTime(2);
                    DateTime aStartDate = reader.GetDateTime(5);
                    DateTime aEndDate = reader.GetDateTime(7);
                    var ri = new ReportItem { instrument = instrument, state = aState, curDate = aCurrDate , sTime = aStartDate, eTime = aEndDate};
                    result.Add(ri);                    
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                logger.LogAdd("GetReport. " + ex.Message, Category.Error);                
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            return result;
        }

        internal object GetSymbolIdByName(string p)
        {
            MySqlDataReader reader = null;
            var id = -1;
            try
            {                
                reader = getReader("SELECT * FROM `t_symbols` WHERE `s_symcode` = '"+p+"'");
                if (reader.Read())
                {
                    id = reader.GetInt32(0);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                logger.LogAdd("GetSymbolIdByName. " + ex.Message, Category.Error);
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return id;
        }
    }

    public struct ReportItem
    {
        public string instrument;
        public DateTime curDate;
        public string state;
        public string startDay;
        public DateTime sTime;
        public string endDay;
        public DateTime eTime;
    }
}

