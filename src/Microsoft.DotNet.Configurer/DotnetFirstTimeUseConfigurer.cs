﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Configurer
{
    public class DotnetFirstTimeUseConfigurer
    {
        private IReporter _reporter;
        private DotnetFirstRunConfiguration _dotnetFirstRunConfiguration;
        private INuGetCachePrimer _nugetCachePrimer;
        private INuGetCacheSentinel _nugetCacheSentinel;
        private IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
        private IAspNetCertificateSentinel _aspNetCertificateSentinel;
        private IAspNetCoreCertificateGenerator _aspNetCoreCertificateGenerator;
        private IFileSentinel _toolPathSentinel;
        private string _cliFallbackFolderPath;
        private readonly IEnvironmentPath _pathAdder;

        public DotnetFirstTimeUseConfigurer(
            INuGetCachePrimer nugetCachePrimer,
            INuGetCacheSentinel nugetCacheSentinel,
            IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
            IAspNetCertificateSentinel aspNetCertificateSentinel,
            IAspNetCoreCertificateGenerator aspNetCoreCertificateGenerator,
            IFileSentinel toolPathSentinel,
            DotnetFirstRunConfiguration dotnetFirstRunConfiguration,
            IReporter reporter,
            string cliFallbackFolderPath,
            IEnvironmentPath pathAdder)
        {
            _nugetCachePrimer = nugetCachePrimer;
            _nugetCacheSentinel = nugetCacheSentinel;
            _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
            _aspNetCertificateSentinel = aspNetCertificateSentinel;
            _aspNetCoreCertificateGenerator = aspNetCoreCertificateGenerator;
            _toolPathSentinel = toolPathSentinel;
            _dotnetFirstRunConfiguration = dotnetFirstRunConfiguration;
            _reporter = reporter;
            _cliFallbackFolderPath = cliFallbackFolderPath;
            _pathAdder = pathAdder ?? throw new ArgumentNullException(nameof(pathAdder));
        }

        public void Configure()
        {
            if (ShouldAddPackageExecutablePath())
            {
                AddPackageExecutablePath();
            }

            if (ShouldPrintFirstTimeUseNotice())
            {
                PrintFirstTimeUseNotice();
            }

            if (ShouldPrimeNugetCache())
            {
                if (_nugetCacheSentinel.UnauthorizedAccess)
                {
                    PrintUnauthorizedAccessMessage();
                }
                else
                {
                    PrintNugetCachePrimeMessage();

                    _nugetCachePrimer.PrimeCache();
                }
            }

            if (ShouldGenerateAspNetCertificate())
            {
                GenerateAspNetCertificate();
            }
        }

        private void GenerateAspNetCertificate()
        {
            _aspNetCoreCertificateGenerator.GenerateAspNetCoreDevelopmentCertificate();

            _reporter.WriteLine();
            _reporter.WriteLine(LocalizableStrings.AspNetCertificateInstalled);

            _aspNetCertificateSentinel.CreateIfNotExists();
        }

        private bool ShouldGenerateAspNetCertificate()
        {
#if EXCLUDE_ASPNETCORE
            return false;
#else
            return ShouldRunFirstRunExperience() &&
                _dotnetFirstRunConfiguration.GenerateAspNetCertificate &&
                !_aspNetCertificateSentinel.Exists();
#endif
        }

        private bool ShouldAddPackageExecutablePath()
        {
            return ShouldRunFirstRunExperience() && !_toolPathSentinel.Exists();
        }

        private void AddPackageExecutablePath()
        {
            _pathAdder.AddPackageExecutablePathToUserPath();

            _toolPathSentinel.Create();
        }

        private bool ShouldPrintFirstTimeUseNotice()
        {
            return ShouldRunFirstRunExperience() &&
                _dotnetFirstRunConfiguration.PrintTelemetryMessage &&
                !_firstTimeUseNoticeSentinel.Exists();
        }

        private void PrintFirstTimeUseNotice()
        {
            _reporter.WriteLine();
            _reporter.WriteLine(LocalizableStrings.FirstTimeWelcomeMessage);

            _firstTimeUseNoticeSentinel.CreateIfNotExists();
        }

        private void PrintUnauthorizedAccessMessage()
        {
            _reporter.WriteLine();
            _reporter.WriteLine(string.Format(
                LocalizableStrings.UnauthorizedAccessMessage,
                _cliFallbackFolderPath));
        }

        private bool ShouldPrimeNugetCache()
        {
            return ShouldRunFirstRunExperience() &&
                !_nugetCacheSentinel.Exists() &&
                !_nugetCacheSentinel.InProgressSentinelAlreadyExists() &&
                !_nugetCachePrimer.SkipPrimingTheCache();
        }

        private void PrintNugetCachePrimeMessage()
        {
            string cachePrimeMessage = LocalizableStrings.NugetCachePrimeMessage;
            _reporter.WriteLine();
            _reporter.WriteLine(cachePrimeMessage);
        }

        private bool ShouldRunFirstRunExperience()
        {
            return !_dotnetFirstRunConfiguration.SkipFirstRunExperience;
        }
    }
}
