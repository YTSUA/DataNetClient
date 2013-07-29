using System;
using System.Collections.Generic;
using System.Linq;
using CQG;
using System.Threading;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing;

namespace DataNetClient
{
    class DataCollector
    {
        const string STAUS_READY = "Ready";
        const string STAUS_WORKING = "Please wait. Working. ";
        int MaxBarsLookBack = 3000;

        readonly DbSelector dbSel;
        readonly Logger logger;
        Semaphore aSemaphoreHolidays;
        Semaphore aSemaphoreSessions;
        Semaphore aSemaphoreWait;
        //Semaphore SemaphoreEndOfCollecting;

        string aTableType;
        int aIntradayPeriod;
        eHistoricalPeriod aHistoricalPeriod;
        string aContinuationType;

        readonly Dictionary<string, SymbolState> aSymbolStates = new Dictionary<string, SymbolState>();
        public struct SymbolState
        {
            public bool isCollected;
            public bool isSuccess;
        }
        enum SessionStates { OpenedHoliday, OpenedNormal, ClosedHoliday, ClosedNormal, Missed }
        public struct MissedStr
        {
            public DateTime start;
            public DateTime end;
        }
        //*** UI

        DevComponents.DotNetBar.LabelItem aL_status;
        ListBox aLB_symbols;
        CheckedListBox aCB_lists;
        DevComponents.DotNetBar.ProgressBarItem aPB_state;
        ListView aLV_report;
        //**

        public DataCollector(Logger log)
        {
            dbSel = DbSelector.GetInstance();
            logger = log;            
        }

        public void Subscribe(DevComponents.DotNetBar.LabelItem _L_status, ListBox _LB_symbols, CheckedListBox _CB_lists, DevComponents.DotNetBar.ProgressBarItem _PB_state, ListView _LV_report)
        {
            aL_status = _L_status;
            aLB_symbols = _LB_symbols;
            aCB_lists = _CB_lists;
            aPB_state = _PB_state;
            aLV_report = _LV_report;
        }

        #region Sessions & Holidays /////////////////////////////////////////////////////////////////////////////////////////////

