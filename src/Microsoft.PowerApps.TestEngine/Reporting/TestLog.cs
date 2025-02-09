// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    public class TestLog
    {
        private Func<DateTime> _timeStamper = () => DateTime.Now;
        public Func<DateTime> TimeStamper
        {
            get
            {
                return _timeStamper;
            }

            set
            {
                _timeStamper = value;
                When = _timeStamper();
            }
        }

        public TestLog()
        {
            When = TimeStamper();
        }

        public DateTime When { get; private set; }

        public string ScopeFilter { get; set; }
        public string LogMessage { get; set; }
    }
}
