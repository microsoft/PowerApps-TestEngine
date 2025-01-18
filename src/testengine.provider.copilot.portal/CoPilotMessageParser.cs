// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public class CoPilotMessageParser
{
    /// <summary>
    /// Convert observes messages into JSON messages for test interaction
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static List<string> ParseMessages(string lines)
    {
        var jsonList = new List<string>();

        using (var reader = new StringReader(lines))
        {
            while (reader.Peek()>=0)
            {
                string lineToParse = reader.ReadLine();
                var trimmedLine = lineToParse.Trim();

                // Remove "event:" or "data:" prefix from the line
                if (!string.IsNullOrEmpty(trimmedLine) && trimmedLine.StartsWith("event:"))
                {
                    trimmedLine = trimmedLine.Substring("event:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("data:"))
                {
                    trimmedLine = trimmedLine.Substring("data:".Length).Trim();
                }

                // Add lines if starts with '{'
                if (trimmedLine.StartsWith("{"))
                {
     
                    jsonList.Add(trimmedLine);
                }
            }
        }
        
        return jsonList;
    }
}