        public void SessionAdd(CQGSessions sessions, string symbol)
        {
            try
            {
                foreach (CQGSession session in sessions)
                {
                    SessionData one = new SessionData();
                    one.startTime = session.StartTime;
                    one.endTime = session.EndTime;
                    one.dayOfWeek = session.WorkingWeekDays;
                    one.symbol = symbol;
                    one.DayStartsYesterday = session.DayStartsYesterday;
                    listSession.Add(one);

                    dbSel.AddToSessionTable(symbol, symbol, session.StartTime, session.EndTime, "Open",
                        GetSessionWorkingDays(session.WorkingWeekDays), session.DayStartsYesterday, session.PrimaryFlag, session.Number, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                logger.LogAdd("SessionAdd. " + ex.Message, Category.Error);
            }

            aSemaphoreSessions.Release();      
        }

        public void HolidaysAdd(CQGSessionsCollection sessions, string symbol)
        {
            try
            {
                foreach (CQGSessions session in sessions)
                {
                    foreach (CQGHoliday holiday in session.Holidays)
                    {
                        dbSel.AddToSessionTable(symbol, symbol, holiday.HolidayDate, holiday.HolidayDate, "Holiday", "", false, false, 0, DateTime.Now);
                    }

                }
                
            }
            catch (Exception ex)
            {
                logger.LogAdd("HolidaysAdd. " + ex.Message, Category.Error);
            }

            aSemaphoreHolidays.Release();
        }

        public struct SessionData
        {
            public string symbol;
            public DateTime startTime;
            public DateTime endTime;
            public eSessionWeekDays dayOfWeek;
            public bool DayStartsYesterday;
        }

        private List<SessionData> listSession = new List<SessionData>();

        private string GetSessionWorkingDays(eSessionWeekDays weekDay)
        {
            string sResult;

            sResult = (((weekDay & eSessionWeekDays.swdSunday) == eSessionWeekDays.swdSunday) ? "S" : "-").ToString();
            sResult += (((weekDay & eSessionWeekDays.swdMonday) == eSessionWeekDays.swdMonday) ? "M" : "-").ToString();
            sResult += (((weekDay & eSessionWeekDays.swdTuesday) == eSessionWeekDays.swdTuesday) ? "T" : "-").ToString();
            sResult += (((weekDay & eSessionWeekDays.swdWednesday) == eSessionWeekDays.swdWednesday) ? "W" : "-").ToString();
            sResult += (((weekDay & eSessionWeekDays.swdThursday) == eSessionWeekDays.swdThursday) ? "T" : "-").ToString();
            sResult += (((weekDay & eSessionWeekDays.swdFriday) == eSessionWeekDays.swdFriday) ? "F" : "-").ToString();
            sResult += (((weekDay & eSessionWeekDays.swdSaturday) == eSessionWeekDays.swdSaturday) ? "S" : "-").ToString();

            return sResult;
        }
        
        #endregion /////////////////////////////////////////////////////////////////////////////////////////////

        #region Bars  /////////////////////////////////////////////////////////////////////////////////////////////

        public void BarRequest(CQGCEL CEL,List<string> symbols, int rangeStart, int rangeEnd, int sessionFilter, string historicalPeriod, string continuationType)
        {
            aSymbolStates.Clear();
            aContinuationType = continuationType;
            aHistoricalPeriod = eHistoricalPeriod.hpUndefined;
            TableType(historicalPeriod);

            foreach (string smb in symbols)
            {                

                dbSel.CreateBarsTable(smb, aTableType);

                CQGTimedBarsRequest request = CEL.CreateTimedBarsRequest();
                //LineTime = CEL.Environment.LineTime;

                request.RangeStart = rangeStart;
                request.RangeEnd = rangeEnd;
                request.SessionsFilter = sessionFilter;
                request.Symbol = smb;
                request.IntradayPeriod = aIntradayPeriod;
                if (aHistoricalPeriod!=eHistoricalPeriod.hpUndefined)
                    request.HistoricalPeriod = aHistoricalPeriod;
                                                               
                var bars = CEL.RequestTimedBars(request);
                var curTimedBars = CEL.AllTimedBars.get_ItemById(bars.Id);

                if (curTimedBars.Status == eRequestStatus.rsInProgress)
                {
                    var ss = new SymbolState();
                   
                    ss.isCollected = false;
                    ss.isSuccess = false;
                    aSymbolStates.Add(smb, ss);
                }    
            }            

            UpdateUI();
        }

        public void BarsAdd(CQGTimedBars m_CurTimedBars, CQGError cqgError)
        {
            try
            {
                if (cqgError != null && cqgError.Code != 0)
                {
                    logger.LogAdd("Invalid symbol: " + m_CurTimedBars.Request.Symbol, Category.Warning);
                    SymbolState ss = aSymbolStates[m_CurTimedBars.Request.Symbol];
                    ss.isCollected = true;
                    ss.isSuccess = false;
                    aSymbolStates[m_CurTimedBars.Request.Symbol] = ss;
                    UpdateUI();
                }
                else
                {
                    if (m_CurTimedBars.Status == eRequestStatus.rsSuccess)
                    {
                        DateTime runDateTime = DateTime.Now;
                        if (m_CurTimedBars.Count != 0)
                        {
                            for (int i = m_CurTimedBars.Count - 1; i >= 0; i--)
                            {
                                AddBar(m_CurTimedBars[i], (long)(i + 1), m_CurTimedBars.Request.Symbol, runDateTime, aTableType);
                            }
                        }
                        dbSel.CommitQueue();
                    }// else      
             
                    logger.LogAdd("Collecting finished for symbol: " + m_CurTimedBars.Request.Symbol, Category.Information);
                    dbSel.COMMIT();

                    SymbolState ss = aSymbolStates[m_CurTimedBars.Request.Symbol];
                    ss.isCollected = true;
                    ss.isSuccess = true;
                    aSymbolStates[m_CurTimedBars.Request.Symbol] = ss;
                    UpdateUI();
                }
            }
            catch (Exception exception)
            {
                logger.LogAdd("BarsAdd. " + exception, Category.Error);
            }            
        }        

        private void AddBar(CQGTimedBar timedBar, long recordIndex,string symbol, DateTime runDateTime, string tType)
        {            
            try
            {
                long num;
                string SQL = "";
                string str5 = symbol.Trim();
                string[] str = str5.Split('.');
                str5 = str[str.Length - 1];

                if (GetValueAsString(timedBar.Open) == "N/A")
                {
                }
                else
                {
                    GetValueAsString(timedBar.Open);
                }
                num = timedBar.Timestamp.ToFileTime();
                this.GetValueAsString(timedBar.Timestamp);
                string str3 = "'" + symbol + "'," +
                            GetValueAsString(Math.Max(timedBar.Open, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.High, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.Low, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.Close, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.TickVolume, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.ActualVolume, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.AskVolume, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.Avg, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.BidVolume, 0)) + "," +
                            GetValueAsString(string.Format("{0:0.00000}", timedBar.HLC3)) + "," +
                            GetValueAsString(Math.Max(timedBar.Mid, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.OpenInterest, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.Range, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.TrueHigh, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.TrueLow, 0)) + "," +
                            GetValueAsString(Math.Max(timedBar.TrueRange, 0)) + "," +
                            GetValueAsString("") + "," +
                            GetValueAsString(timedBar.Timestamp) + "," +
                            GetValueAsString(timedBar.Timestamp) + ",'" +
                            runDateTime.ToShortDateString() + "','" +
                            aContinuationType + "'";                

                SQL = "INSERT IGNORE INTO t_candle_" + str5 + "_" + tType + " (cdSymbol, cdOpen, cdHigh, cdLow, cdClose, cdTickVolume,cdActualVolume,cdAskVol,cdAvg,cdBidVol,cdHLC3,cdMid,cdOpenInterest," +
                    "cdRange,cdTrueHigh,cdTrueLow,cdTrueRange,cdTimeInterval, cdDT ,cddatenum, cdSystemDT,cn_type) VALUES (" + str3 + ");";

                //dbSel.DumpRecord_DBS(SQL);
                //todo: dbSel.DumpRecord_DBS(SQL);
                dbSel.AddSQLToQueueWithOutCommit(SQL);
            }
            catch (Exception exception)
            {
                logger.LogAdd("AddBar. " + exception, Category.Error);
            }
        }

        private string GetValueAsString(object val)
        {
            try
            {                 
                if ((val is Double) || (val is float))
                {
                    var v = (Double)val;
                    if (v == 0.0)
                        return "0.0";
                    return v.ToString("G", CultureInfo.InvariantCulture);
                }
                if (val is int)
                {
                    return Convert.ToString(val);
                }
                if (val is System.DateTime)
                    return "'" + Convert.ToDateTime(val).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                return "NULL";
            }
            catch (Exception)
            {
                logger.LogAdd("GetValueAsString", Category.Error);                
                return "0";
            }
        }

        #endregion /////////////////////////////////////////////////////////////////////////////////////////////

        #region Ticks  /////////////////////////////////////////////////////////////////////////////////////////////

        internal void TickRequest(CQGCEL CEL, List<string> symbols, DateTime rangeStart, DateTime rangeEnd, string continuationType)
        {
            CQGTicks _ticks;
            aSymbolStates.Clear();
            aContinuationType = continuationType;                        

            foreach (string smb in symbols)
            {
                CQGTicksRequest tickRequest = CEL.CreateTicksRequest();
                //LineTime = CEL.Environment.LineTime;
                tickRequest.RangeStart = rangeStart;
                tickRequest.RangeEnd = rangeEnd;
                tickRequest.Type = eTicksRequestType.trtSinceTimeNotify;
                tickRequest.Symbol = smb;

                _ticks = CEL.RequestTicks(tickRequest);

                if (_ticks.Status == eRequestStatus.rsInProgress)
                {
                    SymbolState ss = new SymbolState();

                    ss.isCollected = false;
                    ss.isSuccess = false;
                    aSymbolStates.Add(smb, ss);
                }
            }            
            UpdateUI();
        }

        public void TicksAdd(CQGTicks cqg_ticks, CQGError cqgError)
        {
            try
            {
                if (cqgError != null && cqgError.Code != 0)
                {
                    
                   logger.LogAdd("Invalid symbol or bad time range: " + cqg_ticks.Request.Symbol, Category.Warning); 

                    SymbolState ss = aSymbolStates[cqg_ticks.Request.Symbol];
                    ss.isCollected = true;
                    ss.isSuccess = false;
                    aSymbolStates[cqg_ticks.Request.Symbol] = ss;
                    UpdateUI();
                }
                else
                {
                    dbSel.CreateTickTable(cqg_ticks.Request.Symbol);

                    
                    DateTime runDateTime = DateTime.Now;
                    int groupId = 0;

                    if (cqg_ticks.Count != 0)
                    {
                        for (int i = cqg_ticks.Count - 1; i >= 0; i--)
                        {                            
                            AddTick(cqg_ticks[i], cqg_ticks.Request.Symbol, runDateTime, ++groupId );                            
                        }
                        
                    }
                    dbSel.CommitQueue();
                    dbSel.COMMIT();

                    SymbolState ss = aSymbolStates[cqg_ticks.Request.Symbol];
                    ss.isCollected = true;
                    ss.isSuccess = true;
                    aSymbolStates[cqg_ticks.Request.Symbol] = ss;
                    UpdateUI();
                }


            }
            catch (Exception exception)
            {
                logger.LogAdd("TicksAdd. " + exception, Category.Error);
            }
        }

        void AddTick(CQGTick tick,  string symbol, DateTime runDateTime, int groupId)
        {
            try{                                
    
                string[] str = symbol.Trim().Split('.');
                String query = "INSERT IGNORE INTO t_tick_" + str[str.Length - 1];
                query += "(Symbol, Price, Volume, TickTime, CollectTime, ContinuationType, PriceType, GroupID) VALUES";
                query += "('";
                query += symbol + "',";
                query += GetValueAsString(tick.Price) + ",";
                query += GetValueAsString(tick.Volume) + ",";
                query += GetValueAsString(tick.Timestamp) + ",";
                query += GetValueAsString(runDateTime) + ",";
                query += "'" + aContinuationType + "',";
                query += "'" + tick.PriceType.ToString() + "',";
                query += GetValueAsString(groupId) + ");";

                dbSel.AddSQLToQueueWithOutCommit(query);
            }
            catch (Exception exception)
            {
                logger.LogAdd("AddTick. " + exception, Category.Error);
            }
        }
        #endregion /////////////////////////////////////////////////////////////////////////////////////////////

        #region LISTS /////////////////////////////////////////////////////////////////////////////////////////////

        #endregion

        #region Missing bars ////

        readonly List<ListViewItem> aItems = new List<ListViewItem>();
        readonly List<ListViewGroup> aGroups = new List<ListViewGroup>();
        Semaphore SemaphoreGettingSessionData;

        internal void MissingBarRequest(CQGCEL CEL, string[] symbols,  int maxCount, bool isAuto = false)
        {            
            MaxBarsLookBack = Math.Abs(maxCount);
            aSymbolStates.Clear();
            dbSel.CreateMissingBarExceptionTable();
            dbSel.CreateSessionHolidayTimesTable();

            SemaphoreGettingSessionData = new Semaphore(0, 1);
            List<string> aList = new List<string>();
            //string[] newSymbols;

            for (int i = 0; i < symbols.Length; i++)
            {
                if (!aList.Contains(symbols[i]))
                {
                    aList.Add(symbols[i]);
                }
            }
            symbols = aList.ToArray();


            foreach (string smb in symbols)
            {
                SymbolState ss = new SymbolState();

                ss.isCollected = false;
                ss.isSuccess = false;
                if (!aSymbolStates.ContainsKey(smb))
                    aSymbolStates.Add(smb, ss);    
                //TODO ggg
            }

            aItems.Clear();
            aGroups.Clear();

            UpdateUI();            

            // Store Holidays
            new Thread(() =>
            {
                Thread.CurrentThread.Name = "AsyncGetingSessionsDataThread";
                StartAsyncGetingSessionsData(CEL, symbols);
                SemaphoreGettingSessionData.Release();
            }).Start();

            // Finding Missed bars
            new Thread(() =>
            {
                Thread.CurrentThread.Name = "AsyncCheckingMissedBarsThread";
                SemaphoreGettingSessionData.WaitOne();
                if (isAuto)
                    StartAsyncCheckingMissedBarsAuto(symbols, MaxBarsLookBack);
                else
                    StartAsyncCheckingMissedBars(symbols);
            }).Start();

           
        }

        private void StartAsyncGetingSessionsData(CQGCEL CEL, string[] symbols)
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                string symbol = symbols[i];
                aSemaphoreHolidays = new Semaphore(0, 1);
                aSemaphoreSessions = new Semaphore(0, 1);

                List<DateTime> aResultDateTimes = dbSel.getAllDateTimes(dbSel.getTableFromSymbol(symbol));

                if (aResultDateTimes==null || aResultDateTimes.Count == 0)
                    continue;                

                DateTime RangeBegin = aResultDateTimes.First();
                DateTime RangeEnd = aResultDateTimes.Last();
                CQGHistoricalSessionsRequest req;
                req = CEL.CreateHistoricalSessionsRequest();

                eHistoricalSessionsRequestType histSessionsReqType;
                histSessionsReqType = eHistoricalSessionsRequestType.hsrtTimeRange;
                req.Type = histSessionsReqType;
                req.Symbol = symbol;
                req.RangeStart = RangeBegin;
                req.RangeEnd = RangeEnd;

                CEL.RequestHistoricalSessions(req);
                aSemaphoreHolidays.WaitOne(20000);// wait


                CEL.NewInstrument(symbol);
                aSemaphoreSessions.WaitOne(20000);// wait
            }
        }

