using System;
using System.Windows.Forms;

namespace DataNetClient.Forms
{
    public partial class ControlNewSymbol : UserControl
    {
        public ControlNewSymbol()
        {
            InitializeComponent();
        }

        private MetroBillCommands _commands;
        /// <summary>
        /// Gets or sets the commands associated with the control.
        /// </summary>
        public MetroBillCommands Commands
        {
            get { return _commands; }
            set
            {
                if (value != _commands)
                {
                    MetroBillCommands oldValue = _commands;
                    _commands = value;
                    OnCommandsChanged(oldValue, value);
                }
            }
        }
        /// <summary>
        /// Called when Commands property has changed.
        /// </summary>
        /// <param name="oldValue">Old property value</param>
        /// <param name="newValue">New property value</param>
        protected virtual void OnCommandsChanged(MetroBillCommands oldValue, MetroBillCommands newValue)
        {
            if (newValue != null)
            {
                saveButton.Command = newValue.NewSymbolCommands.Add;
                cancelButtonX.Command = newValue.NewSymbolCommands.Cancel;                
            }
            else
            {
                saveButton.Command = null;
                cancelButtonX.Command = null;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            cancelButtonX.Command.Execute();
        }

    }
}
