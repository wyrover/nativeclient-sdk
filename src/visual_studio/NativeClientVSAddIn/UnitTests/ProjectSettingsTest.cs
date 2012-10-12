﻿// Copyright (c) 2012 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

namespace UnitTests
{
  using System;

  using EnvDTE;
  using EnvDTE80;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using Microsoft.VisualStudio.VCProjectEngine;

  /// <summary>
  /// This test class contains tests related to the custom project settings
  /// and property pages for PPAPI and NaCl configurations.
  /// </summary>
  [TestClass]
  public class ProjectSettingsTest
  {
    /// <summary>
    /// The below string holds the path to a NaCl solution used in some tests.
    /// A NaCl solution is a valid nacl/pepper plug-in VS solution.
    /// It is copied into the testing deployment directory and opened in some tests.
    /// In this solution, NaCl and pepper settings are copied from 'Empty' initial settings.
    /// </summary>
    private static string naclSolutionEmptyInitialization;

    /// <summary>
    /// The main visual studio object.
    /// </summary>
    private DTE2 dte_;

    /// <summary>
    /// The project configuration for debug settings of a test's platform.
    /// </summary>
    private VCConfiguration debug_;

    /// <summary>
    /// The project configuration for release settings of a test's platform
    /// </summary>
    private VCConfiguration release_;

    /// <summary>
    /// Gets or sets the test context which provides information about,
    /// and functionality for the current test run.
    /// </summary>
    public TestContext TestContext { get; set; }

    /// <summary>
    /// This is run one time before any test methods are called. Here we set-up test-copies of
    /// new NaCl solutions for use in the tests.
    /// </summary>
    /// <param name="testContext">Holds information about the current test run</param>
    [ClassInitialize]
    public static void ClassSetup(TestContext testContext)
    {
      DTE2 dte = TestUtilities.StartVisualStudioInstance();
      try
      {
        naclSolutionEmptyInitialization = TestUtilities.CreateBlankValidNaClSolution(
            dte,
            "ProjectSettingsTestEmptyInit",
            NativeClientVSAddIn.Strings.PepperPlatformName,
            NativeClientVSAddIn.Strings.NaCl64PlatformName,
            testContext);
      }
      finally
      {
        TestUtilities.CleanUpVisualStudioInstance(dte);
      }
    }

    /// <summary>
    /// This is run before each test to create test resources.
    /// </summary>
    [TestInitialize]
    public void TestSetup()
    {
      dte_ = TestUtilities.StartVisualStudioInstance();
      try
      {
        TestUtilities.AssertAddinLoaded(dte_, NativeClientVSAddIn.Strings.AddInName);
      }
      catch
      {
        TestUtilities.CleanUpVisualStudioInstance(dte_);
        throw;
      }
    }

    /// <summary>
    /// This is run after each test to clean up things created in TestSetup().
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
      TestUtilities.CleanUpVisualStudioInstance(dte_);
    }

    /// <summary>
    /// Test method to check that the NaCl platform compiles a test project.
    /// </summary>
    [TestMethod]
    public void CheckNaClCompile()
    {
      string naclPlatform = NativeClientVSAddIn.Strings.NaCl64PlatformName;
      TryCompile(naclSolutionEmptyInitialization, "Debug", naclPlatform);
      TryCompile(naclSolutionEmptyInitialization, "Release", naclPlatform);
    }

    /// <summary>
    /// Test method to check that the Pepper platform compiles a test project.
    /// </summary>
    [TestMethod]
    public void CheckPepperCompile()
    {
      string pepperPlatform = NativeClientVSAddIn.Strings.PepperPlatformName;
      TryCompile(naclSolutionEmptyInitialization, "Debug", pepperPlatform);
      TryCompile(naclSolutionEmptyInitialization, "Release", pepperPlatform);
    }

