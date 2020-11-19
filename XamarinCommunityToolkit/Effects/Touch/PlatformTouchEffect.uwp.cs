﻿using Windows.UI.Xaml.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using Xamarin.CommunityToolkit.UWP.Effects;
using Xamarin.CommunityToolkit.Effects;

[assembly: ExportEffect(typeof(PlatformTouchEffect), nameof(TouchEffect))]

namespace Xamarin.CommunityToolkit.UWP.Effects
{
	public class PlatformTouchEffect : PlatformEffect
	{
		TouchEffect effect;

		bool isPressed;
		bool isIntentionalCaptureLoss;

		Storyboard pointerDownStoryboard;
		Storyboard pointerUpStoryboard;

		protected override void OnAttached()
		{
			effect = TouchEffect.PickFrom(Element);
			if (effect?.IsDisabled ?? true)
				return;

			effect.Element = Element as VisualElement;
			if (effect.NativeAnimation)
			{
				var nativeControl = Container;
				if (String.IsNullOrEmpty(nativeControl.Name))
					nativeControl.Name = Guid.NewGuid().ToString();

				if (nativeControl.Resources.ContainsKey("PointerDownAnimation"))
				{
					pointerDownStoryboard = (Storyboard)nativeControl.Resources["PointerDownAnimation"];
				}
				else
				{
					pointerDownStoryboard = new Storyboard();
					var downThemeAnimation = new PointerDownThemeAnimation();
					Storyboard.SetTargetName(downThemeAnimation, nativeControl.Name);
					pointerDownStoryboard.Children.Add(downThemeAnimation);
					nativeControl.Resources.Add(new KeyValuePair<object, object>("PointerDownAnimation", pointerDownStoryboard));
				}

				if (nativeControl.Resources.ContainsKey("PointerUpAnimation"))
				{
					pointerUpStoryboard = (Storyboard)nativeControl.Resources["PointerUpAnimation"];
				}
				else
				{
					pointerUpStoryboard = new Storyboard();
					var upThemeAnimation = new PointerUpThemeAnimation();
					Storyboard.SetTargetName(upThemeAnimation, nativeControl.Name);
					pointerUpStoryboard.Children.Add(upThemeAnimation);
					nativeControl.Resources.Add(new KeyValuePair<object, object>("PointerUpAnimation", pointerUpStoryboard));
				}
			}

			if (Container != null)
			{
				Container.PointerPressed += OnPointerPressed;
				Container.PointerReleased += OnPointerReleased;
				Container.PointerCanceled += OnPointerCanceled;
				Container.PointerExited += OnPointerExited;
				Container.PointerEntered += OnPointerEntered;
				Container.PointerCaptureLost += OnPointerCaptureLost;
			}
		}

		protected override void OnDetached()
		{
			if (effect?.Element == null)
				return;

			effect.Element = null;
			effect = null;
			if (Container != null)
			{
				Container.PointerPressed -= OnPointerPressed;
				Container.PointerReleased -= OnPointerReleased;
				Container.PointerCanceled -= OnPointerCanceled;
				Container.PointerExited -= OnPointerExited;
				Container.PointerEntered -= OnPointerEntered;
				Container.PointerCaptureLost -= OnPointerCaptureLost;

				isPressed = false;
			}
		}

		private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (effect?.IsDisabled ?? true)
				return;

			effect?.HandleHover(HoverStatus.Entered);

			if (isPressed)
			{
				effect?.HandleTouch(TouchStatus.Started);
				AnimateTilt(pointerDownStoryboard);
			}
		}

		private void OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (effect?.IsDisabled ?? true)
				return;

			if (isPressed)
			{
				effect?.HandleTouch(TouchStatus.Canceled);
				AnimateTilt(pointerUpStoryboard);
			}

			effect?.HandleHover(HoverStatus.Exited);
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (effect?.IsDisabled ?? true)
				return;

			isPressed = false;
			effect?.HandleTouch(TouchStatus.Canceled);
			effect?.HandleUserInteraction(TouchInteractionStatus.Completed);
			effect?.HandleHover(HoverStatus.Exited);
			AnimateTilt(pointerUpStoryboard);
		}

		private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (effect?.IsDisabled ?? true)
				return;

			if (isIntentionalCaptureLoss)
				return;

			isPressed = false;

			if (effect.Status != TouchStatus.Canceled)
				effect?.HandleTouch(TouchStatus.Canceled);

			effect?.HandleUserInteraction(TouchInteractionStatus.Completed);

			if (effect.HoverStatus != HoverStatus.Exited)
				effect?.HandleHover(HoverStatus.Exited);

			AnimateTilt(pointerUpStoryboard);
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (effect?.IsDisabled ?? true)
				return;

			if (isPressed && (effect.HoverStatus == HoverStatus.Entered))
			{
				effect?.HandleTouch(TouchStatus.Completed);
				AnimateTilt(pointerUpStoryboard);
			}
			else if (effect.HoverStatus != HoverStatus.Exited)
			{
				effect?.HandleTouch(TouchStatus.Canceled);
				AnimateTilt(pointerUpStoryboard);
			}

			effect?.HandleUserInteraction(TouchInteractionStatus.Completed);
			isPressed = false;
			isIntentionalCaptureLoss = true;
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (effect?.IsDisabled ?? true)
				return;

			isPressed = true;
			Container.CapturePointer(e.Pointer);
			effect?.HandleUserInteraction(TouchInteractionStatus.Started);
			effect?.HandleTouch(TouchStatus.Started);
			AnimateTilt(pointerDownStoryboard);
			isIntentionalCaptureLoss = false;
		}

		private void AnimateTilt(Storyboard storyboard)
		{
			if ((effect?.NativeAnimation ?? false) && storyboard != null)
			{
				try
				{
					storyboard.Stop();
					storyboard.Begin();
				}
				catch
				{
					// Suppress
				}
			}
		}
	}
}