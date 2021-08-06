using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Patching;

namespace Patcher
{
    public partial class MainWindow : Window
    {
        private static string DIALOG_CAPTION = "Hearthstone Access";

        private Patching.Patcher m_patcher;

        private SoundPlayer m_soundPlayer;

        public MainWindow()
        {
            InitializeComponent();

            Show();

            try
            {
                InitializeSoundPlayer();

                InitializePatcher();

                // Check if we need to force a patcher update
                if (m_patcher.IsOutdated())
                {
                    var downloadUrl = GithubFileRetriever.PATCHER_REPO_RELEASES_PATH;
                    ShowMessageBox($"Your patcher is out of date. Opening {downloadUrl} so you can download the latest version manually");
                    Process.Start(downloadUrl);
                    Close();
                    return;
                }

                // Find HS dir
                var hsDirs = PatchingUtils.FindHearthstoneCandidateDirectories();

                if (hsDirs.Count == 1)
                {
                    OnHearthstoneDirUpdated(hsDirs[0]);
                }
                else
                {
                    if (hsDirs.Count == 0)
                    {
                        ShowMessageBox("Could not find your Hearthstone install directory. Please select it manually");
                    }
                    else
                    {
                        ShowMessageBox("Found multiple Hearthstone install directories in your computer. Please select the correct one manually");
                    }

                    if (!ManuallySelectHearthstoneDirectory())
                    {
                        Close();
                        return;
                    }
                }

                // See if we already have an HSA version installed
                try
                {
                    m_patcher.LoadHearthstoneAccessVersion();
                }
                catch (FileNotFoundException)
                {
                    ShowNotAvailableYetMessageBox();
                    Close();
                    return;
                }

                // Check if an HSA update is available
                MessageBoxResult result;

                if (m_patcher.IsHearthstoneAccessOutOfDate())
                {
                    result = MessageBox.Show("A new patch of Hearthstone Access is available. Apply patch?", "", MessageBoxButton.YesNo);
                }
                else
                {
                    result = MessageBox.Show("Your version of Hearthstone Access is already up to date. Do you want to patch it again?", "", MessageBoxButton.YesNo);
                }

                HandlePatchQueryResponse(result);
                Close();
                return;
            }
            catch (Exception e)
            {
                SimpleExceptionLogger.LogException(e);
                ShowFatalErrorMessage();
                Close();
                return;
            }
        }

        private void ShowNotAvailableYetMessageBox()
        {
            MessageBox.Show("Your version of Hearthstone Access is out of date, but the new version isn't available yet. Please wait for Guide Dev to release the new version");
        }

        private void InitializePatcher()
        {
            var fileRetriever = new GithubFileRetriever();
            var fileDecoder = new FileDecoder();

            m_patcher = new Patching.Patcher(fileRetriever, fileDecoder);
        }

        private void HandlePatchQueryResponse(MessageBoxResult result)
        {
            if (result == MessageBoxResult.Yes)
            {
                PlayLoadingSound();
                m_patcher.PatchHearthstone();
                StopLoadingSound();

                MessageBox.Show("Your game has been patched. Enjoy!");
            }
            else
            {
                MessageBox.Show("Exiting the patcher");
                Close();
                return;
            }
        }

        private void InitializeSoundPlayer()
        {
            m_soundPlayer = new SoundPlayer(Properties.Resources.loading);
        }

        private void PlayLoadingSound()
        {
            m_soundPlayer.PlayLooping();
        }

        private void StopLoadingSound()
        {
            m_soundPlayer.Stop();
        }

        private void ShowFatalErrorMessage()
        {
            ShowMessageBox("Something went wrong. Please make sure you have Internet access and the required permissions to install games on this computer");
        }

        private void ShowMessageBox(string message, MessageBoxButton button = MessageBoxButton.OK)
        {
            MessageBox.Show(message, DIALOG_CAPTION, button);
        }

        private void OnHearthstoneDirUpdated(string hsDir)
        {
            m_patcher.SetHearthstoneDirectory(hsDir);
        }

        private bool ManuallySelectHearthstoneDirectory()
        {
            using (var folderBrowser = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowser.ShowNewFolderButton = false;
                var result = folderBrowser.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = folderBrowser.SelectedPath;

                    if (path != null && PatchingUtils.IsHearthstoneDirectory(path))
                    {
                        OnHearthstoneDirUpdated(path);
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid Hearthstone directory");
                        return ManuallySelectHearthstoneDirectory();
                    }
                }
                else
                {
                    MessageBox.Show("Exiting the patcher. Please make sure Hearthstone is installed and try again");
                    return false;
                }
            }
        }
    }
}