    /// <summary>
    /// Test method which verifies that NaCl and pepper platforms have correct default properties
    /// when initialized from the Win32 platform.
    /// </summary>
    [TestMethod]
    public void VerifySettingsWin32Initialization()
    {
      string naclSolutionWin32Initialization = TestUtilities.CreateBlankValidNaClSolution(
            dte_, "ProjectSettingsTestWin32Init", "Win32", "Win32", TestContext);
      VerifyDefaultPepperSettings(naclSolutionWin32Initialization);
      VerifyDefaultNaClSettings(naclSolutionWin32Initialization);

      // Win32 inherit specific checks on the Pepper platform.
      OpenSolutionAndGetProperties(
          naclSolutionWin32Initialization, NativeClientVSAddIn.Strings.PepperPlatformName);

      // When inheriting from Win32 preprocessor the PPAPI preprocessor definition gets grouped
      // into the inherited %(PreprocessorDefinitions) variable. It still exists but doesn't appear
      // in the property page, thus we can't check for it here.

      //// TODO(tysand): When inheriting from Win32 this won't set and the Win32 platform
      //// incorrectly(?) sets this to true in the Release config even for regular win32 projects.
      ////TestUtilities.AssertPropertyEquals(
      ////    release_, "Link", "GenerateDebugInformation", "false", false);
      dte_.Solution.Close();

      // NaCl inherit specific checks on the NaCl platform.
      OpenSolutionAndGetProperties(
          naclSolutionWin32Initialization, NativeClientVSAddIn.Strings.NaCl64PlatformName);
      AllConfigsAssertPropertyEquals("ConfigurationGeneral", "TargetExt", ".so", true);
      AllConfigsAssertPropertyEquals(
          "ConfigurationGeneral", "ConfigurationType", "DynamicLibrary", true);
      dte_.Solution.Close();
    }

    /// <summary>
    /// Test method which verifies that the NaCl platform has correct default properties
    /// when initialized from the Pepper platform. And that the pepper platform has the correct
    /// settings when initialized from the 'empty' settings.
    /// </summary>
    [TestMethod]
    public void VerifySettingsPepperInitialization()
    {
      string naclSolutionPepperInitialization = TestUtilities.CreateBlankValidNaClSolution(
          dte_,
          "ProjectSettingsTestPepperInit",
          NativeClientVSAddIn.Strings.PepperPlatformName,
          NativeClientVSAddIn.Strings.PepperPlatformName,
          TestContext);
      VerifyDefaultPepperSettings(naclSolutionPepperInitialization);
      VerifyDefaultNaClSettings(naclSolutionPepperInitialization);

      // Pepper inherit specific checks on the Pepper platform.
      OpenSolutionAndGetProperties(
          naclSolutionPepperInitialization, NativeClientVSAddIn.Strings.PepperPlatformName);
      AllConfigsAssertPropertyContains("CL", "PreprocessorDefinitions", "PPAPI", false);
      TestUtilities.AssertPropertyEquals(
          release_, "Link", "GenerateDebugInformation", "false", false);
      dte_.Solution.Close();

      // NaCl inherit specific checks on the NaCl platform.
      OpenSolutionAndGetProperties(
          naclSolutionPepperInitialization, NativeClientVSAddIn.Strings.NaCl64PlatformName);
      AllConfigsAssertPropertyEquals("ConfigurationGeneral", "TargetExt", ".nexe", true);
      AllConfigsAssertPropertyEquals(
          "ConfigurationGeneral", "ConfigurationType", "Application", true);
      dte_.Solution.Close();
    }

