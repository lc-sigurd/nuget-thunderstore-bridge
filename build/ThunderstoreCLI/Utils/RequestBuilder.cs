/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Utils/RequestBuilder.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ThunderstoreCLI.Utils;

public class RequestBuilder
{
    private UriBuilder builder { get; } = new()
    {
        Scheme = "https"
    };
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public AuthenticationHeaderValue? AuthHeader { get; set; } = null;
    public HttpContent? Content { get; set; } = null;

    public RequestBuilder() { }

    public RequestBuilder(string host)
    {
        if (host.StartsWith("https://"))
            host = host[8..];
        builder.Host = host;
    }

    public RequestBuilder StartNew()
    {
        return new(builder.Uri.Host);
    }

    public HttpRequestMessage GetRequest()
    {
        var req = new HttpRequestMessage(Method, builder.Uri)
        {
            Content = Content
        };

        req.Headers.Authorization = AuthHeader;

        return req;
    }

    public RequestBuilder WithEndpoint(string endpoint)
    {
        if (!endpoint.EndsWith('/'))
        {
            endpoint += '/';
        }
        builder.Path = endpoint;
        return this;
    }

    public RequestBuilder WithAuth(AuthenticationHeaderValue auth)
    {
        AuthHeader = auth;
        return this;
    }

    public RequestBuilder WithContent(HttpContent content)
    {
        Content = content;
        return this;
    }

    public RequestBuilder WithMethod(HttpMethod method)
    {
        Method = method;
        return this;
    }
}
