﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.AppiumTests.Issues
{
	public class Issue18242 : _IssuesUITest
	{
		public Issue18242(TestDevice device) : base(device)
		{
		}

		public override string Issue => "Button ImageSource not Scaling as expected";

		[Test]
		public void Issue18242Test()
		{
			this.IgnoreIfPlatforms(new TestDevice[] { TestDevice.Android }, "Ignore Android until https://github.com/dotnet/maui/pull/19834 lands.");

			App.WaitForElement("WaitForStubControl");

			VerifyScreenshot();
		}
	}
}
