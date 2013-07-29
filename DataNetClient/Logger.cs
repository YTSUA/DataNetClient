using System;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

namespace DataNetClient
{
    public enum Category { Information , Warning , Error}
    public class Logger
    {
        static Logger _logger;
        static ListView _listView;

        public static Logger GetInstance(ListView listView)
        {
            return _logger ?? (_logger = new Logger(listView));
        }

        private Logger(ListView listView)
        {
            _listView = listView;

        }

        public void LogAdd(string Message, Category cat)
        {
            try
            {
                if (_listView != null)
                {
                    _listView.Invoke((Action)delegate
                    {
                        ListViewItem item = _listView.Items.Add(Message);
                        item.SubItems.Add(DateTime.Now.ToString(CultureInfo.InvariantCulture));
                        item.SubItems.Add(cat.ToString());
                    });
                }
            }catch(Exception)
            {
            }
            File.AppendAllText("log_" + DateTime.Today.ToString("MM.yyyy") + ".log", DateTime.Now.ToString("dd.MM HH:mm:ss")
            + " | " + Message +
            " | " + cat.ToString() + Environment.NewLine);                        
        }
    }
}