        private void StartAsyncCheckingMissedBars(string[] symbols)
        {
            dbSel.COMMIT();
            
            for (int sInd = 0; sInd < symbols.Length; sInd++)
            {
                
                string currentSymbol = symbols[sInd];
                
                dbSel.DelFromReport(currentSymbol);

                List<DateTime> aResultDates=new List<DateTime>();
                List<DateTime> aResultDateTimes = new List<DateTime>();

                if (dbSel.TableExists(dbSel.getTableFromSymbol(currentSymbol)))
                {
                    aResultDates = dbSel.getAllDates(dbSel.getTableFromSymbol(currentSymbol));
                    aResultDateTimes = dbSel.getAllDateTimes(dbSel.getTableFromSymbol(currentSymbol));

                }
                if (aResultDates==null||aResultDates.Count == 0)
                {
                    SymbolState ss = aSymbolStates[currentSymbol];
                    ss.isCollected = true;
                    ss.isSuccess = true;
                    aSymbolStates[currentSymbol] = ss;

                    UpdateUI();

                    logger.LogAdd("No records in database for symbol: " + currentSymbol , Category.Warning);                    
                    continue;
                }
                DateTime refresh = DateTime.Now;
                if (aResultDates != null)
                {

                    ListViewGroup LVgroup;
                    ListViewItem LVitem;
                    List<DateTime> missingList = new List<DateTime>();

                    DateTime MissDateTimeStart;
                    DateTime MissDateTimeEnd;

                    // ADD SESSION TABLE ??

                    // ADD GROUP "F.US.KCEK3"
                    LVgroup = new ListViewGroup();
                    LVgroup.Name = currentSymbol;
                    LVgroup.Header = currentSymbol;
                    aGroups.Add(LVgroup);

                    // ADD SESSION DAYS
                    for (DateTime curDT = aResultDates.First(); curDT <= aResultDates.Last(); curDT = curDT.AddDays(1))
                    {
                        bool DayStartsYesterday = false;
                        var State = SessionStates.OpenedNormal;
                        if (dbSel.HolidaysContains(dbSel.getTableFromSymbol(currentSymbol), curDT))
                        {
                            State = SessionStates.ClosedHoliday;
                        }

                        DateTime sTime = curDT.Date.Add(GetStartTime(currentSymbol, listSession, curDT.DayOfWeek, out DayStartsYesterday).TimeOfDay);
                        DateTime eTime = curDT.Date.Add(GetEndTime(currentSymbol, listSession, curDT.DayOfWeek).TimeOfDay);
                        // DayStartsYesterday
                        if (sTime.TimeOfDay >= eTime.TimeOfDay)
                        {
                            DayStartsYesterday = true;
                        }
                        else
                        {
                            DayStartsYesterday = false;
                        }
                        // change first
                        if (curDT == aResultDates.First())
                            MissDateTimeStart = aResultDateTimes.First();
                        else
                            MissDateTimeStart = sTime;

                        // change last
                        if (curDT == aResultDates.Last() && eTime.TimeOfDay > aResultDateTimes.Last().TimeOfDay)
                            MissDateTimeEnd = aResultDateTimes.Last();
                        else
                            MissDateTimeEnd = eTime;


                        if (State == SessionStates.ClosedHoliday)
                        {

                            if (aResultDateTimes.Where(a => a.Date == curDT.Date).Count() > 0)
                            {
                                State = SessionStates.OpenedHoliday;
                            }
                            else
                            {
                                sTime = sTime.Date;
                                eTime = eTime.Date;
                            }
                        }
                        else if (sTime.TimeOfDay == eTime.TimeOfDay && sTime.TimeOfDay == DateTime.Today.TimeOfDay)
                        {
                            State = SessionStates.ClosedNormal;
                            DayStartsYesterday = false;
                        }
                        if (DayStartsYesterday) sTime = sTime.AddDays(-1);

                        LVitem = new ListViewItem();
                        LVitem.Group = LVgroup;
                        switch (State)
                        {
                            case SessionStates.OpenedNormal:
                                LVitem.ForeColor = Color.DarkGreen;
                                break;
                            case SessionStates.ClosedNormal:
                                LVitem.ForeColor = Color.Blue;
                                break;
                            case SessionStates.ClosedHoliday:
                                LVitem.ForeColor = Color.DarkGoldenrod;
                                break;
                            case SessionStates.OpenedHoliday:
                                LVitem.ForeColor = Color.MediumSeaGreen;
                                break;
                            default:
                                LVitem.ForeColor = Color.DarkGreen;
                                break;
                        }
                        LVitem.Text = curDT.ToShortDateString();                                                    // Date                                
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, State.ToString()));            // state
                        //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, curDT.DayOfWeek.ToString()));  // Day

                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, sTime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, sTime.ToString("dd.MM HH:mm")));     //  start
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, eTime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, eTime.ToString("dd.MM HH:mm")));     // end

                        aItems.Add(LVitem);
                        
                        dbSel.AddToReport(currentSymbol, curDT, State.ToString(), sTime.DayOfWeek.ToString(), sTime, eTime.DayOfWeek.ToString(), eTime);

                        if (State == SessionStates.OpenedHoliday || State == SessionStates.OpenedNormal)
                        {
                            List<MissedStr> aMissedList = new List<MissedStr>();
                            // FINDING MISSED BARS in current day
                            
                            aMissedList = MissedInTable(currentSymbol, aResultDateTimes, MissDateTimeStart, MissDateTimeEnd, DayStartsYesterday);
                         


                            foreach (MissedStr item in aMissedList)
                            {
                                State = SessionStates.Missed;

                                LVitem = new ListViewItem();
                                LVitem.Group = LVgroup;
                                LVitem.ForeColor = Color.DarkRed;
                                LVitem.Text = item.start.ToShortDateString();                                                    // Date                                
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, State.ToString()));            // state
                                //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.DayOfWeek.ToString()));  // Day

                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.ToString("dd.MM HH:mm")));//  start
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.end.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.end.ToString("dd.MM HH:mm")));  // end
                                aItems.Add(LVitem);
                                
                                dbSel.AddToReport(currentSymbol, item.start, State.ToString(), item.start.DayOfWeek.ToString(), item.start, item.end.DayOfWeek.ToString(), item.end);
                            }
                        }
                    }//end:for curDT
                }// end:if                                

                // SECOND PART: Finding Missed bar that now is not missing
                               
                List<DateTime> aMissedBarsForSymbol = dbSel.getMissedBarsForSymbol(currentSymbol);

                int index = Math.Max(0, aResultDateTimes.Count - MaxBarsLookBack);
                DateTime first = aResultDateTimes[index];

                List<DateTime> aSmallMissedBarsForSymbol = aMissedBarsForSymbol.Where(a => a > first).ToList();

                foreach (DateTime missedItem in aSmallMissedBarsForSymbol)
                {
                    //if (dbSel.rowExists(dbSel.getTableFromSymbol(currentSymbol), missedItem))
                    if (aResultDateTimes.Contains(missedItem))
                    {
                        //TODO Without commit update
                        dbSel.ChangeBarStatusInMissingTableWithOutCommit(currentSymbol, refresh, missedItem);
                    }
                }

                dbSel.CommitQueue();

                SymbolState ss1 = aSymbolStates[currentSymbol];
                ss1.isCollected = true;
                ss1.isSuccess = true;
                aSymbolStates[currentSymbol] = ss1;
                logger.LogAdd("Repost finished for symbol: " + currentSymbol, Category.Information);                

                UpdateUI();    

            }// end: for all symbols  
            dbSel.COMMIT();
            ResetSymbols();
            // todo of all
        }

        private void StartAsyncCheckingMissedBarsAuto(string[] symbols, int maxCount)
        {
            dbSel.COMMIT();

            for (int sInd = 0; sInd < symbols.Length; sInd++)
            {

                string currentSymbol = symbols[sInd];                

                List<DateTime> aResultDates = new List<DateTime>();
                List<DateTime> aResultDateTimes = new List<DateTime>();

                if (dbSel.TableExists(dbSel.getTableFromSymbol(currentSymbol)))
                {
                    
                    aResultDateTimes = dbSel.getAllDateTimes(dbSel.getTableFromSymbol(currentSymbol), maxCount+1);

                    aResultDates = dbSel.getAllDates(dbSel.getTableFromSymbol(currentSymbol),maxCount+1);

                    dbSel.DelFromReport(currentSymbol, aResultDateTimes.First());

                }
                if (aResultDates == null || aResultDates.Count == 0)
                {
                    SymbolState ss = aSymbolStates[currentSymbol];
                    ss.isCollected = true;
                    ss.isSuccess = true;
                    aSymbolStates[currentSymbol] = ss;

                    UpdateUI();

                    logger.LogAdd("No records in database for symbol: " + currentSymbol, Category.Warning);
                    continue;
                }
                DateTime refresh = DateTime.Now;

                

                if (aResultDates != null)
                {

                    ListViewGroup LVgroup;
                    ListViewItem LVitem;
                    List<DateTime> missingList = new List<DateTime>();

                    DateTime MissDateTimeStart;
                    DateTime MissDateTimeEnd;

                    // ADD SESSION TABLE ??

                    // ADD GROUP "F.US.KCEK3"
                    LVgroup = new ListViewGroup();
                    LVgroup.Name = currentSymbol;
                    LVgroup.Header = currentSymbol;
                    aGroups.Add(LVgroup);

                    #region Get old report data
                    var res = dbSel.GetReport(currentSymbol);
                    foreach (var reportItem in res)
                    {
                        LVitem = new ListViewItem();
                        LVitem.Group = LVgroup;

                        switch (reportItem.state)
                        {
                            case "OpenedNormal":
                                LVitem.ForeColor = Color.DarkGreen;
                                break;
                            case "ClosedNormal":
                                LVitem.ForeColor = Color.Blue;
                                break;
                            case "ClosedHoliday":
                                LVitem.ForeColor = Color.DarkGoldenrod;
                                break;
                            case "OpenedHoliday":
                                LVitem.ForeColor = Color.MediumSeaGreen;
                                break;
                            default:
                                LVitem.ForeColor = Color.DarkGreen;
                                break;
                        }
                        
                        LVitem.Text = reportItem.curDate.ToShortDateString();                                                    // Date                                
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.state.ToString()));            // state
                        //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, curDT.DayOfWeek.ToString()));  // Day

                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.sTime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.sTime.ToString("dd.MM HH:mm")));     //  start
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.eTime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.eTime.ToString("dd.MM HH:mm")));     // end

                        aItems.Add(LVitem);
                    }
                    #endregion

                    // ADD SESSION DAYS
                    for (DateTime curDT = aResultDates.First(); curDT <= aResultDates.Last(); curDT = curDT.AddDays(1))
                    {
                        #region start settings

                        bool DayStartsYesterday = false;
                        SessionStates State = SessionStates.OpenedNormal;
                        if (dbSel.HolidaysContains(dbSel.getTableFromSymbol(currentSymbol), curDT))
                        {
                            State = SessionStates.ClosedHoliday;
                        }

                        DateTime sTime = curDT.Date.Add(GetStartTime(currentSymbol, listSession, curDT.DayOfWeek, out DayStartsYesterday).TimeOfDay);
                        DateTime eTime = curDT.Date.Add(GetEndTime(currentSymbol, listSession, curDT.DayOfWeek).TimeOfDay);
                        // DayStartsYesterday
                        if (sTime.TimeOfDay >= eTime.TimeOfDay)
                        {
                            DayStartsYesterday = true;
                        }
                        else
                        {
                            DayStartsYesterday = false;
                        }
                        // change first
                        if (curDT == aResultDates.First())
                            MissDateTimeStart = aResultDateTimes.First();
                        else
                            MissDateTimeStart = sTime;

                        // change last
                        if (curDT == aResultDates.Last() && eTime.TimeOfDay > aResultDateTimes.Last().TimeOfDay)
                            MissDateTimeEnd = aResultDateTimes.Last();
                        else
                            MissDateTimeEnd = eTime;


                        if (State == SessionStates.ClosedHoliday)
                        {

                            if (aResultDateTimes.Where(a => a.Date == curDT.Date).Count() > 0)
                            {
                                State = SessionStates.OpenedHoliday;
                            }
                            else
                            {
                                sTime = sTime.Date;
                                eTime = eTime.Date;
                            }
                        }
                        else if (sTime.TimeOfDay == eTime.TimeOfDay && sTime.TimeOfDay == DateTime.Today.TimeOfDay)
                        {
                            State = SessionStates.ClosedNormal;
                            DayStartsYesterday = false;
                        }
                        if (DayStartsYesterday) sTime = sTime.AddDays(-1);

                        #endregion

                        #region updating LV

                        LVitem = new ListViewItem();
                        LVitem.Group = LVgroup;
                        switch (State)
                        {
                            case SessionStates.OpenedNormal:
                                LVitem.ForeColor = Color.DarkGreen;
                                break;
                            case SessionStates.ClosedNormal:
                                LVitem.ForeColor = Color.Blue;
                                break;
                            case SessionStates.ClosedHoliday:
                                LVitem.ForeColor = Color.DarkGoldenrod;
                                break;
                            case SessionStates.OpenedHoliday:
                                LVitem.ForeColor = Color.MediumSeaGreen;
                                break;
                            default:
                                LVitem.ForeColor = Color.DarkGreen;
                                break;
                        }
                        LVitem.Text = curDT.ToShortDateString();                                                    // Date                                
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, State.ToString()));            // state
                        //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, curDT.DayOfWeek.ToString()));  // Day

                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, sTime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, sTime.ToString("dd.MM HH:mm")));     //  start
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, eTime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, eTime.ToString("dd.MM HH:mm")));     // end

                        aItems.Add(LVitem);
                        
                        dbSel.AddToReport(currentSymbol, curDT, State.ToString(), sTime.DayOfWeek.ToString(), sTime, eTime.DayOfWeek.ToString(), eTime);

                        #endregion 

                        #region misssed update LV
                        if (State == SessionStates.OpenedHoliday || State == SessionStates.OpenedNormal)
                        {
                            List<MissedStr> aMissedList = new List<MissedStr>();
                            // FINDING MISSED BARS in current day

                            aMissedList = MissedInTable(currentSymbol, aResultDateTimes, MissDateTimeStart, MissDateTimeEnd, DayStartsYesterday);



                            foreach (MissedStr item in aMissedList)
                            {
                                State = SessionStates.Missed;

                                LVitem = new ListViewItem();
                                LVitem.Group = LVgroup;
                                LVitem.ForeColor = Color.DarkRed;
                                LVitem.Text = item.start.ToShortDateString();                                                    // Date                                
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, State.ToString()));            // state
                                //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.DayOfWeek.ToString()));  // Day

                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.ToString("dd.MM HH:mm")));//  start
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.end.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.end.ToString("dd.MM HH:mm")));  // end
                                aItems.Add(LVitem);
                                
                                    dbSel.AddToReport(currentSymbol, item.start, State.ToString(), item.start.DayOfWeek.ToString(), item.start, item.end.DayOfWeek.ToString(), item.end);
                            }
                        }
                        #endregion

                    }//end:for curDT
                }// end:if                                

                // SECOND PART: Finding Missed bar that now is not missing

                List<DateTime> aMissedBarsForSymbol = dbSel.getMissedBarsForSymbol(currentSymbol);

                int index = Math.Max(0, aResultDateTimes.Count - MaxBarsLookBack);
                DateTime first = aResultDateTimes[index];

                List<DateTime> aSmallMissedBarsForSymbol = aMissedBarsForSymbol.Where(a => a > first).ToList();

                foreach (DateTime missedItem in aSmallMissedBarsForSymbol)
                {
                    //if (dbSel.rowExists(dbSel.getTableFromSymbol(currentSymbol), missedItem))
                    if (aResultDateTimes.Contains(missedItem))
                    {
                        //TODO Without commit update
                        dbSel.ChangeBarStatusInMissingTableWithOutCommit(currentSymbol, refresh, missedItem);
                    }
                }

                dbSel.CommitQueue();

                SymbolState ss1 = aSymbolStates[currentSymbol];
                ss1.isCollected = true;
                ss1.isSuccess = true;
                aSymbolStates[currentSymbol] = ss1;
                logger.LogAdd("Repost finished for symbol: " + currentSymbol, Category.Information);

                UpdateUI();

            }// end: for all symbols  
            dbSel.COMMIT();
            ResetSymbols();
            // todo of all
        }

        private List<MissedStr> MissedInTable(string smb, List<DateTime> aResultDateTimes, DateTime MissDateTimeStart, DateTime MissDateTimeEnd, bool DayStartsYesterday)
        {
            
            List<MissedStr> resultList = new List<MissedStr>();
            List<DateTime> missingList = new List<DateTime>();
            DateTime refresh = DateTime.Now;
            DateTime StartDateTime = DayStartsYesterday ? MissDateTimeStart.AddDays(-1) : MissDateTimeStart;
            // MISSED

            for (DateTime curTime = StartDateTime; curTime < MissDateTimeEnd; curTime = curTime.AddMinutes(1))
            {
                // not exsists and its after first
                if (!ExistsTime(aResultDateTimes, curTime) && curTime > aResultDateTimes[0])
                {
                    
                        dbSel.AddToMissingTableWithOutCommit(smb, refresh, curTime);
                    
                    missingList.Add(curTime);
                }
            }

            dbSel.CommitQueue();
            
            if (missingList.Count == 1)
            {
                resultList.Add(new MissedStr { start = missingList[0], end = missingList[0] });
            }
            if (missingList.Count > 1)
            {
                DateTime first = missingList[0];
                DateTime last = missingList[0];
                bool haveLast = false;
                for (int i = 1; i < missingList.Count; i++)
                {
                    DateTime item = missingList[i];
                    if (last.AddMinutes(1) == item)
                    {
                        last = item;
                        if (i != missingList.Count - 1)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (i == missingList.Count - 1)//last
                        {
                            haveLast = true;
                        }
                    }

                    resultList.Add(new MissedStr { start = first, end = last });

                    last = first = item;
                    if (haveLast)
                    {
                        resultList.Add(new MissedStr { start = first, end = last });
                    }
                }
            }

            return resultList;
        }

        private bool ExistsTime(List<DateTime> aResultDateTimes, DateTime curTime)
        {
            foreach (var item in aResultDateTimes)
            {
                if (item == curTime)
                {
                    return true;
                }
            }
            return false;
        }

        private DateTime GetStartTime(string smb, List<SessionData> listSession, DayOfWeek dayOfWeek, out bool DayStartsYesterday)
        {
            List<SessionData> alist = new List<SessionData>();
            eSessionWeekDays curDay = ConvertToSessionWeekDay(dayOfWeek);

            alist = listSession.Where(a => a.symbol == smb).ToList();
            foreach (var item in alist)
            {
                if ((item.dayOfWeek & curDay) == curDay)
                {
                    DayStartsYesterday = item.DayStartsYesterday;
                    return item.startTime;
                }
            }
            DayStartsYesterday = false;
            return DateTime.Today;
        }

        private DateTime GetEndTime(string smb, List<SessionData> listSession, DayOfWeek dayOfWeek)
        {
            List<SessionData> alist = new List<SessionData>();
            eSessionWeekDays curDay = ConvertToSessionWeekDay(dayOfWeek);

            alist = listSession.Where(a => a.symbol == smb).ToList();
            DateTime res = DateTime.Today;
            foreach (var item in alist)
            {
                if ((item.dayOfWeek & curDay) == curDay)
                {
                    res =  item.endTime;
                }
            }
            return res;
        }

        private eSessionWeekDays ConvertToSessionWeekDay(DayOfWeek dayOfWeek)
        {
            eSessionWeekDays es;
            switch (dayOfWeek)
            {
                case DayOfWeek.Sunday:
                    es = eSessionWeekDays.swdSunday;
                    break;
                case DayOfWeek.Monday:
                    es = eSessionWeekDays.swdMonday;
                    break;
                case DayOfWeek.Tuesday:
                    es = eSessionWeekDays.swdTuesday;
                    break;
                case DayOfWeek.Wednesday:
                    es = eSessionWeekDays.swdWednesday;
                    break;
                case DayOfWeek.Thursday:
                    es = eSessionWeekDays.swdThursday;
                    break;
                case DayOfWeek.Friday:
                    es = eSessionWeekDays.swdFriday;
                    break;
                default:
                    es = eSessionWeekDays.swdSaturday;
                    break;
            }
            return es;
        }

        #endregion


        #region ////

        void TableType(string tableTypeFull)
        {
            switch (tableTypeFull)
            {
                case "1 minute":
                    aIntradayPeriod = 1;
                    aTableType = "1m";
                    break;
                case "2 minutes":
                    aIntradayPeriod = 2;
                    aTableType = "2m";
                    break;
                case "3 minutes":
                    aIntradayPeriod = 3;
                    aTableType = "3m";
                    break;
                case "5 minutes":
                    aIntradayPeriod = 5;
                    aTableType = "5m";
                    break;
                case "10 minute":
                    aIntradayPeriod = 10;
                    aTableType = "10m";
                    break;
                case "15 minutes":
                    aIntradayPeriod = 15;
                    aTableType = "15m";
                    break;
                case "30 minutes":
                    aIntradayPeriod = 30;
                    aTableType = "30m";
                    break;
                case "60 minutes":
                    aIntradayPeriod = 60;
                    aTableType = "60m";
                    break;
                case "240 minutes":
                    aIntradayPeriod = 240;
                    aTableType = "240m";
                    break;

                case "Daily":
                    aHistoricalPeriod = eHistoricalPeriod.hpDaily;
                    aTableType = "Daily";
                    break;
                case "Weekly":
                    aHistoricalPeriod = eHistoricalPeriod.hpWeekly;
                    aTableType = "Weekly";
                    break;
                case "Monthly":
                    aHistoricalPeriod = eHistoricalPeriod.hpMonthly;
                    aTableType = "Monthly";
                    break;
                case "Quarterly":
                    aHistoricalPeriod = eHistoricalPeriod.hpQuarterly;
                    aTableType = "Quarterly";
                    break;
                case "Yearly":
                    aHistoricalPeriod = eHistoricalPeriod.hpYearly;
                    aTableType = "Yearly";
                    break;
                case "Semiannual":
                    aHistoricalPeriod = eHistoricalPeriod.hpSemiannual;
                    aTableType = "Semiannual";
                    break;

                default:
                    aIntradayPeriod = 1;
                    aTableType = "1m";
                    break;
            }
        }

        internal void UpdateUI()
        {

            if (aPB_state != null)
            {
                aPB_state.Invoke((Action)delegate
                {
                    aPB_state.Maximum = aSymbolStates.Count;
                    aPB_state.Value = SymbolsCollected;
                });
            }

            if (aLV_report != null)
            {
                aLV_report.Invoke((Action)delegate
                {
                    if (SymbolsCollected < aSymbolStates.Count)
                    {
                        aLV_report.Items.Clear();
                        aLV_report.Groups.Clear();
                    }
                    else
                    {
                        aLV_report.Groups.AddRange(aGroups.ToArray());
                        aLV_report.Items.AddRange(aItems.ToArray());
                    }
                });
                
            }
            if (aL_status != null)
            {
                aL_status.Invoke((Action)delegate
                {
                    if (SymbolsCollected < aSymbolStates.Count)
                        aL_status.Text = STAUS_WORKING + "[" + SymbolsCollected + "/" + aSymbolStates.Count + "]";
                    else
                    {
                        aL_status.Text = STAUS_READY;
                        try
                        {
                            //if (SemaphoreEndOfCollecting != null) SemaphoreEndOfCollecting.Release();
                            if (aSemaphoreWait != null) aSemaphoreWait.Release();
                        }
                        catch (Exception) { }
                    }
                });
            }

        }

        public int SymbolsCollected {
            get
            {
                int collectedCount = 0;
                foreach (var item in aSymbolStates)
                {
                    if (item.Value.isCollected) collectedCount++;
                }
                return collectedCount;
            }
        }

        internal void WaitEndOfOperation()
        {            
            if (SymbolsCollected < aSymbolStates.Count)
            {
                if (aSemaphoreWait == null) 
                    aSemaphoreWait = new Semaphore(0, 1);
                aSemaphoreWait.WaitOne();
            }            
        }

        internal Brush getColor(string symbol)
        {
            if (aSymbolStates.ContainsKey(symbol) && aSymbolStates[symbol].isCollected)
            {
                return aSymbolStates[symbol].isSuccess ? Brushes.LightGreen : Brushes.Red;
            }
            return Brushes.Black;
        }

        internal void ResetSymbols()
        {            
            aSymbolStates.Clear();
        }

        #endregion

        internal bool IsBusy()
        {
            return SymbolsCollected < aSymbolStates.Count;
        }
    }
}
