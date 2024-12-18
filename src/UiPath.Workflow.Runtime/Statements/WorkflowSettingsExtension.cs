using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Workflow.Runtime.Statements
{
    public class WorkflowSettingsExtension
    {
        /// <summary>
        /// When true, the delay activity will block the persistance idle event until the delay is over, 
        /// preventing the workflow from being persisted.
        /// </summary>
        public bool BlockingDelay { get; set; } = false;
    }
}
