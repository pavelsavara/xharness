﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.Apple;
using Microsoft.DotNet.XHarness.CLI.CommandArguments.Apple;
using Microsoft.DotNet.XHarness.Common.CLI;
using Microsoft.DotNet.XHarness.Common.Logging;
using Microsoft.DotNet.XHarness.iOS.Shared;
using Microsoft.DotNet.XHarness.iOS.Shared.Execution;
using Microsoft.DotNet.XHarness.iOS.Shared.Logging;
using Microsoft.DotNet.XHarness.iOS.Shared.Utilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.XHarness.CLI.Commands.Apple
{
    internal class AppleUninstallCommand : AppleAppCommand<AppleUninstallCommandArguments>
    {
        private const string CommandHelp = "Uninstalls a given iOS/tvOS/watchOS/MacCatalyst application bundle from a target device/simulator";

        protected override AppleUninstallCommandArguments AppleAppArguments { get; } = new();
        protected override string CommandUsage { get; } = "apple uninstall --app=... --output-directory=... --targets=... [OPTIONS] [-- [RUNTIME ARGUMENTS]]";
        protected override string CommandDescription { get; } = CommandHelp;

        public AppleUninstallCommand() : base("uninstall", false, CommandHelp)
        {
        }

        protected override Task<ExitCode> InvokeInternal(
            IMlaunchProcessManager processManager,
            IAppBundleInformationParser appBundleInformationParser,
            DeviceFinder deviceFinder,
            Extensions.Logging.ILogger logger,
            TestTargetOs target,
            ILogs logs,
            IFileBackedLog mainLog,
            CancellationToken cancellationToken)
        {
            if (target.Platform.IsSimulator())
            {
                logger.LogWarning($"XHarness cannot uninstall application from a simulator");
                return Task.FromResult(ExitCode.SUCCESS);
            }

            var args = AppleAppArguments;

            var orchestrator = new UninstallOrchestrator(
                processManager,
                deviceFinder,
                new ConsoleLogger(logger),
                logs,
                mainLog,
                ErrorKnowledgeBase,
                new Helpers());

            return orchestrator.OrchestrateAppUninstall(
                args.BundleIdentifier,
                target,
                args.DeviceName,
                args.Timeout,
                args.ResetSimulator,
                args.EnableLldb,
                cancellationToken);
        }
    }
}
