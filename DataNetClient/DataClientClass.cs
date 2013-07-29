using System;
using System.Collections.Generic;
using DataAdminCommonLib;
using Hik.Communication.ScsServices.Service;

namespace DataNetClient
{
   
        [Serializable]
        public class DataClientClass : IDataAdminService
        {
            #region EVENTS

            public delegate void RaiseLoginEvent(object sender, MessageFactory.ChangePrivilage msg);

            public delegate void RaiseChangedPrivilagesEvent(object sender, MessageFactory.ChangePrivilage msg);

            public delegate void RaiseLogoutEvent(object sender, object msg);

            public delegate void RaiseBlockEvent(object sender, object msg);

            public delegate void RaiseSymbolListRecievedEvent(object sender, string msg);

            public delegate void RaiseLoginFailedEvent(object sender, MessageFactory.LoginMessage msg);


            public event RaiseLoginEvent login;
            public event RaiseBlockEvent block;
            public event RaiseLogoutEvent logout;
            public event RaiseChangedPrivilagesEvent changePrivilages;
            public event RaiseSymbolListRecievedEvent symblolListRecieved;
            public event RaiseLoginFailedEvent loginFailed;
            #endregion

            #region FIELDS

           private string _username;
        

            public IScsServiceClient Client { get; set; }
            public IDataAdminService ClientProxy { get; set; }
            public int UserID
            {
                get; set; 
            }
            public string UserName
            {
                get { return _username; }
            }
            public MessageFactory.ChangePrivilage Privileges { get; set; }
            public bool BlockedByAdmin { get; set; }
            public List<int> AllowedSymbolGroups { get; set; }

            public bool ConnectedToSharedDb { get; set; }
            public bool ConnectedToLocalDb { get; set; }
            #endregion

            #region Constructor

            public DataClientClass(string username, IScsServiceClient client, IDataAdminService clientProxy)
            {
                _username = username;
                Client = client;
                ClientProxy = clientProxy;
                AllowedSymbolGroups = new List<int>();
              
            }

            public DataClientClass(string username)
            {
                _username = username;
                Privileges = new MessageFactory.ChangePrivilage(false, false, false, false, false, false, false);
                AllowedSymbolGroups = new List<int>();
            }

            #endregion

  

            public void Login(MessageFactory.LoginMessage loginParams)
            {
               if(loginFailed != null)
               {
                   loginFailed(this, loginParams);
               }
            }

            public void onLogon(bool logged, MessageFactory.ChangePrivilage getprivilages)
            {
                if (login != null)
                {
                 login(this, getprivilages);
                }

            }

            public void BlockClient(string user)
            {
                string obj = "Blocked";
                if (block != null)
                    block(this, obj);
            }

            public void ChangePrivilege(string user, MessageFactory.ChangePrivilage newprivilege)
            {
                if (changePrivilages != null)
                {
                    var sdf = newprivilege.LocalDBAllowed;
                    changePrivilages(this, newprivilege);
                }

            }

            public void Logout()
            {
                if (logout != null)
                    logout(this, "YOUR ACCOUNT DELETED BY ADMIN");
            }

            public void SendAllowedSymbolList(object symbolList)
            {
                throw new NotImplementedException();
            }

            public void SendAllowedSymbolGroups(object symbGroupList)
            {
                if (symblolListRecieved != null)
                {
                    symblolListRecieved(this, symbGroupList.ToString());
                }
            }

            public void onSymbolListRecieved(object symbolList)
            {
                //todo send this message when user created a symbol list 
                throw new NotImplementedException();
            }

            public void onSymbolGroupListRecieved(object symbolGroupList)
            {
               
            }

        }
    }

