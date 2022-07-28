// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    /// <summary>
    /// A helper class to do time checking and throw exception when timeout.
    /// </summary>
    public class NullCheckHelper
    {
        public static void NullCheck(RecordValue obj, StringValue propName, FormulaValue value, ILogger logger)
        {
            bool encounteredError = false;
            logger.LogDebug("Checking SetProperty function for null arguments.");

            if (obj == null)
            {
                logger.LogError("SetProperty function cannot take in a null object.");
                encounteredError = true;
            }

            if (propName == null)
            {
                logger.LogError("SetProperty function cannot take in a null property name.");
                encounteredError = true;
            }

            if (value == null)
            {
                logger.LogError("SetProperty function cannot take in a null property value.");
                encounteredError = true;
            }

            if (encounteredError == true)
            {
                throw new ArgumentNullException();
            }
            else
            {
                logger.LogDebug("No null arguments detected.");
            }
        }
    }
}