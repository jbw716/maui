﻿using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Widget;
using Microsoft.Maui.Controls.Internals;
using AAplication = Android.App.Application;

namespace Microsoft.Maui.Controls.Platform
{
	public static class FormattedStringExtensions
	{
		readonly static Java.Lang.Class JavaLangObjectClass = Java.Lang.Class.FromType(typeof(Java.Lang.Object));

		public static SpannableString ToSpannableString(this Label label)
			=> ToSpannableStringNewWay(
				label.FormattedText,
				label.RequireFontManager(),
				label.Handler?.MauiContext?.Context,
				label.CharacterSpacing,
				label.HorizontalTextAlignment,
				label.ToFont(),
				label.TextColor,
				label.TextTransform,
				label.TextDecorations);

		// TODO: NET8 this overload must be removed in net8.0 and replaced with the one below
		public static SpannableString ToSpannableString(
			this FormattedString formattedString,
			IFontManager fontManager,
			TextPaint? textPaint = null,
			Context? context = null,
			double defaultLineHeight = 0,
			TextAlignment defaultHorizontalAlignment = TextAlignment.Start,
			Font? defaultFont = null,
			Graphics.Color? defaultColor = null,
			TextTransform defaultTextTransform = TextTransform.Default)
			=> formattedString.ToSpannableStringNewWay(
				fontManager,
				context,
				0d,
				defaultHorizontalAlignment,
				defaultFont,
				defaultColor,
				defaultTextTransform,
				TextDecorations.None);

		internal static SpannableString ToSpannableStringNewWay(
			this FormattedString? formattedString,
			IFontManager fontManager,
			Context? context = null,
			double defaultCharacterSpacing = 0d,
			TextAlignment defaultHorizontalAlignment = TextAlignment.Start,
			Font? defaultFont = null,
			Graphics.Color? defaultColor = null,
			TextTransform defaultTextTransform = TextTransform.Default,
			TextDecorations defaultTextDecorations = TextDecorations.None)
		{
			if (formattedString == null || formattedString.Spans.Count == 0)
				return new SpannableString(string.Empty);

			var defaultFontSize = defaultFont?.Size ?? fontManager.DefaultFontSize;

			var builder = new StringBuilder();

			for (int i = 0; i < formattedString.Spans.Count; i++)
			{
				Span span = formattedString.Spans[i];

				var transform = span.TextTransform != TextTransform.Default ? span.TextTransform : defaultTextTransform;

				var text = TextTransformUtilites.GetTransformedText(span.Text, transform);
				if (text == null)
					continue;

				builder.Append(text);
			}

			var spannable = new SpannableString(builder.ToString());

			var c = 0;
			for (int i = 0; i < formattedString.Spans.Count; i++)
			{
				Span span = formattedString.Spans[i];
				var text = span.Text;
				if (text == null)
					continue;

				int start = c;
				int end = start + text.Length;
				c = end;

				// TextColor
				var textColor = span.TextColor ?? defaultColor;
				if (textColor is not null)
					spannable.SetSpan(new ForegroundColorSpan(textColor.ToPlatform()), start, end, SpanTypes.InclusiveExclusive);

				// BackgroundColor
				if (span.BackgroundColor is not null)
					spannable.SetSpan(new BackgroundColorSpan(span.BackgroundColor.ToPlatform()), start, end, SpanTypes.InclusiveExclusive);

				// LineHeight
				if (span.LineHeight >= 0)
					spannable.SetSpan(new LineHeightSpan(span.LineHeight), start, end, SpanTypes.InclusiveExclusive);

				// CharacterSpacing
				var characterSpacing = span.CharacterSpacing >= 0
					? span.CharacterSpacing
					: defaultCharacterSpacing;
				if (characterSpacing >= 0)
					spannable.SetSpan(new LetterSpacingSpan(characterSpacing.ToEm()), start, end, SpanTypes.InclusiveInclusive);

				// Font
				var font = span.ToFont(defaultFontSize);
				if (font.IsDefault && defaultFont.HasValue)
					font = defaultFont.Value;
				if (!font.IsDefault)
					spannable.SetSpan(new FontSpan(font, fontManager, context), start, end, SpanTypes.InclusiveInclusive);

				// TextDecorations
				var textDecorations = span.IsSet(Span.TextDecorationsProperty)
					? span.TextDecorations
					: defaultTextDecorations;
				if (textDecorations.HasFlag(TextDecorations.Strikethrough))
					spannable.SetSpan(new StrikethroughSpan(), start, end, SpanTypes.InclusiveInclusive);
				if (textDecorations.HasFlag(TextDecorations.Underline))
					spannable.SetSpan(new UnderlineSpan(), start, end, SpanTypes.InclusiveInclusive);
			}

			return spannable;
		}

		public static void RecalculateSpanPositions(this TextView textView, Label element, SpannableString spannableString, SizeRequest finalSize) =>
			RecalculateSpanPositions(textView, element.Padding, element.FormattedText, spannableString);

		internal static void RecalculateSpanPositions(this TextView textView, Thickness padding, FormattedString text) =>
			RecalculateSpanPositions(textView, padding, text, textView.TextFormatted as SpannableStringInternal);

