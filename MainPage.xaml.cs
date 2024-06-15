using System.Diagnostics;
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace MauiApp2
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private int btn1 = 0;
        private int btn2 = 0;
        private int btn3 = 0;

        // THIS IS THE DEFAULT WAY
        // USING ASYNC-AWAIT ALWAYS WORKS
        // https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/threading-model#block-the-ui-thread
        private async void myButton_Clicked(object sender, EventArgs e)
        {
#if WINDOWS
            btn1++;
            var javaScriptToExecute = "function foo(){return " + btn1 + ";}; foo();";

            WebView2 webView = (WebView2)blazorWebView.Handler.PlatformView;

            var result = await webView.CoreWebView2.ExecuteScriptAsync(javaScriptToExecute);

            Debug.WriteLine(result);
            myButton.Text = "Async: " + result;
#endif
        }

        // WaitTaskCompleted, THAT IS CURRENTLY USED IN WPF (OPENSILVER SIMULATOR), WORKS
        // BUT NOT HERE IN MAUI...
        private void myButton2_Clicked(object sender, EventArgs e)
        {
#if WINDOWS
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _dispatcherQueue = dispatcherQueue;

            btn2++;
            var javaScriptToExecute = "function foo(){return " + btn2 + ";}; foo();";

            // THE OBJECTIVE HERE IS GET THE outResult IN A SYNC WAY
            string outResult = null;

            // METHOD 1
            var task = Task.Run(() =>
            {
                string result = ExecuteScriptSync(dispatcherQueue, javaScriptToExecute);
                outResult = result;
                Trace.WriteLine("The Result Is: " + result);

                Debug.WriteLine("in thread: " + result);
                dispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() =>
                {
                    myButton2.Text = "From Task: " + result;
                }));                       
            });
            // TODO: Uncomment bellow for 2nd button fail
            // BUT This DOESN'T work. If you uncomment the following line, the app will block
            //WaitTaskCompleted(task);
            //myButton2.Text = "From Task: " + outResult;
#endif
        }

        // HERE IS ANOTHER APTEMPT USING THREADS INSTEAD
        private void myButton3_Clicked(object sender, EventArgs e)
        {
#if WINDOWS
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _dispatcherQueue = dispatcherQueue;

            btn3++;
            var javaScriptToExecute = "function foo(){return " + btn3 + ";}; foo();";

            // THE OBJECTIVE HERE IS GET THE outResult IN A SYNC WAY
            string outResult = null;

            // METHOD 2 -  This works
            var t = new Thread(() =>
            {
                string result = ExecuteScriptSync(dispatcherQueue, javaScriptToExecute);
                outResult = result;
                Trace.WriteLine("The Result Is: " + result);

                Debug.WriteLine("in thread: " + result);
                dispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() =>
                {
                    myButton3.Text = "From Thread " + result;
                }));
            });
            t.Name = "My Thread";
            t.Start();

            // TODO: Uncomment bellow for 3rd button failbtn1.Text =
            // BUT This DOESN'T work. If you uncomment following 2 lines, the app will block
            //t.Join();
            //myButton3.Text = "From Thread " + outResult;
#endif
        }


#if WINDOWS
        private static Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

        private static void WaitTaskCompleted(Task execScriptTask)
        {
            SpinWait.SpinUntil(() =>
            {
                bool taskCompleted = execScriptTask.IsCompleted;
                if (!taskCompleted)
                {
                    _dispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() => { }));
                }
                return taskCompleted;
            });
        }

        // ADAPTED FROM HERE:
        // https://github.com/MicrosoftEdge/WebView2Feedback/discussions/3960#discussioncomment-7244322
        public string ExecuteScriptSync(Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue, string javaScriptToExecute)
        {
            string result = null;
            Exception failureValue = null;

            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            dispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(async () =>
            {
                try
                {
                    WebView2 webView = (WebView2)blazorWebView.Handler.PlatformView;
                    result = await webView.CoreWebView2.ExecuteScriptAsync(javaScriptToExecute);
                }
                catch (Exception e)
                {
                    failureValue = e;
                }
                autoResetEvent.Set();
            }));

            autoResetEvent.WaitOne();

            if (failureValue != null)
            {
                throw failureValue;
            }
            return result;
        }
#endif

    }
}
