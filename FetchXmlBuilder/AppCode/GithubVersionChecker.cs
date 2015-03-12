﻿using MarkdownSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Cinteros.Xrm.FetchXmlBuilder.AppCode
{
    internal class GithubVersionChecker
    {
        private static int currentMajorVersion;
        private static int currentMinorVersion;
        private static int currentBuildVersion;
        private static int currentRevisionVersion;
        public GithubVersionChecker(string currentVersion)
        {
            var versionParts = currentVersion.Split('.');
            currentMajorVersion = int.Parse(versionParts[0]);
            currentMinorVersion = int.Parse(versionParts[1]);
            currentBuildVersion = int.Parse(versionParts[2]);
            currentRevisionVersion = int.Parse(versionParts[3]); 
        }

        public static GithubInformation Cpi { get; set; }

        public void Run()
        {
            RunAsync().Wait();
        }

        static async Task RunAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://api.github.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("User-Agent", "Cinteros");

                    HttpResponseMessage response =
                        await
                            client.GetAsync("repos/Cinteros/FetchXMLBuilder/releases")
                                .ConfigureAwait(continueOnCapturedContext: false);
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        var data = response.Content.ReadAsStringAsync();
                        var jSserializer = new JavaScriptSerializer();
                        var releases = jSserializer.Deserialize<List<RootObject>>(data.Result);

                        var lastRelease = releases.OrderByDescending(r => r.created_at).FirstOrDefault();
                        if (lastRelease != null)
                        {
                            var version = lastRelease.tag_name.Replace("v.", "");
                            var versionParts = version.Split('.');
                            int majorVersion = int.Parse(versionParts[0]);
                            int minorVersion = int.Parse(versionParts[1]);
                            int buildVersion = int.Parse(versionParts[2]);
                            int revisionVersion = int.Parse(versionParts[3]);

                            if (currentMajorVersion < majorVersion
                                || currentMajorVersion == majorVersion && currentMinorVersion < minorVersion
                                ||
                                currentMajorVersion == majorVersion && currentMinorVersion == minorVersion &&
                                currentBuildVersion < buildVersion
                                ||
                                currentMajorVersion == majorVersion && currentMinorVersion == minorVersion &&
                                currentBuildVersion == buildVersion && currentRevisionVersion < revisionVersion)
                            {
                                var mdth = new Markdown();
                                var html = mdth.Transform(lastRelease.body).Replace("h1", "div");

                                Cpi = new GithubInformation
                                {
                                    Description = html,
                                    Version = version
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // Do nothing as we don't want to throw exception if something goes wrong with checking update
            }
        }
    }
}
