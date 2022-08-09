// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    /// <summary>
    /// A helper class to check if TestEngine parameters are null.
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


        public static void NullCheck(RecordValue obj, NumberValue rowOrColumn, RecordValue childObj, ILogger logger)
        {
            bool encounteredError = false;
            logger.LogDebug("Checking SetProperty function for null arguments.");

            if (obj == null)
            {
                logger.LogError("SetProperty function cannot take in a null object.");
                encounteredError = true;
            }

            if (rowOrColumn == null)
            {
                logger.LogError("SetProperty function cannot take in a null property name.");
                encounteredError = true;
            }

            if (childObj == null)
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

        public static void NullCheck(RecordValue obj, NumberValue rowOrColumn, ILogger logger)
        {
            bool encounteredError = false;
            logger.LogDebug("Checking SetProperty function for null arguments.");

            if (obj == null)
            {
                logger.LogError("SetProperty function cannot take in a null object.");
                encounteredError = true;
            }

            if (rowOrColumn == null)
            {
                logger.LogError("SetProperty function cannot take in a null property name.");
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
