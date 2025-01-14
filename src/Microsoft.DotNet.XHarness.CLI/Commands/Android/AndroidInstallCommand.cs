﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.Android;
using Microsoft.DotNet.XHarness.CLI.CommandArguments.Android;
using Microsoft.DotNet.XHarness.Common.CLI;
using Microsoft.DotNet.XHarness.Common.CLI.CommandArguments;
using Microsoft.DotNet.XHarness.Common.CLI.Commands;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.XHarness.CLI.Commands.Android
{
    internal class AndroidInstallCommand : XHarnessCommand
    {

        private readonly AndroidInstallCommandArguments _arguments = new();

        protected override XHarnessCommandArguments Arguments => _arguments;

        protected override string CommandUsage { get; } = "android install --package-name=... --app=... [OPTIONS]";

        private const string CommandHelp = "Install an .apk on an Android device without running it";
        protected override string CommandDescription { get; } = @$"
{CommandHelp}
 
Arguments:
";

        public AndroidInstallCommand() : base("install", false, CommandHelp)
        {
        }

        protected override Task<ExitCode> InvokeInternal(ILogger logger)
        {
            logger.LogDebug($"Android Install command called: App = {_arguments.AppPackagePath}");
            logger.LogDebug($"Timeout = {_arguments.Timeout.TotalSeconds} seconds.");

            if (!File.Exists(_arguments.AppPackagePath))
            {
                logger.LogCritical($"Couldn't find {_arguments.AppPackagePath}!");
                return Task.FromResult(ExitCode.PACKAGE_NOT_FOUND);
            }

            var runner = new AdbRunner(logger);

            List<string> apkRequiredArchitecture = new();

            if (string.IsNullOrEmpty(_arguments.DeviceId))
            {
                // trying to choose suitable device
                if (_arguments.DeviceArchitecture.Any())
                {
                    apkRequiredArchitecture = _arguments.DeviceArchitecture.ToList();
                    logger.LogInformation($"Will attempt to run device on specified architecture: '{string.Join("', '", apkRequiredArchitecture)}'");
                }
                else
                {
                    apkRequiredArchitecture = ApkHelper.GetApkSupportedArchitectures(_arguments.AppPackagePath);
                    logger.LogInformation($"Will attempt to run device on detected architecture: '{string.Join("', '", apkRequiredArchitecture)}'");
                }
            }

            // Package Name is not guaranteed to match file name, so it needs to be mandatory.
            return Task.FromResult(InvokeHelper(
                logger: logger,
                apkPackageName: _arguments.PackageName,
                appPackagePath: _arguments.AppPackagePath,
                apkRequiredArchitecture: apkRequiredArchitecture,
                deviceId: _arguments.DeviceId,
                bootTimeoutSeconds: _arguments.LaunchTimeout,
                runner: runner));
        }

        public static ExitCode InvokeHelper(ILogger logger, string apkPackageName, string appPackagePath, IEnumerable<string> apkRequiredArchitecture, string? deviceId, TimeSpan bootTimeoutSeconds, AdbRunner runner)
        {
            try
            {
                using (logger.BeginScope("Initialization and setup of APK on device"))
                {
                    // Make sure the adb server is started
                    runner.StartAdbServer();

                    // if call via install command device id must be set
                    // otherwise - from test command - apkRequiredArchitecture was set by user or .apk architecture
                    deviceId ??= apkRequiredArchitecture.Any()
                        ? runner.GetDeviceToUse(logger, apkRequiredArchitecture, "architecture")
                        : throw new ArgumentException("Required architecture not specified");

                    if (deviceId == null)
                    {
                        throw new Exception($"Failed to find compatible device: {string.Join(", ", apkRequiredArchitecture)}");
                    }

                    runner.SetActiveDevice(deviceId);

                    runner.TimeToWaitForBootCompletion = bootTimeoutSeconds;

                    // Wait till at least device(s) are ready
                    runner.WaitForDevice();

                    logger.LogDebug($"Working with {runner.GetAdbVersion()}");

                    // If anything changed about the app, Install will fail; uninstall it first.
                    // (we'll ignore if it's not present)
                    // This is where mismatched architecture APKs fail.
                    runner.UninstallApk(apkPackageName);
                    if (runner.InstallApk(appPackagePath) != 0)
                    {
                        logger.LogCritical("Install failure: Test command cannot continue");
                        runner.UninstallApk(apkPackageName);
                        return ExitCode.PACKAGE_INSTALLATION_FAILURE;
                    }
                    runner.KillApk(apkPackageName);
                }
                return ExitCode.SUCCESS;
            }
            catch (Exception toLog)
            {
                throw new Exception($"Failed to run test package: {toLog.Message}");
            }
        }
    }
}
