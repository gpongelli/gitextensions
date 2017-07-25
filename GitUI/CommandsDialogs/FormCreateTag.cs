﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GitCommands;
using GitUI.Script;
using ResourceManager;
using GitCommands.Git;

namespace GitUI.CommandsDialogs
{
    public sealed partial class FormCreateTag : GitModuleForm
    {
        private readonly TranslationString _messageCaption = new TranslationString("Tag");

        private readonly TranslationString _noRevisionSelected =
            new TranslationString("Select 1 revision to create the tag on.");

        private readonly TranslationString _pushToCaption = new TranslationString("Push tag to '{0}'");

        private string _currentRemote = "";

        private IGitTagController _gitTagController;

        public FormCreateTag(GitUICommands aCommands, GitRevision revision)
            : base(aCommands)
        {
            InitializeComponent();
            Translate();

            tagMessage.MistakeFont = new Font(SystemFonts.MessageBoxFont, FontStyle.Underline);
            commitPickerSmallControl1.UICommandsSource = this;
            if (IsUICommandsInitialized)
                commitPickerSmallControl1.SetSelectedCommitHash(revision == null ? Module.GetCurrentCheckout() : revision.Guid);

            _gitTagController = new GitTagController(Module);
        }

        private void FormCreateTag_Load(object sender, EventArgs e)
        {
            textBoxTagName.Select();
            _currentRemote = Module.GetCurrentRemote();
            if (String.IsNullOrEmpty(_currentRemote))
                _currentRemote = "origin";
            pushTag.Text = string.Format(_pushToCaption.Text, _currentRemote);
        }

        private void OkClick(object sender, EventArgs e)
        {
            try
            {
                var tagName = CreateTag();

                if (pushTag.Checked && !string.IsNullOrEmpty(tagName))
                    PushTag(tagName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private string CreateTag()
        {
            string revision = commitPickerSmallControl1.SelectedCommitHash;

            if (revision.IsNullOrEmpty())
            {
                MessageBox.Show(this, _noRevisionSelected.Text, _messageCaption.Text);
                return string.Empty;
            }

            GitTagController.TagOperation _tagOperation = (GitTagController.TagOperation) annotate.SelectedIndex;
            var s = _gitTagController.CreateTag(revision, textBoxTagName.Text, ForceTag.Checked, _tagOperation, tagMessage.Text, textBoxGpgKey.Text);
            if (!string.IsNullOrEmpty(s))
            {
                MessageBox.Show(this, s, _messageCaption.Text);
            }

            if (s.Contains("fatal:"))
                return string.Empty;

            DialogResult = DialogResult.OK;
            return textBoxTagName.Text;
        }

        private void PushTag(string tagName)
        {
            var pushCmd = GitCommandHelpers.PushTagCmd(_currentRemote, tagName, false);

            ScriptManager.RunEventScripts(this, ScriptEvent.BeforePush);

            using (var form = new FormRemoteProcess(Module, pushCmd)
            {
                Remote = _currentRemote,
                Text = string.Format(_pushToCaption.Text, _currentRemote),
            })
            {

                form.ShowDialog();

                if (!Module.InTheMiddleOfAction() && !form.ErrorOccurred())
                {
                    ScriptManager.RunEventScripts(this, ScriptEvent.AfterPush);
                }
            }
        }

        private void AnnotateDropDownChanged(object sender, EventArgs e)
        {
            textBoxGpgKey.Enabled = annotate.SelectedIndex == 3;
            keyIdLbl.Enabled = annotate.SelectedIndex == 3;
            tagMessage.Enabled = annotate.SelectedIndex > 0;
        }
    }
}
