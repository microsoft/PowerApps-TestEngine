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
            logger.LogDebug("Checking SetProperty function for null arguments.");

            if (obj == null)
            {
                logger.LogError("SetProperty function cannot take in a null object.");
            }

            if (propName == null)
            {
                logger.LogError("SetProperty function cannot take in a null property name.");
            }

            if (value == null)
            {
                logger.LogError("SetProperty function cannot take in a null property value.");
            }

            logger.LogDebug("No null arguments detected.");
        }


        public static void NullCheck(RecordValue obj, NumberValue rowOrColumn, RecordValue childObj, ILogger logger)
        {
            logger.LogDebug("Checking SetProperty function for null arguments.");

            if (obj == null)
            {
                logger.LogError("SetProperty function cannot take in a null object.");
            }

            if (rowOrColumn == null)
            {
                logger.LogError("SetProperty function cannot take in a null property name.");
            }

            if (childObj == null)
            {
                logger.LogError("SetProperty function cannot take in a null property value.");
            }
            
            logger.LogDebug("No null arguments detected.");
        }

        public static void NullCheck(RecordValue obj, NumberValue rowOrColumn, ILogger logger)
        {
            logger.LogDebug("Checking SetProperty function for null arguments.");

            if (obj == null)
            {
                logger.LogError("SetProperty function cannot take in a null object.");
            }

            if (rowOrColumn == null)
            {
                logger.LogError("SetProperty function cannot take in a null property name.");
            }

            logger.LogDebug("No null arguments detected.");
        }
    }
}