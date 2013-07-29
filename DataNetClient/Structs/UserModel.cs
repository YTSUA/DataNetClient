namespace DataNetClient.Structs
{
    struct UserModel
    {
        public int Id;
        public string Name;
        public string Password;
        public string FullName;
        public string Email;
        public string Phone;
        public string IpAdress;
        public bool Blocked;
        public bool AllowDataNet;
        public bool AllowTickNet;
        public bool AllowLocalDb;
        public bool AllowRemoteDb;
        public bool AllowAnyIp;
        public bool AllowMissBars;
        public bool AllowCollectFrCqg;
    }
}
