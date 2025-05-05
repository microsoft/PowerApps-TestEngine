// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;

public interface IHttpRequest
{
    string HttpMethod { get; }
    Uri Url { get; }
    Stream InputStream { get; }
    Encoding ContentEncoding { get; }
    string ContentType { get; }
}
