using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StandaloneGeneratorV3
{
    class Logger
    {
        readonly Dispatcher dispatcher = null;
        readonly TextBox container = null;

        private ThcrapDll.log_print_cb thcrap_print;
        private ThcrapDll.log_nprint_cb thcrap_nprint;
        public Logger(Dispatcher dispatcher, TextBox container)
        {
            this.dispatcher = dispatcher;
            this.container = container;

            thcrap_print  = (string text) => this.Log(text);
            thcrap_nprint = (byte[] text, UInt32 size) =>
            {
                var encoding = new UTF8Encoding();
                char[] chars = encoding.GetChars(text);
                this.Log(chars.ToString());
            };
            ThcrapDll.log_set_hook(thcrap_print, thcrap_nprint);
            ThcrapDll.log_init(false);
        }

        private void Log(string msg)
        {
            Console.Write(msg);
            if (dispatcher.Thread == Thread.CurrentThread)
            {
                container.AppendText(msg);
                (container.Parent as ScrollViewer).ScrollToBottom();
            }
            else
            {
                dispatcher.Invoke(() =>
                {
                    container.AppendText(msg);
                    (container.Parent as ScrollViewer).ScrollToBottom();
                });
            }
        }
    }
}