    /// <summary>
    /// Test method which verifies that the Pepper platform has correct default properties
    /// when initialized from the NaCl platform. And that the NaCl platform has the correct
    /// settings when initialized from the 'empty' settings.
    /// </summary>
    [TestMethod]
    public void VerifySettingsNaClInitialization()
    {
      string naclSolutionNaClInitialization = TestUtilities.CreateBlankValidNaClSolution(
          dte_,
          "ProjectSettingsTestNaClInit",
          NativeClientVSAddIn.Strings.NaCl64PlatformName,
          NativeClientVSAddIn.Strings.NaCl64PlatformName,
          TestContext);
      VerifyDefaultPepperSettings(naclSolutionNaClInitialization);
      VerifyDefaultNaClSettings(naclSolutionNaClInitialization);

      // NaCl inherit specific checks on the Pepper platform.
      OpenSolutionAndGetProperties(
          naclSolutionNaClInitialization, NativeClientVSAddIn.Strings.PepperPlatformName);
      AllConfigsAssertPropertyContains("CL", "PreprocessorDefinitions", "PPAPI", false);
      TestUtilities.AssertPropertyEquals(
          release_, "Link", "GenerateDebugInformation", "false", false);
      dte_.Solution.Close();

      // NaCl inherit specific checks on the NaCl platform.
      OpenSolutionAndGetProperties(
          naclSolutionNaClInitialization, NativeClientVSAddIn.Strings.NaCl64PlatformName);
      AllConfigsAssertPropertyEquals("ConfigurationGeneral", "TargetExt", ".nexe", true);
      AllConfigsAssertPropertyEquals(
          "ConfigurationGeneral", "ConfigurationType", "Application", true);
      dte_.Solution.Close();
    }

