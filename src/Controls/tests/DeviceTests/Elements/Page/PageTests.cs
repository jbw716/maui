﻿#if !IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Page)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public partial class PageTests : ControlsHandlerTestBase
	{
		protected override MauiAppBuilder ConfigureBuilder(MauiAppBuilder mauiAppBuilder) =>
			base.ConfigureBuilder(mauiAppBuilder)
				.ConfigureMauiHandlers(handlers =>
					handlers.AddHandler<Controls.ContentPage, PageHandler>());

		[Theory("Page Background Initializes Correctly With Background Property")]
		[InlineData("#FF0000")]
		[InlineData("#00FF00")]
		[InlineData("#0000FF")]
		public async Task InitializingBackgroundUpdatesBackground(string colorStr)
		{
			var color = Color.Parse(colorStr);

			var page = new ContentPage();
			page.Background = color;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async (handler) =>
			{
				await handler.PlatformView.AssertContainsColor(color, handler.MauiContext);
			});
		}

		[Theory("Page Background Initializes Correctly With BackgroundColor Property")]
		[InlineData("#FF0000")]
		[InlineData("#00FF00")]
		[InlineData("#0000FF")]
		public async Task InitializingBackgroundColorUpdatesBackground(string colorStr)
		{
			var color = Color.Parse(colorStr);

			var page = new ContentPage();
			page.BackgroundColor = color;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async (handler) =>
			{
				await handler.PlatformView.AssertContainsColor(color, handler.MauiContext);
			});
		}

		[Theory("Page Background Updates Correctly With Background Property")]
		[InlineData("#FF0000")]
		[InlineData("#00FF00")]
		[InlineData("#0000FF")]
		public async Task UpdatingBackgroundUpdatesBackground(string colorStr)
		{
			var color = Color.Parse(colorStr);

			var page = new ContentPage();
			page.Background = Colors.HotPink;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async (handler) =>
			{
				page.Background = color;

				await handler.PlatformView.AssertContainsColor(color, handler.MauiContext);
			});
		}

		[Theory("Page Background Updates Correctly With BackgroundColor Property")]
		[InlineData("#FF0000")]
		[InlineData("#00FF00")]
		[InlineData("#0000FF")]
		public async Task UpdatingBackgroundColorUpdatesBackground(string colorStr)
		{
			var color = Color.Parse(colorStr);

			var page = new ContentPage();
			page.BackgroundColor = Colors.HotPink;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async (handler) =>
			{
				page.BackgroundColor = color;

				await handler.PlatformView.AssertContainsColor(color, handler.MauiContext);
			});
		}

		[Fact("No issues using Page IsBusy property")]
		public async Task UsingIsBusyNoCrash()
		{
			var page = new ContentPage();
			page.IsBusy = true;

			await CreateHandlerAndAddToWindow<PageHandler>(page, (handler) =>
			{
				// Validate that no exceptions are thrown
				((IElementHandler)handler).DisconnectHandler();
				return Task.CompletedTask;
			});
		}
	}
}
#endif