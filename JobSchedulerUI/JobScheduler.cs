﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Configuration;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

using SysconCommon.Common;
using SysconCommon.Common.Environment;

using Syscon.ScheduledJob;

namespace Syscon.JobSchedulerUI
{
    public partial class JobScheduler : Form
    {
        #region Member Variables

        private CompositionContainer _container;

        private SysconCommon.COMMethods mbapi = new SysconCommon.COMMethods();
        bool loaded = false;

        IList<IScheduledJob> _scheduledJobs = new List<IScheduledJob>();
        private IDictionary<string, string> _scheduledJobAndTime = new Dictionary<string, string>();

        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        public JobScheduler()
        {
            InitializeComponent();

            LoadJobPlugIns();

            //Load which jobs are already scheduled in the service.
            string exeLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(string.Format(@"{0}\JobSchedulerService.exe", exeLocation));

            foreach (KeyValueConfigurationElement keyVal in config.AppSettings.Settings)
            {
                _scheduledJobAndTime.Add(keyVal.Key, keyVal.Value);
            }
        }

        /// <summary>
        /// List of all available jobs.
        /// </summary>
        [ImportMany(typeof(IScheduledJob))]
        public IList<IScheduledJob> ScheduledJobs 
        {
            get { return _scheduledJobs; }
            set { _scheduledJobs = value; } 
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadJobPlugIns()
        {
            try
            {
                //Creating an instance of aggregate catalog. It aggregates other catalogs
                var aggregateCatalog = new AggregateCatalog();

                //Build the directory path where the parts will be available
                var directoryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PlugIns");                            

                //Load parts from the available DLLs in the specified path 
                //using the directory catalog
                var directoryCatalog = new DirectoryCatalog(directoryPath, "*.Plugin.dll");

                //Load parts from the current assembly if available
                var asmCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

                //Add to the aggregate catalog
                aggregateCatalog.Catalogs.Add(directoryCatalog);
                aggregateCatalog.Catalogs.Add(asmCatalog);

                //Crete the composition container
                _container = new CompositionContainer(aggregateCatalog);

                // Composable parts are created here i.e. 
                // the Import and Export components assembles here
                _container.ComposeParts(this);
            }
            catch (CompositionException ex)
            {

            }
        }
        
        private void JobScheduler_Load(object sender, EventArgs e)
        {
            // resets it everytime it is run so that the user can't just change to a product they already have a license for
            Env.SetConfigVar("product_id", 178507);

            var product_id = Env.GetConfigVar("product_id", 0, false);
            var product_version = "1.0.0.0";
            bool require_login = false;

            if (!loaded)
            {
                require_login = true;
                loaded = true;
                this.Text += " (version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ")";
            }

            try
            {
                var license = SysconCommon.Protection.ProtectionInfo.GetLicense(product_id, product_version, 15751);
                if (license.IsTrial)
                {
                    if (!license.IsValid())
                    {
                        SetupInvalid();
                    }
                    else
                    {
                        var l = license as SysconCommon.Protection.TrialLicense;
                        SetupTrial(l.DaysLeft);
                    }
                }
                else
                {
                    SetupFull();
                }
            }
            catch
            {
                SetupInvalid();
            }

            txtDataDir.TextChanged += new EventHandler(txtDataDir_TextChanged);

            if (require_login)
            {
                mbapi.smartGetSMBDir();

                if (mbapi.RequireSMBLogin() == null)
                    this.Close();
            }

            txtDataDir.Text = mbapi.smartGetSMBDir();

            ////TEMP Code - Just for testing how to get data from lgrtrn table
            //using (var con = SysconCommon.Common.Environment.Connections.GetOLEDBConnection())
            //{
            //    using (var jobtyps = con.GetTempDBF())
            //    {
            //        int count = con.GetScalar<int>("select count(*) from lgrtrn");
            //        //Log the result
            //        //Env.Log()
            //    }
            //}
        }

        private void txtDataDir_TextChanged(object sender, EventArgs e)
        {
            SysconCommon.Common.Environment.Connections.SetOLEDBFreeTableDirectory(txtDataDir.Text);
        }

        private void SetupTrial(int daysLeft)
        {
            var msg = string.Format("You have {0} days left to evaluate this software", daysLeft);
            this.demoLabel.Text = msg;
            jobsDataGridView.Enabled = true;
            LoadJobsDataGrid();
        }

        private void SetupInvalid()
        {
            jobsDataGridView.Enabled = false;
            this.demoLabel.Text = "Your License has expired or is invalid";
        }

        private void SetupFull()
        {
            this.demoLabel.Text = "";
            this.activateToolStripMenuItem.Visible = false;
            jobsDataGridView.Enabled = true;
            LoadJobsDataGrid();
        }

