using DevComponents.DotNetBar;

namespace DataNetClient
{
    /// <summary>
    /// Represents all application commands.
    /// </summary>
    public class MetroBillCommands
    {
        private StartControlCommands _startControlCommands = new StartControlCommands();        
        private NewSymbolCommands _newSymbolCommands = new NewSymbolCommands();
        private NewListCommands _newListCommands = new NewListCommands();
        private EditListCommands _editListCommands = new EditListCommands();

        public StartControlCommands StartControlCommands
        {
            get { return _startControlCommands; }
            set { _startControlCommands = value; }
        }

        public NewSymbolCommands NewSymbolCommands
        {
            get { return _newSymbolCommands; }
            set { _newSymbolCommands = value; }
        }

        public NewListCommands NewListCommands
        {
            get { return _newListCommands; }
            set { _newListCommands = value; }
        }
        public EditListCommands EditListCommands
        {
            get { return _editListCommands; }
            set { _editListCommands = value; }
        }


    }
    public class StartControlCommands
    {
        public Command Logon { get; set; }
        public Command Exit { get; set; }
    }

    public class NewSymbolCommands
    {
        public Command Add { get; set; }

        public Command Cancel { get; set; }
    }

    public class NewListCommands
    {
        public Command Add { get; set; }

        public Command Cancel { get; set; }
    }

    public class EditListCommands
    {
        public Command Save { get; set; }

        public Command Cancel { get; set; }
    }
}
