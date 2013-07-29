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
        const string StausReady = "Ready";
        const string StausWorking = "Please wait. Working. ";
        int _maxBarsLookBack = 3000;

        //readonly DbSelector dbSel;
        readonly Logger _logger;
        Semaphore _aSemaphoreHolidays;
        Semaphore _aSemaphoreSessions;
        Semaphore _aSemaphoreWait;
        //Semaphore SemaphoreEndOfCollecting;

        string _aTableType;
        int _aIntradayPeriod;
        eHistoricalPeriod _aHistoricalPeriod;
        string _aContinuationType;

        readonly Dictionary<string, SymbolState> _aSymbolStates = new Dictionary<string, SymbolState>();
        public struct SymbolState
        {
            public bool IsCollected;
            public bool IsSuccess;
        }
        enum SessionStates { OpenedHoliday, OpenedNormal, ClosedHoliday, ClosedNormal, Missed }
        public struct MissedStr
        {
            public DateTime Start;
            public DateTime End;
        }
        //*** UI

        DevComponents.DotNetBar.LabelItem _aLStatus;
        ListBox _aLbSymbols;
        CheckedListBox _aCbLists;
        DevComponents.DotNetBar.ProgressBarItem _aPbState;
        ListView _aLvReport;
        //**

        public DataCollector(Logger log)
        {
            //dbSel = DbSelector.GetInstance();
            _logger = log;            
        }

        public void Subscribe(DevComponents.DotNetBar.LabelItem lStatus, ListBox lbSymbols, CheckedListBox cbLists, DevComponents.DotNetBar.ProgressBarItem pbState, ListView lvReport)
        {
            _aLStatus = lStatus;
            _aLbSymbols = lbSymbols;
            _aCbLists = cbLists;
            _aPbState = pbState;
            _aLvReport = lvReport;
        }

        #region Sessions & Holidays /////////////////////////////////////////////////////////////////////////////////////////////

        public void SessionAdd(CQGSessions sessions, string symbol)
        {
            try
            {
                foreach (CQGSession session in sessions)
                {
                    SessionData one = new SessionData();
                    one.StartTime = session.StartTime;
                    one.EndTime = session.EndTime;
                    one.DayOfWeek = session.WorkingWeekDays;
                    one.Symbol = symbol;
                    one.DayStartsYesterday = session.DayStartsYesterday;
                    _listSession.Add(one);

                    ClientDataManager.AddToSessionTable(symbol, symbol, session.StartTime, session.EndTime, "Open",
                        GetSessionWorkingDays(session.WorkingWeekDays), session.DayStartsYesterday, session.PrimaryFlag, session.Number, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogAdd("SessionAdd. " + ex.Message, Category.Error);
            }

            _aSemaphoreSessions.Release();      
        }

        public void HolidaysAdd(CQGSessionsCollection sessions, string symbol)
        {
            try
            {
                foreach (CQGSessions session in sessions)
                {
                    foreach (CQGHoliday holiday in session.Holidays)
                    {
                        ClientDataManager.AddToSessionTable(symbol, symbol, holiday.HolidayDate, holiday.HolidayDate, "Holiday", "", false, false, 0, DateTime.Now);
                    }

                }
                
            }
            catch (Exception ex)
            {
                _logger.LogAdd("HolidaysAdd. " + ex.Message, Category.Error);
            }

            _aSemaphoreHolidays.Release();
        }

        public struct SessionData
        {
            public string Symbol;
            public DateTime StartTime;
            public DateTime EndTime;
            public eSessionWeekDays DayOfWeek;
            public bool DayStartsYesterday;
        }

        private readonly List<SessionData> _listSession = new List<SessionData>();

        private string GetSessionWorkingDays(eSessionWeekDays weekDay)
        {
            string sResult = (((weekDay & eSessionWeekDays.swdSunday) == eSessionWeekDays.swdSunday) ? "S" : "-").ToString();
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
            _aSymbolStates.Clear();
            _aContinuationType = continuationType;
            _aHistoricalPeriod = eHistoricalPeriod.hpUndefined;
            TableType(historicalPeriod);

            foreach (string smb in symbols)
            {                

                ClientDataManager.CreateBarsTable(smb, _aTableType);

                CQGTimedBarsRequest request = CEL.CreateTimedBarsRequest();
                //LineTime = CEL.Environment.LineTime;

                request.RangeStart = rangeStart;
                request.RangeEnd = rangeEnd;
                request.SessionsFilter = sessionFilter;
                request.Symbol = smb;
                request.IntradayPeriod = _aIntradayPeriod;
                if (_aHistoricalPeriod!=eHistoricalPeriod.hpUndefined)
                    request.HistoricalPeriod = _aHistoricalPeriod;
                                                               
                var bars = CEL.RequestTimedBars(request);
                var curTimedBars = CEL.AllTimedBars.get_ItemById(bars.Id);

                if (curTimedBars.Status == eRequestStatus.rsInProgress)
                {
                    var ss = new SymbolState {IsCollected = false, IsSuccess = false};

                    _aSymbolStates.Add(smb, ss);
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
                    _logger.LogAdd("Invalid symbol: " + m_CurTimedBars.Request.Symbol, Category.Warning);
                    SymbolState ss = _aSymbolStates[m_CurTimedBars.Request.Symbol];
                    ss.IsCollected = true;
                    ss.IsSuccess = false;
                    _aSymbolStates[m_CurTimedBars.Request.Symbol] = ss;
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
                                AddBar(m_CurTimedBars[i], (long)(i + 1), m_CurTimedBars.Request.Symbol, runDateTime, _aTableType);
                            }
                        }
                        ClientDataManager.CommitQueue();
                    }// else      
             
                    _logger.LogAdd("Collecting finished for symbol: " + m_CurTimedBars.Request.Symbol, Category.Information);
                    //dbSel.COMMIT();

                    SymbolState ss = _aSymbolStates[m_CurTimedBars.Request.Symbol];
                    ss.IsCollected = true;
                    ss.IsSuccess = true;
                    _aSymbolStates[m_CurTimedBars.Request.Symbol] = ss;
                    UpdateUI();
                }
            }
            catch (Exception exception)
            {
                _logger.LogAdd("BarsAdd. " + exception, Category.Error);
            }            
        }        

        private void AddBar(CQGTimedBar timedBar, long recordIndex,string symbol, DateTime runDateTime, string tType)
        {            
            try
            {
                long num;
                var sql = "";
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
                            _aContinuationType + "'";                

                sql = "INSERT IGNORE INTO t_candle_" + str5 + "_" + tType + " (cdSymbol, cdOpen, cdHigh, cdLow, cdClose, cdTickVolume,cdActualVolume,cdAskVol,cdAvg,cdBidVol,cdHLC3,cdMid,cdOpenInterest," +
                    "cdRange,cdTrueHigh,cdTrueLow,cdTrueRange,cdTimeInterval, cdDT ,cddatenum, cdSystemDT,cn_type) VALUES (" + str3 + ");";

                //dbSel.DumpRecord_DBS(SQL);
                //todo: dbSel.DumpRecord_DBS(SQL);
                ClientDataManager.AddToQueue(sql);
            }
            catch (Exception exception)
            {
                _logger.LogAdd("AddBar. " + exception, Category.Error);
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
                _logger.LogAdd("GetValueAsString", Category.Error);                
                return "0";
            }
        }

        #endregion /////////////////////////////////////////////////////////////////////////////////////////////

        #region Ticks  /////////////////////////////////////////////////////////////////////////////////////////////

        internal void TickRequest(CQGCEL CEL, List<string> symbols, DateTime rangeStart, DateTime rangeEnd, string continuationType)
        {
            CQGTicks _ticks;
            _aSymbolStates.Clear();
            _aContinuationType = continuationType;                        

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

                    ss.IsCollected = false;
                    ss.IsSuccess = false;
                    _aSymbolStates.Add(smb, ss);
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
                    
                   _logger.LogAdd("Invalid symbol or bad time range: " + cqg_ticks.Request.Symbol, Category.Warning); 

                    SymbolState ss = _aSymbolStates[cqg_ticks.Request.Symbol];
                    ss.IsCollected = true;
                    ss.IsSuccess = false;
                    _aSymbolStates[cqg_ticks.Request.Symbol] = ss;
                    UpdateUI();
                }
                else
                {
                    ClientDataManager.CreateTickTable(cqg_ticks.Request.Symbol);

                    
                    DateTime runDateTime = DateTime.Now;
                    int groupId = 0;

                    if (cqg_ticks.Count != 0)
                    {
                        for (int i = cqg_ticks.Count - 1; i >= 0; i--)
                        {                            
                            AddTick(cqg_ticks[i], cqg_ticks.Request.Symbol, runDateTime, ++groupId );                            
                        }
                        
                    }
                    ClientDataManager.CommitQueue();
                    //dbSel.COMMIT();

                    SymbolState ss = _aSymbolStates[cqg_ticks.Request.Symbol];
                    ss.IsCollected = true;
                    ss.IsSuccess = true;
                    _aSymbolStates[cqg_ticks.Request.Symbol] = ss;
                    UpdateUI();
                }


            }
            catch (Exception exception)
            {
                _logger.LogAdd("TicksAdd. " + exception, Category.Error);
            }
        }

        void AddTick(CQGTick tick,  string symbol, DateTime runDateTime, int groupId)
        {
            try{                                
    
                var str = symbol.Trim().Split('.');
                var query = "INSERT IGNORE INTO t_tick_" + str[str.Length - 1];
                query += "(Symbol, Price, Volume, TickTime, CollectTime, ContinuationType, PriceType, GroupID) VALUES";
                query += "('";
                query += symbol + "',";
                query += GetValueAsString(tick.Price) + ",";
                query += GetValueAsString(tick.Volume) + ",";
                query += GetValueAsString(tick.Timestamp) + ",";
                query += GetValueAsString(runDateTime) + ",";
                query += "'" + _aContinuationType + "',";
                query += "'" + tick.PriceType.ToString() + "',";
                query += GetValueAsString(groupId) + ");";

                ClientDataManager.AddToQueue(query);
            }
            catch (Exception exception)
            {
                _logger.LogAdd("AddTick. " + exception, Category.Error);
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
            _maxBarsLookBack = Math.Abs(maxCount);
            _aSymbolStates.Clear();
            ClientDataManager.CreateMissingBarExceptionTable();
            ClientDataManager.CreateSessionHolidayTimesTable();

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

                ss.IsCollected = false;
                ss.IsSuccess = false;
                if (!_aSymbolStates.ContainsKey(smb))
                    _aSymbolStates.Add(smb, ss);    
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
                    StartAsyncCheckingMissedBarsAuto(symbols, _maxBarsLookBack);
                else
                    StartAsyncCheckingMissedBars(symbols);
            }).Start();

           
        }

        private void StartAsyncGetingSessionsData(CQGCEL CEL, string[] symbols)
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                string symbol = symbols[i];
                _aSemaphoreHolidays = new Semaphore(0, 1);
                _aSemaphoreSessions = new Semaphore(0, 1);

                List<DateTime> aResultDateTimes = ClientDataManager.GetAllDateTimes(ClientDataManager.GetTableFromSymbol(symbol));

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
                _aSemaphoreHolidays.WaitOne(20000);// wait


                CEL.NewInstrument(symbol);
                _aSemaphoreSessions.WaitOne(20000);// wait
            }
        }

        private void StartAsyncCheckingMissedBars(string[] symbols)
        {
            //dbSel.COMMIT();
            
            for (int sInd = 0; sInd < symbols.Length; sInd++)
            {
                
                string currentSymbol = symbols[sInd];
                
                ClientDataManager.DelFromReport(currentSymbol);

                List<DateTime> aResultDates=new List<DateTime>();
                List<DateTime> aResultDateTimes = new List<DateTime>();

                if (ClientDataManager.TableExists(ClientDataManager.GetTableFromSymbol(currentSymbol)))
                {
                    aResultDates = ClientDataManager.GetAllDates(ClientDataManager.GetTableFromSymbol(currentSymbol));
                    aResultDateTimes = ClientDataManager.GetAllDateTimes(ClientDataManager.GetTableFromSymbol(currentSymbol));

                }
                if (aResultDates==null||aResultDates.Count == 0)
                {
                    SymbolState ss = _aSymbolStates[currentSymbol];
                    ss.IsCollected = true;
                    ss.IsSuccess = true;
                    _aSymbolStates[currentSymbol] = ss;

                    UpdateUI();

                    _logger.LogAdd("No records in database for symbol: " + currentSymbol , Category.Warning);                    
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
                        if (ClientDataManager.HolidaysContains(ClientDataManager.GetTableFromSymbol(currentSymbol), curDT))
                        {
                            State = SessionStates.ClosedHoliday;
                        }

                        DateTime sTime = curDT.Date.Add(GetStartTime(currentSymbol, _listSession, curDT.DayOfWeek, out DayStartsYesterday).TimeOfDay);
                        DateTime eTime = curDT.Date.Add(GetEndTime(currentSymbol, _listSession, curDT.DayOfWeek).TimeOfDay);
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

                            if (aResultDateTimes.Exists(a => a.Date == curDT.Date))
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
                        
                        ClientDataManager.AddToReport(currentSymbol, curDT, State.ToString(), sTime.DayOfWeek.ToString(), sTime, eTime.DayOfWeek.ToString(), eTime);

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
                                LVitem.Text = item.Start.ToShortDateString();                                                    // Date                                
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, State.ToString()));            // state
                                //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.DayOfWeek.ToString()));  // Day

                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.Start.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.Start.ToString("dd.MM HH:mm")));//  start
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.End.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.End.ToString("dd.MM HH:mm")));  // end
                                aItems.Add(LVitem);
                                
                                ClientDataManager.AddToReport(currentSymbol, item.Start, State.ToString(), item.Start.DayOfWeek.ToString(), item.Start, item.End.DayOfWeek.ToString(), item.End);
                            }
                        }
                    }//end:for curDT
                }// end:if                                

                // SECOND PART: Finding Missed bar that now is not missing
                               
                IEnumerable<DateTime> aMissedBarsForSymbol = ClientDataManager.GetMissedBarsForSymbol(currentSymbol);

                int index = Math.Max(0, aResultDateTimes.Count - _maxBarsLookBack);
                DateTime first = aResultDateTimes[index];

                List<DateTime> aSmallMissedBarsForSymbol = aMissedBarsForSymbol.Where(a => a > first).ToList();

                foreach (DateTime missedItem in aSmallMissedBarsForSymbol)
                {
                    //if (dbSel.rowExists(dbSel.getTableFromSymbol(currentSymbol), missedItem))
                    if (aResultDateTimes.Contains(missedItem))
                    {
                        //TODO Without commit update
                        ClientDataManager.ChangeBarStatusInMissingTableWithOutCommit(currentSymbol, refresh, missedItem);
                    }
                }

                ClientDataManager.CommitQueue();

                SymbolState ss1 = _aSymbolStates[currentSymbol];
                ss1.IsCollected = true;
                ss1.IsSuccess = true;
                _aSymbolStates[currentSymbol] = ss1;
                _logger.LogAdd("Repost finished for symbol: " + currentSymbol, Category.Information);                

                UpdateUI();    

            }// end: for all symbols  
            //dbSel.COMMIT();
            ResetSymbols();
            // todo of all
        }

        private void StartAsyncCheckingMissedBarsAuto(string[] symbols, int maxCount)
        {
            //dbSel.COMMIT();

            for (int sInd = 0; sInd < symbols.Length; sInd++)
            {

                string currentSymbol = symbols[sInd];                

                List<DateTime> aResultDates = new List<DateTime>();
                List<DateTime> aResultDateTimes = new List<DateTime>();

                if (ClientDataManager.TableExists(ClientDataManager.GetTableFromSymbol(currentSymbol)))
                {

                    aResultDateTimes = ClientDataManager.GetAllDateTimes(ClientDataManager.GetTableFromSymbol(currentSymbol), maxCount + 1);

                    aResultDates = ClientDataManager.GetAllDates(ClientDataManager.GetTableFromSymbol(currentSymbol), maxCount + 1);
                    if (aResultDateTimes.Count>0)
                        ClientDataManager.DelFromReport(currentSymbol, aResultDateTimes.First());

                }
                if (aResultDates == null || aResultDates.Count == 0)
                {
                    SymbolState ss = _aSymbolStates[currentSymbol];
                    ss.IsCollected = true;
                    ss.IsSuccess = true;
                    _aSymbolStates[currentSymbol] = ss;

                    UpdateUI();

                    _logger.LogAdd("No records in database for symbol: " + currentSymbol, Category.Warning);
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
                    var res = ClientDataManager.GetReport(currentSymbol);
                    foreach (var reportItem in res)
                    {
                        LVitem = new ListViewItem();
                        LVitem.Group = LVgroup;

                        switch (reportItem.State)
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
                        
                        LVitem.Text = reportItem.CurDate.ToShortDateString();                                                    // Date                                
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.State.ToString()));            // state
                        //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, curDT.DayOfWeek.ToString()));  // Day

                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.STime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.STime.ToString("dd.MM HH:mm")));     //  start
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.ETime.DayOfWeek.ToString()));  // Day
                        LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, reportItem.ETime.ToString("dd.MM HH:mm")));     // end

                        aItems.Add(LVitem);
                    }
                    #endregion

                    // ADD SESSION DAYS
                    for (DateTime curDT = aResultDates.First(); curDT <= aResultDates.Last(); curDT = curDT.AddDays(1))
                    {
                        #region start settings

                        bool DayStartsYesterday = false;
                        SessionStates State = SessionStates.OpenedNormal;
                        if (ClientDataManager.HolidaysContains(ClientDataManager.GetTableFromSymbol(currentSymbol), curDT))
                        {
                            State = SessionStates.ClosedHoliday;
                        }

                        DateTime sTime = curDT.Date.Add(GetStartTime(currentSymbol, _listSession, curDT.DayOfWeek, out DayStartsYesterday).TimeOfDay);
                        DateTime eTime = curDT.Date.Add(GetEndTime(currentSymbol, _listSession, curDT.DayOfWeek).TimeOfDay);
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

                        ClientDataManager.AddToReport(currentSymbol, curDT, State.ToString(), sTime.DayOfWeek.ToString(), sTime, eTime.DayOfWeek.ToString(), eTime);

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
                                LVitem.Text = item.Start.ToShortDateString();                                                    // Date                                
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, State.ToString()));            // state
                                //LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.start.DayOfWeek.ToString()));  // Day

                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.Start.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.Start.ToString("dd.MM HH:mm")));//  start
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.End.DayOfWeek.ToString()));  // Day
                                LVitem.SubItems.Add(new ListViewItem.ListViewSubItem(LVitem, item.End.ToString("dd.MM HH:mm")));  // end
                                aItems.Add(LVitem);

                                ClientDataManager.AddToReport(currentSymbol, item.Start, State.ToString(), item.Start.DayOfWeek.ToString(), item.Start, item.End.DayOfWeek.ToString(), item.End);
                            }
                        }
                        #endregion

                    }//end:for curDT
                }// end:if                                

                // SECOND PART: Finding Missed bar that now is not missing

                var aMissedBarsForSymbol =  ClientDataManager.GetMissedBarsForSymbol(currentSymbol);

                int index = Math.Max(0, aResultDateTimes.Count - _maxBarsLookBack);
                DateTime first = aResultDateTimes[index];

                List<DateTime> aSmallMissedBarsForSymbol = aMissedBarsForSymbol.Where(a => a > first).ToList();

                foreach (DateTime missedItem in aSmallMissedBarsForSymbol)
                {                    
                    if (aResultDateTimes.Contains(missedItem))
                    {
                        //TODO Without commit update
                        ClientDataManager.ChangeBarStatusInMissingTableWithOutCommit(currentSymbol, refresh, missedItem);
                    }
                }

                ClientDataManager.CommitQueue();

                SymbolState ss1 = _aSymbolStates[currentSymbol];
                ss1.IsCollected = true;
                ss1.IsSuccess = true;
                _aSymbolStates[currentSymbol] = ss1;
                _logger.LogAdd("Repost finished for symbol: " + currentSymbol, Category.Information);

                UpdateUI();

            }// end: for all symbols              
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
                    
                        ClientDataManager.AddToMissingTableWithOutCommit(smb, refresh, curTime);
                    
                    missingList.Add(curTime);
                }
            }

            ClientDataManager.CommitQueue();
            
            if (missingList.Count == 1)
            {
                resultList.Add(new MissedStr { Start = missingList[0], End = missingList[0] });
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

                    resultList.Add(new MissedStr { Start = first, End = last });

                    last = first = item;
                    if (haveLast)
                    {
                        resultList.Add(new MissedStr { Start = first, End = last });
                    }
                }
            }

            return resultList;
        }

        private bool ExistsTime(List<DateTime> aResultDateTimes, DateTime curTime)
        {
            return aResultDateTimes.Any(item => item == curTime);
        }

        private DateTime GetStartTime(string smb, List<SessionData> listSession, DayOfWeek dayOfWeek, out bool DayStartsYesterday)
        {
            List<SessionData> alist = new List<SessionData>();
            eSessionWeekDays curDay = ConvertToSessionWeekDay(dayOfWeek);

            alist = listSession.Where(a => a.Symbol == smb).ToList();
            foreach (var item in alist)
            {
                if ((item.DayOfWeek & curDay) == curDay)
                {
                    DayStartsYesterday = item.DayStartsYesterday;
                    return item.StartTime;
                }
            }
            DayStartsYesterday = false;
            return DateTime.Today;
        }

        private DateTime GetEndTime(string smb, List<SessionData> listSession, DayOfWeek dayOfWeek)
        {
            List<SessionData> alist = new List<SessionData>();
            eSessionWeekDays curDay = ConvertToSessionWeekDay(dayOfWeek);

            alist = listSession.Where(a => a.Symbol == smb).ToList();
            DateTime res = DateTime.Today;
            foreach (var item in alist)
            {
                if ((item.DayOfWeek & curDay) == curDay)
                {
                    res =  item.EndTime;
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
                    _aIntradayPeriod = 1;
                    _aTableType = "1m";
                    break;
                case "2 minutes":
                    _aIntradayPeriod = 2;
                    _aTableType = "2m";
                    break;
                case "3 minutes":
                    _aIntradayPeriod = 3;
                    _aTableType = "3m";
                    break;
                case "5 minutes":
                    _aIntradayPeriod = 5;
                    _aTableType = "5m";
                    break;
                case "10 minute":
                    _aIntradayPeriod = 10;
                    _aTableType = "10m";
                    break;
                case "15 minutes":
                    _aIntradayPeriod = 15;
                    _aTableType = "15m";
                    break;
                case "30 minutes":
                    _aIntradayPeriod = 30;
                    _aTableType = "30m";
                    break;
                case "60 minutes":
                    _aIntradayPeriod = 60;
                    _aTableType = "60m";
                    break;
                case "240 minutes":
                    _aIntradayPeriod = 240;
                    _aTableType = "240m";
                    break;

                case "Daily":
                    _aHistoricalPeriod = eHistoricalPeriod.hpDaily;
                    _aTableType = "Daily";
                    break;
                case "Weekly":
                    _aHistoricalPeriod = eHistoricalPeriod.hpWeekly;
                    _aTableType = "Weekly";
                    break;
                case "Monthly":
                    _aHistoricalPeriod = eHistoricalPeriod.hpMonthly;
                    _aTableType = "Monthly";
                    break;
                case "Quarterly":
                    _aHistoricalPeriod = eHistoricalPeriod.hpQuarterly;
                    _aTableType = "Quarterly";
                    break;
                case "Yearly":
                    _aHistoricalPeriod = eHistoricalPeriod.hpYearly;
                    _aTableType = "Yearly";
                    break;
                case "Semiannual":
                    _aHistoricalPeriod = eHistoricalPeriod.hpSemiannual;
                    _aTableType = "Semiannual";
                    break;

                default:
                    _aIntradayPeriod = 1;
                    _aTableType = "1m";
                    break;
            }
        }

        internal void UpdateUI()
        {

            if (_aPbState != null)
            {
                _aPbState.Invoke((Action)delegate
                {
                    _aPbState.Maximum = _aSymbolStates.Count;
                    _aPbState.Value = SymbolsCollected;
                });
            }

            if (_aLvReport != null)
            {
                _aLvReport.Invoke((Action)delegate
                {
                    if (SymbolsCollected < _aSymbolStates.Count)
                    {
                        _aLvReport.Items.Clear();
                        _aLvReport.Groups.Clear();
                    }
                    else
                    {
                        _aLvReport.Groups.AddRange(aGroups.ToArray());
                        _aLvReport.Items.AddRange(aItems.ToArray());
                    }
                });
                
            }
            if (_aLStatus != null)
            {
                _aLStatus.Invoke((Action)delegate
                {
                    if (SymbolsCollected < _aSymbolStates.Count)
                        _aLStatus.Text = StausWorking + "[" + SymbolsCollected + "/" + _aSymbolStates.Count + "]";
                    else
                    {
                        _aLStatus.Text = StausReady;
                        try
                        {
                            //if (SemaphoreEndOfCollecting != null) SemaphoreEndOfCollecting.Release();
                            if (_aSemaphoreWait != null) _aSemaphoreWait.Release();
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
                foreach (var item in _aSymbolStates)
                {
                    if (item.Value.IsCollected) collectedCount++;
                }
                return collectedCount;
            }
        }

        internal void WaitEndOfOperation()
        {            
            if (SymbolsCollected < _aSymbolStates.Count)
            {
                if (_aSemaphoreWait == null) 
                    _aSemaphoreWait = new Semaphore(0, 1);
                _aSemaphoreWait.WaitOne();
            }            
        }

        internal Brush getColor(string symbol)
        {
            if (_aSymbolStates.ContainsKey(symbol) && _aSymbolStates[symbol].IsCollected)
            {
                return _aSymbolStates[symbol].IsSuccess ? Brushes.LightGreen : Brushes.Red;
            }
            return Brushes.Black;
        }

        internal void ResetSymbols()
        {            
            _aSymbolStates.Clear();
        }

        #endregion

        internal bool IsBusy()
        {
            return SymbolsCollected < _aSymbolStates.Count;
        }
    }
}
