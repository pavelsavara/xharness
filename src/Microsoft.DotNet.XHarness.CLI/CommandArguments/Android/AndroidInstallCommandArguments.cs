﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Options;

namespace Microsoft.DotNet.XHarness.CLI.CommandArguments.Android
{
    internal class AndroidInstallCommandArguments : TestCommandArguments
    {
        private string? _packageName;
        private readonly List<string> _deviceArchitecture = new();

        public string PackageName
        {
            get => _packageName ?? throw new ArgumentNullException("Package name not specified");
            set => _packageName = value;
        }

        /// <summary>
        /// If specified, attempt to run APK on that device.
        /// If there is more than one device with required architecture, failing to specify this may cause execution failure.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// If specified, attempt to run on a compatible attached device, failing if unavailable.
        /// If not specified, we will open the apk using Zip APIs and guess what's usable based off folders found in under /lib
        /// </summary>
        public IEnumerable<string> DeviceArchitecture => _deviceArchitecture;

        /// <summary>
        /// Time to wait for boot completion. Defaults to 5 minutes.
        /// </summary>
        public TimeSpan LaunchTimeout { get; set; } = TimeSpan.FromMinutes(5);

        protected override OptionSet GetTestCommandOptions() => new()
        {
            { "package-name=|p=", "Package name contained within the supplied APK",
                v => PackageName = v
            },
            {
                "device-id=", "Device where APK should be installed",
                v => DeviceId = v
            },
            {
                "device-arch=",
                "If specified, forces running on a device with given architecture (x86, x86_64, arm64-v8a or armeabi-v7a). Otherwise inferred from supplied APK. " +
                "Can be used more than once.",
                v => _deviceArchitecture.Add(v)
            },
            {
                "launch-timeout=|lt=", "Time span in the form of \"00:00:00\" or number of seconds to wait for the device to boot to complete",
                v =>
                {
                    if (int.TryParse(v, out var timeout))
                    {
                        LaunchTimeout = TimeSpan.FromSeconds(timeout);
                        return;
                    }

                    if (TimeSpan.TryParse(v, out var timespan))
                    {
                        LaunchTimeout = timespan;
                        return;
                    }

                    throw new ArgumentException("launch-timeout must be an integer - a number of seconds, or a timespan (00:30:00)");
                }
            },
        };

        public override void Validate()
        {
            base.Validate();

            foreach (var archName in _deviceArchitecture ?? throw new ArgumentException("architecture cannot be empty"))
            {
                try
                {
                    AndroidArchitectureHelper.ParseAsAndroidArchitecture(archName);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentException(
                        $"Failed to parse architecture '{archName}'. Available architectures are:" +
                        GetAllowedValues<AndroidArchitecture>(t => t.AsString()));
                }
            }

            // Validate this field
            _ = PackageName;
            _ = AppPackagePath;
        }
    }
}
