using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using SexyFeeds.Properties;


namespace SexyMail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       
        bool waitForPlayToEnd = false;
        bool waitForReadSessionToEnd = false;

        System.Windows.Forms.Timer myRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();

            //myMediaElement.LoadedBehavior = MediaState.Manual;
            mediaElement.MediaEnded += new RoutedEventHandler(myMediaElement_MediaEnded);
            mediaElement.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(myMediaElement_MediaFailed);

            myRefreshTimer = new System.Windows.Forms.Timer();
            myRefreshTimer.Interval = Settings.Default.AutoRefreshMinutes * 1000 * 60;
            myRefreshTimer.Tick += new EventHandler(myRefreshTimer_Tick);
        }

        void myRefreshTimer_Tick(object sender, EventArgs e)
        {
            RunSexxyFeeds();
        }

        private void btnRock_Click(object sender, RoutedEventArgs e)
        {
            RunSexxyFeeds();           

        }

        private void RunSexxyFeeds()
        {
            if (waitForReadSessionToEnd) return;

            waitForReadSessionToEnd = true;

            if (comboMode.SelectedValue == null) return;

            if (comboPaths.SelectedValue == null) return;

            var selectedMode = comboMode.Text;
            var selectedPath = comboPaths.Text;

            lbxFeed.Items.Clear();
            btnRock.IsEnabled = false;


            btnRock.IsEnabled = true;

            if (selectedMode == "Mail")
            {
                new Thread(() =>
                {
                    loadMailFeed(selectedPath);

                }).Start();
            }
            else
            {

            }
        }

        private void SexxySpeakFeed(String message)
        {
            this.Dispatcher.Invoke((Action)delegate
            {
                tbxStatus.AppendText(String.Format("\r\n{0}",message));
                tbxStatus.ScrollToEnd();

                waitForPlayToEnd = true;
                mediaElement.Source = new Uri(String.Format("http://translate.google.com/translate_tts?tl=en&q={0}", Uri.EscapeUriString(message.Trim())));
            });
            
        }

        void myMediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            waitForPlayToEnd = false;
        }

        void myMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            waitForPlayToEnd = false;
        }

        private void loadMailFeed(String selectedPath)
        {
            try
            {
                ImapX.ImapClient client = new ImapX.ImapClient(Settings.Default.IMAPServer, Settings.Default.IMAPPort, Settings.Default.IMAPUseSSL);
                bool result = client.Connection();
                if (result)
                {
                    result = client.LogIn(Settings.Default.Username, Settings.Default.Password);

                    if (result)
                    {

                        this.Dispatcher.Invoke((Action)delegate
                        {
                            tbxStatus.AppendText(Environment.NewLine + "Log-on successful");
                        });


                        bool foundSomeMail = false;

                        bool hasSexxyFolder = false;                        

                        if (client.Folders["SexxyFeeds"] != null)
                            hasSexxyFolder = true;
                        else
                            if (client.CreateFolder("SexxyFeeds"))
                            hasSexxyFolder = true;

                        var folder = client.Folders[selectedPath];

                        foreach (ImapX.Message m in folder.Messages)
                        {
                            m.ProcessFlags();

                            if (!m.Flags.Contains("\\Seen")) //new mail
                            {
                                m.ProcessHeader();                                

                                if (hasSexxyFolder)
                                    folder.CopyMessageToFolder(m, client.Folders["SexxyFeeds"]);                                

                                // m.Process(); -- processes entire mail! chewing up bandwidth -- good only if u need entire mail body

                                var feedItem = String.Format("From {0} : {1}", m.From[0].DisplayName, m.Subject);

                                this.Dispatcher.Invoke((Action)delegate
                                  {

                                      lbxFeed.Items.Add(feedItem);
                                      lbxFeed.ScrollIntoView(feedItem);
                                  });


                                SexxySpeakFeed(feedItem);
                                while (waitForPlayToEnd) ;

                                foundSomeMail = true;


                                /*if (m.Subject == "test email with 3 attachments (txt, png, wav)")
                                {
                                    tbxStatus.AppendText(Environment.NewLine + Environment.NewLine + "Found email in question..." +
                                        Environment.NewLine + Environment.NewLine);
                                    string path = Application.StartupPath + "\\email\\";
                                    string filename = "TxtPngWav";
                                    //comment-out following line if you don't want to d/l the actual email
                                    m.SaveAsEmlToFile(path, filename);
                                    //use above line, and comment out the following if(){}, if you prefer to download the whole
                                    //email in .eml format
                                    if (m.Attachments.Count > 0)
                                    {
                                        for (int i = 0; i < m.Attachments.Count; i++)
                                        {
                                            m.Attachments[i].SaveFile(path);
                                            tbxStatus.AppendText("Saved attachment #" + (i + 1).ToString());
                                            tbxStatus.AppendText(Environment.NewLine + Environment.NewLine);
                                        }
                                    }
                                }*/
                            }

                            //meaning this folder is meant to only hold mail temporary so it can be captured by SexxyMail
                            //thus we delete it from here once it has been seen
                            if (selectedPath == Settings.Default.IMAPMailRouterFolder)
                            {
                                folder.DeleteMessage(m);
                            }
                        }

                        if (foundSomeMail)
                        {
                            SexxySpeakFeed(String.Format("SexxyFeeds for {0} comes to an end. {1}.", Settings.Default.MailServerName,Greetings()));
                            while (waitForPlayToEnd) ;
                        }

                        this.Dispatcher.Invoke((Action)delegate
                        {
                            tbxStatus.AppendText(Environment.NewLine + "Reading Messages Over!");
                            tbxStatus.ScrollToEnd();
                        });

                        waitForReadSessionToEnd = false;

                    }
                }
                else
                {

                    this.Dispatcher.Invoke((Action)delegate
                    {
                        tbxStatus.AppendText(Environment.NewLine + "connection failed");
                        tbxStatus.ScrollToEnd();

                        SexxySpeakFeed(String.Format("SexxyFeeds was denied access to {0} Servers!", Settings.Default.MailServerName));
                        while (waitForPlayToEnd) ;

                        waitForReadSessionToEnd = false;
                    });

                }
            }
            catch (Exception ex)
            {

                this.Dispatcher.Invoke((Action)delegate
                {
                    tbxStatus.AppendText(String.Format("\r\n{0}", ex.Message));
                    tbxStatus.ScrollToEnd();
                });

                waitForReadSessionToEnd = false;
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnGetReady_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                SexxySpeakFeed("Welcome to Sexxy Feeds by Nemesis Fixx!");
                while (waitForPlayToEnd) ;
            }).Start();

            if (comboMode.SelectedValue == null) return;

            var selectedMode = comboMode.Text;

            if (selectedMode == "Mail")
            {
                new Thread(() =>
                {
                    PrepareMail();

                }).Start();
            }
            else
            {
                PrepareRazor();
            }

        }

        private void PrepareRazor()
        {
            comboPaths.Items.Clear();
        }

        private void PrepareMail()
        {
            this.Dispatcher.Invoke((Action)delegate
            {
                comboPaths.Items.Clear();
            });

            //greet the owner
            SexxySpeakFeed(String.Format("{0} {1}!",Greetings(),Settings.Default.Owner));
            while (waitForPlayToEnd) ;

            ImapX.ImapClient client = new ImapX.ImapClient(Settings.Default.IMAPServer, Settings.Default.IMAPPort, Settings.Default.IMAPUseSSL);
            bool result = client.Connection();
            if (result)
            {
                result = client.LogIn(Settings.Default.Username, Settings.Default.Password);

                if (result)
                {

                    this.Dispatcher.Invoke((Action)delegate
                    {
                        tbxStatus.AppendText(Environment.NewLine + "Log-on successful");
                    });


                    SexxySpeakFeed(String.Format("Access to {0} Servers was Granted.",Settings.Default.MailServerName));
                    while (waitForPlayToEnd) ;


                    ImapX.FolderCollection folders = client.Folders;


                    foreach (var folder in folders)
                        this.Dispatcher.Invoke((Action)delegate
                        {
                            comboPaths.Items.Add(folder.Name);
                        });

                    SexxySpeakFeed(String.Format("Please select a {0} Path for us to get Rocking!",Settings.Default.MailServerName));
                    while (waitForPlayToEnd) ;
                }
            }
            else
            {

                this.Dispatcher.Invoke((Action)delegate
                {
                    tbxStatus.AppendText(Environment.NewLine + "connection failed");

                    SexxySpeakFeed(String.Format("SexxyFeeds was denied access to {0} Servers!",Settings.Default.MailServerName));                    
                });
                while (waitForPlayToEnd) ;

            }
        }

        private string Greetings()
        {
            var hour = DateTime.Now.Hour;

            if (hour < 12)
                return "Good Morning";
            else if (hour < 16)
                return "Good Afternoon";
            else return "Good Evening";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            myRefreshTimer.Start();
        }
    }
}
