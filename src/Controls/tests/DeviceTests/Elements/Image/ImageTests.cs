﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	public partial class ImageTests : ControlsHandlerTestBase
	{
		protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
			base.ConfigureBuilder(mauiAppBuilder)
				.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Image, ImageHandler>();
				});

		[Fact]
		public async Task ImageWithUndefinedSizeAndWithBackgroundSetRenders()
		{
			var layout = new VerticalStackLayout();

			var image = new Image
			{
				Background = Colors.Black,
				Source = "red.png",
			};

			layout.Add(image);

			await InvokeOnMainThreadAsync(async () =>
			{
				var handler = CreateHandler<LayoutHandler>(layout);
				await image.Wait();
				await handler.ToPlatform().AssertContainsColor(Colors.Red, MauiContext);
			});
		}

		[Fact("Image Does Not Leak")]
		public async Task DoesNotLeak()
		{
			WeakReference platformViewReference = null;
			WeakReference handlerReference = null;

			await InvokeOnMainThreadAsync(async () =>
			{
				var layout = new VerticalStackLayout();
				var image = new Image
				{
					Background = Colors.Black,
					Source = "red.png",
				};
				layout.Add(image);

				var handler = CreateHandler<LayoutHandler>(layout);
				handlerReference = new WeakReference(image.Handler);
				platformViewReference = new WeakReference(image.Handler.PlatformView);
				await image.Wait();
			});

			Assert.NotNull(handlerReference);
			Assert.NotNull(platformViewReference);

			// Several GCs required on iOS
			for (int i = 0; i < 5; i++)
			{
				if (!handlerReference.IsAlive && !platformViewReference.IsAlive)
					break;
				await Task.Yield();
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.False(handlerReference.IsAlive, "Handler should not be alive!");
			Assert.False(platformViewReference.IsAlive, "PlatformView should not be alive!");
		}
	}
}