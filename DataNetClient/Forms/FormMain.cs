using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CQG;
using System.Globalization;
using System.Threading;
using DataAdminCommonLib;
using DataNetClient.Properties;
using DataNetClient.Structs;
using DevComponents.DotNetBar;
using System.Diagnostics;
using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using Hik.Communication.ScsServices.Client;
using DataAdminCommonLib;


namespace DataNetClient.Forms
{
    

    public enum eSymbolOperatiomState
    {
        DEFAULT_STATUS = 0,
        ERROR_STATUS = 1,
        SECESS_STATUS = 2
    }

    public partial class FormMain : DevComponents.DotNetBar.Metro.MetroAppForm
    {
        private readonly MetroBillCommands _commands; // All application commands
        private readonly StartControl _startControl;
        //internal DbSelector dbSel;
        private DataCollector dataCollector;
        private CQGCEL CEL;
        private bool IsStartedCQG;
        private Dictionary<String, TimeRange> customeListsDict;
        private DateTime endTime;
        private DateTime startTime;
        private List<Brush> lbxColors;
        public bool CreateNewList;
        public Logger logger;
        public List<string> glTables = new List<string>();
        public Semaphore pool1 { get; set; }
        public Semaphore waitForEndCollecting;
        public Semaphore waitForEndCollectingList;

        private ControlNewSymbol _addSymbolControl;
        private ControlEditList _editListControl;

        private List<GroupModel> _groups = new List<GroupModel>();
        private List<SymbolModel> _symbols = new List<SymbolModel>();

        #region Its Need to be changed


        private string _connectionToSharedDb;

        #endregion


        #region CLIENT-SERVER VARIABLES

        private DataClientClass _client;
        private IScsServiceClient<IDataAdminService> client;

        #endregion

        public FormMain()
        {
            SuspendLayout();

            InitializeComponent();

            _commands = new MetroBillCommands
                {
                    StartControlCommands = {Logon = new Command(), Exit = new Command()},
                    NewSymbolCommands = {Add = new Command(), Cancel = new Command()},
                    NewListCommands = {Add = new Command(), Cancel = new Command()},
                    EditListCommands = {Save = new Command(), Cancel = new Command()}
                };

            _commands.StartControlCommands.Logon.Executed += StartControl_LogonClick;
            _commands.StartControlCommands.Exit.Executed += StartControl_ExitClick;

            _commands.NewSymbolCommands.Add.Executed += AddNewSymbolExecuted;
            _commands.NewSymbolCommands.Cancel.Executed += CancelNewSymbolExecuted;

            _commands.NewListCommands.Add.Executed += AddNewListExecuted;
            _commands.NewListCommands.Cancel.Executed += CancelNewListExecuted;

            _commands.EditListCommands.Save.Executed += SaveEditListExecuted;
            _commands.EditListCommands.Cancel.Executed += CancelEditListExecuted;


            labelItem2.Text = @"ver " + Application.ProductVersion;

            _startControl = new StartControl {Commands = _commands};
            //_addUserControl = new AddUserControl {Commands = _commands, Tag = 0};

            Controls.Add(_startControl);
            _startControl.BringToFront();
            _startControl.SlideSide = DevComponents.DotNetBar.Controls.eSlideSide.Right;
            ResumeLayout(false);
        }

        #region CLIENT - SERVER LOGIC IMPLEMENTATION

