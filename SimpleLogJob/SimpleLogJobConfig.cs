﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

using Syscon.ScheduledJob;

namespace Syscon.ScheduledJob.SimpleLogJob
{
    /// <summary>
    /// Job configuration settings for the Log job
    /// </summary>
    public class SimpleLogJobConfig : ScheduledJobConfig
    {

        /// <summary>
        /// Default Ctor
        /// </summary>
        public SimpleLogJobConfig()
            :base()
        {

        }

        #region IScheduleJobConfig Members

        /// <summary>
        /// Load config
        /// </summary>
        public override void LoadConfig()
        {
            XmlSerializer _xmlSerializer = new XmlSerializer(this.GetType());

            try
            {
                string configFile = string.Format(@"{0}\{1}.xml", AssemblyPath, AssemblyName);

                if (File.Exists(configFile))
                {
                    using (FileStream stream = new FileInfo(configFile).OpenRead())
                    {
                        var dsObj = _xmlSerializer.Deserialize(stream);
                        SimpleLogJobConfig config   = dsObj as SimpleLogJobConfig;
                        SMBDir                      = config.SMBDir;
                        ScheduledTime               = config.ScheduledTime;
                        LogFilePath                 = config.LogFilePath;
                        UserId                      = config.UserId;
                        Password                    = config.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                //Log exception
            }
            finally
            {
                _xmlSerializer = null;
            }
        }

        #endregion

    }
}