    /// <summary>
    /// Method to run a battery of tests on a particular solution.  Checks that all Pepper platform
    /// settings are set correctly.
    /// </summary>
    /// <param name="naclSolution">Path to the solution file to verify.</param>
    private void VerifyDefaultPepperSettings(string naclSolution)
    {
      OpenSolutionAndGetProperties(naclSolution, NativeClientVSAddIn.Strings.PepperPlatformName);

      string page;

      // General
      page = "ConfigurationGeneral";
      AllConfigsAssertPropertyEquals(page, "OutDir", @"$(ProjectDir)win\", true);
      AllConfigsAssertPropertyEquals(page, "IntDir", @"win\$(Configuration)\", true);
      AllConfigsAssertPropertyEquals(page, "TargetExt", ".dll", true);
      AllConfigsAssertPropertyEquals(page, "ConfigurationType", "DynamicLibrary", true);
      AllConfigsAssertPropertyEquals(page, "VSNaClSDKRoot", @"$(NACL_SDK_ROOT)\", false);
      AllConfigsAssertPropertyEquals(page, "NaClWebServerPort", "5103", false);
      AllConfigsAssertPropertyEquals(page, "CharacterSet", "Unicode", false);
      AllConfigsAssertPropertyIsNotNullOrEmpty(page, "NaClAddInVersion");

      // Debugging
      page = "WindowsLocalDebugger";
      string chromePath = System.Environment.GetEnvironmentVariable(
          NativeClientVSAddIn.Strings.ChromePathEnvironmentVariable);
      AllConfigsAssertPropertyEquals(
          page, "LocalDebuggerCommand", chromePath, true);

      string propName = "LocalDebuggerCommandArguments";
      string dataDir = "--user-data-dir=\"$(ProjectDir)/chrome_data\"";
      string address = "localhost:$(NaClWebServerPort)";
      string naclFlag = "--enable-nacl";
      string targetFlag = "--register-pepper-plugins=\"$(TargetPath)\";application/x-nacl";
      string noSandBoxFlag = "--no-sandbox";
      string debugChildrenFlag = "--wait-for-debugger-children";
      AllConfigsAssertPropertyContains(page, propName, dataDir, true);
      AllConfigsAssertPropertyContains(page, propName, address, true);
      AllConfigsAssertPropertyContains(page, propName, naclFlag, true);
      AllConfigsAssertPropertyContains(page, propName, targetFlag, true);
      TestUtilities.AssertPropertyContains(debug_, page, propName, debugChildrenFlag, true);
      TestUtilities.AssertPropertyContains(debug_, page, propName, noSandBoxFlag, true);

      // VC++ Directories
      page = "ConfigurationDirectories";
      AllConfigsAssertPropertyContains(page, "IncludePath", @"$(VSNaClSDKRoot)include;", true);
      AllConfigsAssertPropertyContains(page, "IncludePath", @"$(VSNaClSDKRoot)include\win;", true);
      AllConfigsAssertPropertyContains(page, "IncludePath", @"$(VCInstallDir)include", true);
      AllConfigsAssertPropertyContains(
          page, "LibraryPath", @"$(VSNaClSDKRoot)lib\win_x86_32_host\$(Configuration);", true);
      AllConfigsAssertPropertyContains(page, "LibraryPath", @"$(VCInstallDir)lib", true);

      // C/C++ Code Generation
      page = "CL";
      TestUtilities.AssertPropertyEquals(release_, page, "RuntimeLibrary", "MultiThreaded", false);
      TestUtilities.AssertPropertyEquals(
          debug_, page, "RuntimeLibrary", "MultiThreadedDebug", false);
      TestUtilities.AssertPropertyEquals(release_, page, "BasicRuntimeChecks", "Default", false);
      TestUtilities.AssertPropertyEquals(
          debug_, page, "BasicRuntimeChecks", "EnableFastChecks", false);
      TestUtilities.AssertPropertyEquals(release_, page, "MinimalRebuild", "false", false);
      TestUtilities.AssertPropertyEquals(debug_, page, "MinimalRebuild", "true", false);
      TestUtilities.AssertPropertyEquals(
          release_, page, "DebugInformationFormat", "ProgramDatabase", false);
      TestUtilities.AssertPropertyEquals(
          debug_, page, "DebugInformationFormat", "EditAndContinue", false);

      // C/C++ Optimization
      TestUtilities.AssertPropertyEquals(debug_, page, "Optimization", "Disabled", false);
      TestUtilities.AssertPropertyEquals(release_, page, "Optimization", "MaxSpeed", false);

      // Linker Input
      page = "Link";
      AllConfigsAssertPropertyContains(page, "AdditionalDependencies", "ppapi_cpp.lib", true);
      AllConfigsAssertPropertyContains(page, "AdditionalDependencies", "ppapi.lib", true);

      // Note: Release check of this property is specific to the platform settings were inherited
      // from. Checks on release are done in the specific methods testing each inherit type.
      TestUtilities.AssertPropertyEquals(debug_, page, "GenerateDebugInformation", "true", false);

      TestUtilities.AssertPropertyEquals(release_, page, "LinkIncremental", "false", false);
      TestUtilities.AssertPropertyEquals(debug_, page, "LinkIncremental", "true", false);

      AllConfigsAssertPropertyEquals(page, "SubSystem", "Windows", false);

      dte_.Solution.Close();
    }

    /// <summary>
    /// Method to run a battery of tests on a particular solution.  Checks that all NaCl platform
    /// settings are set correctly.
    /// </summary>
    /// <param name="naclSolution">Path to the solution file to verify.</param>
    private void VerifyDefaultNaClSettings(string naclSolution)
    {
      OpenSolutionAndGetProperties(naclSolution, NativeClientVSAddIn.Strings.NaCl64PlatformName);

      string page;

      // General
      page = "ConfigurationGeneral";
      AllConfigsAssertPropertyEquals(page, "OutDir", @"$(ProjectDir)$(Platform)\$(ToolchainName)\$(Configuration)\", true);
      AllConfigsAssertPropertyEquals(
          page, "IntDir", @"$(Platform)\$(ToolchainName)\$(Configuration)\", true);
      AllConfigsAssertPropertyEquals(page, "ToolchainName", "newlib", true);
      AllConfigsAssertPropertyEquals(page, "TargetArchitecture", "x86_64", true);
      AllConfigsAssertPropertyEquals(page, "VSNaClSDKRoot", @"$(NACL_SDK_ROOT)\", false);
      AllConfigsAssertPropertyEquals(page, "NaClManifestPath", string.Empty, false);
      AllConfigsAssertPropertyEquals(page, "NaClWebServerPort", "5103", false);
      AllConfigsAssertPropertyIsNotNullOrEmpty(page, "NaClAddInVersion");

      // Debugging
      page = "WindowsLocalDebugger";
      string chromePath = System.Environment.GetEnvironmentVariable(
          NativeClientVSAddIn.Strings.ChromePathEnvironmentVariable);
      AllConfigsAssertPropertyEquals(
          page, "LocalDebuggerCommand", chromePath, true);

      string propName = "LocalDebuggerCommandArguments";
      string dataDir = "--user-data-dir=\"$(ProjectDir)/chrome_data\"";
      string address = "localhost:$(NaClWebServerPort)";
      string naclFlag = "--enable-nacl";
      string naclDebugFlag = "--enable-nacl-debug";
      string noSandBoxFlag = "--no-sandbox";
      TestUtilities.AssertPropertyContains(debug_, page, propName, dataDir, true);
      TestUtilities.AssertPropertyContains(debug_, page, propName, address, true);
      TestUtilities.AssertPropertyContains(debug_, page, propName, naclFlag, true);
      TestUtilities.AssertPropertyContains(debug_, page, propName, naclDebugFlag, true);
      TestUtilities.AssertPropertyContains(debug_, page, propName, noSandBoxFlag, true);
      TestUtilities.AssertPropertyContains(release_, page, propName, dataDir, true);
      TestUtilities.AssertPropertyContains(release_, page, propName, address, true);
      TestUtilities.AssertPropertyContains(release_, page, propName, naclFlag, true);

      // VC++ Directories
      page = "ConfigurationDirectories";
      AllConfigsAssertPropertyContains(page, "IncludePath", @"$(VSNaClSDKRoot)include;", true);
      AllConfigsAssertPropertyContains(page, "LibraryPath", @"$(VSNaClSDKRoot)lib;", true);

      // C/C++ General
      page = "CL";
      TestUtilities.AssertPropertyEquals(
          debug_, page, "GenerateDebuggingInformation", "true", false);
      TestUtilities.AssertPropertyEquals(
          release_, page, "GenerateDebuggingInformation", "false", false);

      AllConfigsAssertPropertyEquals(page, "Warnings", "NormalWarnings", true);
      AllConfigsAssertPropertyEquals(page, "WarningsAsErrors", "false", true);
      AllConfigsAssertPropertyEquals(page, "ConfigurationType", "$(ConfigurationType)", true);
      AllConfigsAssertPropertyEquals(page, "UserHeaderDependenciesOnly", "true", true);
      AllConfigsAssertPropertyEquals(page, "OutputCommandLine", "false", false);

      // C/C++ Optimization
      TestUtilities.AssertPropertyEquals(debug_, page, "OptimizationLevel", "O0", false);
      TestUtilities.AssertPropertyEquals(release_, page, "OptimizationLevel", "O3", false);

      // C/C++ Preprocessor
      AllConfigsAssertPropertyContains(page, "PreprocessorDefinitions", "NACL", false);

      // C/C++ Code Generation
      AllConfigsAssertPropertyEquals(page, "ExceptionHandling", "true", false);

      // C/C++ Output Files
      AllConfigsAssertPropertyEquals(page, "ObjectFileName", @"$(IntDir)", false);

      // C/C++ Advanced
      AllConfigsAssertPropertyEquals(page, "CompileAs", "Default", true);

      // Linker Input
      page = "Link";
      AllConfigsAssertPropertyContains(page, "AdditionalDependencies", "ppapi_cpp;ppapi", true);
      dte_.Solution.Close();
    }

    /// <summary>
    /// Helper function which opens the given solution, sets the configuration and platform and
    /// tries to compile, failing the test if the build does not succeed.
    /// </summary>
    /// <param name="solutionPath">Path to the solution to open.</param>
    /// <param name="configName">Solution Configuration name (Debug or Release).</param>
    /// <param name="platformName">Platform name.</param>
    private void TryCompile(string solutionPath, string configName, string platformName)
    {
      string failFormat = "Project compile failed for {0} platform {1} config. Build output: {2}";
      string cygwinWarningFormat = "Did not pass cygwin nodosfilewarning environment var to tools"
                                 + " Platform: {0}, configuration: {1}";
      StringComparison ignoreCase = StringComparison.InvariantCultureIgnoreCase;

      // Open Debug configuration and build.
      dte_.Solution.Open(solutionPath);
      TestUtilities.SetSolutionConfiguration(
          dte_, TestUtilities.BlankNaClProjectUniqueName, configName, platformName);
      dte_.Solution.SolutionBuild.Build(true);

      string compileOutput = TestUtilities.GetPaneText(
          dte_.ToolWindows.OutputWindow.OutputWindowPanes.Item("Build"));
      Assert.IsTrue(
          compileOutput.Contains("Build succeeded.", ignoreCase),
          string.Format(failFormat, platformName, configName, compileOutput));
      Assert.IsFalse(
          compileOutput.Contains("MS-DOS style path detected", ignoreCase),
          string.Format(cygwinWarningFormat, platformName, configName));

      dte_.Solution.Close();
    }

    /// <summary>
    /// Helper function to reduce repeated code.  Opens the given solution and sets the debug_
    /// and release_ member variables to point to the given platform type.
    /// </summary>
    /// <param name="solutionPath">Path to the solution to open.</param>
    /// <param name="platformName">Platform type to load.</param>
    private void OpenSolutionAndGetProperties(string solutionPath, string platformName)
    {
      // Extract the debug and release configurations for Pepper from the project.
      dte_.Solution.Open(solutionPath);
      Project project = dte_.Solution.Projects.Item(TestUtilities.BlankNaClProjectUniqueName);
      Assert.IsNotNull(project, "Testing project was not found");
      debug_ = TestUtilities.GetVCConfiguration(project, "Debug", platformName);
      release_ = TestUtilities.GetVCConfiguration(project, "Release", platformName);
    }

    /// <summary>
    /// Tests that a given property has a specific value for both Debug and Release
    /// configurations under the current test's platform.
    /// </summary>
    /// <param name="pageName">Property page name where property resides.</param>
    /// <param name="propertyName">Name of the property to check.</param>
    /// <param name="expectedValue">Expected value of the property.</param>
    /// <param name="ignoreCase">Ignore case when comparing the expected and actual values.</param>
    private void AllConfigsAssertPropertyEquals(
        string pageName,
        string propertyName,
        string expectedValue,
        bool ignoreCase)
    {
      TestUtilities.AssertPropertyEquals(
          debug_,
          pageName,
          propertyName,
          expectedValue,
          ignoreCase);
      TestUtilities.AssertPropertyEquals(
          release_,
          pageName,
          propertyName,
          expectedValue,
          ignoreCase);
    }

    /// <summary>
    /// Tests that a given property contains a specific string for both Debug and Release
    /// configurations under the current test's platform.
    /// </summary>
    /// <param name="pageName">Property page name where property resides.</param>
    /// <param name="propertyName">Name of the property to check.</param>
    /// <param name="expectedValue">Expected value of the property.</param>
    /// <param name="ignoreCase">Ignore case when comparing the expected and actual values.</param>
    private void AllConfigsAssertPropertyContains(
        string pageName,
        string propertyName,
        string expectedValue,
        bool ignoreCase)
    {
      TestUtilities.AssertPropertyContains(
          debug_,
          pageName,
          propertyName,
          expectedValue,
          ignoreCase);
      TestUtilities.AssertPropertyContains(
          release_,
          pageName,
          propertyName,
          expectedValue,
          ignoreCase);
    }

    /// <summary>
    /// Tests that a given property's value is not null or empty for both Debug and Release
    /// configurations under the current test's platform.
    /// </summary>
    /// <param name="pageName">Property page name where property resides.</param>
    /// <param name="propertyName">Name of the property to check.</param>
    private void AllConfigsAssertPropertyIsNotNullOrEmpty(
        string pageName,
        string propertyName)
    {
      TestUtilities.AssertPropertyIsNotNullOrEmpty(
          debug_,
          pageName,
          propertyName);
      TestUtilities.AssertPropertyIsNotNullOrEmpty(
          release_,
          pageName,
          propertyName);
    }
  }
}