        private void LoginToServer(string username, string password, string host)
        {
            _client = new DataClientClass(username);
            client = ScsServiceClientBuilder.CreateClient<IDataAdminService>(new ScsTcpEndPoint(host, 10048), _client);
            client.Connected += ScsClient_Connected;
            try
            {
                client.Connect();
                _client.login += LoggedIn;
                _client.block += BlockedByAdmin;
                _client.loginFailed += LoginFailed;
                _client.changePrivilages += ChangedPrivileges;
                _client.logout += DeletedClient;
                _client.symblolListRecieved += GroupSymbolChange;
            }
            catch(Exception ex)
            {
                MessageBox.Show("INCORRECT IP ADDRESS!");
                return;
            }
            var loginMSG = new MessageFactory.LoginMessage(username, password, 'd');
            try
            {
                client.ServiceProxy.Login(loginMSG);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void DeletedClient(object sender, object msg)
        {
            MessageBox.Show(msg.ToString());

            Invoke((Action)Close);
        }

        private void LoginFailed(object sender, MessageFactory.LoginMessage msg)
        {
            MessageBox.Show(msg.ServerMessage);
        }

        private void ScsClient_Connected(object sender, EventArgs e)
        {

        }

        public void LoggedIn(object sender, MessageFactory.ChangePrivilage msg)
        {


            var xml = new XmlDocument();
            xml.LoadXml(msg.ServerMessage);

            string host = "";
            string dbName = "";
            string usName = "";
            string passw = "";

            var connString = xml.GetElementsByTagName("ConnectionString");
            var attr = connString[0].Attributes;
            if (attr != null)
            {
              host =  (attr["Host"].Value);
                dbName = attr["dbName"].Value;
                usName = attr["userName"].Value;
                passw = attr["password"].Value;
            }
            _connectionToSharedDb = "SERVER="+host+"; DATABASE="+dbName+"; UID="+usName+"; PASSWORD="+passw;
            SetPrivilages(msg);
        }

        public void ChangedPrivileges(object sender, MessageFactory.ChangePrivilage msg)
        {
            SetPrivilages(msg);

        }

        private void SetPrivilages(MessageFactory.ChangePrivilage msg)
        {
            if (msg == null) return;

            var privileges = msg;

            _client.Privileges.AnyIPAllowed = privileges.AnyIPAllowed;
            _client.Privileges.CollectSQGAllowed = privileges.CollectSQGAllowed;
            _client.Privileges.DatanetEnabled = privileges.DatanetEnabled;
            _client.Privileges.LocalDBAllowed = privileges.LocalDBAllowed;
            _client.Privileges.MissingBarFAllowed = privileges.MissingBarFAllowed;
            _client.Privileges.SharedDBAllowed = privileges.SharedDBAllowed;
            _client.Privileges.TicknetEnabled = privileges.TicknetEnabled;

            string sharedDbstring = "";
            Color sharedDbColor = Color.Green;

            string localDbstring = "";
            Color localDbColor = Color.Green;

            if (_client.Privileges.SharedDBAllowed)
            {

                sharedDbstring = "AVAILABLE";
                sharedDbColor = Color.Green;


            }
            else
            {

                sharedDbstring = "UNAVAILABLE";
                sharedDbColor = Color.OrangeRed;
            }


            if (_client.Privileges.LocalDBAllowed)
            {

                localDbstring = "AVAILABLE";
                localDbColor = Color.Green;
            }
            else
            {
                localDbstring = "UNAVAILABLE";
                localDbColor = Color.OrangeRed;
            }

            Task.Factory.StartNew(delegate
                {
                    ui_buttonX_localConnect.Invoke(
                        (Action) delegate { ui_buttonX_localConnect.Enabled = _client.Privileges.LocalDBAllowed; });

                    ui_buttonX_shareConnect.Invoke((Action) delegate
                        {
                            ui_buttonX_shareConnect.Enabled = _client.Privileges.SharedDBAllowed;
                        });
                    ui_LabelX_localAvaliable.Invoke((MethodInvoker) delegate
                        {
                            ui_LabelX_localAvaliable.Text = localDbstring;
                            ui_LabelX_localAvaliable.ForeColor = localDbColor;
                        });
                    ui_LabelX_sharedAvaliable.Invoke((MethodInvoker) delegate
                        {
                            ui_LabelX_sharedAvaliable.Text = sharedDbstring;
                            ui_LabelX_sharedAvaliable.ForeColor = sharedDbColor;
                        });

                        _startControl.Invoke((Action)delegate { _startControl.Hide(); })
                    ;


                });
        }
    

    public void BlockedByAdmin(object sender, object msg)
        {
            _client.BlockedByAdmin = true;
        }

        public void GroupSymbolChange(object sender, string groupList)
        {
            var xml = new XmlDocument();
            xml.LoadXml(groupList);
            //var elemList = xml.GetElementsByTagName("GroupSymb");
            //for (int i = 0; i < elemList.Count; i++)
            //{
            //    var xmlAttributeCollection = elemList[i].Attributes;
               

            //    if (xmlAttributeCollection != null)
            //    {
            //        string attrVal = xmlAttributeCollection["ID"].Value;
            //        _client.AllowedSymbolGroups.Add(Convert.ToInt32(attrVal));
            //    }
            //}
            var elemUserID = xml.GetElementsByTagName("UserID");
            var attr = elemUserID[0].Attributes;
            if (attr != null)
                _client.UserID = Convert.ToInt32(attr["ID"].Value);

            if (_client.ConnectedToSharedDb)
            {
                RefreshGroups();
                RefreshGroups();
            }
            //todo refresh symbol list

        }


        #endregion 

        private Rectangle GetStartControlBounds()
        {
            var captionHeight = metroShell1.MetroTabStrip.GetCaptionHeight() + 2;
            var borderThickness = GetBorderThickness();
            return new Rectangle((int)borderThickness.Left, captionHeight, Width - (int)borderThickness.Horizontal, Height - captionHeight - 1);
        }

        private void UpdateControlsSizeAndLocation()
        {
            if (_startControl != null)
            {
                if (!_startControl.IsOpen)
                    _startControl.OpenBounds = GetStartControlBounds();
                else
                    _startControl.Bounds = GetStartControlBounds();
                if (!IsModalPanelDisplayed)
                    _startControl.BringToFront();
            }
            tableLayoutPanel1.Size = new Size(Width-7, Height-77);
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            UpdateControlsSizeAndLocation();
        }

        private void StartControl_ExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                ClientDataManager.ConnectionStatusChanged += ClientDataManager_ConnectionStatusChanged;
                UpdateControlsSizeAndLocation();

                logger = Logger.GetInstance(listViewLogger);
                logger.LogAdd("Application Start", Category.Information);
                dataCollector = new DataCollector(logger);

                textBoxX1.Text = Settings.Default.Host;
                textBoxX2.Text = Settings.Default.DB;
                textBoxX3.Text = Settings.Default.User;
                textBoxX4.Text = Settings.Default.Password;
                nudEndBar.Value = Settings.Default.valFinish;
                checkBoxAutoCheckForMissedBars.Value= Settings.Default.AutoMissingBarReport;
                checkBoxX1.Checked = Settings.Default.SavePass;
                //**
                metroShell1.SelectedTab = metroTabItem1;

                cmbContinuationType.Items.Clear();
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctNoContinuation);
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctStandard);
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctStandardByMonth);
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctActive);
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctActiveByMonth);
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctAdjusted);
                cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctAdjustedByMonth);
                cmbContinuationType.SelectedIndex = 0;
                cmbHistoricalPeriod.SelectedIndex = 0;
                
                resetColorMarks();

                CEL = new CQGCEL();
                CEL.APIConfiguration.TimeZoneCode = eTimeZone.tzGMT;
                CEL.APIConfiguration.ReadyStatusCheck = eReadyStatusCheck.rscOff;
                CEL.APIConfiguration.CollectionsThrowException = false;
                CEL.APIConfiguration.LogSeverity = eLogSeverity.lsDebug;
                CEL.APIConfiguration.MessageProcessingTimeout = 30000;

                CEL.DataConnectionStatusChanged += CEL_DataConnectionStatusChanged;
                CEL_DataConnectionStatusChanged(eConnectionStatus.csConnectionDown);
                CEL.DataError += CEL_DataError;
                CEL.TimedBarsResolved += CEL_TimedBarsResolved;
                CEL.IncorrectSymbol += CEL_IncorrectSymbol;
                CEL.HistoricalSessionsResolved += CEL_HistoricalSessionsResolved;
                CEL.TicksResolved += CQG_TicksResolved;
                CEL.InstrumentSubscribed += CEL_InstrumentSubscribed;                

                CEL.Startup();

                //currStatus = DEFAULT_STATUS;
                dateTimeInputStart.Value = DateTime.Now.AddDays(-1);
                dateTimeInputEnd.Value = DateTime.Now;
                listBoxSymbols.DrawItem += listBox1_DrawItem;                

                dataCollector.Subscribe(labelItem1,listBoxSymbols,checkedListBoxLists,progressBarItemCollecting, listViewResult);
            }
            catch (Exception exception)
            {
                logger.LogAdd("Error in loading. " + exception.Message, Category.Error);                
                Close();
            }
        }


        #region CEL

        void CEL_InstrumentSubscribed(string symbol, CQGInstrument cqg_instrument)
        {
            dataCollector.SessionAdd(cqg_instrument.Sessions, symbol);
        }         

        void CEL_HistoricalSessionsResolved(CQGSessionsCollection cqg_historical_sessions, CQGHistoricalSessionsRequest cqg_historical_sessions_request, CQGError cqg_error)
        {
            dataCollector.HolidaysAdd(cqg_historical_sessions, cqg_historical_sessions_request.Symbol);        
        }

        void CEL_DataConnectionStatusChanged(eConnectionStatus eConnectionStatus)
        {
            CqgConnectionStatusChanged(eConnectionStatus == eConnectionStatus.csConnectionUp);
        }


        void CEL_DataError(object cqg_error, string error_description)
        {
            try
            {
                var error = cqg_error as CQGError;
                if (error != null)
                {
                    if (error.Code == 0x66)
                    {
                        error_description = error_description + " Restart the application.";
                    }
                    else if (error.Code == 0x7d)
                    {
                        error_description = error_description + " Turn on CQG Client and restart the application.";
                    }
                }
                //MessageBox.Show(error_description, "DataNet", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                logger.LogAdd(error_description, Category.Error);
            }
            catch (Exception exception)
            {
                logger.LogAdd("CEL data eroor. "+exception, Category.Error);
            }                       
        }
       
        void CEL_IncorrectSymbol(string symbol_)
        {            
            logger.LogAdd("Incorrect symbol", Category.Warning);
        }

        void CQG_TicksResolved(CQGTicks cqg_ticks, CQGError cqg_error)
        {
            dataCollector.TicksAdd(cqg_ticks, cqg_error);
        }

        void CEL_TimedBarsResolved(CQGTimedBars cqg_timed_bars, CQGError cqg_error)
        {
            dataCollector.BarsAdd(cqg_timed_bars, cqg_error);
        }
          
    #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //TODO: CloseConnection
            ClientDataManager.CloseConnectionToDb();

            if (checkBoxX1.Checked)
            {
                Settings.Default.Host = textBoxX1.Text;
                Settings.Default.DB = textBoxX2.Text;
                Settings.Default.User = textBoxX3.Text;
                Settings.Default.Password = textBoxX4.Text;
                Settings.Default.AutoMissingBarReport = checkBoxAutoCheckForMissedBars.Value;
            }
            else
            {
                Settings.Default.User = "";
                Settings.Default.Password = "";
            }
            Settings.Default.SavePass = checkBoxX1.Checked;
            Settings.Default.Save();

            
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        /*
        private void InitilizeWhenConnectToDb()
        {
            dbSel.LoadSymbolList(listBoxSymbols);
            dbSel.LoadCmList(checkedListBoxLists,null);

            listBoxTablesName.Items.Clear();
            //listBoxTablesName.Items.AddRange(dbSel.LoadTablesForRequest("1m").ToArray());
            dbSel.LoadSymbolList(listBoxTablesName);
        }
     
        private void connectButton_Click(object sender, EventArgs e)
        {
            if (dbSel == null || !dbSel.IsConnected())
                Connect();
        }
      
        void Connect()
        {
            dbSel = DbSelector.GetInstance();

            dbSel.Initialize(textBoxX1.Text, textBoxX2.Text, textBoxX3.Text, textBoxX4.Text);
            dbSel.OpenConnection();

            if (dbSel.IsConnected())
            {
                if (IsStartedCQG)
                {
                    ui_status_labelItemStatusSB.Text = @"Connected  ";
                    styleManager1.MetroColorParameters = new DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(styleManager1.MetroColorParameters.CanvasColor, Color.Green);
                    InitilizeWhenConnectToDb();
                    refreshCustomList();
                }
                else
                {                  
                    ui_status_labelItemStatusSB.Text = "Connected  ";
                    //styleManager1.MetroColorParameters = new DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(styleManager1.MetroColorParameters.CanvasColor, Color.Red);
                    InitilizeWhenConnectToDb();
                }

            }
            else
            {
                ui_status_labelItemStatusSB.Text = @"Not connected  ";
                dbSel = null;
                //styleManager1.MetroColorParameters = new DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(styleManager1.MetroColorParameters.CanvasColor, Color.Red);
            }
        }
        */

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxSymbols.Items.Count; i++)
            {
                listBoxSymbols.SetSelected(i, true);
            }
        }
        private void unselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxSymbols.Items.Count; i++)
            {
                listBoxSymbols.SetSelected(i, false);
            }
        }

        private void inverseSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxSymbols.Items.Count; i++)
            {

                listBoxSymbols.SetSelected(i, !listBoxSymbols.SelectedItems.Contains(listBoxSymbols.Items[i]));
            }      
        }

        public void resetColorMarks()
        {
            lbxColors = new List<Brush>();
            for (int i = 0; i < listBoxSymbols.Items.Count; i++)
            {
                lbxColors.Add(Brushes.Black);
            }
        }

        private void listBox1_DrawItem(object sender,  DrawItemEventArgs e)
        {
            if (e.Index == -1) return;
            listBoxSymbols.DrawMode = DrawMode.OwnerDrawFixed;
            e.DrawBackground();
            {
                e.Graphics.DrawString(listBoxSymbols.Items[e.Index].ToString(),
                                        e.Font, dataCollector.getColor(listBoxSymbols.Items[e.Index].ToString()), e.Bounds, StringFormat.GenericDefault);
            }
            e.DrawFocusRectangle();
            //changedStatus = false;
            //currStatus = DEFAULT_STATUS;
        }


        public void setSymbolState(String symbol, eSymbolOperatiomState state)
        {
            if (listBoxSymbols.Items.Count > 0)
            {
                int index = listBoxSymbols.FindString(symbol);
                if (index < 0) return;
                if (state == eSymbolOperatiomState.ERROR_STATUS)
                    lbxColors[index] = Brushes.Red;
                if (state == eSymbolOperatiomState.SECESS_STATUS)
                    lbxColors[index] = Brushes.LimeGreen;
                if (state == eSymbolOperatiomState.DEFAULT_STATUS)
                    lbxColors[index] = Brushes.Black;
            }
        }



        private void StartCollecting()
        {
            
            var symbols = (from object item in listBoxSymbols.SelectedItems select item.ToString()).ToList();

            var continuationType = cmbContinuationType.SelectedItem.ToString();                

            if (radioButBars.Checked)
            {
                int rangeStart = Convert.ToInt32(nudStartBar.Value);
                int rangeEnd = Convert.ToInt32(nudEndBar.Value);
                int sessionFilter = rdb1.Checked ? 1 : 31;
                string historicalPeriod = cmbHistoricalPeriod.SelectedItem.ToString();
                
                dataCollector.BarRequest(CEL, symbols, rangeStart, rangeEnd, sessionFilter, historicalPeriod, continuationType);
            }
            else
            {
                DateTime rangeStart = dateTimeInputStart.Value.Date;
                DateTime rangeEnd = dateTimeInputEnd.Value;

                dataCollector.TickRequest(CEL, symbols, rangeStart, rangeEnd, continuationType);
            }           
        }
        private void radioButBars_Click(object sender, EventArgs e)
        {
            if (radioButBars.Checked)
            {
                panelExTimeInterval.Enabled = false;
                panelExBARS.Enabled = true;
                
            }
            else
            {
                panelExTimeInterval.Enabled = true;
                panelExBARS.Enabled = false;
            }
        }

        #region MetroList
        /*
        void NewEditListExecuted(object sender, EventArgs e)
        {
            if(dbSel==null || !dbSel.IsConnected())
            {
                return;
            }
            if (CreateNewList)
            {
                Debug.Assert(_EditListControl == null);
                _commands.EditListCommands.New.Enabled = false; // Disable new EditList command to prevent re-entrancy

                _EditListControl = new ControlEditList
                                       {
                                           Commands = _commands,
                                           labelXTitle = {Text = "NEW SYMBOLS LIST"}
                                       };
                ShowModalPanel(_EditListControl, DevComponents.DotNetBar.Controls.eSlideSide.Right);
                //***
                _EditListControl.textBoxXListName.Text = "Untitled list";
                dbSel.LoadSymbolList(_EditListControl.lbAvbList);

                return;
            }
            if (checkedListBoxLists.SelectedItem == null) return;

            Debug.Assert(_EditListControl == null);
            _commands.EditListCommands.New.Enabled = false; // Disable new EditList command to prevent re-entrancy

            _EditListControl = new ControlEditList
                                   {
                                       Commands = _commands,
                                       labelXTitle = {Text = "EDIT SYMBOLS LIST"}
                                   };
            ShowModalPanel(_EditListControl, DevComponents.DotNetBar.Controls.eSlideSide.Right);
            //******
            _EditListControl.textBoxXListName.Text = checkedListBoxLists.SelectedItem.ToString();
            //_EditListControl.textBoxX1.Text = listBoxLists.SelectedItem.ToString();

            _EditListControl.lbSelList.Items.Clear();
            dbSel.load_cmList(_EditListControl.textBoxXListName.Text, _EditListControl.lbSelList);            
            dbSel.LoadSymbolList(_EditListControl.lbAvbList);
            for (int i = 0; i < _EditListControl.lbSelList.Items.Count; i++)
            {
                _EditListControl.lbAvbList.Items.Remove(_EditListControl.lbSelList.Items[i]);

                _EditListControl.startTimeCollect.Value = DateTime.Now;
                _EditListControl.endTimeCollect.Value = DateTime.Now;
                _EditListControl.checkBoxUseTI.Checked = false;
            }
            //btnDelete.Enabled = true;
            try
            {
                Dictionary<String, TimeRange> cList = dbSel.LoadCmList(null);
                TimeRange tr = cList[_EditListControl.textBoxXListName.Text];
                eHistoricalPeriod res;
                if (Enum.TryParse(tr.strTF_Tyoe, out res))
                    _EditListControl.cmbHistoricalPeriod.SelectedItem = res;
                else
                    _EditListControl.cmbHistoricalPeriod.SelectedItem = tr.strTF_Tyoe;
                eTimeSeriesContinuationType res1;
                if (Enum.TryParse(tr.strContinuationType, out res1))
                {
                    _EditListControl.cmbContinuationType.SelectedItem = res1;
                }
                string str = tr.StartTime.ToShortDateString();
                DateTime dt = DateTime.ParseExact(str, "dd.MM.yyyy", CultureInfo.InvariantCulture);

                _EditListControl.startTimeCollect.Value = dt;
                _EditListControl.endTimeCollect.Value = tr.endTime;
                _EditListControl.checkBoxUseTI.Checked = true;
            }
            catch (Exception)
            {
                _EditListControl.startTimeCollect.Value = DateTime.Now;
                _EditListControl.endTimeCollect.Value = DateTime.Now;
                _EditListControl.checkBoxUseTI.Checked = false;                
            }

        }
         * */

        /*
        void SaveEditListExecuted(object sender, EventArgs e)
        {
            try
            {

                if (_EditListControl.lbSelList.Items.Count < 1)
                {
                    MessageBox.Show("Custom symbols not selected", "Information",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
                }
                else
                {
                    if (_EditListControl.textBoxXListName.Text == "")
                        _EditListControl.textBoxXListName.Text = "Untitled list";
                    CloseEditListDialog(false);

                    var symbolList = dbSel.LoadSymbolList(null);
                    var list = _EditListControl.lbSelList.Items.Cast<string>().ToDictionary(item => item, item => symbolList[item]);

                    if (_EditListControl.checkBoxUseTI.Checked)
                        dbSel.saveCustomList(_EditListControl.textBoxXListName.Text, list, _EditListControl.cmbHistoricalPeriod.SelectedItem.ToString(), (eTimeSeriesContinuationType)_EditListControl.cmbContinuationType.SelectedItem,
                                          _EditListControl.startTimeCollect.Value, _EditListControl.endTimeCollect.Value);
                    else
                        dbSel.saveCustomList(_EditListControl.textBoxXListName.Text, list, _EditListControl.cmbHistoricalPeriod.SelectedItem.ToString(), (eTimeSeriesContinuationType)_EditListControl.cmbContinuationType.SelectedItem, new DateTime(),
                                          new DateTime());
                    CloseEditListDialog();
                }
       
            }
            catch (Exception ex)
            {
                logger.LogAdd("SaveEditListExecuted. " + ex.Message, Category.Error);                
            }
            refreshCustomList();
        }
        */
        /*
        private void CancelEditListExecuted(object sender, EventArgs e)
        {
            Debug.Assert(_EditListControl != null);
            CloseEditListDialog();
        }*/
        #endregion  
        /*
        private void CloseEditListDialog(bool dispose =true)
        {
            _commands.EditListCommands.New.Enabled = true; // Enable new EditList command

            try
            {
                CloseModalPanel(_EditListControl, DevComponents.DotNetBar.Controls.eSlideSide.Right);
            }
            catch (Exception ex)
            {
                logger.LogAdd("CloseEditListDialog. " + ex.Message, Category.Error);                
            }
            if (dispose)
            {
                _EditListControl.Commands = null;
                _EditListControl.Dispose();
                _EditListControl = null;
            }
        }
        */
        private void listBoxLists_MouseDown(object sender, MouseEventArgs e)
        {
            checkedListBoxLists.SelectedIndex = checkedListBoxLists.IndexFromPoint(e.X, e.Y);
            if (e.Button == MouseButtons.Right)
            {
                checkedListBoxLists.Show();
            }
        }


        #region MetroSymbol

        ControlNewSymbol _NewSymbolControl;
        /*
        void NewNewSymbolExecuted(object sender, EventArgs e)
        {
            if (dbSel == null || !dbSel.IsConnected())
            {
                return;
            }
           
            Debug.Assert(_NewSymbolControl == null);
            _commands.NewSymbolCommands.New.Enabled = false; // Disable new EditList command to prevent re-entrancy

            _NewSymbolControl = new ControlNewSymbol {Commands = _commands};

            _NewSymbolControl.Focus();
            _NewSymbolControl.textBoxXSymbolName.Text = "";
            _NewSymbolControl.textBoxXSymbolName.Focus();
            
            ShowModalPanel(_NewSymbolControl, DevComponents.DotNetBar.Controls.eSlideSide.Left);
     
        }
         
        private void CancelNewSymbolExecuted(object sender, EventArgs e)
        {
            Debug.Assert(_NewSymbolControl != null);
            CloseNewSymbolDialog();
            //refreshCustomList();
            if (dbSel != null)
            {
                dbSel.LoadSymbolList(listBoxSymbols);
                resetColorMarks();
            }

        }
        
        void SaveNewSymbolExecuted(object sender, EventArgs e)
        {
            try
            {

                if (_NewSymbolControl.textBoxXSymbolName.Text.Length < 1)
                {
                    MessageBox.Show("Enter symbol.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                else
                {                                    
                     dbSel.AddSymbol(_NewSymbolControl.textBoxXSymbolName.Text);

                     MessageBox.Show("Symbol '" + _NewSymbolControl.textBoxXSymbolName.Text + "' added.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    _NewSymbolControl.textBoxXSymbolName.SelectAll();
                    _NewSymbolControl.textBoxXSymbolName.Focus();
                }                                
            }
            catch (Exception ex)
            {
                logger.LogAdd("SaveNewSymbolExecuted. " + ex.Message, Category.Error);
            }
            
        }
        
        private void CloseNewSymbolDialog(bool dispose = true)
        {
            _commands.NewSymbolCommands.New.Enabled = true; // Enable new EditList command

            try
            {
                CloseModalPanel(_NewSymbolControl, DevComponents.DotNetBar.Controls.eSlideSide.Left);
            }
            catch (Exception ex)
            {
                logger.LogAdd("CloseNewSymbolDialog. " + ex.Message, Category.Error);
            }
            if (dispose)
            {
                _NewSymbolControl.Commands = null;
                _NewSymbolControl.Dispose();
                _NewSymbolControl = null;
            }
        }
        */

        #endregion


        /*
        private void iNewListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewList = true;
            NewEditListExecuted(sender, e);
        }

        private void iEditListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewList = false;
            NewEditListExecuted(sender, e);
        }

        private void iDeleteListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dbSel == null || !dbSel.IsConnected()) return;
           
            if (checkedListBoxLists.SelectedItem == null)
            {
                return;
            }
            if (MessageBox.Show("Do you want to delete custom list?", "Deleting custom list", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dbSel.doSQL("DELETE FROM `t_cm_list_names` WHERE `smName` = '" + checkedListBoxLists.SelectedItem + "';COMMIT;");
                dbSel.LoadCmList(checkedListBoxLists, null);
            }            
        }        
        */

        private void metroTileItemCollect_Click(object sender, EventArgs e)
        {
            return;
            if (dataCollector.IsBusy())
            {                
                return;
            }
            if (!IsStartedCQG)
            {
                labelItem1.Text = "Start CQG first, please.";
                return;
            }
            if (listBoxSymbols.SelectedItems.Count == 0)
            {
                labelItem1.Text = "Please, select the instruments.";
                return;
            }

            StartCollecting();

            var symbols = (from object item in listBoxSymbols.SelectedItems select item.ToString()).ToList();

            new Thread(() =>
                {
                    Thread.Sleep(1000);
                    dataCollector.WaitEndOfOperation();
                    // if last                    
                    Invoke((Action)delegate
                    {
                        Thread.Sleep(1000);
                        //dbSel.LoadSymbolList(listBoxSymbols);
                        //TODO: LoadSymbolList(listBoxSymbols);

                        if (checkBoxAutoCheckForMissedBars.Value)
                            dataCollector.MissingBarRequest(CEL, symbols.ToArray(),(int)nudEndBar.Value,true);

                        //dataCollector.ResetSymbols();
                    });                    
                }).Start();
        
        }
        
        /*
        public void refreshCustomList()
        {
            customeListsDict = dbSel.LoadCmList(null);
            IEnumerator<String> customeLists = customeListsDict.Keys.GetEnumerator();
            checkedListBoxLists.Items.Clear();
            while (customeLists.MoveNext())
            {
                checkedListBoxLists.Items.Add(customeLists.Current);
            }            
        }
        */
        string lastTip;
        //private TimeSpan step;
        private void listBoxSymbols_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var listBox = (ListBox)sender;
                int index = listBox.IndexFromPoint(e.Location);
                if (index > -1 && index < listBox.Items.Count)
                {
                    string tip = listBox.Items[index].ToString();
                    if (tip != lastTip)
                    {
                        toolTip1.SetToolTip(listBox, tip);
                        lastTip = tip;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogAdd("listBoxSymbols_MouseMove. " + ex.Message, Category.Error);
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxTablesName.Items.Count; i++)
            {
                listBoxTablesName.SetSelected(i, true);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxTablesName.Items.Count; i++)
            {
                listBoxTablesName.SetSelected(i, false);
            }
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxTablesName.Items.Count; i++)
            {

                listBoxTablesName.SetSelected(i, !listBoxTablesName.SelectedItems.Contains(listBoxTablesName.Items[i]));
            }      
        }

        private void loadAllSymbolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*
            if (dbSel != null)
            {
                dbSel.LoadSymbolList(listBoxSymbols);
                resetColorMarks();
            }*/
        }

        private void metroShell1_SettingsButtonClick(object sender, EventArgs e)
        {
            var form2 = new FormSettings();
            form2.ShowDialog();
        }

        public bool CollectingFromList { get; set; }

        private void metroTileItem1_Click(object sender, EventArgs e)
        {
            return;
            if (dataCollector.IsBusy())
            {
                return;
            }
            if (!IsStartedCQG)
            {
                labelItem1.Text = "Start CQG first, please.";
                return;
            }
            if (listBoxTablesName.SelectedItems.Count < 1)
            {
                labelItem1.Text="Select tables, please.";
                return;
            }

            int N = listBoxTablesName.SelectedItems.Count;
            var symbols = new string[N];

            for (int i = 0; i < N; i++)
            {
                symbols[i] =
                    listBoxTablesName.SelectedItems[i].ToString();
            }


            dataCollector.MissingBarRequest(CEL, symbols, (int)nudEndBar.Value);
        }
        /*
        private List<MissedStr> MissedInTable(string smb, List<DateTime> aResultDateTimes, DateTime MissDateTimeStart, DateTime MissDateTimeEnd, bool DayStartsYesterday)
        {
            var resultList = new List<MissedStr>();
            var missingList = new List<DateTime>();
            DateTime refresh=DateTime.Now;
            DateTime StartDateTime = DayStartsYesterday ? MissDateTimeStart.AddDays(-1) : MissDateTimeStart;            
            // MISSED
            for (DateTime curTime = StartDateTime; curTime < MissDateTimeEnd; curTime = curTime.AddMinutes(1))
            {
                // not exsists and its after first
                if (!ExistsTime(aResultDateTimes, curTime) && curTime>aResultDateTimes[0])
                {                    
                    dbSel.AddToMissingTable(smb, refresh, curTime);
                    missingList.Add(curTime);                    
                }
            }
            if (missingList.Count == 1)
            {
                resultList.Add(new MissedStr{start = missingList[0], end = missingList[0]});
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
        */
        public struct MissedStr
        {
            public DateTime start;
            public DateTime end;
        }
        /*
        private bool ExistsTime(IEnumerable<DateTime> aResultDateTimes, DateTime curTime)
        {
            return aResultDateTimes.Any(item => item == curTime);
        }
        */        



        private void listBoxSymbols_MouseDown(object sender, MouseEventArgs e)
        {
            
            if (e.Button == MouseButtons.Right)
            {
                listBoxSymbols.SelectedIndex = listBoxSymbols.IndexFromPoint(e.X, e.Y);
                listBoxSymbols.Show();
            }
        }

        

        private void metroTileItemCollectList_Click(object sender, EventArgs e)
        {
            return;
            if (dataCollector.IsBusy())
            {                
                return;
            }
            if (!IsStartedCQG)
            {
                labelItem1.Text = "Start CQG first, please.";
                return;
            }
            if (checkedListBoxLists.CheckedItems.Count == 0)
            {
                labelItem1.Text = "Please, select the lists.";
                return;
            }
            //***********    

            var AllSymbols= new List<string>();
            
            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 Thread.CurrentThread.Name = "ForAllLists";
                                                 foreach (var listName in checkedListBoxLists.CheckedItems)
                                                 {
                                                     //bool useTimeRange;
                                                     //collectSyncMutex.WaitOne();
                                                     object name = listName;
                                                     Invoke((Action)delegate
                                                                        {
                                                                            //***********
                                                                            TimeRange tr = customeListsDict[name.ToString()];

                                                                            if (!tr.StartTime.Equals(new DateTime()))
                            
                                                                            {
                                                                                //useTimeRange = true;
                                                                                startTime = tr.StartTime;
                                                                                endTime = tr.endTime;
                            
                                                                                dateTimeInputStart.Value = startTime;
                                                                                dateTimeInputEnd.Value = endTime;
                            
                                                                            }
                                                                            eHistoricalPeriod res;
                                                                            if (Enum.TryParse(tr.strTF_Tyoe, out res))
                                                                                cmbHistoricalPeriod.SelectedItem = res;

                                                                            else
                                                                                cmbHistoricalPeriod.SelectedItem = tr.strTF_Tyoe;
                                                                            eTimeSeriesContinuationType res1;
                                                                            if (Enum.TryParse(tr.strContinuationType, out res1))
                                                                            {
                                                                                cmbContinuationType.SelectedItem = res1;
                                                                            }
                                                                            //***********

                                                                            List<String> symbolList =new List<string>();
                                                                            //TODO: = dbSel.load_cmList(name.ToString(), listBoxSymbols);

                                                                            AllSymbols.AddRange(symbolList);
                                                                            for (int i = 0; i < listBoxSymbols.Items.Count; i++)
                                                                            {
                                                                                listBoxSymbols.SetSelected(i, true);
                                                                            }

                                                                            StartCollecting();// STRAT COLLECTING SYMBOLS FROM CURRENT LIST
                                                                        });

                                                     Thread.Sleep(1000);
                                                     dataCollector.WaitEndOfOperation();
                                                     // if last
                                                     if (listName == checkedListBoxLists.CheckedItems[checkedListBoxLists.CheckedItems.Count-1])
                                                     {
                        
                                                         Invoke((Action)delegate
                                                                            {
                                                                                Thread.Sleep(1000);
                                                                                // TODO: dbSel.LoadSymbolList(listBoxSymbols);

                                                                                if(checkBoxAutoCheckForMissedBars.Value)
                                                                                    dataCollector.MissingBarRequest(CEL, AllSymbols.ToArray(),(int)nudEndBar.Value);

                                                                                //dataCollector.ResetSymbols();
                                                                            });
                                                     }
                        
                                                 }
                                             });
                
	                                
        }

        private void progressBarItemCollecting_ValueChanged(object sender, EventArgs e)
        {
            Refresh();
        }


        #region  NEW REGION

        #region SYMBOLS


        private void newSymbolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _addSymbolControl = new ControlNewSymbol { Commands = _commands };
            ShowModalPanel(_addSymbolControl, DevComponents.DotNetBar.Controls.eSlideSide.Right);  
        }

        private void AddNewSymbolExecuted(object sender, EventArgs e)
        {
            var symbName = _addSymbolControl.ui_textBoxXSymbolName.Text;
            if (!_symbols.Exists(a => a.SymbolName ==symbName))
            {                
                CloseAddSymbolControl();
                ClientDataManager.AddNewSymbol(symbName);
                RefreshSymbols();
            }else
            {
                ToastNotification.Show(_addSymbolControl, @"This symbol already exists!");
            }
        }

        private void CancelNewSymbolExecuted(object sender, EventArgs e)
        {
            CloseAddSymbolControl();
        }

        private void CloseAddSymbolControl()
        {
            if (_addSymbolControl == null) return;
            CloseModalPanel(_addSymbolControl, DevComponents.DotNetBar.Controls.eSlideSide.Right);
            _addSymbolControl.Dispose();
            _addSymbolControl = null;            
        }

        private void iEditListToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void iDeleteListToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        #endregion

        #region

        //**
        private void CancelNewListExecuted(object sender, EventArgs e)
        {

        }

        private void AddNewListExecuted(object sender, EventArgs e)
        {
            
        }
        //**
        private void SaveEditListExecuted(object sender, EventArgs e)
        {
            // TODO: Save changes 
            CloseEditListControl();
        }

        private void CancelEditListExecuted(object sender, EventArgs e)
        {            
            CloseEditListControl();
        }
        
        private void CloseEditListControl()
        {
            if (_editListControl == null) return;
            CloseModalPanel(_editListControl, DevComponents.DotNetBar.Controls.eSlideSide.Right);
            _editListControl.Dispose();
            _editListControl = null;
        }
 
        #endregion


        private void StartControl_LogonClick(object sender, EventArgs e)
        {

            Settings.Default.connectionUser = _startControl.ui_textBoxX_login.Text;
            Settings.Default.connectionPassword = _startControl.ui_textBoxX_password.Text;
            Settings.Default.connectionHost = _startControl.ui_textBoxX_host.Text;

           LoginToServer(Settings.Default.connectionUser, Settings.Default.connectionPassword, Settings.Default.connectionHost);
           
        }



        private void CqgConnectionStatusChanged(bool isConnectionUp)
        {
            if (isConnectionUp)
            {
                _startControl.ui_labelX_CQGstatus.Text = @"CQG started";
                labelItemStatusCQG.Text = @"CQG started";
                IsStartedCQG = true;
                //TODO:_startControl.ui_buttonX_logon.Enabled = true;                
            }
            else
            {
                _startControl.ui_labelX_CQGstatus.Text = @"CQG not started";
                labelItemStatusCQG.Text = @"CQG not started";                
                IsStartedCQG = false;
                //TODO:_startControl.ui_buttonX_logon.Enabled = false;
            }
            Refresh();
        }

        private void ui_buttonX_shareConnect_Click(object sender, EventArgs e)
        {
            ClientDataManager.ConnectToShareDb(_connectionToSharedDb, _client.UserID);
            _client.ConnectedToSharedDb = true;

            RefreshGroups();
            RefreshSymbols();
            Refresh();
        }

        private void ui_buttonX_localConnect_Click(object sender, EventArgs e)
        {
            //TODO: local connection
            var conn = _connectionToSharedDb;

            ClientDataManager.ConnectToLocalDb(conn, _client.UserID);
        }

        private void ClientDataManager_ConnectionStatusChanged(bool connected, bool isShared)
        {
            var strConn = connected ? @"Connnected to " + (isShared ? @"Shared DB" : @"Local DB") : "Not connected";
            ui_status_labelItemStatusSB.Text = strConn;
            if(connected)
            {
                metroTabItem2.Visible = true;
                metroTabItem3.Visible = true;
                RefreshSymbols();
                if(_client.AllowedSymbolGroups.Count !=0)
                RefreshGroups();
                else
                {
                    checkedListBoxLists.Items.Clear();
                }
            }
            else
            {
                metroTabItem2.Visible = false;
                metroTabItem3.Visible = false;
            }
RefreshGroups();
        }

        private void RefreshGroups( )
        {
            _groups = ClientDataManager.GetGroups(_client.UserID);
        //    Task.Factory.StartNew((Action) checkedListBoxLists.Invoke((Action) (() => checkedListBoxLists.Items.Clear()))
        //);
        //  Task.Factory.StartNew(
        //      delegate
        //          {
        //              foreach (var item in _groups)
        //              {
        //                  var item1 = item;
        //                  checkedListBoxLists.Invoke((Action) (() => checkedListBoxLists.Items.Add(item1.GroupName)));
        //              }
        //          });
            checkedListBoxLists.Invoke((Action) (() => checkedListBoxLists.Items.Clear()));
             foreach (var item in _groups)
             {
                 var item1 = item;
                 checkedListBoxLists.Invoke((Action)(() => checkedListBoxLists.Items.Add(item1.GroupName)));
             }
        }

        private void RefreshSymbols()
        {
            listBoxSymbols.Items.Clear();
            listBoxTablesName.Items.Clear();

            _symbols = ClientDataManager.GetSymbols();
            foreach (var item in _symbols)
            {
                listBoxSymbols.Items.Add(item.SymbolName);
                listBoxTablesName.Items.Add(item.SymbolName);
            }            
        }

        private void metroShell1_LogOutButtonClick(object sender, EventArgs e)
        {
            //TODO: call LogOut function
            _startControl.IsOpen = true;
        }

        #region COLLECT DATA TAB


        #endregion



        #endregion



        
    }

}