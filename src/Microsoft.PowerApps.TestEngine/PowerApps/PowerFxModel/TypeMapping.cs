// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel
{
    /// <summary>
    /// Maps Power Apps internal types to Power FX types
    /// </summary>
    public class TypeMapping
    {
        private Dictionary<string, FormulaType> typeMappings = new Dictionary<string, FormulaType>();

        public TypeMapping()
        {
            // Default types
            typeMappings.Add("s", FormulaType.String);
            typeMappings.Add("b", FormulaType.Boolean);
            typeMappings.Add("d", FormulaType.DateTime);
            typeMappings.Add("D", FormulaType.Date);
            typeMappings.Add("h", FormulaType.Hyperlink);
            typeMappings.Add("c", FormulaType.Color);
            typeMappings.Add("n", FormulaType.Number);
            typeMappings.Add("Z", FormulaType.DateTimeNoTimeZone);
            typeMappings.Add("g", FormulaType.Guid);
        }

        /// <summary>
        /// Adds a new type to the mapping
        /// </summary>
        /// <param name="typeString">String representation of the type</param>
        /// <param name="formulaType">Power FX type</param>
        public void AddMapping(string typeString, FormulaType formulaType)
        {
            if (!typeMappings.ContainsKey(typeString))
            {
                typeMappings.Add(typeString, formulaType);
            }
        }

        private List<string> GetSubTypes(string typeString)
        {
            List<string> subTypes = new List<string>();

            // Extract the names of the types out of the string
            var regex = new Regex(@"(?<subType>\w+):\w");
            var matches = regex.Matches(typeString);
            foreach (Match match in matches)
            {
                var subType = match.Groups["subType"].Value;
                subTypes.Add(subType);
            }
            return subTypes;
        }

        private bool IsTable(string typeString)
        {
            return typeString.StartsWith("*");
        }

        private bool IsRecord(string typeString)
        {
            return typeString.StartsWith("!");
        }

        /// <summary>
        /// Tries to get the type from the string representation
        /// </summary>
        /// <param name="typeString">String representation of the type</param>
        /// <param name="formulaType">Power FX type</param>
        /// <returns>True if type was found</returns>
        public bool TryGetType(string typeString, out FormulaType formulaType)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                formulaType = null;
                return false;
            }

            var isTable = IsTable(typeString);
            var isRecord = IsRecord(typeString);

            if (isTable || isRecord)
            {
                var recordType = RecordType.Empty();

                // Either Table value - Example: *[Gallery2:v, Icon2:v, Label4:v]
                // Or Record value - Example: ![Gallery2:v, Icon2:v, Label4:v]
                var subTypes = GetSubTypes(typeString);

                foreach (var subType in subTypes)
                {
                    if (TryGetType(subType, out var subFormulaType))
                    {
                        recordType = recordType.Add(new NamedFormulaType(subType, subFormulaType));
                    }
                    else
                    {
                        formulaType = null;
                        return false;
                    }
                }

                if (isTable)
                {
                    formulaType = recordType.ToTable();
                    return true;
                }
                else
                {
                    formulaType = recordType;
                    return true;
                }
            }

            return typeMappings.TryGetValue(typeString, out formulaType);
        }
    }
}