		internal static void RecalculateSpanPositions(this TextView textView, Thickness padding, FormattedString text, SpannableStringInternal? spannableString)
		{
			if (textView.Width <= 0 || textView.Height <= 0)
				return;

			if (spannableString is null || spannableString.IsDisposed())
				return;

			var strlen = spannableString.Length();
			if (strlen == 0)
				return;

			var layout = textView.Layout;
			if (layout is null)
				return;

			int next = 0;
			int count = 0;

			var padLeft = (int)textView.Context.ToPixels(padding.Left);
			var padTop = (int)textView.Context.ToPixels(padding.Top);

			for (int i = 0; i < strlen; i = next)
			{
				var span = text.Spans[count];

				count++;

				if (string.IsNullOrEmpty(span.Text))
					continue;

				// Find the next span
				next = spannableString.NextSpanTransition(i, spannableString.Length(), JavaLangObjectClass);

				// Get all spans in the range - Android can have overlapping spans
				var spans = spannableString.GetSpans(i, next, JavaLangObjectClass);

				if (spans is null || spans.Length == 0)
					continue;

				var startSpan = spans[0];
				var endSpan = spans[spans.Length - 1];

				var spanStartOffset = spannableString.GetSpanStart(startSpan);
				var spanEndOffset = spannableString.GetSpanEnd(endSpan);

				var spanStartLine = layout.GetLineForOffset(spanStartOffset);
				var spanEndLine = layout.GetLineForOffset(spanEndOffset);

				// Go through all lines that are affected by the span and calculate a rectangle for each
				List<Graphics.Rect> spanRectangles = new List<Graphics.Rect>();
				for (var curLine = spanStartLine; curLine <= spanEndLine; curLine++)
				{
					var bounds = new global::Android.Graphics.Rect();
					layout.GetLineBounds(curLine, bounds);

					var lineHeight = bounds.Height();
					var lineStartOffset = layout.GetLineStart(curLine);
					var lineVisibleEndOffset = layout.GetLineVisibleEnd(curLine);

					var startOffset = (curLine == spanStartLine) ? spanStartOffset : lineStartOffset;
					var spanStartX = (int)layout.GetPrimaryHorizontal(startOffset);

					var endOffset = (curLine == spanEndLine) ? spanEndOffset : lineVisibleEndOffset;
					var spanEndX = (int)layout.GetSecondaryHorizontal(endOffset);

					var spanWidth = spanEndX - spanStartX;
					var spanLeftX = spanStartX;

					// If rtl is used, startX would be bigger than endX
					if (spanStartX > spanEndX)
					{
						spanWidth = spanStartX - spanEndX;
						spanLeftX = spanEndX;
					}

					if (spanWidth > 1)
					{
						var rectangle = new Graphics.Rect(spanLeftX + padLeft, bounds.Top + padTop, spanWidth, lineHeight);
						spanRectangles.Add(rectangle);
					}
				}

				((ISpatialElement)span).Region = Region.FromRectangles(spanRectangles).Inflate(10);
			}
		}

		class FontSpan : MetricAffectingSpan
		{
			readonly Font _font;
			readonly IFontManager _fontManager;
			readonly Context? _context;

			public FontSpan(Font font, IFontManager fontManager, Context? context)
			{
				_font = font;
				_fontManager = fontManager;
				_context = context;
			}

			public override void UpdateDrawState(TextPaint? tp)
			{
				if (tp != null)
					Apply(tp);
			}

			public override void UpdateMeasureState(TextPaint p)
			{
				Apply(p);
			}

			void Apply(TextPaint paint)
			{
				paint.SetTypeface(_font.ToTypeface(_fontManager));

				paint.TextSize = TypedValue.ApplyDimension(
					_font.AutoScalingEnabled ? ComplexUnitType.Sp : ComplexUnitType.Dip,
					(float)_font.Size,
					(_context ?? AAplication.Context)?.Resources?.DisplayMetrics);
			}
		}

		class LetterSpacingSpan : MetricAffectingSpan
		{
			readonly float _letterSpacing;

			public LetterSpacingSpan(double letterSpacing)
			{
				_letterSpacing = (float)letterSpacing;
			}

			public override void UpdateDrawState(TextPaint? tp)
			{
				if (tp != null)
					Apply(tp);
			}

			public override void UpdateMeasureState(TextPaint p)
			{
				Apply(p);
			}

			void Apply(TextPaint paint)
			{
				paint.LetterSpacing = _letterSpacing;
			}
		}

		class LineHeightSpan : Java.Lang.Object, ILineHeightSpan
		{
			readonly double _relativeLineHeight;

			public LineHeightSpan(double relativeLineHeight)
			{
				_relativeLineHeight = relativeLineHeight;
			}

			public void ChooseHeight(Java.Lang.ICharSequence? text, int start, int end, int spanstartv, int lineHeight, Paint.FontMetricsInt? fm)
			{
				if (fm is null)
					return;

				fm.Ascent = (int)(fm.Top * _relativeLineHeight);
			}
		}
	}
}