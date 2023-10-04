/*
 * ------------------------------------------------ Easing --------------------------------------------------|
 * by: MarcWerk. October 2023                                                                                |
 * License: MIT                                                                                              |
 * ----------------------------------------------------------------------------------------------------------|
 */

using Godot;
using System;

public class Easing
{
	protected Func<float, Easing, float> method;
	public enum EasingType
	{
		Linear,
		SinIn,
		SinOut,
		SinInOut,
		BounceIn,
		BounceOut,
		BounceInOut,
		ExpoIn,
		ExpoOut,
		ElasticIn,
		ElasticOut,
		BackIn,
		BackOut,
		Smoothstep,
		Smootherstep
	}

	public static Easing Get (EasingType method)
	{
		switch (method)
		{
			default:
			case EasingType.Linear:
				return Linear;
			case EasingType.SinIn:
				return SinIn;
			case EasingType.SinOut:
				return SinOut;
			case EasingType.SinInOut:
				return SinInOut;
			case EasingType.BounceIn:
				return BounceIn;
			case EasingType.BounceOut:
				return BounceOut;
			case EasingType.BounceInOut:
				return BounceInOut;
			case EasingType.ExpoIn:
				return ExpoIn;
			case EasingType.ExpoOut:
				return ExpoOut;
			case EasingType.ElasticIn:
				return ElasticIn;
			case EasingType.ElasticOut:
				return ElasticOut;
			case EasingType.BackIn:
				return BackIn;
			case EasingType.BackOut:
				return BackOut;
			case EasingType.Smoothstep:
				return Smoothstep;
			case EasingType.Smootherstep:
				return Smootherstep;
		}
	}

	public static Easing Linear => new Easing() { method = LinearEasing };
	public static Easing SinIn => new Easing() { method = SinInEasing };
	public static Easing SinOut => new Easing() { method = SinOutEasing };
	public static Easing SinInOut => new Easing() { method = SinInOutEasing };
	public static Easing BounceIn => new Easing() { method = BounceInEasing };
	public static Easing BounceOut => new Easing() { method = BounceOutEasing };
	public static Easing BounceInOut => new Easing() { method = BounceInOutEasing };
	public static Easing ExpoIn => new Easing() { method = ExpoInEasing };
	public static Easing ExpoOut => new Easing() { method = ExpoOutEasing };
	public static Easing ElasticIn => new Easing() { method = ElasticInEasing };
	public static Easing ElasticOut => new Easing() { method = ElasticOutEasing };
	public static Easing BackIn => new Easing() { method = BackInEasing };
	public static Easing BackOut => new Easing() { method = BackOutEasing };
	public static Easing Smoothstep => new Easing() { method = SmoothstepEasing };
	public static Easing Smootherstep => new Easing() { method = SmootherstepEasing };


	/// <summary>
	/// Interpolate between 2 values using the selected easing
	/// </summary>
	/// <param name="start">The start value</param>
	/// <param name="end">The end value</param>
	/// <param name="time">The interpolation between the values represented as a value from 0 to 1</param>
	/// <returns></returns>
	public float Ease(float start, float end, float time)
	{
		return Mathf.Lerp(start, end, method?.Invoke(time, this) ?? 0f);
	}

	static float LinearEasing(float t, Easing _ = null)
	{
		return t;
	}

	public static float SinInEasing(float t, Easing _ = null)
    {
		return Mathf.Sin((t * Mathf.Pi) / 2f);
	}

	public static float SinOutEasing(float t, Easing _ = null)
	{
		return Mathf.Sin((t * Mathf.Pi) / 2f - Mathf.Pi / 2f) + 1f;
	}

	public static float SinInOutEasing(float t, Easing _ = null)
	{
		return 0.5f * (1f - Mathf.Cos(t * Mathf.Pi));
	}

	public static float BounceInEasing(float t, Easing _ = null)
	{
		return 1f - BounceOutEasing(1f - t);
	}

	public static float BounceOutEasing(float t, Easing _ = null)
	{
		if (t < 4f / 11f)
		{
			return (121f * t * t) / 16f;
		}
		else if (t < 8f / 11f)
		{
			return (363f / 40f * t * t) - (99f / 10f * t) + 17f / 5f;
		}
		else if (t < 9f / 10f)
		{
			return (4356f / 361f * t * t) - (35442f / 1805f * t) + 16061f / 1805f;
		}
		else
		{
			return (54f / 5f * t * t) - (513f / 25f * t) + 268f / 25f;
		}
	}

	public static float BounceInOutEasing(float t, Easing _ = null)
	{
		if (t < 0.5f)
		{
			return 0.5f * BounceInEasing(t * 2f);
		}
		else
		{
			return 0.5f * BounceOutEasing(t * 2f - 1f) + 0.5f;
		}
	}

	static float ExpoInEasing(float t, Easing _ = null)
	{
		return (t == 0) ? 0 : Mathf.Pow(2, 10 * (t - 1));
	}

	static float ExpoOutEasing(float t, Easing _ = null)
	{
		return (t == 1) ? 1 : 1 - Mathf.Pow(2, -10 * t);
	}

	static float ElasticInEasing(float t, Easing _ = null)
	{
		float amplitude = 1f;
		float period = 0.3f;
		if (t == 0 || t == 1) return t;
		float s = period / (2 * Mathf.Pi) * Mathf.Asin(1 / amplitude);
		t -= 1;
		return -(amplitude * Mathf.Pow(2, 10 * t) * Mathf.Sin((t - s) * (2 * Mathf.Pi) / period));
	}

	static float ElasticOutEasing(float t, Easing _ = null)
	{
		float amplitude = 1f;
		float period = 0.3f;
		if (t == 0 || t == 1) return t;
		float s = period / (2 * Mathf.Pi) * Mathf.Asin(1 / amplitude);
		return amplitude * Mathf.Pow(2, -10 * t) * Mathf.Sin((t - s) * (2 * Mathf.Pi) / period) + 1;
	}

	static float BackInEasing(float t, Easing _ = null)
	{
		float s = 1.70158f;
		return t * t * ((s + 1) * t - s);
	}

	static float BackOutEasing(float t, Easing _ = null)
	{
		float s = 1.70158f;
		t = t - 1;
		return t * t * ((s + 1) * t + s) + 1;
	}

	static float SmoothstepEasing(float t, Easing _ = null)
	{
		return t * t * (3 - 2 * t);
	}

	static float SmootherstepEasing(float t, Easing _ = null)
	{
		return t * t * t * (t * (t * 6 - 15) + 10);
	}
}
