// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public interface IHttpContext
{
    IHttpRequest Request { get; }
    IHttpResponse Response { get; }
}