        private void LoadJobsDataGrid()
        {
            this.jobsDataGridView.DataSource = this.jobListBindingSrc;

            DataGridViewColumn desCol = new DataGridViewTextBoxColumn();
            desCol.DataPropertyName = "Desc";
            desCol.Name = "Desc";
            desCol.ReadOnly = true;
            jobsDataGridView.Columns.Add(desCol);

            DataGridViewColumn statusCol = new DataGridViewTextBoxColumn();
            statusCol.DataPropertyName = "JobStatus";
            statusCol.ReadOnly = true;
            statusCol.Name = "JobStatus";
            jobsDataGridView.Columns.Add(statusCol);

            DataGridViewTextBoxColumn timeCol = new DataGridViewTextBoxColumn();
            timeCol.DataPropertyName = "ScheduledTime";
            timeCol.Name = "Time";
            timeCol.ReadOnly = true;
            jobsDataGridView.Columns.Add(timeCol);

            DataGridViewButtonColumn confiCol = new DataGridViewButtonColumn();
            confiCol.Name = "Config";
            confiCol.Text = "Configure";
            confiCol.UseColumnTextForButtonValue = true;
            jobsDataGridView.Columns.Add(confiCol);

            DataGridViewCheckBoxColumn enueueCol = new DataGridViewCheckBoxColumn();
            enueueCol.DataPropertyName = "Enqueued";
            enueueCol.Name = "Enqueue";
            jobsDataGridView.Columns.Add(enueueCol);

            DataGridViewLinkColumn column6 = new DataGridViewLinkColumn();
            column6.DataPropertyName = "LogFile";
            column6.Name = "Log";
            column6.UseColumnTextForLinkValue = true;
            jobsDataGridView.Columns.Add(column6);

            foreach (IScheduledJob job in _scheduledJobs)
            {
                if (_scheduledJobAndTime.ContainsKey(job.JobId.ToString()))
                {
                    job.Enqueued = true;
                }
                else
                {
                    job.Enqueued = false;
                }

                ScheduledJobModel jobModel = new ScheduledJobModel(job);
                jobListBindingSrc.Add(jobModel);
            }
        }        

        private void jobsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            ScheduledJobModel scheduledJobModel = (ScheduledJobModel)jobsDataGridView.Rows[e.RowIndex].DataBoundItem;

            if (e.ColumnIndex == jobsDataGridView.Columns["Config"].Index && e.RowIndex >= 0)
            {
                scheduledJobModel.Job.SetJobConfiguration();

                //Update job scheduler service config.
                //Add the entry to service app.config file.
                string exeLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(string.Format(@"{0}\JobSchedulerService.exe", exeLocation));
                if (scheduledJobModel.Enqueued)
                {
                    config.AppSettings.Settings.Remove(scheduledJobModel.Job.JobId.ToString());//, scheduledJobModel.Job.ScheduledTime.ToShortTimeString());
                    config.AppSettings.Settings.Add(scheduledJobModel.Job.JobId.ToString(), scheduledJobModel.Job.ScheduledTime.ToShortTimeString());
                }
                else
                {
                    config.AppSettings.Settings.Remove(scheduledJobModel.Job.JobId.ToString());
                }
                config.Save(ConfigurationSaveMode.Modified);
            }
            if (e.ColumnIndex == jobsDataGridView.Columns["Log"].Index && e.RowIndex >= 0)
            {
                string filepath = (string)jobsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                if (File.Exists(filepath))
                {
                    System.Diagnostics.Process.Start(filepath);
                }
                else
                {                    
                    MessageBox.Show("Log file does not exist.");
                }
            }

            if (e.ColumnIndex == jobsDataGridView.Columns["Enqueue"].Index && e.RowIndex >= 0)
            {
                //TODO: Correct this one. Checkbox state is not available
                DataGridViewCheckBoxColumn col = jobsDataGridView.Columns["Enqueue"] as DataGridViewCheckBoxColumn;
                DataGridViewCheckBoxCell cell = jobsDataGridView.Rows[e.RowIndex].Cells["Enqueue"] as DataGridViewCheckBoxCell;

                scheduledJobModel.Enqueued = !((bool)cell.EditingCellFormattedValue);
                
                //Add the entry to service app.config file.
                string exeLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(string.Format(@"{0}\JobSchedulerService.exe", exeLocation));

                if (scheduledJobModel.Enqueued)
                {
                    config.AppSettings.Settings.Add(scheduledJobModel.Job.JobId.ToString(), scheduledJobModel.Job.ScheduledTime.ToShortTimeString());
                }
                else
                {
                    config.AppSettings.Settings.Remove(scheduledJobModel.Job.JobId.ToString());
                }
                config.Save(ConfigurationSaveMode.Modified);
            }

            jobsDataGridView.Refresh();

        }

        private void btnSMBDir_Click(object sender, EventArgs e)
        {
            mbapi.smartSelectSMBDirByGUI();
            var usr = mbapi.RequireSMBLogin();
            if (usr != null)
            {
                txtDataDir.Text = mbapi.smartGetSMBDir();
            }
        }

        #region Menu Items Event Handlers

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion       

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new About();
            frm.ShowDialog();
        }

        private void activateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var product_id = Env.GetConfigVar("product_id", 0, false);
            var product_version = Env.GetConfigVar("product_version", "0.0.0.0", false);

            var frm = new SysconCommon.Protection.ProtectionPlusOnlineActivationForm(product_id, product_version);
            frm.ShowDialog();
            this.OnLoad(null);
        }

        private void onlineHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://syscon-inc.com/product-support/2165/support.asp");
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //var frm = new Settings();
            //frm.ShowDialog();
        }

    }
}